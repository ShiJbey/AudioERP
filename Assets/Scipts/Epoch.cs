using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Accord.Math;

public class Epoch {

	// Which object generated the event
	private GameObject eventOrigin;
	// Time of the event (Sound) LSL clock time
	private float eventTime;
	// Start time offset from the event time
	public static float startTimeOffset = 0f;
	// End time relative to the event time (seconds)
	public static float duration = 1f;
	// Data associated with this epoch
	private float[,] data;
	// During training epochs, the target object is marked
	// so it has a class of one
	private bool isTarget;

	public Epoch (GameObject eventOrigin, float eventTime, bool isTarget, float[,] data) {
		this.eventOrigin = eventOrigin;
		this.eventTime = eventTime;
		this.data = data;
        this.isTarget = isTarget;
    }

	public Epoch (GameObject eventOrigin, float eventTime, bool isTarget) {
		this.eventOrigin = eventOrigin;
		this.eventTime = eventTime;
		this.data = null;
        this.isTarget = isTarget;
    }

	public Epoch(float eventTime) {
		this.eventOrigin = null;
		this.eventTime = eventTime;
		this.data = null;
        this.isTarget = false;
    }

	public GameObject GetEventOrigin() {
		return this.eventOrigin;
	}

	public bool IsTarget() {
		return this.isTarget;
	}

	public void SetIsTarget(bool val) {
		this.isTarget = val;
	}

	public void SetEventOrigin(GameObject eventOrigin) {
		this.eventOrigin = eventOrigin;
	}

	public float GetEventTime() {
		return this.eventTime;
	}

	public void SetEventTime(float eventTime) {
		this.eventTime = eventTime;
	}

	public float[,]  GetData() {
		return this.data;
	}

	public void SetData(float[,] data) {
		this.data = data;
	}

	// Insert a function for converting epochs to feature vectors
	public float[] GetFeatureVector() {
		float[] featureVector = new float[this.data.GetNumberOfElements ()];
		featureVector = this.data.Reshape<float> (0);
		return featureVector;
	}

	public int GetLabel() {
		if (this.isTarget) {
			return 1;
		} else {
			return 0;
		}
	}
 }
