using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EmulatedWebSocket
{
	private	byte[] socketBuffer;
	private ChainXController controller;
	private WebSocket ws;

	public EmulatedWebSocket(ChainXController aController) {
		this.controller = aController;
		this.ws = new WebSocket(new Uri("ws://127.0.0.1:18080"));
	}

	public IEnumerator Connect() {
		yield return this.ws.Connect();
	}

	public IEnumerator Listen() {
		int i=0;
		while (true) {
			string receivedMessage = this.ws.RecvString();
			if (receivedMessage != null)
			{
				//Debug.Log ("Received: "+receivedMessage);
				this.operate(receivedMessage);

				//w.SendString("Hi there"+i++);
			}
			if (this.ws.error != null)
			{
				Debug.LogError ("Error: " + this.ws.error);
				break;
			}
			yield return 0;
		}
		this.ws.Close();
	}

	private void operate(string receivedMessage)
	{
		receivedMessage = receivedMessage.Trim();
		if (receivedMessage.IndexOf(MessageType.OPERATION) == 0) {
			receivedMessage = receivedMessage.Remove(0, MessageHeader.OPERATION.Length);
			Operation op = Operation.FromJson(receivedMessage);
			this.controller.cv.apply (op);
		}
		if (receivedMessage.IndexOf(MessageType.ERROR) == 0) {
			Debug.LogError(receivedMessage); //TODO(Tasuku): Think about this errors
		}
		else if (receivedMessage.IndexOf(MessageType.TEXT_FILE) == 0) {
			receivedMessage = receivedMessage.Remove(0, MessageHeader.TEXT_FILE.Length);
			this.controller.cv.LoadSavedData(receivedMessage);
		}
	}

	public void Send(string message) {
		ws.SendString(message);
	}
	public void Close() {
		ws.SendString(MessageHeader.EXIT);
		ws.Close();
	}

	public static void Test()
	{
		//EmuratedWebSocketをここに書くと、new connectionができ、2重に送ってしまう。これはバグの元になるので、テストを書かない！！
	}
}