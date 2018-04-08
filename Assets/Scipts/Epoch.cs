using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Accord.Math;
using System;

public class Epoch
{

	// Which object generated the event
	private Transform eventOrigin;
	// Time of the event (Sound) LSL clock time
	private float eventTime;
	// Data associated with this epoch
	private float[,] data;
	// During training epochs, the target object is marked
	// so it has a class of one
	private bool isTarget;

	public Epoch (Transform eventOrigin, float eventTime, bool isTarget, float[,] data)
	{
		this.eventOrigin = eventOrigin;
		this.eventTime = eventTime;
		this.data = data;
		this.isTarget = isTarget;
	}

	public Epoch (Transform eventOrigin, float eventTime, bool isTarget)
	{
		this.eventOrigin = eventOrigin;
		this.eventTime = eventTime;
		this.data = null;
		this.isTarget = isTarget;
	}

    public Epoch(Transform eventOrigin, float eventTime)
    {
        this.eventOrigin = eventOrigin;
        this.eventTime = eventTime;
        this.data = null;
        this.isTarget = false;
    }

    public Epoch (float eventTime)
	{
		this.eventOrigin = null;
		this.eventTime = eventTime;
		this.data = null;
		this.isTarget = false;
	}

	public Transform GetEventOrigin ()
	{
		return this.eventOrigin;
	}

	public bool IsTarget ()
	{
		return this.isTarget;
	}

	public void SetIsTarget (bool val)
	{
		this.isTarget = val;
	}

	public void SetEventOrigin (Transform eventOrigin)
	{
		this.eventOrigin = eventOrigin;
	}

	public float GetEventTime ()
	{
		return this.eventTime;
	}

	public void SetEventTime (float eventTime)
	{
		this.eventTime = eventTime;
	}

	public float[,]  GetData ()
	{
		return this.data;
	}

	public void SetData (float[,] data)
	{
		this.data = data;
	}

	// Insert a function for converting epochs to feature vectors
	public float[] GetFeatureVector ()
	{
		if (this.data != null) {
			float[] featureVector = new float[this.data.GetNumberOfElements ()];
			featureVector = this.data.Reshape<float> (0);
			return featureVector;
		} else {
			return null;
		}
	}

	public int GetLabel ()
	{
		if (this.isTarget) {
			return 1;
		} else {
			return 0;
		}
	}

	public static Epoch Average(Epoch ep1, Epoch ep2) {
		int nColsEp1 = ep1.GetData().Columns();
		int nColsEp2 = ep2.GetData().Columns();
		int nRowsEp1 = ep1.GetData().Rows();
		int nRowsEp2 = ep2.GetData().Rows();

		// Make sure that the data matrices are the same size
		if (nColsEp1 != nColsEp2) {
			throw new ArgumentException("Matrices do not have the same number of columns");
		}

		float[,] averagedData = null;
		float[,] truncatedData = null;

		if (nRowsEp1 > nRowsEp2) {
			// Remove some rows from the end of the data
			int difference = nRowsEp1 - nRowsEp2;
			averagedData = ep2.GetData ().Copy();
			int[] indicesToRemove = new int[difference];
			for (int i = 0; i < difference; ++i) {
				indicesToRemove [i] = nRowsEp1 - i;
			}
			truncatedData = ep1.GetData ().Remove (indicesToRemove, null);
		} else if (nRowsEp1 < nRowsEp2) {
			// Remove some rows from the end of epoch2 data
			int difference = nRowsEp2 - nRowsEp1;
			averagedData = ep1.GetData ().Copy ();
			int[] indicesToRemove = new int[difference];
			for (int i = 0; i < difference; ++i) {
				indicesToRemove [i] = nRowsEp2 - i;
			}
			truncatedData = ep2.GetData ().Remove (indicesToRemove, null);
		}

		averagedData = averagedData.Add (truncatedData);
		averagedData = averagedData.Multiply(1/2);

		Epoch averagedEpoch = new Epoch (ep1.eventOrigin, ep1.eventTime, ep1.IsTarget ());
		return averagedEpoch;
	}
}
