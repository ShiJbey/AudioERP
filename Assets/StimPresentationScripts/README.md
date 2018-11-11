# Stimulus Presentation Order

Generates the presentation order for participants and exports them to JSON files. These files are then imported by the game engine at runtime. 

# Usage:

```
python stim_presentation.py sequences blocks trials target_percentage participants
```
example:
```
python stim_presentation.py 15 3 45 0.33 10
```

## Arguments
```sequence```: Number of sequences the participant will hear

```blocks```: Number of blocks per sequence (this is assumed to be the number of stimuli in the experiment)

```trials```: Number of trials per block

```target_percentage```: Percentage of trials in a block that are target trials

```participants```: Number of participants to generate files for

## Output JSON Format:
```
{
    num_sequences: (int),
    blocks_per_sequence: (int),
    num_stimuli: (int), 
    trials_per_block: (int),
    target_trial_percentage: (float),
    'sequences' : [
        {
            blocks:[
                [(int), ..., (int)],
                ...
                [(int), ..., (int)]
            ]
        },
        ...
        {
            blocks:[
                [(int), ..., (int)],
                ...
                [(int), ..., (int)]
            ]
        }
    ]
}
```
