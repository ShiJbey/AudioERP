using System;
using UnityEngine;
using System.Threading;
using LSL;
using System.IO;

namespace AudioERP
{
    public class UnityLSLConnector : MonoBehaviour
    {

        // Thread that gets data from the LSL inlet
        private Thread dataFetchThread;
        // Checked by the data thread loop
        private bool killDataThread = false;
        // Inlet to receive data from LSL
        private liblsl.StreamInlet inlet;
        // Number of channels associated with LSL stream
        public int numChannels { get; private set; }
        // Nominal sample rate of the stream
        public double sampleRate { get; private set; }
        // Hold samples read from inlet
        private double[] sample;


        // Reference to data manager object
        [Header("Objects that Receive LSL Data")]
        public SimpleDataManager dataManager;
        public LSLDataListener[] dataListeners;

        // Params for finding the desired LSL stream
        [Header("LSL Connection")]
        public string streamType = "EEG";
        public string streamName = "BioSemi";
        // Internal state of the connection
        public enum ConnectionState { NOT_CONNECTED, CONNECTED, COLLECTING, PROCESSING };
        public ConnectionState state = ConnectionState.NOT_CONNECTED;

        void Start()
        {
            dataFetchThread = new Thread(new ThreadStart(FecthEEGData));
            dataFetchThread.Name = "DataFetchThread";
        }

        void Update()
        {
            // Do Nothing
        }

        public void ConnectToLSL()
        {
            if (dataFetchThread.IsAlive)
                return;

            if (dataFetchThread == null)
            {
                dataFetchThread = new Thread(new ThreadStart(FecthEEGData));
                dataFetchThread.Name = "DataFetchThread";
            }

            if (!dataFetchThread.IsAlive)
            {
                liblsl.StreamInfo[] results = liblsl.resolve_stream("type", streamType, 1, 1);
                if (results.Length < 1)
                {
                    Debug.Log("No Stream Found");
                    return;
                }
                else
                {
                    Debug.Log("Stream Found");
                }
                // Loop through all the found streams and find the one with the desired name
                liblsl.StreamInlet temp;
                liblsl.StreamInfo tempInfo;
                for (int i = 0; i < results.Length; ++i)
                {
                    temp = new liblsl.StreamInlet(results[i]);
                    try
                    {
                        tempInfo = temp.info();
                        if (tempInfo.name().Equals(this.streamName))
                        {
                            this.sampleRate = tempInfo.nominal_srate();
                            numChannels = tempInfo.channel_count();
                            inlet = temp;
                            ExportInletMetadata();
                            sample = new double[numChannels];
                            if (InletConnected())
                            {
                                state = ConnectionState.CONNECTED;
                                if (!dataFetchThread.IsAlive)
                                {
                                    StartFetchThread();
                                }
                                Debug.Log("Inlet Connected");
                                ExportInletMetadata();
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        // Do nothing
                    }
                    catch (liblsl.LostException)
                    {
                        // Do nothing
                    }
                }
            }
        }

        // Write out the data about the inlet to an xml file
        void ExportInletMetadata()
        {
            string inletInfo = inlet.info().as_xml();
            string path = "Assets/Data/inlet_metadata.xml";
            using (StreamWriter writer = File.AppendText(path))
            {
                writer.Write(inletInfo);
            }
        }

        // Starts the data fetching thread
        public void StartFetchThread()
        {
            dataFetchThread.Start();
        }

        // returns true, if LSL inlet is connected, false otherwise.
        public bool InletConnected()
        {
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
        public void FecthEEGData()
        {
            while (!killDataThread && this.state != ConnectionState.NOT_CONNECTED)
            {
                try
                {
                    // Get data from inlet
                    double sampleTime = inlet.pull_sample(sample, 1);
                    sampleTime += inlet.time_correction();

                    if (this.state == ConnectionState.COLLECTING)
                    {
                        // Push data to the data manager
                        if (this.dataManager != null)
                            dataManager.PushDataSample(sample, sampleTime);
                    }
                }
                catch (TimeoutException)
                {
                    Debug.Log("No Sample Available");
                    this.state = ConnectionState.NOT_CONNECTED;
                    killDataThread = true;
                    inlet.close_stream();
                    inlet = null;
                    this.dataFetchThread = null;
                }
                catch (liblsl.LostException)
                {
                    Debug.Log("LSL Connection Lost");
                    this.state = ConnectionState.NOT_CONNECTED;
                    killDataThread = true;
                    inlet.close_stream();
                    inlet = null;
                    this.dataFetchThread = null;
                }
            }
        }

        // Pulls a single sample from the LSL inlet
        public double PullSample()
        {
            try
            {
                // Get data from inlet
                double sampleTime = inlet.pull_sample(sample, 1);
                sampleTime += inlet.time_correction();
                if (this.dataManager != null)
                    dataManager.PushDataSample(sample, (float)sampleTime);
                return sampleTime;
            }
            catch (Exception)
            {

            }
            return Double.PositiveInfinity;
        }

        // Release inlet resources
        void OnDestroy()
        {
            // Stop the data thread
            killDataThread = true;
            // Close the stream
            if (InletConnected())
            {
                this.inlet.close_stream();
            }
        }
    }
}