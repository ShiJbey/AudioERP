using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AudioERP
{
    public class LslUIMonitor : MonoBehaviour
    {

        private Image textBackground;
        public UnityLSLConnector lslConnection;

        // Use this for initialization
        void Start()
        {
            textBackground = transform.GetChild(0).GetComponent<Image>();
        }

        public void OnConnectionLost()
        {
            Color noColor = new Color();
            ColorUtility.TryParseHtmlString("#FF000064", out noColor);
            this.GetComponent<Text>().text = "LSL Stream: Not Connected";
            textBackground.color = noColor;
        }

        public void OnConnectionGained()
        {
            Color goColor = new Color();
            ColorUtility.TryParseHtmlString("#00FF6B64", out goColor);
            this.GetComponent<Text>().text = "LSL Stream: Connected";
            textBackground.color = goColor;
        }

        // Update is called once per frame
        void Update()
        {
            if (lslConnection.InletConnected())
            {
                OnConnectionGained();
            }
            else
            {
                OnConnectionLost();
            }
        }
    }

}
