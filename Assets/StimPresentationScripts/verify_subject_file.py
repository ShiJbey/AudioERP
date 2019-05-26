import numpy as np

def print_info(participant_config):
    """
    Given a dictionary representing a participant
    prints all the relevant information
    """
    print(participant_config)

def print_actual_sequence_info(participant_config):
    """
    Determines the number of each type of trial and prints
    the results
    """
    num_stimuli = participant_config['num_stimuli']

    # Hold the counts of the number of trials
    target_trials = np.zeros(num_stimuli)
    nontarget_trials = np.zeros(num_stimuli)

    for sequence in participant_config['sequences']:
        for block_index in range(participant_config['blocks_per_sequence']):
            # What stimulus is the current target
            target_index = sequence['targets'][block_index]
            for trial in sequence['blocks'][block_index]:
                if trial == target_index:
                    target_trials[trial] += 1
                else:
                    nontarget_trials[trial] += 1

    print('non-target/left: {}'.format(nontarget_trials[0]))
    print('non-target/middle: {}'.format(nontarget_trials[1]))
    print('non-target/right: {}'.format(nontarget_trials[2]))
    print('target/left: {}'.format(target_trials[0]))
    print('target/middle: {}'.format(target_trials[1]))
    print('target/right: {}'.format(target_trials[2]))

def verify_data_file(path):
    """
    Prints the number of non target and target trials
    recorded inside a data.csv file
    """
    eeg_data = np.genfromtxt(path, delimiter=',')
    event_channel = eeg_data[:,1]
    print(event_channel.shape)
    target = {}
    nontarget = {}
    for i in range(event_channel.size):
        event_code = int(event_channel[i])
        if event_code > 3:
            if str(event_code) in target.keys():
                target[str(event_code)] = target[str(event_code)] + 1
            else:
                target[str(event_code)] = 1
        elif event_code > 0 and event_code <= 3:
            if str(event_code) in nontarget.keys():
                nontarget[str(event_code)] = nontarget[str(event_code)] + 1
            else:
                nontarget[str(event_code)] = 1

    print(nontarget)
    print(target)





