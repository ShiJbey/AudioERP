# NASA-TLX Task Component Comparision Order

The first part of calculateing the NASA-TLX score is to determine the
weights for each one of the components (Physical Demand, Menatal Demand, 
Temporal Demand, Performace, Effort, Frustration). We do this by showing
the participant pairs of all the components (15 in total), 
and asking them to choose which one factored more into the workload
of the system.

This script was written to come up with pseudorandom orderings for showing each
pair of components. The NASA-TLX handbook says that each participant should
be should be shown each pair individually and in a unique random order.

# Usage:

```
python component_comparision.py participants out_dir
```

## Arguments
```participants```: Number of participants to orderings for

```out_dir```: Where to export the SUBJECT json files

## Output JSON Format:
```
{
    'order' : [
        {
            'component_a': (String),
            'component_b': (String)
        },
        ...
        {
            'component_a': (String),
            'component_b': (String)
        }
    ]
}
```