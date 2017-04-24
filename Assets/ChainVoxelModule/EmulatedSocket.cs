using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class EmulatedSocket
{
	private Socket socket;
	private	byte[] socketBuffer;
	private ChainXController controller;

	public EmulatedSocket(ChainXController aController) {
		this.controller = aController;
		this.socketBuffer = new byte[1024];
		this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		IPAddress ServerIP = IPAddress.Parse("127.0.0.1");
		IPEndPoint remoteEP = new IPEndPoint(ServerIP, 18080);
		try {
			this.socket.Connect(remoteEP);
		}
		catch(Exception e) {
			Debug.Log (e);
			UnityEditor.EditorApplication.isPlaying = false;
			//Application.Quit();
		}
		this.socket.BeginReceive (socketBuffer, 0, this.socketBuffer.Length, SocketFlags.None, this.OnReceiveData, this.socket);
	}

	private void OnReceiveData(IAsyncResult ar)
	{
		var aSocket = ar.AsyncState as Socket;

		int len = 0;
		try {
			len = aSocket.EndReceive(ar);
		}
		catch (System.ObjectDisposedException) {
			Debug.Log("閉じました。");
			return;
		}
		if (len <= 0) {
			Debug.Log("サーバーが切断されました。");
			aSocket.Close(); //ここがバグってる
			return;
		}

		string receivedMessage = System.Text.Encoding.Default.GetString (this.socketBuffer, 0, len);
		receivedMessage = receivedMessage.Trim();
		Debug.Log ("中間（Operationに変換する前）" + receivedMessage);
		Operation opX = Operation.FromJson(receivedMessage); //Slient Error!!!!!!!
		Debug.Log ("受信したJson: " + Operation.ToJson(opX));
		
		/*
		Debug.Log ("受信したJsonのID: " + op.getId());
		Debug.Log ("受信したJsonのposID: " + op.getPosID());
		Debug.Log ("受信したJsonのopType: " + op.getOpType());
		*/
		//this.controller.OperateVoxelOnLocal(op);
		this.socket.BeginReceive (socketBuffer, 0, this.socketBuffer.Length,
			SocketFlags.None, this.OnReceiveData, socket);
	}

	public void Send(string message) {
		byte[] byteMessage = System.Text.Encoding.UTF8.GetBytes(message);
		this.socket.Send(byteMessage);
	}

	public void Close() {
		this.Send("EXIT");
		this.socket.Shutdown(SocketShutdown.Both);
		this.socket.Close();
	}
}