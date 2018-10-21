using UnityEngine;
using LSL;
using MNEServer;

namespace AudioERP
{
    
    /*
     * Presents all stimuli in a serial manner where each presentation is
     * spaced by a SOA time and presentations of target stimuli after
     * specific non-target stimuli are balanced for each condition
     */
    public class SimpleStimPresenter : SerialStimPresenter
    {
        [Header("Stimulus Presentation")]
        public float interTrialInterval = 1.5f;
        // Count of the number of sequences per trial
        private int sequenceCount;
        public int sequencesPerTrial = 10;
        // Count of the number of subtrials per sequence
        private int subtrialCount;
        private int subtrialsPerSequence;
        // Count of the number of elapsed Trials
        private int trialCount;

        // Determines if the system pauses to present the next cure
        public bool calibrationMode = true; 
        // Used to present stimuli during training
        int[] stimOrdering;


        void InitCalibrationMode()
        {
            // Ensure that the target stimuli index is valid
            if (targetStimulusIndex < 0 || targetStimulusIndex >= stimuli.Length)
            {
                Debug.LogError("Stimulus index falls outside bounds. Defaulting to 0");
                targetStimulusIndex = 0;
            }


            // Calculate to total number of trials
            totalTrials = Mathf.CeilToInt(nTargetTrials / targetTrialPercentage);
            // Calculate how many trials will need to be non target trials
            nNonTargetTrials = totalTrials - nTargetTrials;
            // How many trials will each non-target stimulus receive
            trialsPerNonTarget = nNonTargetTrials / (stimuli.Length - 1);

            // Create the presentation Order
            GeneratePresentationOrder();
        }

        void InitLiveMode()
        {
            sequenceCount = 0;
            subtrialCount = 0;
            subtrialsPerSequence = stimuli.Length;
            stimOrdering = GetRandomStimOrdering();
        }

        // Use this for initialization
        void Start()
        {            
            // Obtain the array of options
            stimuli = this.transform.GetComponentsInChildren<ERPMenuOption>();
            if (stimuli == null)
            {
                Debug.LogError("No stimuli have been attached to this object");
            }
            // Intialize based on the current mode
            if (calibrationMode)
            {
                InitCalibrationMode();
            }
            else
            {
                InitLiveMode();
            }
            // Set the export path for the Data manager
            dataManager.exportDirectory = exportPath;
            dataManager.calibrationMode = calibrationMode;
        }

        void CalibrationModeUpdate()
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

        void LiveModeUpdate()
        {
            // If we are processing then do not decrement the SOA timer
            if (lslConnector.state == UnityLSLConnector.ConnectionState.PROCESSING)
            {
                
                lslConnector.state = UnityLSLConnector.ConnectionState.COLLECTING;
                timeUntilNextStim = interTrialInterval;
                // Output data

                //vrCamera.FadeToBlack();

                
                double timeOfLastEvent = eventInformation[eventInformation.Count - 1][0];
                dataManager.PullRemainingData(timeOfLastEvent);
                Debug.Log("Total Samples: " + dataManager.GetAllData().Count);

                Debug.Log("Processing Data");
                dataManager.AddEventCodes(eventInformation);
                string exportedPath = dataManager.ExportData();

                Debug.Log("Wrote out data");
                dataManager.ClearData();
                eventInformation.Clear();

                // Send the filename to the server
                Debug.Log("Kicking out data to the server");
                string classification = SynchronousSocketClient.StartClient(exportedPath);
                int predictedSelection;
                if (int.TryParse(classification, out predictedSelection))
                {
                    Debug.Log("Classification Server Predicted: " + predictedSelection);
                    SelectOption(predictedSelection);
                }
            }
            // Decrement the SOA timer and play stimuli
            else if (lslConnector.state == UnityLSLConnector.ConnectionState.COLLECTING)
            {
                // Loop through trials indefinitely
                timeUntilNextStim -= Time.deltaTime;
                if (sequenceCount < sequencesPerTrial)
                {
                    if (subtrialCount < subtrialsPerSequence && timeUntilNextStim <= 0)
                    {
                        ERPMenuOption currentStim = stimuli[stimOrdering[subtrialCount]];
                        // Highlight option if available
                        currentStim.VisualHighlight(true);
                        // Play the current option
                        currentStim.Play();
                        // Create Event for this play
                        CreateEvent(liblsl.local_clock(), false, stimOrdering[subtrialCount]);
                        
                        // Reset the timer for when to play the next sound
                        timeUntilNextStim = stimOnsetAsync;
                        subtrialCount++;
                    }
                    // Reached the end of a sequence
                    else if (subtrialCount == subtrialsPerSequence)
                    {
                        Debug.Log("End of Sequence");
                        // Reset the subtrial count and the subtrial ordering
                        subtrialCount = 0;
                        stimOrdering = GetRandomStimOrdering();
                        sequenceCount++;
                    }

                }
                // Reached the end of the trial
                else if (sequenceCount == sequencesPerTrial)
                {
                    // Kick the data out to the data processing server
                    Debug.Log("End of Trial");
                    sequenceCount = 0;
                    trialCount++;
                    lslConnector.state = UnityLSLConnector.ConnectionState.PROCESSING;
                }
            }
            
        }

        // Update is called once per frame
        void Update()
        {
            if (running)
            {
                if (calibrationMode)
                {
                    CalibrationModeUpdate();
                }
                else
                {
                    LiveModeUpdate();
                }
            }
        }

        int[] GetRandomStimOrdering()
        {
            // Create an array with the indices of the stimuli in order
            int[] stimOrdering = new int[stimuli.Length];
            for (int i = 0; i < stimuli.Length; i++)
            {
                stimOrdering[i] = i;
            }

            // Shuffle the array with: https://en.wikipedia.org/wiki/Fisher–Yates_shuffle
            for (int i = 0; i < stimOrdering.Length; i++)
            {
                int j = i + Mathf.RoundToInt(UnityEngine.Random.value * ((stimOrdering.Length - 1) - i));
                int temp = stimOrdering[i];
                stimOrdering[i] = stimOrdering[j];
                stimOrdering[j] = temp;
            }

            return stimOrdering;
        }

        // Creates presentation order where items are presented in
        // a random sequence
        protected override void GeneratePresentationOrder()
        {
            stimPresentationOrder.Clear();

            for (int i = 0; i < nTargetTrials; i++)
            {
                int[] stimOrdering = GetRandomStimOrdering();
                stimPresentationOrder.AddRange(stimOrdering);
            }
        }
  
    }
}
