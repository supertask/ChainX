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

	private int leaderID = -1;
	private int id = -1;
	private List<int> siteIDs = new List<int>();


	public EmulatedWebSocket(ChainXController aController) {
		this.controller = aController;
		this.ws = new WebSocket(new Uri("ws://" + Const.SERVER_ID + ":18080"));
	}

	public IEnumerator Connect() {
		yield return this.ws.Connect();
	}

	public int getID() {
		return this.id;
	}

	public IEnumerator Listen() {
		//this.SendBinary(-1, Const.START_BINARY_HEADER);

		byte[] msgBinary = null;
		string[] filepaths = {"", ""};

		while (true) {
			msgBinary = this.ws.Recv();
			if (msgBinary != null) {
				Debug.Log (Encoding.ASCII.GetString(msgBinary));
				//byte[] idBinary = this.getIdFromEndUntilAt(ref receivedBinary);
				//int destID = System.BitConverter.ToInt32(idBinary, 0); //送り先!!
				//byte[] msgBinary = this.getOperationFromEndUntilAt(ref receivedBinary);

				if (this.partEqual (ref msgBinary, ref Const.OPERATION_BINARY_HEADER)) {
					string line = Encoding.UTF8.GetString (msgBinary);
					line = line.Remove (0, Const.OPERATION_HEADER.Length).Trim();
					Operation op = Operation.FromJson (line);
					this.controller.cv.apply (op);
				}
				else if (this.partEqual (ref msgBinary, ref Const.JOIN_BINARY_HEADER)) {
					string msg = Encoding.UTF8.GetString(msgBinary);
					msg = msg.Replace(Const.JOIN_HEADER, "");
					int intID = int.Parse(msg);
					if (! this.siteIDs.Contains(intID)) { this.siteIDs.Add(intID); }
					//foreach (int sid in this.siteIDs) { Debug.Log("sid: " + sid); }
					//Debug.Log("this.id:" + this.id);
					//Debug.Log("joined id:" + intID);
				}
				else if (this.partEqual (ref msgBinary, ref Const.ID_LIST_BINARY_HEADER)) {
					string msg = Encoding.UTF8.GetString(msgBinary);
					int atIndex = msg.IndexOf(Const.MSG_SPLIT_CHAR);
					string idsLine = msg.Substring(atIndex + 1); //atIndex+1から最後まで
					String[] strIDs = idsLine.Split(Const.SPLIT_CHAR);
					foreach (string strID in strIDs) {
						int intID = int.Parse(strID);
						if (! this.siteIDs.Contains(intID)) { this.siteIDs.Add(intID); }
					}
					this.leaderID = this.siteIDs[0];
					this.id = this.siteIDs[this.siteIDs.Count - 1];
				}
				else if (this.partEqual (ref msgBinary, ref Const.SOME_FILE_BINARY_HEADER)) {
					string filepath = this.getPathUntilAt(ref msgBinary);
					int start_i = Const.SOME_FILE_HEADER.Length + filepath.Length + 1; //1='@'
					string path = Application.persistentDataPath + "/" + filepath;
					FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
					fs.Write (msgBinary, start_i, msgBinary.Length - start_i);
					fs.Close ();

					if (Path.GetExtension (path) == ".txt") {
						//Debug.Log (path);
						this.controller.cv.LoadSavedData (path);
					}
					else if (Path.GetExtension (path) == ".obj") {
						filepaths[0] = path;
					}
					else if (Path.GetExtension (path) == ".jpg") {
						filepaths[1] = path;
					}

					if (filepaths[0] != "" && filepaths[1] != "") {
						//
						// When all dependent files of a 3d obj have been collected, 
						// Build the 3d obj.
						//
						string[] posIDs = ObjLoadHelper.LoadObj(filepaths, new Vector3 (0,5,0));
						//string[] posIDs = ObjLoadHelper.LoadOnlyObj(filepaths[0], new Vector3 (0,5,0));
						Operation op = new Operation (0, Operation.INSERT_POLYGON,
							"{\"posIDs\": \"" + Util.GetCommaLineFrom(posIDs) +
							"\", \"gid\": \"" + ChainXModel.CreateGID() +
							"\", \"objPath\":\"" + filepaths[0] + "\"}");
						//Debug.Log(Operation.ToJson(op));
						this.controller.cv.apply(op);

						filepaths = new string[]{"", ""}; //Clear for the next 3d objs 
					}
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



	private bool partEqual(ref byte[] all, ref byte[] part) {
		if (part.Length < all.Length) {
			int i = 0;
			while(i < part.Length && (all[i] == part[i])) { i++; }
			return i == part.Length;
		}
		return false;
	}

	/*
	public int getAtIndexFromBegin(ref byte[] all) {
		byte part = Convert.ToByte(Const.MSG_SPLIT_CHAR);	
		int i = 0;
		while(i < all.Length && (all[i] != part)) { i++; }
		return i;
	}
	*/
	
	public int getAtIndexFromEnd(ref byte[] all) {
		byte part = Convert.ToByte(Const.MSG_SPLIT_CHAR);	
		int  i = all.Length - 1;
		while(i >= 0 && (all[i] != part)) { i--; }
		return i;
	}

	public string getPathUntilAt(ref byte[] all) {
		byte part = Convert.ToByte(Const.MSG_SPLIT_CHAR);	
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


	public byte[] getIdFromEndUntilAt(ref byte[] all) {
		int indexAt = this.getAtIndexFromEnd(ref all);
		int start_i = indexAt + 1; //@の後のindex
		int end_i = all.Length - 1;
		int idLength = end_i + 1 - start_i;

		byte[] idBinary = new byte[idLength];
		Array.Copy (all, start_i, idBinary, 0, idLength);
		//Debug.Log(System.Text.Encoding.GetEncoding("UTF-8").GetString(idBinary));
		return idBinary;
	}

	public byte[] getOperationFromEndUntilAt(ref byte[] all) {
		int indexAt = this.getAtIndexFromEnd(ref all);
		int start_i = indexAt + 1; //@の後のindex

		int messageLength = start_i - 1;
		byte[] messageBinary = new byte[messageLength];
		Array.Copy (all, 0, messageBinary, 0, messageLength);
		//Debug.Log(System.Text.Encoding.GetEncoding("UTF-8").GetString(messageBinary));
		return messageBinary;
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

	public void SendBinary(int destID, byte[] message) {
		byte[] destIdBinary = System.Text.Encoding.ASCII.GetBytes(
			Const.MSG_SPLIT_CHAR + destID.ToString()
		);
		ws.Send(Util.CombineBytes(message, destIdBinary));
	}

	public byte[] AddInfo(byte[] headerBinary, string additionalInfo) {
		byte[] additionalInfoBinary = System.Text.Encoding.ASCII.GetBytes(
			additionalInfo	
		);
		return Util.CombineBytes(headerBinary, additionalInfoBinary);
	}

	public void Close() {
		byte[] exitBinary = this.AddInfo(Const.EXIT_BINARY_HEADER, this.id.ToString());
		this.SendBinary(-1, exitBinary);
		ws.Close();
	}

	public static void Test()
	{
		//EmuratedWebSocketをここに書くと、new connectionができ、2重に送ってしまう。これはバグの元になるので、テストを書かない！！

		
	}
}
