using UnityEngine;
using UnityEngine.UI;

namespace AudioERP
{
    public class OptionsMenu : MonoBehaviour
    {
        public UnityLSLConnector lslConnection;
        public SerialStimPresenter stimPresenter;
        public SimpleDataManager dataManager;

        public void ChangeExportPath(InputField inputField)
        {
            string fieldText = inputField.textComponent.text;
            dataManager.exportDirectory = fieldText;
            stimPresenter.exportPath = fieldText;
        }

        public void ChangeStreamType(InputField inputField)
        {
            lslConnection.streamType = inputField.text;
        }

        public void ChangeStreamName(InputField inputField)
        {
            lslConnection.streamName = inputField.text;
        }

        public void ChangeTargetStim(InputField inputField)
        {
            string fieldText = inputField.text;
            int desiredIndex;
            if (int.TryParse(fieldText,out desiredIndex))
            {
                if (desiredIndex >= 0 && desiredIndex < stimPresenter.GetNumStimuli())
                {
                    stimPresenter.targetStimulusIndex = int.Parse(inputField.text);
                }
                else
                {
                    inputField.text = "invalid index";
                }
                
            }
            else
            {
                inputField.text = "Must be integer";
            }
            

        }
    }
}

