using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LSL;

namespace AudioERP
{
    
    /*
     * Presents all stimuli in a serial manner where each presentation is
     * spaced by a SOA time and presentations of target stimuli after
     * specific non-target stimuli are balanced for each condition
     */
    public class SerialStimPresenter : MonoBehaviour
    {
        /*
         * Tracks the number of events where a certain target
         * stimulus is preceded by a given stimulus
         * 
         * This struct is used in determining the order of stimulus presentation
         */
        protected class StimPair
        {
            public int targetStim;
            public int preceedingStim;
            public int count;
            public int leftToAdd;

            public StimPair(int target, int previous, int leftToAdd)
            {
                targetStim = target;
                preceedingStim = previous;
                this.leftToAdd = leftToAdd;
                count = 0;
            }

            public override string ToString()
            {
                return preceedingStim + " => " + targetStim + " [Presented " + count + " (" + leftToAdd +")]";
            }
        }


        [Header("Stimulus Presentation")]
        // Number of sequences contraining blocks of trials
        public int sequences = 15;
        // Number of blocks per sequence (defined by number of options)
        private int blocks;
        // Number of trials per block
        public int trials = 45;
        // Percentage of trials within a block associated with target stimulus
        public float targetTrialPercentage = 0.2f;
        // Total number of trials in a block
        protected int totalTrials;
        // Number of trials in a block where the target stimulus is presented
        public int nTargetTrials;
        // Total number of trials in a block that are for nontarget stimuli
        protected int nNonTargetTrials;
        // Number of trials devoted to each nontarget stimulus within a block
        protected int trialsPerNonTarget;


        // Time between stimulus presentations
        public float stimOnsetAsync = 0.375f;
        // Index of the stimulus that is the target
        public int targetStimulusIndex = 0;

        // Allow repetitions of the target stimulus
        public bool allowTargetRepeat = false;

        // Array of stimuli (children of this GameObject)
        protected ERPMenuOption[] stimuli;
        // Array of stimulus presentation indices
        protected List<int> stimPresentationOrder = new List<int>();

        // Minimum non target stimuli between target presentations
        public int minTargetSeparation = 2;
        // Maximum non target stimuli between target presentations
        public int maxTargetSeparation = 6;
        
       
        
        [HideInInspector]
        // Manages whether or not this is currently running
        public bool running = false;
        // Time offset (seconds) for when to start presenting stimuli after starting presentation
        protected float presentationStartTimeOffset = 5f;
        // Timer until the next stimulus presentation
        protected float timeUntilNextStim;
        // Number of elapsed trails in this current sequence
        protected int elapsedTrialCount = 0;
        
        // Pairings of conditions characterized by a target stimulus preceeded by another stimulus
        protected List<StimPair> stimPairs;
        // How many trials should devoted to presenting each stimulus directly prior to a target stimulus
        protected int desiredStimPairings;
        

        [Header("Data Collection")]
        public SimpleDataManager dataManager;
        public UnityLSLConnector lslConnector;
        public string exportPath = "Assets/Data/";
        // List of event information for use when exporting the data
        protected List<double[]> eventInformation = new List<double[]>();




        // Use this for initialization
        void Start()
        {
            // Obtain the array of options
            stimuli = this.transform.GetComponentsInChildren<ERPMenuOption>();
            if (stimuli == null)
            {
                Debug.LogError("No stimuli have been attached to this object");
            }
            blocks = stimuli.Length;

            // Ensure that the target stimuli index is valid
            if (targetStimulusIndex < 0 || targetStimulusIndex >= stimuli.Length)
            {
                Debug.LogError("Stimulus index falls outside bounds. Defaulting to 0");
                targetStimulusIndex = 0;
            }

            // Calculate to total number of trials
            totalTrials = sequences * blocks * trials;

            // Calculate how manytrials each stimulus will have in a block
            nTargetTrials = Mathf.RoundToInt((float)trials * targetTrialPercentage);
            nNonTargetTrials = trials - nTargetTrials;
            trialsPerNonTarget = (trials - nTargetTrials) / (stimuli.Length - 1);
            
            // How many trials should devoted to presenting each stimulus prior to a
            // target stimulus
            if (allowTargetRepeat)
            {
                // We are allowing the target stimulus to be preceeded by itself
                desiredStimPairings = nTargetTrials / stimuli.Length;
            }
            else
            {
                desiredStimPairings = nTargetTrials / (stimuli.Length - 1);
            }

            // Get a list to count the number of each stimulus pairing
            stimPairs = GenerateTargetEventPairings(desiredStimPairings);

            // Create the presentation Order
            GeneratePresentationOrder();
        }

        // Update is called once per frame
        void Update()
        {
            if (running)
            {
                // Update the stimulus timer
                timeUntilNextStim -= Time.deltaTime;
                if (elapsedTrialCount < stimPresentationOrder.Count && timeUntilNextStim <= 0)
                {
                    if (elapsedTrialCount > 0)
                    {
                        stimuli[stimPresentationOrder[elapsedTrialCount - 1]].VisualHighlight(false);
                    }
                    // Present Stimulus
                    stimuli[stimPresentationOrder[elapsedTrialCount]].Play();
                    // Highlight
                    stimuli[stimPresentationOrder[elapsedTrialCount]].VisualHighlight(true);
                    CreateEvent(liblsl.local_clock(),
                        stimPresentationOrder[elapsedTrialCount] == targetStimulusIndex,
                        stimPresentationOrder[elapsedTrialCount]);
                    elapsedTrialCount++;
                    timeUntilNextStim = stimOnsetAsync;
                }
                if (elapsedTrialCount >= stimPresentationOrder.Count && timeUntilNextStim <= 0)
                {

                    StopPresentation();
                    stimuli[stimPresentationOrder[elapsedTrialCount - 1]].VisualHighlight(false);
                    double timeOfLastEvent = eventInformation[eventInformation.Count - 1][0];
                    dataManager.PullRemainingData(timeOfLastEvent);
                    lslConnector.state = UnityLSLConnector.ConnectionState.PROCESSING;
                    Debug.Log("Total Samples: " + dataManager.GetAllData().Count);
              
                    Debug.Log("Processing Data");
                    dataManager.AddEventCodes(eventInformation);
                    dataManager.ExportData();
                    Debug.Log("Wrote out data");
                    dataManager.ClearData();
                    eventInformation.Clear();
                    Debug.Log("Exporting the Config files");
                    // Export the presentation order
                    ExportPresentationOrder();
                    // Write out the configuration
                    ExportConfig();
                }
            }
        }

        public void ResetPresenter()
        {
            elapsedTrialCount = 0;
            // Get a list to count the number of each stimulus pairing
            stimPairs = GenerateTargetEventPairings(desiredStimPairings);
            // Create the presentation Order
            GeneratePresentationOrder();
        }

        public void StartPresentation()
        {
            running = true;
            timeUntilNextStim = presentationStartTimeOffset;
        }

        public void StopPresentation()
        {
            running = false;
        }

        // Creates a random ordered 
        protected virtual void GeneratePresentationOrder()
        {
            stimPresentationOrder.Clear();
            // Create an array of counts for how many of each stimulus needing
            // to be added to the presentation order
            int[] remainingStimCounts = new int[stimuli.Length];
            for (int i = 0; i < remainingStimCounts.Length; ++i)
            {
                if (i == targetStimulusIndex)
                {
                    remainingStimCounts[i] = nTargetTrials;
                }
                else
                {
                    remainingStimCounts[i] = trialsPerNonTarget - desiredStimPairings;
                }
            }

            // Count of the remaining number of non-target stimuli to add
            int remainingNTPresentations = (stimuli.Length - 1) * (trialsPerNonTarget - desiredStimPairings);

            // For each space insert a random order of non-target stimuli
            // followed by a non-target->target pairing
            while (remainingNTPresentations > 0 || remainingStimCounts[targetStimulusIndex] > 0)
            {
                if (remainingNTPresentations > 0)
                {
                    int stimuliToAdd = Mathf.RoundToInt(UnityEngine.Random.value * Math.Min(remainingNTPresentations, maxTargetSeparation - 1));
                    if (stimuliToAdd < minTargetSeparation && remainingStimCounts[targetStimulusIndex] > 0)
                    {
                        stimuliToAdd = minTargetSeparation;
                    }
                    for (int i = 0; i < stimuliToAdd; ++i)
                    {
                        // Randomly choose a non-target stim
                        int stimIndex = ChooseAvailableStimulus(remainingStimCounts, true);
                        stimPresentationOrder.Add(stimIndex);
                        remainingStimCounts[stimIndex]--;
                        remainingNTPresentations--;
                    }
                }
                else
                {
                    int stimuliToAdd = Mathf.RoundToInt(UnityEngine.Random.value * maxTargetSeparation);
                    if (stimuliToAdd < minTargetSeparation && remainingStimCounts[targetStimulusIndex] > 0)
                    {
                        stimuliToAdd = minTargetSeparation;
                    }
                    for (int i = 0; i < stimuliToAdd; ++i)
                    {
                        // Randomly choose a non-target stim
                        int stimIndex = ChooseAvailableStimulus(remainingStimCounts, true);
                        stimPresentationOrder.Add(stimIndex);
                    }
                }
                
                if (remainingStimCounts[targetStimulusIndex] > 0)
                {
                    // Randomly choose a non-target->target pairing
                    int pairIndex = ChooseAvailableStimPair();
                    // Add the associated stimuli to the order and decrease the remaining count
                    stimPresentationOrder.Add(stimPairs[pairIndex].preceedingStim);
                    stimPresentationOrder.Add(stimPairs[pairIndex].targetStim);
                    stimPairs[pairIndex].leftToAdd--;
                    stimPairs[pairIndex].count++;
                    remainingStimCounts[targetStimulusIndex]--;
                }
            }
        }

        /*
         * Returns an array of the indices of the pairings in the stimPairs List which
         * still have a remaining count greater than 0;
         */
         protected int[] GetAvailablePairings()
        {
            List<int> availablePairings = new List<int>();
            for (int i = 0; i < stimPairs.Count; ++i)
            {
                if (stimPairs[i].leftToAdd > 0)
                {
                    availablePairings.Add(i);
                }
            }
            return availablePairings.ToArray();
        }

        /*
         * Randomly returns the index to a stimulus pair with a leftToAdd count higher than 0
         */
        protected int ChooseAvailableStimPair()
        {
            int[] availablePairrings = GetAvailablePairings();
            int index = Mathf.RoundToInt(UnityEngine.Random.value * (availablePairrings.Length - 1));
            return availablePairrings[index];
        }

        /*
         * Exports the order that stimuli are presented
         */
        protected virtual void ExportPresentationOrder()
        {
            String currentDate = DateTime.Now.ToString("MM-dd-yy_H-mm-ss");
            string path = exportPath + "presentation_order_" + currentDate + ".txt";
            using (StreamWriter writer = File.CreateText(path))
            {
                foreach(int stimIndex in stimPresentationOrder)
                {
                    writer.WriteLine(stimIndex);
                }
            }
        }
        
        /*
         * Write out the calculated configuration for stimulus presentation
         */
        protected void ExportConfig()
        {
            String currentDate = DateTime.Now.ToString("MM-dd-yy_H-mm-ss");

            string path = exportPath + "config_" + currentDate + ".txt";
            using (StreamWriter writer = File.CreateText(path))
            {
                writer.WriteLine("Audio ERP Experiment");
                writer.WriteLine(DateTime.Now.ToString("MM/dd/yy H:mm:ss"));
                writer.WriteLine();
                writer.WriteLine("Total number of stimuli: {0}", stimuli.Length);
                writer.WriteLine("Target stimulus: {0}", targetStimulusIndex);
                writer.WriteLine("Total number of tials: {0}", totalTrials);
                writer.WriteLine("Total number of target Tials: {0}", nTargetTrials);
                writer.WriteLine("Total number of non-target tials: {0}", nNonTargetTrials);
                writer.WriteLine("Trials per non-target stimulus: {0}", trialsPerNonTarget);
                if (allowTargetRepeat)
                {
                    writer.WriteLine("Target Can Repeat: Yes");
                }
                else
                {
                    writer.WriteLine("Target Can Repeat: No");
                }
                writer.WriteLine("Desired number of each stimulus pairing: {0}", desiredStimPairings);
                writer.WriteLine("=== Stimulus Pairings ===");
                foreach (StimPair stimPair in stimPairs)
                {
                    writer.WriteLine(stimPair.ToString());
                }
            }
        }

        /*
         * Returns true if the array contains all zeros
         */
        private bool AllZeros(int[] arr)
        {
            for (int i = 0; i < arr.Length; ++i)
            {
                if (arr[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        /*
         * Returns a list of all the pairings of a target stimulus preceeded by
         * a not-target stimulus
         */
        protected virtual List<StimPair> GenerateTargetEventPairings(int numPresentations)
        {
            List<StimPair> eventPairings = new List<StimPair>();
            for (int i = 0; i < stimuli.Length; ++i)
            {
                if (i != targetStimulusIndex)
                {
                    // Create a new event pairing
                    eventPairings.Add(new StimPair(targetStimulusIndex, i, numPresentations));
                }
            }
            return eventPairings;
        }

        /*
         * Increments the count of a stimulus pair (target condition balancing)
         */
        protected void IncrementPairingCount(int target, int nontarget, List<StimPair> stimPairs)
        {
            for (int i = 0; i < stimPairs.Count; ++i)
            {
                if (stimPairs[i].targetStim == target && stimPairs[i].preceedingStim == nontarget)
                {
                    stimPairs[i].count++;
                    return;
                }
            }
            Debug.LogError("Could not find valid pairing to increment");
        }

        /*
         * Returns a random number between [0, # stimuli - 1]
         * The Target stimulus may be excluded from the range
         */
        protected int RandomStimIndex(bool excludeTarget)
        {
            int nAttempts = 0; 
            while (nAttempts < 100)
            {
                int stimIndex = Mathf.RoundToInt((stimuli.Length - 1) * UnityEngine.Random.value);
                if (excludeTarget)
                {
                    if (stimIndex != targetStimulusIndex)
                    {
                        return stimIndex;
                    }
                }
                else
                {
                    return stimIndex;
                }
            }
            Debug.LogError("Could not generate a proper random stimulus index");
            return -1;
        }

        /*
         * Given an array of counts for remainig stimuli presentations to add,
         * returns a list of the indices which have counts greater than 0
         */
        protected List<int> GetAvailableStimuli(int[] remainingStimCounts, bool excludeTarget)
        {
            List<int> availableStimuli = new List<int>();
            for (int i = 0; i < remainingStimCounts.Length; ++i)
            {
                if (remainingStimCounts[i] > 0)
                {
                    if (i == targetStimulusIndex)
                    {
                        if (!excludeTarget)
                        {
                            availableStimuli.Add(i);
                        }
                    }
                    else
                    {
                        availableStimuli.Add(i);
                    }
                }
            }
            return availableStimuli;
        }

        /*
         * Given an array of counts for remainig stimuli presentations to add,
         * randomly selects and returns the index of a stimulus that has a count > 0
         */
        protected int ChooseAvailableStimulus(int[] remainingStimCounts, bool excludeTarget)
        {
            List<int> availableStimuli = GetAvailableStimuli(remainingStimCounts, excludeTarget);
            if (availableStimuli.Count == 0)
            {
                return RandomStimIndex(excludeTarget);
            }
            else
            {
                int chosenStim = Mathf.RoundToInt((availableStimuli.Count - 1) * UnityEngine.Random.value);
                return availableStimuli[chosenStim];
            }
        }

        // Given: if an event is a target event and the associated index
        // of the option that emiited the event, Returns: an integer code
        // representing the event
        public static int EncodeEvent(bool isTarget, int optionIndex)
        {
            int optionNumber = optionIndex + 1;
            int classification = 0;
            if (isTarget)
                classification = 1;
            return (classification << 2) + optionNumber;
        }

        protected void CreateEvent(double time, bool isTarget, int optionIndex)
        {
            int eventCode = EncodeEvent(isTarget, optionIndex);
            double[] info = { time, eventCode };
            eventInformation.Add(info);
        }

        public int GetNumStimuli()
        {
            return stimuli.Length;
        }

        public void PlayStimulus(int stimIndex)
        {
            stimuli[stimIndex].Play();
        }

        public void SelectOption(int stimIndex)
        {
            if (stimIndex >= 0 && stimIndex < stimuli.Length)
            {
                stimuli[stimIndex].OnSelect();
            }
        }
    }
}
