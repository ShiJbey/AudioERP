using UnityEngine;
using LSL;
using System.Collections.Generic;

namespace AudioERP
{
    public class ERPMenu : MonoBehaviour
    {


        [Header("Training Settings")]
        // Number of target trials per option
        public int targetTrialsPerOption = 30;
        public bool trainSingleStimulus = true;
        public int trainTarget = 0;
        [Header("Stimulus Settings")]
        public float interStimInterval = .250f;
        public float interTrialInterval = 1.5f;
        public bool shuffleStimulusPositions = false;
        public bool shuffleStimulusPresentation = true;
        [Header("Menu Configuration")]
        public ERPMenuOption[] options;
        [Header("Data Handling")]
        public SimpleDataManager dataManager;
        public UnityLSLConnector lslConnector;
        public enum ExecutionMode { TRAINING, LIVE, NODATA };
        public ExecutionMode mode = ExecutionMode.TRAINING;

        // Timer for playing stimuli
        private float timeUntilNextPlay;
        // Index of stimuli (option) currently being played
        private int currentOption;
        // Number of elapsed trails in this current sequence
        private int elapsedTrialCount;
        // Index of item that should be fixated on during a training sequence
        private int trainTargetIndex;
        // List of the initial positions of the stimuli
        private List<Vector3> stimulusPositions = new List<Vector3>();
        // List of remaining options to choose from when selecting which option/
        // stimulus to play next
        private List<int> remainingOptions = new List<int>();
        // List of event information for use when exporting the data
        private List<double[]> eventInformation = new List<double[]>();
        // Total number of trials for this trianing
        private int totalTrials;
        private float timeUntilStart = 5f;
        
        // Use this for initialization
        void Start()
        {
            // Ititialize timer until the next stimulus
            timeUntilNextPlay = timeUntilStart;
            // Set the counter for the number of elapsed trials
            elapsedTrialCount = 0;
            // Store the original positions of the stimuli
            foreach (ERPMenuOption option in options)
            {
                stimulusPositions.Add(option.transform.position);
            }
            // Shuffle the positions of the options
            if (shuffleStimulusPositions)
            {
                ShufflePositions();
            }
            // Reset the remaining options for plating options in the coming trial
            ResetRemainingOptions();
            Debug.Log(remainingOptions.Count);
            if (trainSingleStimulus)
            {
                trainTargetIndex = trainTarget;
            }else
            {
                // Set the current training target to be the first option
                trainTargetIndex = 0;
            }
            
            ResetHighlights();
            options[trainTargetIndex].VisualHighlight(true);
            // Calculate the total number of trials for this training session
            totalTrials = targetTrialsPerOption * options.Length;
        }

        // Update is called once per frame
        void Update()
        {
            if (lslConnector.state == UnityLSLConnector.ConnectionState.PROCESSING)
            {
                // Manipulate data, export for training, make class preditions
                // Read from inlet until sample time is outside of current  epoch durations
                double timeOfLastEvent = eventInformation[eventInformation.Count - 1][0];
                dataManager.PullRemainingData(timeOfLastEvent);

                //Debug.Log("Processing Data");
                //dataManager.AddEventCodes(eventInformation);

                if (mode == ExecutionMode.TRAINING)
                {
                    //dataManager.ExportData();
                    //Debug.Log("Wrote out data");

                    // Clear the event information, and data
                    //dataManager.ClearData();
                    //eventInformation.Clear();

                    // Choose a new training Target
                    // Set training to resume on the next options
                    trainTargetIndex++;

                    ResetHighlights();
                    if (trainTargetIndex < options.Length)
                        options[trainTargetIndex].VisualHighlight(true);

                    if (trainTargetIndex <= options.Length)
                    {
                        Debug.Log("Supposed to write out: " + eventInformation.Count + " - on trial:" + elapsedTrialCount);
                        Debug.Log("Processing Data");
                        dataManager.AddEventCodes(eventInformation);
                        dataManager.ExportData();
                        Debug.Log("Wrote out data");
                        dataManager.ClearData();
                        eventInformation.Clear();
                    }
                }

                // Set the connection back to collecting data
                this.lslConnector.state = UnityLSLConnector.ConnectionState.COLLECTING;
            }
            else
            {
                
                // Proceed according to the execution mode
                if (this.mode == ExecutionMode.TRAINING && lslConnector.InletConnected())
                {
                    // Update the stimulus timer
                    timeUntilNextPlay -= Time.deltaTime;
                    // Actively collect data if the inlet is connected
                    if (elapsedTrialCount < totalTrials && timeUntilNextPlay <= 0 && trainTargetIndex < options.Length)
                    {
                        
                        // Select the next option from those that have not been played yer
                        currentOption = SelectNextOption();
                        // Play the current option
                        options[currentOption].Play();
                        // Create Event for this play
                        CreateEvent(liblsl.local_clock(), currentOption == trainTargetIndex, currentOption);
                        // Reset the timer for when to play the next sound
                        timeUntilNextPlay = interStimInterval;
                        if (remainingOptions.Count == 0)
                        {
                            // We have reached the end of the options
                            Debug.Log("Ending trial: " + elapsedTrialCount);
                            // Set the time until the next stimulus to be the time between trials
                            timeUntilNextPlay = interTrialInterval;
                            // Increment the trial count
                            ++elapsedTrialCount;
                            // Reset the available options
                            ResetRemainingOptions();
                            if (shuffleStimulusPositions)
                            {
                                ShufflePositions();
                            }
                            // Handle the end of a sequence
                            if (elapsedTrialCount != 0 && elapsedTrialCount % targetTrialsPerOption == 0)
                            {
                                Debug.Log("End of Training on Option: " + trainTargetIndex);
                                // Set the LSL connector to start processing data
                                lslConnector.state = UnityLSLConnector.ConnectionState.PROCESSING;
                            }
                        }
                        
                    }
                }
                else if (this.mode == ExecutionMode.NODATA)
                {
                    // Update the stimulus timer
                    timeUntilNextPlay -= Time.deltaTime;
                    // Just play the stimuli without pulling data samples
                    if (timeUntilNextPlay <= 0)
                    {
                        // Play the current option
                        options[currentOption].Play();
                        // Increment the current option index
                        ++currentOption;
                        // Reset the timer for when to play the next sound
                        timeUntilNextPlay = interStimInterval;
                        if (currentOption == options.Length)
                        {
                            // We have reached the end of the options
                            currentOption = 0;
                            Debug.Log("Ending trial: " + elapsedTrialCount);
                            ++elapsedTrialCount;
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
            trainTargetIndex = Mathf.RoundToInt(UnityEngine.Random.Range(0, options.Length - 1));
            options[trainTargetIndex].VisualHighlight(true);
        }

        int SelectNextOption()
        {
            if (remainingOptions.Count > 1 && shuffleStimulusPresentation)
            {
                System.Random rnd = new System.Random();
                int indexOfNextOption = rnd.Next(remainingOptions.Count);
                int nextOption = remainingOptions[indexOfNextOption];
                remainingOptions.RemoveAt(indexOfNextOption);
                Debug.Log("Next Option: " + nextOption);
                return nextOption;
            }
            else
            {
                int nextOption = remainingOptions[0];
                remainingOptions.RemoveAt(0);
                Debug.Log("Next Option: " + nextOption);
                return nextOption;
            }
            
        }

        void ResetHighlights()
        {
            foreach (ERPMenuOption option in options)
            {
                option.VisualHighlight(false);
            }
        }

        void ResetRemainingOptions()
        {
            remainingOptions.Clear();
            for (int i = 0; i < options.Length; i++)
            {
                remainingOptions.Add(i);
            }
        }

        // Shuffles the positions of the stimuli
        void ShufflePositions()
        {
            // NOTE: WE NEED A WAY TO SHUFFLE STIMULI
            for (int i = 0; i < options.Length; i++)
            {
                options[i].transform.position = stimulusPositions[i];
            }
        }

        // Given a if an event is a target event and the associated index
        // of the option that emiited the event, returns a unique integer
        // representing the event
        public static int EncodeEvent(bool isTarget, int optionIndex)
        {
            int optionNumber = optionIndex + 1;
            int classification = 0;
            if (isTarget)
                classification = 1;
            return (classification << 2) + optionNumber;
        }

        void CreateEvent(double time, bool isTarget,int optionIndex)
        {
            int eventCode = EncodeEvent(isTarget, optionIndex);
            double[] info = { time, eventCode };
            eventInformation.Add(info);
        }
    }
}
