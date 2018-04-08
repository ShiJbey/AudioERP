using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Accord.MachineLearning.VectorMachines;
namespace AudioERP
{
    public class EventDetector : MonoBehaviour
    {

        //SupportVectorMachine classifier;
        public int featureVectorLength;

        // Use this for initialization
        void Start()
        {
            //this.classifier = new SupportVectorMachine (featureVectorLength);
        }

        // Update is called once per frame
        void Update()
        {

        }

        /*
         * Given a set of epochs select the ERP Menu options that
         * are predicted to have positive ERP events
         */
        //public void detectEvent(double[][] epochs) {
        //	for (int i = 0; i < epochs.Length; ++i) {
        //		double[] featureVec = epochs[i];
        //		bool erpPresent = classifier.Decide(featureVec);
        //		if (erpPresent) {

        //		} else {

        //		}
        //	}
        //}
    }

}
