using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioERP
{
    // This class is used to play the different sounds after mapping them to
    // the keys 1 - 3
    public class StimuliPrimer : MonoBehaviour
    {

        public StimPresenter stimPresenter;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            string key = Input.inputString;
            int desiredIndex;
            if (int.TryParse(key, out desiredIndex))
            {
                desiredIndex = desiredIndex - 1;
                if (desiredIndex >= 0 && desiredIndex < stimPresenter.GetNumStimuli())
                {
                    SteamVR_Fade.Start(Color.black, 5f);
                    stimPresenter.PlayStimulus(desiredIndex);
                    stimPresenter.SelectOption(desiredIndex);
                }
            }
        }
    }
}
