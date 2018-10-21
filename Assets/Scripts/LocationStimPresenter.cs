using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using LSL;

namespace AudioERP
{
    /*
     * This class serves the same purpose as the SerialStimPresenter
     * however, this component is only used when testing the condiction
     * of having non-identical stimuli presented with their positions being
     * randomly shuffled
     */
    public class LocationStimPresenter : SerialStimPresenter
    {
        /*
         * Tracks the number of times a stimulus at the target position
         * is preceeded by another stimulus at a given position
         */
        private class StimPosPair : StimPair
        {
            public Vector3 targetPos;
            public Vector3 preceedingPos;

            public StimPosPair(int target, int preceeding, Vector3 targetPos, Vector3 preceedingPos, int leftToAdd)
            : base(target, preceeding, leftToAdd)
            {
                this.targetPos = targetPos;
                this.preceedingPos = preceedingPos;
            }

            public override string ToString()
            {
                return preceedingStim + " @ " +  preceedingPos.ToString() + " => " + targetPos.ToString() + " [Presented " + count + " (" + leftToAdd + ")]";
            }
        }

        // Companion List to the presentation order
        private List<Vector3> positionOrder = new List<Vector3>();
        // Array of the stimulus positions
        private Vector3[] stimPositions;

        // Use this for initialization
        void Start()
        {
            // Obtain the array of options
            stimuli = this.transform.GetComponentsInChildren<ERPMenuOption>();
            // Save the original positions of the stimuli
            stimPositions = ExtractStimPositions();
            if (stimuli == null)
            {
                Debug.LogError("No stimuli have been attached to this object");
            }
            // Ensure that the target stimuli index is valid
            if (targetStimulusIndex < 0 || targetStimulusIndex >= stimuli.Length)
            {
                Debug.LogError("Stimulus index falls outside bounds. Defaulting to 0");
                targetStimulusIndex = 0;
            }
            // Calculate to toral number of trials
            totalTrials = Mathf.CeilToInt(nTargetTrials / targetTrialPercentage);
            // Calculate how many trials will need to be non target trials
            nNonTargetTrials = totalTrials - nTargetTrials;
            // How many trials will each non-target stimulus receive
            trialsPerNonTarget = nNonTargetTrials / (stimuli.Length - 1);
            if (allowTargetRepeat)
            {
                desiredStimPairings = Mathf.CeilToInt((float)nTargetTrials / Mathf.Pow(stimuli.Length, 3));
            }
            else
            {
                desiredStimPairings = Mathf.CeilToInt((float)nTargetTrials / (Mathf.Pow(stimuli.Length, 2) * (stimuli.Length - 1)));
            }
            // Get a list to count the number of each stimulus pairing
            stimPairs = (List<StimPair>)GenerateTargetEventPairings(desiredStimPairings);
            // Create the presentation Order
            GeneratePresentationOrder();
        }

        // Update is called once per frame
        void Update()
        {
            if (running)
            {
                timeUntilNextStim -= Time.deltaTime;
                if (elapsedTrialCount < stimPresentationOrder.Count && timeUntilNextStim <= 0)
                {
                    stimuli[stimPresentationOrder[elapsedTrialCount]].transform.position = positionOrder[elapsedTrialCount];
                    stimuli[stimPresentationOrder[elapsedTrialCount]].Play();
                    CreateEvent(liblsl.local_clock(),
                        stimPresentationOrder[elapsedTrialCount] == targetStimulusIndex,
                        stimPresentationOrder[elapsedTrialCount]);
                    elapsedTrialCount++;
                    timeUntilNextStim = stimOnsetAsync;
                }
                if (elapsedTrialCount >= stimPresentationOrder.Count && timeUntilNextStim <= 0)
                {
                    StopPresentation();
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

        protected override void ExportPresentationOrder()
        {
            String currentDate = DateTime.Now.ToString("MM-dd-yy_H-mm-ss");
            string path = exportPath + "presentation_order_" + currentDate + ".txt";
            using (StreamWriter writer = File.CreateText(path))
            {
                for(int i = 0; i < stimPresentationOrder.Count; ++i)
                {
                    writer.WriteLine(stimPresentationOrder[i] + ", " + positionOrder[i].ToString());
                }
            }
        }

        Vector3[] ExtractStimPositions()
        {
            if(stimuli == null)
            {
                return null;
            }
            Vector3[] stimPos = new Vector3[stimuli.Length];
            for (int i = 0; i < stimPos.Length; ++i)
            {
                stimPos[i] = stimuli[i].transform.position;
            }
            return stimPos;
        }


        
        protected override List<StimPair> GenerateTargetEventPairings(int numPresentations)
        {
            List<StimPair> stimPairings = new List<StimPair>();
            for (int i = 0; i < stimPositions.Length; ++i)
            {
                for (int j = 0; j < stimuli.Length; ++j)
                {
                    for (int k = 0; k < stimPositions.Length; ++k)
                    {
                        if (j != targetStimulusIndex)
                        {
                            stimPairings.Add(new StimPosPair(targetStimulusIndex,
                            j, stimPositions[i], stimPositions[k], numPresentations));

                        }
                    }
                }
            }
            return stimPairings;
        }

        /*
         * Generates the the presentation stimulus order, doing its best to
         * balance equal numbers of conditions where a target stimulus at a
         * certain position is presented with a specific preceeding stimulus
         * at a certain position
         */
        protected override void GeneratePresentationOrder()
        {
            // Non-target stimuli stay associated with an index for simplicity
            // and are moved to different positions
            // The postitions are
            stimPresentationOrder.Clear();
            int[] remainingStimCounts = new int[stimuli.Length];
            for (int i = 0; i < remainingStimCounts.Length; ++i)
            {
                if (i == targetStimulusIndex)
                {
                    remainingStimCounts[i] = nTargetTrials;
                }
                else
                {
                    remainingStimCounts[i] = trialsPerNonTarget;
                }
            }


            // Count of the remaining number of non-target stimuli to add
            int remainingNTPresentations = (stimuli.Length - 1) * (trialsPerNonTarget) - nTargetTrials;

            // For each space insert a random order of non-target stimuli
            // followed by a non-target->target pairing
            while (remainingNTPresentations > 0)
            {
                int stimuliToAdd = Mathf.RoundToInt(UnityEngine.Random.value * Math.Min(remainingNTPresentations, maxTargetSeparation - 1));
                if (remainingNTPresentations > 0)
                {
                    for (int i = 0; i < stimuliToAdd; ++i)
                    {
                        // Randomly choose a non-target stim
                        int stimIndex = ChooseAvailableStimulus(remainingStimCounts, true);
                        stimPresentationOrder.Add(stimIndex);
                        remainingStimCounts[stimIndex]--;
                        int posIndex = Mathf.RoundToInt(UnityEngine.Random.value * (stimPositions.Length - 1));
                        positionOrder.Add(stimPositions[posIndex]);
                        remainingNTPresentations--;
                    }
                }
                if (remainingStimCounts[targetStimulusIndex] > 0)
                {
                    // Randomly choose a non-target->target pairing
                    int pairIndex = ChooseAvailableStimPair();
                    // Add the associated stimuli to the order and decrease the remaining count
                    stimPresentationOrder.Add(stimPairs[pairIndex].preceedingStim);
                    stimPresentationOrder.Add(stimPairs[pairIndex].targetStim);
                    positionOrder.Add(((StimPosPair)stimPairs[pairIndex]).preceedingPos);
                    positionOrder.Add(((StimPosPair)stimPairs[pairIndex]).targetPos);
                    stimPairs[pairIndex].leftToAdd--;
                    stimPairs[pairIndex].count++;
                    remainingStimCounts[targetStimulusIndex]--;
                    remainingStimCounts[stimPairs[pairIndex].preceedingStim]--;
                }
            }
        }
    }
}
