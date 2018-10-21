using UnityEngine;
using UnityEngine.UI;

namespace AudioERP
{
    public class ExperimentButton : MonoBehaviour
    {

        public SerialStimPresenter stimPresenter;
        public UnityLSLConnector lslConnection;

        private void Update()
        {
            if (lslConnection.InletConnected())
            {
                this.transform.GetComponent<Button>().interactable = true;
            }
            else
            {
                this.transform.GetComponent<Button>().interactable = false;
            }
            Text buttonText = this.transform.GetComponentInChildren<Text>();
            if (!stimPresenter.running)
            {
                buttonText.text = "Start\nExp";
            }
            else
            {
                buttonText.text = "Stop\nExp";
            }
        }

        public void StartStopExperiment()
        {
            Text buttonText = this.transform.GetComponentInChildren<Text>();
            if (stimPresenter.running)
            {
                stimPresenter.StopPresentation();
                buttonText.text = "Start\nExp";
            }
            else
            {
                buttonText.text = "Stop\nExp";
                stimPresenter.ResetPresenter();
                stimPresenter.StartPresentation();
                lslConnection.state = UnityLSLConnector.ConnectionState.COLLECTING;
            }
        }

        public void changeToStop()
        {
            Text buttonText = this.transform.GetComponentInChildren<Text>();
            buttonText.text = "Stop\nExp";
            stimPresenter.StartPresentation();
        }

        public void changeToStart()
        {
            Text buttonText = this.transform.GetComponentInChildren<Text>();
            buttonText.text = "Start\nExp";
            stimPresenter.StopPresentation();
        }
    }
}


