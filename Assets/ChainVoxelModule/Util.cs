using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util {
	public static long currentTimeNanos() {
		return System.DateTime.Now.Ticks; //Nano second
	}

	public static long currentTimeMillis() {
		//return System.DateTime.Now.Millisecond;
		return System.DateTime.Now.Ticks / 10000; //Milli second
	}	

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
