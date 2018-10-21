using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.Runtime.Serialization.Formatters;
using System;
using System.IO;

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
        // Index of the stimulus that is the target
        private int currentTrialIndex = 0;
        private int currentSequenceIndex = 0;
        private int currentBlockIndex = 0;

        [Header("Subject Information")]
        public int subjectNumber = -1;
        private string subjectFileDirectory = "./Assets/StimPresentationScripts/out/";
        

        [Header("Data Collection")]
        public SimpleDataManager dataManager;
        public UnityLSLConnector lslConnector;
        public string exportPath = "Assets/Data/";
        protected List<double[]> eventInformation = new List<double[]>();

        [HideInInspector]
        // Manages whether or not this is currently running
        public bool running = false;
        // Timer until the next stimulus presentation
        protected float timeUntilNextStim;
        // Number of elapsed trails in this current sequence
        protected int elapsedTrialCount = 0;


        // Use this for initialization
        void Start()
        {
            // Obtain the array of options
            stimuli = this.transform.GetComponentsInChildren<ERPMenuOption>();
            if (stimuli == null)
            {
                Debug.LogError("No stimuli have been attached to this object");
            }

            ImportSubjectFile();
        }

        // Update is called once per frame
        void Update()
        {
            if (running)
            {

            }
        }

        // Imports a subject stimulus presentation order file
        public void ImportSubjectFile()
        {
            string filename = "SUBJECT_" + subjectNumber + ".json";
            String importPath = Path.Combine(subjectFileDirectory, filename);
            String jsonString = File.ReadAllText(importPath);

            SimpleJSON.JSONNode n = SimpleJSON.JSONNode.Parse(jsonString);
            SimpleJSON.JSONArray seq = n["sequences"].AsArray;
            SimpleJSON.JSONObject pizza = seq[0].AsObject;
            Debug.Log(pizza["blocks"][0][0]);
            int ns = n["num_sequences"].AsInt;
            Debug.Log("Num Seq:" + ns);
            Debug.Log("Actual num:" + seq.Count);
            Debug.Log("Pizza");
            //stimulusOrder = StimulusOrder.CreateFromJSON(jsonString);

            // Check that stimulusOrder was created
            Assert.IsNotNull(stimulusOrder);
            // Check that this file is for the correct number of stimuli
            Assert.AreEqual(stimulusOrder.num_stimuli, stimuli.Length);
            // Check that the number of sequences is correct
            Assert.AreEqual(stimulusOrder.num_sequences, stimulusOrder.sequences.Length);
            // Check that the number of block and trials are also correct
            for (int s = 0; s < stimulusOrder.num_sequences; s++)
            {
                Assert.AreEqual(stimulusOrder.blocks_per_sequence, stimulusOrder.sequences[s].blocks.Length);
                for (int b = 0; b < stimulusOrder.blocks_per_sequence; b++)
                {
                    Assert.AreEqual(stimulusOrder.trials_per_block, stimulusOrder.sequences[s].blocks[b].Length);
                }
            }

            Debug.Log(stimulusOrder.ToString());
        }

        // Resets all the current indices back to 0;
        public void ResetPresenter()
        {
            elapsedTrialCount = 0;
            currentTrialIndex = 0;
            currentSequenceIndex = 0;
            currentBlockIndex = 0;
        }

        // Starts the timer for presenting stimuli
        public void StartPresentation()
        {
            running = true;
            timeUntilNextStim = presentationStartTimeOffset;
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
    }

    [System.Serializable]
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

            // Pull out the sequence data
            for (int i = 0; i < stimOrder.num_sequences; i++)
            {
                stimOrder.sequences[i] = new Sequence();
            }

            return stimOrder;
        }

        public override string ToString()
        {
            return String.Format("Sequences: {0}\nBlocks: {1}\nStimuli: {2}\nTrials: {3}\nPercent Target Trials: {4}\n",
                num_sequences, blocks_per_sequence, num_stimuli, trials_per_block, target_trial_percentage);
        }
    }

    class Sequence
    {
        public int[][] blocks;
    }
}
