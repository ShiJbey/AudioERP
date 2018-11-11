using System.Collections;
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
        /* Selectable objects */
        protected ERPMenuOption[] stimuli;

        [Header("Stimulus Presentation")]
        private StimulusOrder stimulusOrder = null;
        // Time between stimulus presentations
        public float stimOnsetAsync = 0.375f;
        // Time offset (seconds) for when to start presenting stimuli after starting presentation
        protected float presentationStartTimeOffset = 5f;
        // Timer until the next stimulus presentation
        protected float timeUntilNextStim;
        // Index of the stimulus that is the target
        private int currentTrialIndex = 0;
        private int currentSequenceIndex = 0;
        private int currentBlockIndex = 0;
        public BreakMenu breakMenu;
        [HideInInspector]
        // Manages whether or not this is currently running
        public bool running = false;
        

        [Header("Subject Information")]
        public int subjectNumber = -1;
        private string subjectFileDirectory = "./Assets/StimPresentationScripts/out/";
        

        [Header("Data Collection")]
        public SimpleDataManager dataManager;
        public UnityLSLConnector lslConnector;
        public string exportPath = "Assets/Data/";
        protected List<double[]> eventInformation = new List<double[]>();

        // Need this when breaking up the update loop
        public enum PresentationState { CUE, PLAY, EXPORT, BREAK };
        public enum ExperimentMode { CALIBRATION, LIVE };
        public PresentationState state = PresentationState.CUE;
        public ExperimentMode mode = ExperimentMode.CALIBRATION;
        private int timesToCue = 3;
        private int elapsedCues = 0;

        public GameObject endScreen;

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
            timesToCue = 3;
            state = PresentationState.CUE;
        }

        // Update is called once per frame
        void Update()
        {
            if (running)
            {   
                // Update the play timer
                timeUntilNextStim -= Time.deltaTime;


                switch(state)
                {
                    case PresentationState.CUE:
                        PlayCue();
                        break;
                    case PresentationState.PLAY:
                        // Play the next trial
                        PlayStimulus();
                        break;
                    case PresentationState.BREAK:
                        HandleBreak();
                        break;
                }
                
                


                // Ends the presenter when we reach the end of a block, sequence, or
                // the experiment
                if (currentBlockIndex >= stimulusOrder.blocks_per_sequence
                    || currentSequenceIndex >= stimulusOrder.num_sequences
                    || currentTrialIndex >= stimulusOrder.trials_per_block)
                {
                    Debug.Log("Experiment Finished");
                    running = false;
                    ResetHighlights();
                }
                
            }
        }

        private void PlayCue()
        {
            if (elapsedCues < timesToCue && timeUntilNextStim <= 0.0f)
            {
                
                ResetHighlights();
                int currentStimulusIndex = stimulusOrder.sequences[currentSequenceIndex].GetTargetStim(currentBlockIndex);
                stimuli[currentStimulusIndex].VisualHighlight(true);
                PlayStimulus(currentStimulusIndex);
                timeUntilNextStim = stimOnsetAsync;

                Debug.Log("Cue Presentation: " + (elapsedCues + 1) + " of " + timesToCue + " for stim: " + currentStimulusIndex);

                elapsedCues++;

                if (elapsedCues == timesToCue)
                {
                    state = PresentationState.PLAY;
                    elapsedCues = 0;
                    timeUntilNextStim = 3.0f;
                }
            }
        }

        // Is called when the state changes to PresentationState.BREAK
        private void OnParticipantBreak(bool endOfSequence)
        {
            state = PresentationState.BREAK;
            Debug.Log("Breaking");
            if (endOfSequence)
            {
                breakMenu.StartBreak(10.0f);
            }
            else
            {
                breakMenu.StartBreak(5.0f);
            }
            breakMenu.gameObject.SetActive(true);
        }

        // Handles the menu when the participant is taking a break
        private void HandleBreak()
        {
            
        }

        // Plays sounds
        private void PlayStimulus()
        {
            if (currentTrialIndex < stimulusOrder.trials_per_block && timeUntilNextStim <= 0)
            {
                Debug.Log("Trial: " + currentTrialIndex + " of Block: " + currentBlockIndex + " in Sequence: " + currentSequenceIndex);
                ResetHighlights();
                int currentStimulusIndex = stimulusOrder.sequences[currentSequenceIndex].blocks[currentBlockIndex][currentTrialIndex];
                stimuli[currentStimulusIndex].VisualHighlight(true);
                PlayStimulus(currentStimulusIndex);
                // Create a new event for chopping the data
                CreateEvent(liblsl.local_clock(),
                    stimulusOrder.sequences[currentSequenceIndex].targetStimuli[currentBlockIndex] == currentStimulusIndex,
                    currentStimulusIndex);

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
                            Debug.Log("Experiment should end");
                            running = false;
                            endScreen.SetActive(true);
                        }
                        else
                        {
                            Debug.Log("Finished Block: " + currentBlockIndex);
                            Debug.Log("Finished Sequence: " + currentSequenceIndex);
                            // Prep for the next sequence
                            currentSequenceIndex++;
                            currentBlockIndex = 0;
                            currentTrialIndex = 0;
                            timeUntilNextStim = stimOnsetAsync;
                            OnParticipantBreak(true);
                            dataManager.AddEventCodes(this.eventInformation);
                            Debug.Log(" Wrote to: " + dataManager.ExportData(subjectNumber));
                            this.eventInformation.Clear();
                            dataManager.ClearData();
                        }
                    }
                    else
                    {
                        Debug.Log("Finished Block: " + currentBlockIndex);
                        // Prep for the next block in the sequence
                        currentBlockIndex++;
                        currentTrialIndex = 0;
                        timeUntilNextStim = stimOnsetAsync;
                        OnParticipantBreak(false);
                        dataManager.AddEventCodes(this.eventInformation);
                        Debug.Log(" Wrote to: " + dataManager.ExportData(subjectNumber));
                        this.eventInformation.Clear();
                        dataManager.ClearData();
                    }
                }
                else
                {
                    timeUntilNextStim = stimOnsetAsync;
                    currentTrialIndex++;
                }
            }
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
                timeUntilNextStim = presentationStartTimeOffset;
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
        public static int EncodeEvent(bool isTarget, int optionIndex)
        {
            int optionNumber = optionIndex + 1;
            int classification = 0;
            if (isTarget)
                classification = 1;
            return (classification << 2) + optionNumber;
        }

        // Creates a new event for the stimulus presentation
        protected void CreateEvent(double time, bool isTarget, int optionIndex)
        {
            int eventCode = EncodeEvent(isTarget, optionIndex);
            double[] info = { time, eventCode };
            eventInformation.Add(info);
        }

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
