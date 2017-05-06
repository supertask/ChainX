using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Networking;

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
		if (receivedMessage.IndexOf("OPERATION_FAIL") == 0) {
			Debug.LogError(receivedMessage); //TODO(Tasuku): Think about this errors
		}

		Operation op = Operation.FromJson(receivedMessage);
		//Debug.Log ("受信したJson: " + Operation.ToJson(op));

		//Debug.Log ("始まり at OnReceiveData()");
		//ChainVoxel cv = this.controller.getChainVoxel ();
		ChainXController.cv.apply(op);

		//Debug.Log ("終わり at OnReceiveData()");
		//Debug.Log ("--------------------");

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