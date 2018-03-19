using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Accord.Math;
using LSL;

public class ERPMenu : MonoBehaviour {
	
	public int numTrials = 3;
	public int selectionAttempts = -1;
	public int numTrainingSequences = 36;
	public bool trainingMode = true;
	public ERPMenuOption[] options;
	public float interStimInterval = .1f;
	public float interTrialInterval = 1f;
	private float timeUntilNextPlay;
	private int currentOption;
	private int trialCount;
	public DataManager dataManager;
	public UnityLSLConnector lslConnector;
    public enum ExecutionMode { TRAINING, LIVE, NODATA };
    public ExecutionMode mode = ExecutionMode.TRAINING;
    private int trainTargetIndex;

	// Use this for initialization
	void Start () {
		// Sets the timer to play the next option immediately
		timeUntilNextPlay = interTrialInterval;
		trialCount = 0;
        if (mode == ExecutionMode.TRAINING)
        {
            trainTargetIndex = Mathf.RoundToInt(Random.Range(0, options.Length - 1));
            options[trainTargetIndex].VisualHighlight(true);
        }
	}

    
	
	// Update is called once per frame
	void Update () {
		if (lslConnector.InletConnected ()) {
			timeUntilNextPlay -= Time.deltaTime;
			if (timeUntilNextPlay <= 0 && trialCount <= numTrials) {
				// Play the current option
				options [currentOption].OnHighlight ();
				// Create Event for this play
				CreateEvent ((float)liblsl.local_clock (), options [currentOption]);
				// Increment the current option index
				++currentOption;
				if (currentOption == options.Length) {
					// We have reached the end of the options
					currentOption = 0;
					Debug.Log ("Ending trial: " + trialCount);
					++trialCount;
					timeUntilNextPlay = interTrialInterval;
				}
				// Reset the timer for when to play the next sound
				timeUntilNextPlay = interStimInterval;
			}
			else if (trialCount == numTrials) {
				lslConnector.state = UnityLSLConnector.ConnectionState.PROCESSING;
				Debug.Log ("Processing Data");
				Debug.Log (dataManager.GetAllData().Count);
				dataManager.FillEpochs ();
				Debug.Log ("Samples per epoch: " + dataManager.GetAllEpochs ()[0].GetData().Rows());
                dataManager.WriteOutTrainingData("train.csv", "labels.csv");
                if (mode == ExecutionMode.TRAINING)
                {
                    trainTargetIndex = Mathf.RoundToInt(Random.Range(0, options.Length - 1));
                }
                ++trialCount;
			}
		}
	}

	// Resets the menu so that no options are selected
	void Reset() {
		foreach(ERPMenuOption option in options){
			option.Deselect ();
		}
	}

	// Markes a specific object as selected
	void Select(ERPMenuOption option) {
		option.Select ();
	}

	private void CreateEvent(float time, ERPMenuOption obj) {
        if (mode == ExecutionMode.TRAINING && obj.Equals(options[trainTargetIndex]))
        {
            dataManager.AddEpoch(new Epoch(obj.gameObject, time, true));
        }
		dataManager.AddEpoch(new Epoch (obj.gameObject, time, false));
	}
}
