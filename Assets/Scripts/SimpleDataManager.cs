using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO;

namespace AudioERP
{
    // Receives data from the UnityLSLConnector and stores it for later
    // processing
    public class SimpleDataManager : MonoBehaviour
    {

        // Maintains concurrency between the gameobject and data fetching thread
        private Mutex dataHistoryMut = new Mutex();
        // Data samples read from the inlet
        private List<double[]> data = new List<double[]>();

        [Header("Epoch Attributes")]
        // Start time offset relative to the event time of the event
        public float startTimeOffset = 0f;
        // Total duration of the epoch
        public float duration = 1f;

        [Header("Export Options")]
        public string exportFilename = "rawdata.csv";

        [HideInInspector]
        public string exportDirectory = "Data";
        [HideInInspector]
        public bool calibrationMode = true;

        // Clears all data samples that have been stored
        public void ClearData()
        {
            this.dataHistoryMut.WaitOne();
            this.data.Clear();
            this.dataHistoryMut.ReleaseMutex();
        }

        // Given the time of a sample, returns the index of the
        // sample with the closest time without going over
        public int GetIndexOfSample(double sampleTime)
        {
            // Default the index to be the last data sample
            int index = data.Count - 1;
            this.dataHistoryMut.WaitOne();
            for (int i = 0; i < data.Count; ++i)
            {
                if (data[i][0] == sampleTime)
                {
                    index = i;
                    break;
                }
                else if (data[i][0] > sampleTime && i > 1)
                {
                    index = i - 1;
                    break;
                }
            }
            this.dataHistoryMut.ReleaseMutex();
            return index;
        }

        // Adds a sample to the data list
        public void PushDataSample(double[] dataSample, double sampleTime)
        {
            // Create the default event info with class = 0 and no option
            double[] eventInfo = { sampleTime, 0 };
            List<double> samplyEntry = new List<double>();
            samplyEntry.AddRange(eventInfo);
            samplyEntry.AddRange(dataSample);
            this.dataHistoryMut.WaitOne();
            this.data.Add(samplyEntry.ToArray());
            this.dataHistoryMut.ReleaseMutex();
        }

        // Gets all of the remaining data samples off the LSL inlet that
        // belong to stored epochs
        public void PullRemainingData(double timeOfLastEvent)
        {
            // Read from inlet until sample time is outside of current  epoch durations
            double sampleTime = 0;
            double stopTime = timeOfLastEvent + (duration - startTimeOffset);
            UnityLSLConnector lslConnector = GetComponent<UnityLSLConnector>();
            while (sampleTime < stopTime)
            {
                sampleTime = lslConnector.PullSample();
            }
        }

        // Returns a string representation of all the data currently stored
        public string PrintAllData()
        {
            string output = "";
            this.dataHistoryMut.WaitOne();
            foreach (double[] sample in data)
            {
                output += "Time: " + sample[0].ToString() + "- ";
                for (int channel = 1; channel < sample.Length; ++channel)
                {
                    if (channel == sample.Length - 1)
                    {
                        // Last channel in the sample
                        output += sample[channel].ToString() + "\n";
                    }
                    else
                    {
                        output += sample[channel].ToString() + ", ";
                    }
                }
            }
            this.dataHistoryMut.ReleaseMutex();
            return output;
        }

        // Returns the list of sample arrays
        public List<double[]> GetAllData()
        {
            return data;
        }

        // Given arrays containing the [time of event, class {0,1}, 
        // hashcode of the event transform]
        public void AddEventCodes(List<double[]> events)
        {
            // Loop through all the event codes
            foreach(double[] erpEvent in events)
            {
                double eventTime = erpEvent[0];
                double eventCode = erpEvent[1];
                // Find the index of the sample that most closely
                // matches that of the current erpEvent
                int closestSampleIndex = GetIndexOfSample(eventTime);
                // Set the event class and option index to match that of
                // the event
                this.dataHistoryMut.WaitOne();
                data[closestSampleIndex][1] = eventCode;
                this.dataHistoryMut.ReleaseMutex();
            }
        }

        // Writes the data out to a .csv file for processing
        public string ExportData(int subjectNumber)
        {
            string exportPath = string.Empty;

            if (Directory.Exists(exportDirectory))
            {
                exportPath = Path.Combine(exportPath, exportDirectory);
            } else
            {
                exportPath = Path.Combine(Path.Combine(exportPath, Application.dataPath), "Data");
            }

            Debug.Log("Path to export to: " + exportPath);

            
            //string currentDate = System.DateTime.Now.ToString("MM-dd-yy_H.mm");
            //exportFilename = "Subject_" + subjectNumber + "_" + currentDate;

            exportFilename = "Subject_" + subjectNumber + "_Data.csv";
            exportPath = Path.Combine(exportPath, exportFilename);

            if (!File.Exists(exportPath) && calibrationMode)
            {
                using (StreamWriter writer = File.CreateText(exportPath))
                {
                    this.dataHistoryMut.WaitOne();
                    foreach (double[] sample in data)
                    {
                        for (int i = 0; i < sample.Length; i++)
                        {
                            if (i == sample.Length - 1)
                            {
                                writer.Write("{0}\n", sample[i]);
                            }
                            else
                            {
                                writer.Write("{0},", sample[i]);
                            }
                        }
                    }
                    this.dataHistoryMut.ReleaseMutex();
                }
            }
            else
            {
                using (StreamWriter writer = File.AppendText(exportPath))
                {
                    this.dataHistoryMut.WaitOne();
                    foreach (double[] sample in data)
                    {
                        for (int i = 0; i < sample.Length; i++)
                        {
                            if (i == sample.Length - 1)
                            {
                                writer.Write("{0}\n", sample[i]);
                            }
                            else
                            {
                                writer.Write("{0},", sample[i]);
                            }
                        }
                    }
                    this.dataHistoryMut.ReleaseMutex();
                }
            }
            return exportPath;
        }
    }
}
