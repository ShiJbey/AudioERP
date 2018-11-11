using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Brings up the break menu and ask that the participant press
// a button to continue
public class BreakMenu : MonoBehaviour {

    private float breakDuration = 0.0f;
    private Text timeLeftText;
    public Button resumeButton;
    public AudioERP.StimPresenter stimPresenter;

	// Use this for initialization
	void Start () {
        timeLeftText = this.transform.Find("TimeLeft").gameObject.GetComponent<Text>();
        timeLeftText.text = this.breakDuration.ToString();
        resumeButton = this.transform.Find("Resume").GetComponent<Button>();
        resumeButton.interactable = false;
    }
	
	// Update is called once per frame
	void Update () {
        breakDuration -= Time.deltaTime;

        if (breakDuration <= 0 && !resumeButton.IsInteractable())
        {
            timeLeftText.text = "0:00";
            resumeButton.interactable = true;
        } else if (breakDuration > 0)
        {
            int minutes = Mathf.CeilToInt(breakDuration) / 60;
            int seconds = Mathf.CeilToInt(breakDuration) % 60;
            string secondString = (seconds < 10) ? "0" + seconds.ToString() : seconds.ToString();
            timeLeftText.text = minutes.ToString() + ":" + secondString;
        }
	}

    public void ResumeExperiment()
    {
        Debug.Log("Resume Button Clicked");
        stimPresenter.state = AudioERP.StimPresenter.PresentationState.CUE;
        resumeButton.interactable = false;
    }

    public void StartBreak(float duration)
    {
        resumeButton.interactable = false;
        this.breakDuration = duration;
    }
}
