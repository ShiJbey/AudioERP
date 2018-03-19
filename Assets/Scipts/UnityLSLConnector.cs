using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using LSL;

public class UnityLSLConnector: MonoBehaviour {

	// Reference to the indicator UI object
	public LslUIMonitor hudIndicator;
	// Thread that gets data from the LSL inlet
	private Thread dataFetchThread;
	// Inlet to receive data from LSL
	private liblsl.StreamInlet inlet;
	// Hold samples read from inlet
	private float[] sample;
	// Checked by the data thread loop
	private bool killDataThread = false;
	// Number of channels associated with LSL stream
	private int numChannels;
	// Internal state of the connection
	public enum ConnectionState { NOT_CONNECTED, COLLECTING, PROCESSING };
	public ConnectionState state = ConnectionState.NOT_CONNECTED;
	// Reference to data manager object
	public DataManager dataManager;
	public UIChart voltagePlot;

	// Params for finding the desired LSL stream
	[Header("LSL Connection")]
	public string streamType = "EEG";
	public string streamName = "BioSemi";

	void Start () {
		//Debug.Log (Network.connectionTesterIP);
		// Create the thread

	}

	public void ConnectToLSL() {
		if (dataFetchThread == null) {
			dataFetchThread = new Thread (new ThreadStart (FecthEEGData));
		}
		if (!dataFetchThread.IsAlive) {
			liblsl.StreamInfo[] results = liblsl.resolve_stream ("type", streamType, 1, 1);
			if (results.Length < 1) {
				Debug.Log ("No Stream Found");
				return;
			} else {
				Debug.Log ("Stream Found");
			}
			// Loop through all the found streams and find the one with the desired name
			liblsl.StreamInlet temp;
			liblsl.StreamInfo tempInfo;
			for (int i = 0; i < results.Length; ++i) {
				temp = new liblsl.StreamInlet (results [i]);
				try {
					tempInfo = temp.info ();
					Debug.Log (tempInfo.name ());
					Debug.Log (this.streamName);
					if (tempInfo.name ().Equals (this.streamName)) {
						Debug.Log ("Inlet Conncected");
						numChannels = tempInfo.channel_count ();
						inlet = temp;
						sample = new float[numChannels];
						if (InletConnected ()) {
							if (hudIndicator != null) {
								hudIndicator.OnConnectionGained ();
							}
							if (!dataFetchThread.IsAlive) {
								StartFetchThread ();
							}

							Debug.Log ("Inlet Conncected");
						}
					}
				} catch (TimeoutException) {
					// Do nothing
				} catch (liblsl.LostException) {
					// Do nothing
				}
			}
		}
	}



	/// <summary>
	/// Starts the Ddata fetching thread
	/// </summary>
	public void StartFetchThread() {
		this.state = ConnectionState.COLLECTING;
		dataFetchThread.Name = "DataFetchThread";
		dataFetchThread.Start ();		
	}

	/// <summary>
	/// </summary>
	/// <returns><c>true</c>, if LSL inlet is connected, <c>false</c> otherwise.</returns>
	public bool InletConnected() {
		return this.inlet != null;
	}

	/// <summary>
	/// Function rand by the dataFetch Thread
	/// The function retrieves data from the inlet and pushes
	/// it onto the array list of data samples being managed
	/// by the data manager.
	/// 
	/// This function stops when this object is destroyed
	/// </summary>
	public void FecthEEGData() {
		while (!killDataThread) {
			try {
				// Get data from inlet
				double sampleTime = inlet.pull_sample (sample, 1);
				sampleTime += inlet.time_correction();
				//Debug.Log("Sample Read");
				// Push data to the data manager
				if (this.state == ConnectionState.COLLECTING) {
					dataManager.PushData(sample, (float)sampleTime);
				}
				voltagePlot.PushDataSample(sample);
			}
			catch(TimeoutException) {
				Debug.Log ("No Sample Available");
				killDataThread = true;
				if (hudIndicator != null) {
					hudIndicator.OnConnectionLost ();
				}
				inlet.close_stream ();
				inlet = null;
			}
			catch (liblsl.LostException) {
				Debug.Log ("LSL Connection Lost");
				if (hudIndicator != null) {
					hudIndicator.OnConnectionLost ();
				}
				this.state = ConnectionState.NOT_CONNECTED;
				killDataThread = true;
				inlet.close_stream ();
				inlet = null;
			}
		}
	}


	/// <summary>
	/// Raises the destroy event.
	/// Kills the data fetching thread and closes the LSL inlet
	/// </summary>
	void OnDestroy() {
		// Stop the data thread
		killDataThread = true;
		// Close the stream
		if (InletConnected ()) {
			this.inlet.close_stream ();
		}
	}


	void Update() {

	}


}
