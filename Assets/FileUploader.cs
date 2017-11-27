using UnityEngine;
using System.Collections;

public class FileUploader : MonoBehaviour {
	IEnumerator LoadTexture (string url) {
		WWW www = new WWW (url);
		yield return www;
		Debug.Log(www.bytes);
	}

	public void FileSelected (string url) {
		StartCoroutine(LoadTexture (url));
	}
}