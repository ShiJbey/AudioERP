using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.IO;
using LSL;

namespace AudioERP
{

    public class StimPresenter :  MonoBehaviour
    {
        //Selectable objects
        protected ERPMenuOption[] stimuli;

        [Header("Stimulus Presentation")]
        // Time between stimulus presentations
        public const float stimOnsetAsync = 0.375f;
        // Time offset (seconds) for presenting stimuli after starting the experiment
        protected const float presentationStartTimeOffset = 5f;
        // Number of seconds to wait before writing out data at the end of block
        protected const float timeBeforeDataWrite = 2f;
        // Time between cue period and actual trials
        public const float timeBetweenCueAndTrials = 3f;
        // Time left befor dataWrite
        protected float postBlockTimer;
        // Timer until the next stimulus presentation
        protected float nextStimTimer;
        // Highlight the Objects when playing sounds during a block
        public bool highlightCurrentStimulus = false;
        

        // Indices into the participant's stimulus order
        private StimulusOrder stimulusOrder = null;
        private int currentTrialIndex = 0;
        private int currentSequenceIndex = 0;
        private int currentBlockIndex = 0;

        // Cue Presentation Parameters
        public const int timesToCue = 3;
        private int elapsedCues = 0;

        // Menus/Screens displayed during breaks and at the end of experimentation
        public BreakMenu breakMenu;
        public BreakMenu participantBreakMenu;
        public GameObject endScreen;
        public GameObject vrEndScreen;

        [HideInInspector]
        // Manages whether or not this is currently running
        public bool running = false;
        private bool experimentOver = false;
        private bool atEndOfSequence = false;
     
        
        [Header("Subject Information")]
        public int subjectNumber = -1;
        public string subjectFileDirectory = "./Assets/StimPresentationScripts/out/";
        
        [Header("Data Collection")]
        public SimpleDataManager dataManager;
        public UnityLSLConnector lslConnector;
        public string exportPath = "Assets/Data/";
        protected List<double[]> eventInformation = new List<double[]>();

        // Need this when breaking up the update loop
        public enum PresentationState { SETUP, CUE, PLAY, EXPORT, BREAK, WRITING_DATA };
        public enum ExperimentMode { CALIBRATION, LIVE };
        [Header("Presenter State")]
        public PresentationState state = PresentationState.CUE;
        public ExperimentMode mode = ExperimentMode.CALIBRATION;

        

        // Use this for initialization
        void Start()
        {
            // Obtain the array of options
            stimuli = this.transform.GetComponentsInChildren<ERPMenuOption>();
            if (stimuli == null)
            {
                Debug.LogError("No stimuli have been attached to this object");
            }

            stimulusOrder = null;
            experimentOver = false;
            atEndOfSequence = false;
            state = PresentationState.CUE;
        }

        // Update is called once per frame
        void Update()
        {
            if (running)
            {   
                // Update the play timer
                nextStimTimer -= Time.deltaTime;

                switch(state)
                {
                    case PresentationState.CUE:
                        PlayCue();
                        break;
                    case PresentationState.PLAY:
                        PlayStimulus();
                        break;
                    case PresentationState.WRITING_DATA:
                        HandleEndOfBlock(Time.deltaTime, atEndOfSequence);
                        break;
                    case PresentationState.BREAK:
                        HandleBreak(atEndOfSequence);
                        break;
                }
            }
        }

        private void PlayCue()
        {
            if (elapsedCues < timesToCue && nextStimTimer <= 0.0f)
            {
                
                ResetHighlights();
                int currentStimulusIndex = stimulusOrder.sequences[currentSequenceIndex].GetTargetStim(currentBlockIndex);
                stimuli[currentStimulusIndex].VisualHighlight(true);
                PlayStimulus(currentStimulusIndex);
                nextStimTimer = stimOnsetAsync;

                elapsedCues++;
                Debug.Log("Cue Presentation: " + elapsedCues + " of " + timesToCue + " for stim: " + currentStimulusIndex);

                if (elapsedCues == timesToCue)
                {
                    state = PresentationState.PLAY;
                    nextStimTimer = timeBetweenCueAndTrials;
                    elapsedCues = 0;
                }
            }
        }

        // Handles the menu when the participant is taking a break
        private void HandleBreak(bool endOfSequence)
        {
            if (state != PresentationState.BREAK)
            {
                state = PresentationState.BREAK;
                Debug.Log("Breaking");
                nextStimTimer = presentationStartTimeOffset;
                if (endOfSequence)
                {
                    breakMenu.StartBreak(10.0f);
                    participantBreakMenu.StartBreak(10.0f);
                    
                }
                else
                {
                    breakMenu.StartBreak(5.0f);
                    participantBreakMenu.StartBreak(5.0f);

                }
                breakMenu.gameObject.SetActive(true);
                participantBreakMenu.gameObject.SetActive(true);
            }
        }

        // This function basially just places some time at
        // the end of a block to allow for adequate data collection
        // before writing the data out to file
        private void HandleEndOfBlock(float elapsedTime, bool endOfSequence)
        {
            // Decrement the timer
            postBlockTimer -= elapsedTime;

            if (timeBeforeDataWrite - postBlockTimer >= stimOnsetAsync)
            {
                ResetHighlights();
            }

            if (postBlockTimer <= 0)
            {
                // Write the Data
                WriteData();

                if (experimentOver)
                {
                    HandleEndOfExperiment();
                }
                else
                {
                    // Start the break
                    HandleBreak(endOfSequence);
                }
            }
        }

        // Waits a specified amount of time before wr
        private void HandleEndOfExperiment()
        {
            running = false;
            endScreen.SetActive(true);
            vrEndScreen.SetActive(true);
            Debug.Log("Experiment Finished");
        }

        // Plays sounds
        private void PlayStimulus()
        {
            if (currentTrialIndex < stimulusOrder.trials_per_block && nextStimTimer <= 0)
            {
                Debug.Log("Trial: " + currentTrialIndex + " of Block: " + currentBlockIndex + " in Sequence: " + currentSequenceIndex);
                ResetHighlights();
                int currentStimulusIndex = stimulusOrder.sequences[currentSequenceIndex].blocks[currentBlockIndex][currentTrialIndex];
                if (highlightCurrentStimulus)
                {
                    stimuli[currentStimulusIndex].VisualHighlight(true);
                }
                PlayStimulus(currentStimulusIndex);
                // Create a new event for chopping the data
                double[] evt = CreateEvent(liblsl.local_clock(),
                    stimulusOrder.sequences[currentSequenceIndex].targetStimuli[currentBlockIndex] ==
                    stimulusOrder.sequences[currentSequenceIndex].blocks[currentBlockIndex][currentTrialIndex],
                    stimulusOrder.sequences[currentSequenceIndex].blocks[currentBlockIndex][currentTrialIndex]);
                Debug.Log(evt);
                // Set Handle what happens at the end of the block
                if (currentTrialIndex == stimulusOrder.trials_per_block - 1)
                {
                    if (currentBlockIndex == stimulusOrder.blocks_per_sequence - 1)
                    {
                        // Handle what happens at the end of a block
                        if (currentSequenceIndex == stimulusOrder.num_sequences - 1)
                        {
                            // Handle the end of the experiment
                            Debug.Log("Finished Block: " + currentBlockIndex);
                            Debug.Log("Finished Sequence: " + currentSequenceIndex);
                            currentSequenceIndex++;
                            experimentOver = true;
                            atEndOfSequence = true;
                            state = PresentationState.WRITING_DATA;
                            postBlockTimer = timeBetweenCueAndTrials;
                        }
                        else
                        {
                            Debug.Log("Finished Block: " + currentBlockIndex);
                            Debug.Log("Finished Sequence: " + currentSequenceIndex);
                            // Prep for the next sequence
                            currentSequenceIndex++;
                            currentBlockIndex = 0;
                            currentTrialIndex = 0;
                            atEndOfSequence = true;
                            state = PresentationState.WRITING_DATA;
                            postBlockTimer = timeBeforeDataWrite;
                        }
                    }
                    else
                    {
                        Debug.Log("Finished Block: " + currentBlockIndex);
                        // Prep for the next block in the sequence
                        currentBlockIndex++;
                        currentTrialIndex = 0;
                        nextStimTimer = stimOnsetAsync;
                        atEndOfSequence = false;
                        state = PresentationState.WRITING_DATA;
                        postBlockTimer = timeBeforeDataWrite;
                    }
                }
                else
                {
                    nextStimTimer = stimOnsetAsync;
                    currentTrialIndex++;
                }
            }
        }

        private void WriteData()
        {
            dataManager.AddEventCodes(this.eventInformation);
            Debug.Log(" Wrote to: " + dataManager.ExportData(subjectNumber));
            this.eventInformation.Clear();
            dataManager.ClearData();
        }

        // Imports a subject stimulus presentation order file
        public void ImportSubjectFile()
        {
            clearStimulusOrder();

            string filename = "SUBJECT_" + subjectNumber + ".json";
            String importPath = Path.Combine(subjectFileDirectory, filename);
            try
            {
                String jsonString = File.ReadAllText(importPath);
                stimulusOrder = StimulusOrder.CreateFromJSON(jsonString);
                Debug.Log("SUCCESS:: 'Subject_" + subjectNumber + ".json' imported");
                Debug.Log(stimulusOrder.ToString());
            } catch (FileNotFoundException)
            {
                Debug.Log("ERROR:: 'Subject_" + subjectNumber + ".json' not found");
            }
        }

        // Deletes the current stimuus order
        public void clearStimulusOrder()
        {
            this.stimulusOrder = null;
        }

        // Resets all the current indices back to 0;
        public void ResetPresenter()
        {
            currentTrialIndex = 0;
            currentSequenceIndex = 0;
            currentBlockIndex = 0;
        }

        // Starts the timer for presenting stimuli
        public void StartPresentation()
        {
            if (stimulusOrder != null)
            {
                Debug.Log("Starting Stimulus Presentation");
                running = true;
                nextStimTimer = presentationStartTimeOffset;
            } else
            {
                Debug.Log("ERROR:: No Subject File has been imported.");
            }
        }

        // Stops the timer for presenting stimuli
        public void StopPresentation()
        {
            running = false;
        }

        // Given: if an event is a target event and the associated index
        // of the option that emiited the event, Returns: an integer code
        // representing the event
        public int EncodeEvent(bool isTarget, int stimIndex)
        {
            Assert.IsTrue(stimIndex >= 0 && stimIndex < stimuli.Length);
            if (isTarget)
            {
                return (1 << 2) + (stimIndex + 1);
            }
            else
            {
                return stimIndex + 1;
            }            
        }

        // Creates a new event for the stimulus presentation
        protected double[] CreateEvent(double time, bool isTarget, int stimIndex)
        {
            int eventCode = EncodeEvent(isTarget, stimIndex);
            double[] info = { time, eventCode };
            eventInformation.Add(info);
            return info;
        }

        // Turns off all the highlights surrounding stimuli
        public void ResetHighlights()
        {
            for (int i = 0; i < stimuli.Length; i++)
            {
                stimuli[i].VisualHighlight(false);
            }
        }

        // Plays the sound of an ERPMenuOption in stimuli
        public void PlayStimulus(int stimIndex)
        {
            stimuli[stimIndex].Play();
        }

        // Calls the OnSelect() method of an ERPMenuOption in stimuli
        public void SelectOption(int stimIndex)
        {
            if (stimIndex >= 0 && stimIndex < stimuli.Length)
            {
                stimuli[stimIndex].OnSelect();
            }
        }

        // Returns the number of stimuli
        public int GetNumStimuli()
        {
            return this.stimuli.Length;
        }
    }

    class StimulusOrder
    {
        public int num_sequences;
        public int blocks_per_sequence;
        public int num_stimuli;
        public int trials_per_block;
        public float target_trial_percentage;
        public Sequence[] sequences;

        public static StimulusOrder CreateFromJSON(string jsonString)
        {
            StimulusOrder stimOrder = new StimulusOrder();

            // Start parsing the json
            SimpleJSON.JSONNode rootNode = SimpleJSON.JSONNode.Parse(jsonString);

            // Get the root level values
            stimOrder.num_sequences = rootNode["num_sequences"].AsInt;
            stimOrder.blocks_per_sequence = rootNode["blocks_per_sequence"].AsInt;
            stimOrder.num_stimuli = rootNode["num_stimuli"].AsInt;
            stimOrder.trials_per_block = rootNode["trials_per_block"].AsInt;
            stimOrder.target_trial_percentage = rootNode["target_trial_percentage"].AsFloat;

            // Instantiate the sequences array
            stimOrder.sequences = new Sequence[stimOrder.num_sequences];

            SimpleJSON.JSONArray sequenceArray = rootNode["sequences"].AsArray;
            Assert.AreEqual(stimOrder.num_sequences, sequenceArray.Count);

            // Pull out the sequence data
            for (int i = 0; i < stimOrder.num_sequences; i++)
            {
                stimOrder.sequences[i] = new Sequence(stimOrder.blocks_per_sequence, stimOrder.trials_per_block);
                
                
                // Get the first sequence object
                SimpleJSON.JSONNode jsonSeq = sequenceArray[i];

                // Get the target stimuli data
                for (int t = 0; t < stimOrder.blocks_per_sequence; t++)
                {
                    stimOrder.sequences[i].targetStimuli[t] = jsonSeq["targets"].AsArray[t].AsInt;
                }

                // Get the trial data
                SimpleJSON.JSONArray jsonBlocks = jsonSeq["blocks"].AsArray;

                Assert.AreEqual(stimOrder.blocks_per_sequence, jsonBlocks.Count);

                for (int block = 0; block < stimOrder.blocks_per_sequence; block++)
                {
                    Assert.AreEqual(stimOrder.trials_per_block, jsonBlocks[block].AsArray.Count);
                    for (int trial = 0; trial < stimOrder.trials_per_block; trial++)
                    {
                        stimOrder.sequences[i].blocks[block][trial] = jsonBlocks[block].AsArray[trial].AsInt;
                    }
                }

            }

            return stimOrder;
        }

        public override string ToString()
        {
            return String.Format("Sequences: {0}, Blocks: {1}, Stimuli: {2}, Trials: {3}, Percent Target Trials: {4}",
                num_sequences, blocks_per_sequence, num_stimuli, trials_per_block, target_trial_percentage);
        }
    }

    class Sequence
    {
        public int[] targetStimuli;
        public int[][] blocks;

        public Sequence(int numBlocks, int trialsPerBlock)
        {
            targetStimuli = new int[numBlocks];

            blocks = new int[numBlocks][];
            for (int i = 0; i < numBlocks; i++)
            {
                blocks[i] = new int[trialsPerBlock];
            }
        }

        public int GetTargetStim(int blockIndex)
        {
            return targetStimuli[blockIndex];
        }
    }
}
