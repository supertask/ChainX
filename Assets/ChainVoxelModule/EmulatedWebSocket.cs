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
		this.ws = new WebSocket(new Uri("ws://localhost:18080"));
	}

	public IEnumerator Connect() {
		yield return this.ws.Connect();
	}

	public IEnumerator Listen() {
		while (true) {
			string receivedMessage = this.ws.RecvString();
			if (receivedMessage != null)
			{
				//Debug.Log ("Received: "+receivedMessage);
				this.operate(receivedMessage);

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
		if (receivedMessage.IndexOf(MessageHeader.OPERATION) == 0) {
			receivedMessage = receivedMessage.Remove(0, MessageHeader.OPERATION.Length).Trim();
			//Debug.Log (receivedMessage);
			Operation op = Operation.FromJson(receivedMessage);
			Debug.Log(Operation.ToJson(op));
			//Debug.Log(receivedMessage);
			this.controller.cv.apply (op);
			this.controller.cv.showStructureTable();
		}
		if (receivedMessage.IndexOf(MessageHeader.ERROR) == 0) {
			Debug.LogError(receivedMessage); //TODO(Tasuku): Think about this errors
		}
		else if (receivedMessage.IndexOf(MessageHeader.TEXT_FILE) == 0) {
			receivedMessage = receivedMessage.Remove(0, MessageHeader.TEXT_FILE.Length).Trim();
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