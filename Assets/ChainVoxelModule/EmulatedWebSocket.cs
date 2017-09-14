using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EmulatedWebSocket
{
	private	byte[] socketBuffer;
	private ChainXController controller;
	private WebSocket ws;
	private string filepath = "";


	public EmulatedWebSocket(ChainXController aController) {
		this.controller = aController;
		this.ws = new WebSocket(new Uri("ws://localhost:18080"));
	}

	public IEnumerator Connect() {
		yield return this.ws.Connect();
	}

	private bool partEqual(ref byte[] all, ref byte[] part) {
		if (part.Length < all.Length) {
			int i = 0;
			while(i < part.Length && (all[i] == part[i])) { i++; }
			return i == part.Length;
		}
		return false;
	}

	private string getStringUntilAt(ref byte[] all) {
		byte part = Convert.ToByte(MessageHeader.SPLIT_CHAR);	
		int i = 0;
		while(i < all.Length && (all[i] != part)) { i++; }
		i++;
		//Debug.Log (Convert.ToChar(all[i]));
		int start_i = i;
		while(i < all.Length && (all[i] != part)) { i++; }
		int end_i = i;
		//Debug.Log (Convert.ToChar(all[i]));

		int l = end_i - start_i;
		byte[] filepathByte = new byte[l];
		Array.Copy (all, start_i, filepathByte, 0, l);
		return Encoding.UTF8.GetString (filepathByte);
	}

	public IEnumerator Listen() {
		string exMsgType = "";
		string receivedMessage = "";
		byte[] receivedBinary = null;

		while (true) {
			receivedBinary = this.ws.Recv();
			if (receivedBinary != null) {
				Debug.Log (receivedBinary);
				if (this.partEqual (ref receivedBinary, ref MessageHeader.OPERATION)) {
					string line = Encoding.UTF8.GetString (receivedBinary);
					line = line.Remove (0, MessageHeader.OPERATION.Length).Trim ();
					Debug.Log (line);
				}
				else if (this.partEqual (ref receivedBinary, ref MessageHeader.SOME_FILE)) {
					string filepath = this.getStringUntilAt(ref receivedBinary);
					Debug.Log (filepath);
					int start_i = MessageHeader.SOME_FILE.Length + filepath.Length + 1; //1='@'
					string path = Application.persistentDataPath + "/" + filepath;
					FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
					fs.Write (receivedBinary, start_i, receivedBinary.Length - start_i);
					fs.Close ();
				}
				else if (this.partEqual (ref receivedBinary, ref MessageHeader.EXIT)) {
					Debug.Log ("EXIT");
				}
				else if (this.partEqual (ref receivedBinary, ref MessageHeader.ERROR)) {
					Array.Clear (receivedBinary, 0, MessageHeader.ERROR.Length);
					Debug.Log ("ERROR");
				}
			}

			if (this.ws.error != null) {
				Debug.LogError ("Error: " + this.ws.error);
				break;
			}
			yield return 0;
		}
		this.ws.Close();
	}

	private string operate(string receivedMessage)
	{
		/*
		receivedMessage = receivedMessage.Trim();
		if (receivedMessage.IndexOf (MessageHeader.OPERATION) == 0) {
			receivedMessage = receivedMessage.Remove (0, MessageHeader.OPERATION.Length).Trim ();
			Debug.Log (receivedMessage);
			Operation op = Operation.FromJson (receivedMessage);
			Debug.Log (Operation.ToJson (op));
			//Debug.Log(receivedMessage);
			this.controller.cv.apply (op);
			this.controller.cv.showStructureTable ();
			return MessageType.OPERATION;
		}
		else if (receivedMessage.IndexOf (MessageHeader.SOME_FILE) == 0) {
			receivedMessage = receivedMessage.Remove (0, MessageHeader.SOME_FILE.Length).Trim ();
			int i = 0;
			while(receivedMessage[i] != MessageHeader.SPLIT_CHAR) {
				filepath += receivedMessage[i];
				i++;
			}
			//this.controller.cv.LoadSavedData(receivedMessage);
			return MessageType.SOME_FILE;
		}
		else if (receivedMessage.IndexOf(MessageHeader.ERROR) == 0) {
			Debug.LogError(receivedMessage); //TODO(Tasuku): Think about this errors
			return MessageType.ERROR;
		}	
		*/
			/*
			 * 1. object fileをロードする
			 * 		- object fileをどこかに保存しておく
			 * 		- ChainVoxel用にposIDs for insertを発行
			 * 		- applyするOperationを発行
			 * 		- return the operation
			 * 2. Apply operation
			 */
		return null;
	}

	public void Send(string message) {
		ws.SendString(message);
	}
	public void Close() {
		//ws.SendString(MessageHeader.EXIT);
		ws.Send(MessageHeader.EXIT);
		ws.Close();
	}

	public static void Test()
	{
		//EmuratedWebSocketをここに書くと、new connectionができ、2重に送ってしまう。これはバグの元になるので、テストを書かない！！
	}
}