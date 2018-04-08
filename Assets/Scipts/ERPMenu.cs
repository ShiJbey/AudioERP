using UnityEngine;
using LSL;

namespace AudioERP
{
    public class ERPMenu : MonoBehaviour
    {

        
        [Header("Training Settings")]
        // Number of trials per sequence
        public int numTrials = 3;
        // Total number of sequences in the training session
        public int numTrainingSequences = 36;
        [Header("Stimulus Settings")]
        public float interStimInterval = .1f;
        public float interTrialInterval = 1f;
        [Header("Menu Configuration")]
        public ERPMenuOption[] options;
        [Header("Data Handling")]
        public DataManager dataManager;
        public UnityLSLConnector lslConnector;
        public enum ExecutionMode { TRAINING, LIVE, NODATA };
        public ExecutionMode mode = ExecutionMode.TRAINING;

        // Timer for playing stimuli
        private float timeUntilNextPlay;
        // Index of stimuli (option) currently being played
        private int currentOption;
        // Number of elapsed trails in this current sequence
        private int trialCount;
        // Number of elapsed sequences
        private int sequenceCount;
        // Index of item that should be fixated on during a training sequence
        private int trainTargetIndex;
        
     
        // Use this for initialization
        void Start()
        {
            timeUntilNextPlay = interTrialInterval;
            trialCount = 0;
            if (mode == ExecutionMode.TRAINING)
            {
                SelectNextTrainingTarget();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (lslConnector.state == UnityLSLConnector.ConnectionState.PROCESSING)
            {
                // Manipulate data, export for training, make class preditions
                // Read from inlet until sample time is outside of current  epoch durations
                dataManager.PullRemainingData();

                Debug.Log("Processing Data");
                dataManager.ProcessEpochs(mode);
                // Clear Data since we are done with it
                dataManager.ClearData();

                if (mode == ExecutionMode.TRAINING)
                {
                    // Export the epoch data for training
                    dataManager.WriteOutExampleData("train_data.csv");
                    dataManager.WriteoutTrainLabels("train_labels.csv");
                    Debug.Log("Wrote out data");
                    // We are done with the epochs for this past sequence
                    dataManager.ClearEpochs();
                    dataManager.ClearSortedEpochs();
                    // Choose a new training Target
                    SelectNextTrainingTarget();
                    // Increment the number of elapsed sequences in the training session   
                    //sequenceCount++;
                }

                // Reset the count for the number of elapsed trials in the sequence
                trialCount = 0;

                // Set the connection back to collecting data
                this.lslConnector.state = UnityLSLConnector.ConnectionState.COLLECTING;
            }
            else
            {
                // Update the stimulus timer
                timeUntilNextPlay -= Time.deltaTime;
            
                if ((this.mode == ExecutionMode.TRAINING || this.mode == ExecutionMode.LIVE) 
                    && lslConnector.InletConnected())
                {
                    // Actively collect data if the inlet is connected
                    if ((this.mode == ExecutionMode.TRAINING && sequenceCount < numTrainingSequences) 
                        || this.mode == ExecutionMode.LIVE)
                    {
                        if (timeUntilNextPlay <= 0)
                        {
                            // Play the current option
                            options[currentOption].OnHighlight();
                            // Create Event for this play
                            CreateEvent((float)liblsl.local_clock(), options[currentOption]);
                            // Increment the current option index
                            ++currentOption;
                            // Reset the timer for when to play the next sound
                            timeUntilNextPlay = interStimInterval;
                            if (currentOption == options.Length)
                            {
                                // We have reached the end of the options
                                // Reset the current option
                                currentOption = 0;
                                Debug.Log("Ending trial: " + trialCount + " in Sequence: " + this.sequenceCount);
                                // Set the time until the next stimulus to be the time between trials
                                timeUntilNextPlay = interTrialInterval;
                                // Increment the trial count
                                ++trialCount;
                                // Handle the end of a sequence
                                if (trialCount == numTrials)
                                {
                                    Debug.Log("Ending trial: " + trialCount + " in Sequence: " + this.sequenceCount);
                                    Debug.Log("End of Sequence: " + sequenceCount);
                                    Debug.Log("Data manager has: " + dataManager.GetAllEpochs().Count + " epochs.");
                                    // Increment the number of sequences that have elapsed
                                    ++sequenceCount;
                                    // Reset the trail count for the next sequence
                                    trialCount = 0;
                                    // Set the LSL connector to start processing data
                                    lslConnector.state = UnityLSLConnector.ConnectionState.PROCESSING;
                                }
                            }
                        }
                    }
                }
                else if (this.mode == ExecutionMode.NODATA)
                {
                    // Just play the stimuli without pulling data samples
                    if (timeUntilNextPlay <= 0)
                    {
                        // Play the current option
                        options[currentOption].OnHighlight();
                        // Increment the current option index
                        ++currentOption;
                        // Reset the timer for when to play the next sound
                        timeUntilNextPlay = interStimInterval;
                        if (currentOption == options.Length)
                        {
                            // We have reached the end of the options
                            currentOption = 0;
                            Debug.Log("Ending trial: " + trialCount);
                            ++trialCount;
                            timeUntilNextPlay = interTrialInterval;
                        }
                    }
                }
            }
        }

        // Resets the menu so that no options are selected
        void Reset()
        {
            foreach (ERPMenuOption option in options)
            {
                option.Deselect();
            }
        }

        // Markes a menu option as selected
        void Select(ERPMenuOption option)
        {
            option.Select();
        }

        // Randomly chooses which menu option to highlight as the target
        // for the current training sequence
        void SelectNextTrainingTarget()
        {
            options[trainTargetIndex].VisualHighlight(false);
            trainTargetIndex = Mathf.RoundToInt(Random.Range(0, options.Length - 1));
            options[trainTargetIndex].VisualHighlight(true);
        }

        // Adds a new Epoch to the datamanager's list of epochs
        private void CreateEvent(float time, ERPMenuOption obj)
        {
            if (mode == ExecutionMode.TRAINING)
            {
                if (obj.Equals(options[trainTargetIndex]))
                {
                    dataManager.AddEpoch(new Epoch(obj.transform, time, true));
                }
                else
                {
                    dataManager.AddEpoch(new Epoch(obj.transform, time, false));
                }
            }
            else
            {
                dataManager.AddEpoch(new Epoch(obj.transform, time));
            }
        }
    }
}
