using UnityEngine;
using System.Collections;
using System;

public class EchoTest : MonoBehaviour {

	// Use this for initialization
	IEnumerator Start () {
		//WebSocket w = new WebSocket(new Uri("ws://localhost:18080"));
		WebSocket w = new WebSocket(new Uri("ws://127.0.0.1:18080"));
		yield return StartCoroutine(w.Connect());
		w.SendString("{\"sid\":\"52\",\"ts\":\"636314339710432260\",\"opType\":\"5\",\"opParams\":{\"posID\":\"4:0:3\",\"destPosID\":\"5:0:3\"}}");
		int i=0;
		while (true)
		{
			string reply = w.RecvString();
			if (reply != null)
			{
				Debug.Log ("Received: "+reply);
			}
			if (w.error != null) {
				Debug.LogError ("Error: "+w.error);
				break;
			}
			yield return 0;
		}
		w.Close();
	}
}
