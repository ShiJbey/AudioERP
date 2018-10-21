using UnityEngine;

public class CountManager : MonoBehaviour {

    
    public int targetCount;
    public int nonTargetcount;

	// Use this for initialization
	void Start () {
        targetCount = 0;
        nonTargetcount = 0;
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyUp("z"))
        {
            targetCount++;  
        }
        else if (Input.GetKeyUp("m"))
        {
            nonTargetcount++;
        }
	}

    public void Reset()
    {
        targetCount = 0;
        nonTargetcount = 0;
    }
}
