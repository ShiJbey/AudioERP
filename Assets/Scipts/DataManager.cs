using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Accord.Math;
using System.Threading;
using Accord.IO;

public class DataManager : MonoBehaviour {

	// Maintains concurrency between the gameobject and data fetching thread
	private Mutex dataHistoryMut = new Mutex();
	// Epoch for splitting the data history for classification
	private List<Epoch> epochs = new List<Epoch>();
	// Data samples read from the inlet
	private List<float[]> data = new List<float[]>();

	[Header("Data Format")]
	public int[] desiredChannels;
	public int sampleRateHz = 250;

	[Header("Epoch Attributes")]
	public float startTimeOffset = 0f;
	public float duration = 1f;

	[Header("Raw Data Filtering")]
	public bool filterData = true;
	public float lowpassCutOff = 60f;
	public float highpassCutOff = .1f;


	// Use this for initialization;
	void Start () {
		
	}

	public void processEpochs () {

	}

	public List<Epoch> GetAllEpochs () {
		return this.epochs;
	}

	public void AddEpoch(Epoch epoch) {
		this.epochs.Add (epoch);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	// Iterate through the epochs and give each one the 
	// segment of the data history that corresponds to its
	// timeframe
	public void FillEpochs() {
		foreach (Epoch epoch in epochs) {
			int startSampleIndex = this.GetIndexOfSample (epoch.GetEventTime () + Epoch.startTimeOffset);
			int endSampleIndex =  this.GetIndexOfSample (epoch.GetEventTime() + Epoch.duration);
			int range = endSampleIndex - startSampleIndex;
			this.dataHistoryMut.WaitOne ();
			float[,] dataSubset = Accord.Math.Matrix.Create (this.data.GetRange (startSampleIndex, range).ToArray ());
			dataSubset = dataSubset.RemoveColumn (0);
			epoch.SetData (dataSubset);
			this.dataHistoryMut.ReleaseMutex ();
		}
	}

	// Given the time of a sample, returns the index of the
	// sample with the closest time without going over
	public int GetIndexOfSample(float sampleTime) {
		this.dataHistoryMut.WaitOne ();
		for (int i = 0; i < data.Count; ++i) {
			if (data [i] [0] == sampleTime) {
				this.dataHistoryMut.ReleaseMutex ();
				return i;
			} else if (data [i] [0] > sampleTime && i > 1) {
				this.dataHistoryMut.ReleaseMutex ();
				return i - 1;
			}
		}
		this.dataHistoryMut.ReleaseMutex ();
		return data.Count - 1;
	}

	// Adds a sample to the data list
	public void PushData (float[] dataSample, float sampleTime) {
		this.dataHistoryMut.WaitOne ();
		this.data.Add (Accord.Math.Matrix.Concatenate<float>(sampleTime, dataSample));
		this.dataHistoryMut.ReleaseMutex ();
	}

	public void PrintAllData() {
		this.dataHistoryMut.WaitOne ();
		foreach (float[] sample in data) {
			string output = "";
			output += "Time: " + sample[0].ToString() + "- ";
			for (int i = 1; i < sample.Length; ++i) {
				output += sample[i].ToString() + ", ";
			}
			output += "\n";
			Debug.Log (output);
		} 
		this.dataHistoryMut.ReleaseMutex ();
	}

	public List<float[]> GetAllData() {
		return data;
	}

	public void WriteOutTrainingData(string trainExampleFilename, string trainLabelFilename) {
        WriteOutExampleData(trainExampleFilename);
        WriteoutTrainLabels(trainLabelFilename);
	}

	public void WriteOutExampleData(string trainExampleFilename) {
		string path = "Assets/Data/" + trainExampleFilename;

        System.IO.StreamWriter writer = new System.IO.StreamWriter(path);
		foreach (Epoch epoch in epochs) {
   			float[] example = epoch.GetFeatureVector ();
            for (int i = 0; i < example.Length; ++i)
            {
                if (i  == example.Length - 1)
                {
                    writer.Write("{0}", example[i]);
                }
                else
                {
                    writer.Write("{0},", example[i]);
                }
            }
            writer.Write("\n");
    	}
        writer.Close();
        Debug.Log("Wrote out examples");
	}

    public void WriteoutTrainLabels(string trainLabelFilename)
    {
        string path = "Assets/Data/" + trainLabelFilename;
        System.IO.StreamWriter writer = new System.IO.StreamWriter(path);
        for (int i = 0; i < epochs.Count; ++i)
        {
            if (i == epochs.Count - 1)
            {
                writer.Write("{0}", epochs[i].GetLabel());
            }
            else
            {
                writer.Write("{0},", epochs[i].GetLabel());
            }
        }
        writer.Close();
    }
}
