# component_comparision.py
#
# This script automates the process of determining what order
# to show participants the comparisions of each of the components
# of the NASA-TLX survey (e.g. Mental Demand vs. Physical Demand)

import argparse
import math
import random
import sys
import numpy as np
from itertools import permutations
import json
import os.path
import hashlib

if (__name__ == "__main__"):

    # Get the command line args
    parser = argparse.ArgumentParser()
    parser.add_argument('participants', help='Number of participants to generate orderings for', type=int)
    parser.add_argument('out_dir', help='Directory to export to', type=str)
    args = parser.parse_args()

    # Extract the args
    num_participants = args.participants
    out_dir = args.out_dir

    # Comparisions
    comparisions = [
        ("Effort", "Performance"),
        ("Temporal Demand", "Frustration"),
        ("Temporal Demand", "Effort"),
        ("Physical Demand", "Frustration"),
        ("Performance", "Frustration"),
        ("Physical Demand", "Temporal Demand"),
        ("Physical Demand", "Performance"),
        ("Temporal Demand", "Mental Demand"),
        ("Frustration", "Effort"),
        ("Performance", "Mental Demand"),
        ("Performance", "Temporal Demand"),
        ("Mental Demand", "Effort"),
        ("Mental Demand", "Physical Demand"),
        ("Effort", "Physical Demand"),
        ("Frustration", "Mental Demand")
    ]

    # Generate orderings by creating orders using the indices
    # of the comparison tuples above
    participant_orders = np.zeros((num_participants, len(comparisions)), dtype=int)

    # Seed the random number generator
    seed = 8734627
    np.random.seed(seed=seed)

    # Shuffle index ordering using the seed defined above
    for participant in range(num_participants):
        participant_orders[participant,:] = np.arange(len(comparisions))
        np.random.shuffle(participant_orders[participant,:])

    
    # Write the orderings out to files
    for participant in range(num_participants):
        index_order = participant_orders[participant,:]

        ordering = { 'order': [] }
        for i in range(len(index_order)):
            ordering['order'].append({ 'component_a': comparisions[i][0], 'component_b': comparisions[i][1] })

        filename_w_extention = 'SUBJECT_{0}.json'.format(participant)
        file_path_name = os.path.join(out_dir, filename_w_extention)
        with open (file_path_name, 'w') as fp:
            json.dump(ordering, fp)

    """
    # Write the orderings out to files
    for i in range(num_participants):
        ordering = participant_orders[i,:]
        filename_w_extention = 'SUBJECT_{0}.json'.format(i)
        file_path_name = os.path.join(out_dir, filename_w_extention)
        with open (file_path_name, 'w') as fp:
            json.dump({ 'comparisions': ordering.tolist() }, fp)
    """



