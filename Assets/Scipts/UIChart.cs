using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Accord.Math;

namespace AudioERP
{
    // This script manages the custom chart UI element that
    // plots the voltage time series data
    public class UIChart : MonoBehaviour, LSLDataListener
    {

        // Datamanager for feeding live data
        public UnityLSLConnector lslConnection;
        // Data queue (Experimental)
        private float[,] data = null;
        // Reference to the voltageline prefab
        public Transform linePrefab = null;
        // Referene to each one of the lines in the plot
        private List<Transform> lines = new List<Transform>();
        // Indices of channels that we wish to plot
        public int[] desiredChannels = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        // Total length of time in the past to plot
        public float windowSizeSeconds = 1f;
        [Header("Voltage values for vertical axes")]
        // Votltage limits on the plot
        public float boundVoltage = 0.000020f;


        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            UpdatePlot();
        }

        // Pushes a data point on to the array and pushes out he oldest one
        public void PushDataSample(float[] sample, float sampleTime)
        {
            if (this.data == null)
            {
                int nSamples = Mathf.RoundToInt(this.windowSizeSeconds * lslConnection.sampleRate);
                this.data = Accord.Math.Matrix.Zeros<float>(nSamples, this.desiredChannels.Length);
            }
            // Adds a new row of data at the top of the matrix
            data = Accord.Math.Matrix.InsertRow<float, float>(data, sample, 0);
            // Removes the last row from the matrix
            data = Accord.Math.Matrix.RemoveRow(data, data.GetUpperBound(0));
        }

        void CorrectLineCount()
        {
            int numLines = this.lines.Count;
            if (desiredChannels.Length < numLines)
            {
                int difference = lines.Count - desiredChannels.Length;
                //Debug.Log ("More lines than data columns");
                // Remove the necessary number of lines
                for (int i = 0; i < difference; ++i)
                {
                    int lastIndex = this.lines.Count - 1;
                    // Delete the game object from the list and destroy it;
                    Transform line = lines[lastIndex];
                    lines.RemoveAt(lastIndex);
                    // Destroy the gameobject
                    Destroy(line);
                }
            }
            else if (desiredChannels.Length > numLines)
            {
                //Debug.Log ("More data columns than lines");
                int difference = desiredChannels.Length - numLines;
                // Add a number of lines
                for (int i = 0; i < difference; ++i)
                {
                    this.AddLine();
                }
            }
        }

        void CorrectLineSpacing()
        {
            for (int i = 0; i < lines.Count; ++i)
            {
                float vertOffset = GetVerticalOffset() * i;
                float lineOffset = vertOffset + GetLineOffset();
                RectTransform lineTransform = this.lines[i].GetComponent<RectTransform>();
                lineTransform.anchoredPosition3D = new UnityEngine.Vector3(0, lineOffset, 0);
            }
        }

        float GetVerticalOffset()
        {
            int numLines = this.lines.Count;
            RectTransform trans = this.GetComponent<RectTransform>();
            float height = trans.sizeDelta[1];
            return height / numLines;
        }

        float GetLineOffset()
        {
            return this.GetVerticalOffset() / 2;
        }

        void CorrectLineVerts()
        {
            RectTransform trans = this.GetComponent<RectTransform>();
            float width = trans.sizeDelta[0];
            int nSamples = Mathf.RoundToInt(this.windowSizeSeconds * lslConnection.sampleRate);
            float vectSpacing = width / nSamples;
            for (int i = 0; i < lines.Count; ++i)
            {
                LineRenderer line = this.lines[i].GetComponent<LineRenderer>();
                line.positionCount = nSamples;
                for (int j = 0; j < nSamples; ++j)
                {
                    line.SetPosition(j, new UnityEngine.Vector3(j * vectSpacing, 0, 0));
                }
            }
        }

        void UpdateValues()
        {
            for (int i = 0; i < desiredChannels.Length; ++i)
            {
                int channel = desiredChannels[i];
                LineRenderer line = lines[i].GetComponent<LineRenderer>();
                int nSamples = Mathf.RoundToInt(this.windowSizeSeconds * lslConnection.sampleRate);
                for (int j = 0; j < nSamples; ++j)
                {
                    UnityEngine.Vector3 pointPos = line.GetPosition(j);
                    float newYPos = ((float)(data[j, channel] / this.boundVoltage) * GetLineOffset());
                    line.SetPosition(j, new UnityEngine.Vector3(pointPos.x, newYPos, pointPos.z));
                }
            }
        }

        void UpdatePlot()
        {
            if (data != null)
            {
                // Check to see if we have the correct number of lines on the plot
                CorrectLineCount();
                // Fix the vertical spacing of the plot lines
                CorrectLineSpacing();
                // Correct the number of vertices in each line
                CorrectLineVerts();
                // Update the lines to reflect voltage values
                UpdateValues();
            }
        }


        public void SetData(float[,] data)
        {
            this.data = data;
            UpdatePlot();
        }

        public float[,] GetData()
        {
            return this.data;
        }

        public void AddLine()
        {
            this.lines.Add(Instantiate(linePrefab, UnityEngine.Vector3.zero, Quaternion.identity, this.GetComponent<RectTransform>()));
        }
    }
}
