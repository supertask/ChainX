using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util{

	public static long START_NANO_TIME;

	/*
	 public static List<GameObject> ArrangeGameObjects(List<GameObject> gameobjects,
		string transMatrix)
	{
		List<GameObject> arrangedObjs = new List<GameObject>();
		foreach(string posID in posIDs) { 
			foreach (GameObject anObj in gameobjects) {
				anObj.name
			}

		}
	}
*/

	/*
	public static string[] ArrangePosIDs(string[] posIDs, string transMatrix)
	{
        List<Vector3> vs = new List<Vector3>();
        Vector3 tM = Util.SplitPosID(transMatrix);
        foreach(string posID in posIDs) { vs.Add(Util.SplitPosID(posID)); }

		IEnumerable<Vector3> sortedVs = null;
		if (tM.x > 0) sortedVs = vs.OrderBy (v => v.x);
		else if (tM.x < 0) sortedVs = vs.OrderByDescending (v => v.x);

		else if (tM.y > 0) sortedVs = vs.OrderBy (v => v.y);
		else if (tM.y < 0) sortedVs = vs.OrderByDescending (v => v.y);

		else if (tM.z > 0) sortedVs = vs.OrderBy (v => v.z);
		else if (tM.z < 0) sortedVs = vs.OrderByDescending (v => v.z);
			
		int i = 0;
		foreach (Vector3 v in sortedVs) {
			posIDs[i] = Util.CreatePosID (v);
			i++;
		}
		return posIDs;
    }
	*/


	public static string[] ArrangePosIDs(string[] posIDs, string transPosID)
	{
		return Util.ArrangePosIDs(posIDs, Util.SplitPosID (transPosID));
	}

	public static string[] ArrangePosIDs(string[] posIDs, Vector3 transMatrix)
	{
		List<KeyValuePair<Vector3, GameObject>> pairs = new List<KeyValuePair<Vector3, GameObject>>();
		foreach (string posID in posIDs) {
			pairs.Add (new KeyValuePair<Vector3, GameObject>(Util.SplitPosID(posID), null));
		}
		Util._arrangeGameObjects (ref pairs, transMatrix);	

		int i = 0;
		foreach (KeyValuePair<Vector3, GameObject> p in pairs) {
			posIDs[i] = Util.CreatePosID (p.Key);
			i++;
		}
		return posIDs;
	}

	public static List<GameObject> ArrangeGameObjects(List<GameObject> gameobjects, Vector3 transMatrix)
	{
		List<KeyValuePair<Vector3, GameObject>> pairs = new List<KeyValuePair<Vector3, GameObject>>();
		foreach (GameObject anObj in gameobjects) {
			pairs.Add (new KeyValuePair<Vector3, GameObject>(anObj.transform.position, anObj));
		}
		Util._arrangeGameObjects (ref pairs, transMatrix);	
		for(int i = 0; i < pairs.Count; ++i) {
			gameobjects[i] = pairs[i].Value;
		}
		return gameobjects;
	}

	private static void _arrangeGameObjects(
		ref List<KeyValuePair<Vector3, GameObject>> pairs,
		Vector3 tM)
	{
		//構造体ソート!!!!
		if (tM.x > 0) pairs = pairs.OrderByDescending (p => p.Key.x).ToList();
		else if (tM.x < 0) pairs = pairs.OrderBy (p => p.Key.x).ToList();

		else if (tM.y > 0) pairs = pairs.OrderByDescending (p => p.Key.y).ToList();
		else if (tM.y < 0) pairs = pairs.OrderBy (p => p.Key.y).ToList();

		else if (tM.z > 0) pairs = pairs.OrderByDescending (p => p.Key.z).ToList();
		else if (tM.z < 0) pairs = pairs.OrderBy (p => p.Key.z).ToList();
    }



	public static Vector3 CreateRandomTransMatrix() {
		int[] xs = {0,0,0};
		int t = UnityEngine.Random.Range (0, 3);
		xs[t] = 1;
		return new Vector3(xs[0],xs[1],xs[2]);	
	}

	public static Vector3 CreateRandomVector3(int minValue, int maxValue) {
		int x = UnityEngine.Random.Range (minValue, maxValue);
		int y = UnityEngine.Random.Range (minValue, maxValue);
		int z = UnityEngine.Random.Range (minValue, maxValue);
		return new Vector3(x,y,z);	
	}

	public static byte[] CombineBytes(byte[] a, byte[] b) {
		int length = a.Length + b.Length;
		byte[] combinedBinary = new byte[length];
		a.CopyTo(combinedBinary, 0);
		b.CopyTo(combinedBinary, a.Length);	
		return combinedBinary;
	}

	public static string CreatePosID(Vector3 v) {
		return String.Format ("{0}:{1}:{2}", v.x, v.y, v.z);
	}

	public static Vector3 SplitPosID(string posID) {
		string[] s = posID.Split(':');
        return new Vector3(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]));
    }

	public static string CreateRandomPosID(int minValue, int maxValue) {
		Vector3 v = Util.CreateRandomVector3 (minValue, maxValue);
		return Util.CreatePosID(v);
	}

	public static string CreateRandomPosID() {
		return Util.CreateRandomPosID (int.MinValue, int.MaxValue);
	}


	public static string GetCommaLineFrom (List<string> aList) {
		string res = "";
		foreach (string s in aList) {
			res += s + Const.SPLIT_CHAR;
		}
		return res.TrimEnd(Const.SPLIT_CHAR);
	}

	/*
	 * 
	 */
	public static string GetCommaLineFrom(string[] strs) {
		string res = "";
		foreach (string s in strs) {
			res += s + Const.SPLIT_CHAR;
		}
		return res.TrimEnd(Const.SPLIT_CHAR);
	}


	public static long currentTimeNanos() {
		return System.DateTime.Now.Ticks - Util.START_NANO_TIME; //Nano second
	}

	/*
	public static long currentTimeMillis() {
		//return System.DateTime.Now.Millisecond;
		return System.DateTime.Now.Ticks / 10000; //Milli second
	}	
	*/

	public static long Max(long left, long right) {
		if (left > right) { return left; }
		else { return right; }
	}

	public static long Min(long left, long right) {
		if (left < right) { return left; }
		else { return right; }
	}


	public static string GetGUID() {
		return Guid.NewGuid ().ToString ().Replace("-", "");
	}

	public static int mod(int m, int n) {
		if (n < 0) {
			Debug.LogError("you cannot use minus for modulo.");
		}
		if (m < 0) {
			m = -1 * m;
			if (m % n == 0) { return 0; }
			else { return n - (m % n); }
		}
		return m % n;
	}

	/*
	public static dynamic ConvertType(dynamic anObject) {
		if (anObject.GetType() == typeof(int)) { return (int) anObject; }
		else if (anObject.GetType() == typeof(string)) { return (string) anObject; }
		else if (anObject.GetType() == typeof(bool)) { return (bool) anObject; }
		else if (anObject.GetType() == typeof(float)) { return (float) anObject; }
		return anObject;
	}
	*/
}
