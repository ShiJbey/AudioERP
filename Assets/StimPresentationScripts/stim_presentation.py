# This script generates the presentation order for all the
# participants in the audio P300 VR

import argparse
import math
import random
import sys
import numpy as np
from itertools import permutations
import json
import os.path


class StimPair:
    """
    Manages pairing of a target stimulus index with a preceeding stimulus index
    This class is used when generating the presentation order to balance
    conditions
    """
    def __init__(self, target, previous, desired_count):
        self.target_stim = target
        self.previous_stim = previous
        self.desired_count = desired_count
        self.count = 0

    def __str__(self):
       return 'Pairing: {0} => {1} [Placed {2} of {3}]'.format(self.previous_stim, self.target_stim, self.count, self.desired_count)

def generate_block(num_trials, num_target_trials, target_trial_percentage, target_index, num_stimuli=3, min_target_separation=0, max_target_separation=math.inf, allow_target_repeat=False, verbose=False, **kwargs):
    """
    Generates an ordering of trials within a block
    """
    if kwargs['max_rand_targets']:
        num_target_trials += random.randint(0, kwargs['max_rand_targets'])
        print(num_target_trials)

    # Create stimulus pairings
    stim_pairs = generate_target_stim_pairings(num_stimuli, num_target_trials, not allow_target_repeat, target_index)
    if (verbose):
        print("Generated Stim Pairs:")
        for pairing in stim_pairs:
            print(pairing)

    # Get counts for the number of times each stimulus will be presented
    remaining_trial_counts = generate_remaining_trial_counts(num_trials, num_target_trials, target_index, num_stimuli)
    if (verbose):
        print("Remaining Trial Counts: {}".format(remaining_trial_counts))

    free_unpaired_trials = remaining_trial_counts.copy()
    if (not allow_target_repeat):
        free_unpaired_trials[target_index] = 0
    # Loop through the pairs and remove their counts from the counts for free non-target trials
    for pairing in stim_pairs:
        free_unpaired_trials[pairing.previous_stim] -= pairing.desired_count
    if (verbose):
        print('DEBUG:: Unpaired nontarget trial counts: {0}'.format(free_unpaired_trials))

    # Count of the total number of nontarget trials that still need to be added
    remaining_unpaired_non_target_trials = 0
    for nontarget in free_unpaired_trials:
        remaining_unpaired_non_target_trials += free_unpaired_trials[nontarget]
    if (verbose):
        print("DEBUG:: Total unpaired non-target trials for this block: {0}".format(remaining_unpaired_non_target_trials))

    # List of stimuli indices of length num_trials
    # I use -1 as a place holder for no stimulus
    block = [-1] * num_trials

    target_trials_added = 0
    # index of the stimulus within the block
    i = 0
    max_target_separation = round(num_trials / num_target_trials) - 2
    while (i < len(block)):
        # Add nontarget trials to the block
        if (remaining_unpaired_non_target_trials > 0):

            # Determine how many nontarget trials to add
            if (remaining_unpaired_non_target_trials > 1 ):#and remaining_trial_counts[target_index] > 0):
                trials_to_add = random.randint(0, remaining_unpaired_non_target_trials)
                #trials_to_add = random.randint(0, min(remaining_unpaired_non_target_trials, max_target_separation))
            else:
                trials_to_add = 1
            if (verbose):
                print('DEBUG:: Starting at position ({0}), inserting ({1}) non-target trials'.format(i, trials_to_add))

            # Insert non target trials
            if verbose:
                print('DEBUG:: trial counts before update: {}'.format(remaining_trial_counts))
                print('DEBUG:: unpaired nontarget trial counts before update: {}'.format(free_unpaired_trials))
            for j in range(trials_to_add):
                stim_index = choose_available_stimulus(free_unpaired_trials, not allow_target_repeat, target_index)
                block[i] = stim_index
                free_unpaired_trials[stim_index] -= 1
                remaining_trial_counts[stim_index] -= 1
                remaining_unpaired_non_target_trials -= 1
                if (verbose):
                    print('DEBUG:: Placed stimulus ({0}) at position ({1})'.format(stim_index, i))
                    print('DEBUG:: Update ({0}/{1}) trial counts: {2}'.format(j + 1, trials_to_add,remaining_trial_counts))
                    print('DEBUG:: Update ({0}/{1}) unpaired non-target trial counts: {2}'.format(j + 1, trials_to_add, free_unpaired_trials))
                i += 1
        else:
            #print('WARNING:: No nontarget trials remaining')
            pass

        # Add a target trial to the block
        if (remaining_trial_counts[target_index] > 0):
            target_trials_added += 1
            # Get a target pairing
            pair_index = choose_available_stim_pair(stim_pairs)
            stim_pairs[pair_index].count += 1
            # Set the values in the block
            block[i] = stim_pairs[pair_index].previous_stim
            block[i + 1] = stim_pairs[pair_index].target_stim
            # Update remaining trial counts
            #remaining_unpaired_non_target_trials -= 1
            remaining_trial_counts[target_index] -= 1
            remaining_trial_counts[stim_pairs[pair_index].previous_stim] -= 1
            if (verbose):
                print('DEBUG:: Added target pair: {0} at position {1}'.format(stim_pairs[pair_index], i))
                print('DEBUG:: Updated trial counts: {0}'.format(remaining_trial_counts))
            i += 2

        if (remaining_unpaired_non_target_trials > 1 and remaining_trial_counts[target_index] <= 0):
            # Loop through and add the remaining stims
            for stim_index in free_unpaired_trials:
                for _ in range(free_unpaired_trials[stim_index]):
                    block[i] = stim_index
                    i += 1
                    remaining_trial_counts[stim_index] -= 1

    #end while

    # Check for errors
    for stim_index in remaining_trial_counts:
        if (remaining_trial_counts[stim_index] != 0):
            print('ERROR:: Stimulus ({0}) has count ({1})'.format(stim_index, remaining_trial_counts[stim_index]))

    assert (len(block) == num_trials)
    return block

def generate_remaining_trial_counts(num_trials, num_target_trials, target_index, num_stimuli):
    """
    Returns a dictionary of the stimulus indices mapped to the nuber of trials
    that will be presented within a block
    """
    total_nontarget_trials = num_trials - num_target_trials
    num_trials_per_nontarget = round(total_nontarget_trials / (num_stimuli - 1))
    nontarget_to_fill = num_stimuli - 1

    # Create an array of counts for each stimulus that needs to be added to the block
    remaining_trial_counts = {}
    for stim_index in range(num_stimuli):
        if (stim_index == target_index):
            remaining_trial_counts[stim_index] = num_target_trials
        else:
            if (nontarget_to_fill == 1):
                # Give all remaining trials to this stimuli
                remaining_trial_counts[stim_index] = total_nontarget_trials
                total_nontarget_trials = 0
            else:
                # Give the preset amount
                remaining_trial_counts[stim_index] = num_trials_per_nontarget
                total_nontarget_trials -= num_trials_per_nontarget
                nontarget_to_fill -= 1

    return remaining_trial_counts

def get_available_pairs(stim_pairs):
    """
    Returns the indices of the stim pairs with desired_count values
    greater than 0
    """
    available_pairs = []
    for i in range(len(stim_pairs)):
        if (stim_pairs[i].desired_count - stim_pairs[i].count > 0):
            available_pairs.append(i)
    return available_pairs

def choose_available_stim_pair(stim_pairs):
    """
    Randomly chooses a stim pair that has a desired_count value > 0
    """
    available_pairs = get_available_pairs(stim_pairs)

    if len(available_pairs) == 0:
        raise "No available stim pairs"

    return available_pairs[random.randint(0, len(available_pairs) - 1)]

def export_presentation_order(export_path):
    """
    Writes out the order that stimuli are presented.
    This file is imported by unity when a participant starts
    """
    pass

def export_config(export_path):
    """
    Write out the configuration for this participant's stimulus presentation
    """
    pass

def generate_target_stim_pairings(num_stimuli, num_target_trials, exclude_target, target_index):
    """
    Returns a list off all the stimulus pairings
    """
    remaining_presentations = num_target_trials

    desired_count = 0
    if (exclude_target):
        desired_count = round(num_target_trials / (num_stimuli - 1))
    else:
        desired_count = round(num_target_trials / num_stimuli)

    stim_pairs = []
    pairs_filled = 0
    for i in range(num_stimuli):
        if (exclude_target and i == target_index):
            continue
        else:
            if ((exclude_target and pairs_filled == num_stimuli - 2) or (not exclude_target and pairs_filled - 1)):
                stim_pairs.append(StimPair(target_index, i, remaining_presentations))
                pairs_filled += 1
            else:
                stim_pairs.append(StimPair(target_index, i, desired_count))
                remaining_presentations -= desired_count
                pairs_filled += 1
    return stim_pairs

def all_zeros(arr):
    """
    Returns true if all the values in the list/array are zero
    """
    for i in range(len(arr)):
        if (arr[i] != 0):
            return False
    return True

def increment_pairing_count(target_index, nontarget_index, stim_pairs):
    """
    Increments the count of a stimulus pair for target condition balancing
    """
    pairing_found = False

    for i in range(len(stim_pairs)):
        if (stim_pairs[i].target_stim == target_index and stim_pairs[i].nontarget_index == nontarget_index):
            stim_pairs[i].count += 1
            pairing_found = True

    if (not pairing_found):
        print('WARNING:: Could not find valid pairing to increment')

    return stim_pairs

def get_random_stim_index(num_stimuli, exclude_target, target_index):
    """
    Returns a random index from the range of indices mapped to stimuli
    """
    stim_indices = list(range(num_stimuli))
    if (exclude_target):
        stim_indices.remove(target_index)
    return stim_indices[random.randint(0, len(stim_indices) - 1)]

def get_available_stimuli(stim_counts, exclude_target, target_index):
    """
    Given a dictionary of counts for the remaining stimuli presentations,
    Returns a list of stimulus indices with counts geater than one
    """
    available_stimuli = []
    for stim_index, count in stim_counts.items():
        if (exclude_target and stim_index == target_index):
            continue
        elif (count > 0):
            available_stimuli.append(stim_index)
    return available_stimuli

def choose_available_stimulus(stim_counts, exclude_target, target_index):
    """
    Given a dict of counts for remaining stimuli presentations,
    Returns a randomly chosen stimulus index which has a count > 0
    """
    available_stimuli = get_available_stimuli(stim_counts, exclude_target, target_index)
    if (len(available_stimuli) == 0):
        print('WARNING:: No stimuli available, returning default of (0)')
        return 0
    else:
        return available_stimuli[random.randint(0, len(available_stimuli) - 1)]

def all_rows_unique(mat):
    """
    Returns true if the all the rows are unique.
    This is for checking to make sure all participants see a different order of blocks
    """
    num_rows = mat.shape[0]
    for i in range(num_rows):
        for j in range(num_rows):
            if (i != j and np.array_equal(mat[i,:], mat[j,:])):
                return False
    return True

def create_latin_square(arr):
    """
    Create a latin square form a given array
    of unique values
    """
    mat = np.zeros((len(arr), len(arr)), dtype=int)
    for i in range(len(arr)):
        mat[i,:] = np.roll(arr, i)
    return mat

def export_subjectfile(path, filename, stim_sequences):
    filename_w_extention = '{0}.json'.format(filename)
    file_path_name = os.path.join(path, filename_w_extention)
    with open(file_path_name, 'w') as fp:
        json.dump(stim_sequences, fp)

if (__name__ == '__main__'):
    # Get the command line args
    parser = argparse.ArgumentParser()
    parser.add_argument('sequences', help='Number of sequences per participant', type=int)
    parser.add_argument('blocks', help='Number of blocks per sequence', type=int)
    parser.add_argument('trials', help='Number of trials per block', type=int)
    parser.add_argument('target_percentage', help='Percentage of trials in a block that \
        are target trials', type=float)
    parser.add_argument('participants', help='Number of participants', type=int)
    parser.add_argument('--random_targets', help='Adds a random number of target trials \
        to each block [0, arg)', type=int, default=0, metavar='random_targets')
    parser.add_argument('--verbose', help='Verbose Output', action='store_true', default=False)
    args = parser.parse_args()

    # Default generation parameters
    num_sequences = 15
    blocks_per_sequence = 3
    trials_per_block = 45
    target_trial_percentage = 0.33

    # Set generation parameters from command line args
    num_sequences = args.sequences
    blocks_per_sequence = args.blocks
    trials_per_block = args.trials
    target_trial_percentage = args.target_percentage
    num_participants = args.participants
    max_rand_targets = args.random_targets

    # Seed the random number generator
    rand_seed = 'Pizza'
    random.seed(a=rand_seed)

    # Calculate additional configuration details
    total_trials = num_sequences * blocks_per_sequence * trials_per_block
    target_trials_per_block = round(trials_per_block * target_trial_percentage)
    num_stimuli = blocks_per_sequence

    # Create the block orderings for each participant
    block_permutations = np.array(list(permutations(list(range(num_stimuli)))))

    num_permutations = block_permutations.shape[0]

    latin_square = create_latin_square(np.array(list(range(num_permutations)), dtype=int))
    block_orders = np.concatenate((latin_square, np.fliplr(latin_square)), axis=0)
    block_orders = np.concatenate((block_orders, np.roll(np.flipud(latin_square), 1, axis=1)), axis=0)
    block_orders = np.concatenate((block_orders, np.flipud(block_orders)), axis=1)
    block_orders = np.concatenate((block_orders, block_orders), axis=1)
    block_orders = block_orders[:num_participants,:num_sequences]

    if (not all_rows_unique(block_orders)):
        print("WARNING:: Not all participants have unique block orders")


    for p in range(num_participants):
        participant = { 'num_sequences': num_sequences, 'blocks_per_sequence': blocks_per_sequence, 'num_stimuli': num_stimuli,
        'trials_per_block': trials_per_block, 'target_trial_percentage': target_trial_percentage, 'sequences' : [] }

        for s in range(num_sequences):
            sequence = {'targets': [],'blocks': [] }

            blocks = []
            target_indices = block_permutations[block_orders[p,s],:]
            sequence['targets'] = target_indices.tolist()
            for target_index in target_indices:
                blocks.append(generate_block(trials_per_block, target_trials_per_block, target_trial_percentage, int(target_index), max_rand_targets=max_rand_targets, verbose=args.verbose))

            sequence['blocks'] = (blocks.copy())
            participant['sequences'].append(sequence.copy())

        # Export the participants stimuli presentation file
        export_subjectfile('./out', 'SUBJECT_{0}'.format(p), participant)