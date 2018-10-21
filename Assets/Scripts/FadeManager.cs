using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeManager : MonoBehaviour {

    public float fadeDuration = 2f;

	// Use this for initialization
	void Start () {
		
	}

    public void FadeToBlack()
    {
        SteamVR_Fade.Start(Color.clear, 0f);
        SteamVR_Fade.Start(Color.black, fadeDuration);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
