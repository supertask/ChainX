using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class ImportButton : MonoBehaviour {
	[DllImport("__Internal")]
	private static extern void FileImporterCaptureClick();

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

	}

	public void OnButtonPointerDown()
	{
		FileImporterCaptureClick();
	}

	public void FileSelected(string url)
	{
		StartCoroutine(LoadJson(url));
	}

	private IEnumerator LoadJson(string url)
	{
		WWW www = new WWW(url);
		yield return www;
		Debug.Log(www.bytes);
	}
}