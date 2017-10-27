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
		this.ws = new WebSocket(new Uri("ws://" + Const.SERVER_ID + ":18080"));
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
		byte[] receivedBinary = null;

		string[] filepaths = {"", "", ""};

		while (true) {
			receivedBinary = this.ws.Recv();
			if (receivedBinary != null) {
				//Debug.Log (receivedBinary);
				if (this.partEqual (ref receivedBinary, ref MessageHeader.OPERATION_BINARY)) {
					string line = Encoding.UTF8.GetString (receivedBinary);
					line = line.Remove (0, MessageHeader.OPERATION.Length).Trim ();
					Operation op = Operation.FromJson (line);
					this.controller.cv.apply (op);
				}
				else if (this.partEqual (ref receivedBinary, ref MessageHeader.SOME_FILE_BINARY)) {
					string filepath = this.getStringUntilAt(ref receivedBinary);
					int start_i = MessageHeader.SOME_FILE.Length + filepath.Length + 1; //1='@'
					string path = Application.persistentDataPath + "/" + filepath;
					FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
					fs.Write (receivedBinary, start_i, receivedBinary.Length - start_i);
					fs.Close ();

					if (Path.GetExtension (path) == ".txt") {
						this.controller.cv.LoadSavedData (path);
					}
					else if (Path.GetExtension (path) == ".obj") {
						filepaths[0] = path;
					}
					else if (Path.GetExtension (path) == ".mtl") {
						filepaths[1] = path;
					}
					else if (Path.GetExtension (path) == ".jpg") {
						filepaths[2] = path;
					}

					if (filepaths[0] != "" && filepaths[1] != "" && filepaths[2] != "") {
						//
						// When all dependent files of a 3d obj have been collected, 
						// Build the 3d obj.
						//
						string[] posIDs = ObjLoadHelper.LoadObj(filepaths, new Vector3 (0,5,0));
						Operation op = new Operation (0, Operation.INSERT_POLYGON,
							"{\"posIDs\": \"" + Util.GetCommaLineFrom(posIDs) +
							"\", \"gid\": \"" + ChainXModel.CreateGID() +
							"\", \"objPath\":\"" + filepaths[0] + "\"}");
						//Debug.Log(Operation.ToJson(op));
						this.controller.cv.apply (op);

						filepaths = new string[]{"", "", ""}; //Clear for the next 3d objs 
					}
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

	public void SendBinary(byte[] message) {
		ws.Send(message);
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
