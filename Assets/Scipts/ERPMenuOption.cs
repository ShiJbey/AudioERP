using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ERPMenuOption : MonoBehaviour {
	

	AudioSource audioClip;
	MeshRenderer mesh;
	public bool selected;
	public Material selectedMaterial;
	public Material notSelectedMaterial;


	// Use this for initialization
	void Start () {
		this.audioClip = GetComponent<AudioSource> ();
		this.mesh = GetComponent<MeshRenderer> ();
		if (selected) {
			mesh.material = selectedMaterial;
		} else {
			mesh.material = notSelectedMaterial;
		}
	}

    public void VisualHighlight(bool on)
    {
        UnityEngine.Behaviour h = (Behaviour)this.GetComponent("Halo");
        if (on)
        {
            h.enabled = true;
        }
        else
        {
            h.enabled = false;
        }
    }

	public void Play() {

	}

	// Update is called once per frame
	void Update () {
		
	}

	public void OnHighlight() {
		audioClip.Play ();
	}

	public void Select() {
		this.selected = true;
	}

	public void Deselect() {
		this.selected = false;
	}

	public bool IsSelected() {
		return this.selected;
	}
}
