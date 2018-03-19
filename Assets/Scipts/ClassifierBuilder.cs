using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Accord.Math;
using System.Linq;
using System.Runtime.InteropServices;
using System;

namespace AudioERP {
	
	public class ClassifierBuilder : MonoBehaviour {

		public string classifierOutFilename = "classifier.dat";
		public int[] desiredChannels;

		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void Update () {
			
		}

		float[,] CreateDataMatrix(List<float[]> data) {
			return Matrix.Create (data.ToArray ());
		}

		float[,] extractDesiredChannels(float[,] data) {
			return null;
		}
	}

	/*
	// This is borrowed from Stack Overflow:
	// https://stackoverflow.com/questions/27427527/how-to-get-a-complete-row-or-column-from-2d-array-in-c-sharp
	public static class ArrayExt {

		public static T[] GetRow<T> (this T[,] array, int row) {
			if (!typeof(T).IsPrimitive) {
				throw new InvalidOperationException ("Method not supported for managed types");
			}
			if (array == null) {
				throw new ArgumentNullException ("array is null");
			}
			int cols = array.GetUpperBound (1) + 1;
			T[] result = new T[cols];
			int size = Marshal.SizeOf<T> ();
			Buffer.BlockCopy (array, row * cols * size, result, 0, cols * size);
			return result;
		}

		public static T[] GetCol<T> (this T[,] array, int col) {
			if (!typeof(T).IsPrimitive) {
				throw new InvalidOperationException ("Method not supported for managed types");
			}
			if (array == null) {
				throw new ArgumentNullException ("array is null");
			}
			int rows = array.GetUpperBound (0) + 1;
			int size = Marshal.SizeOf(
		}
	}
	*/
}