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

	public EmulatedSocket() {
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
		if (this.socket.Connected) {
			Debug.Log (this.socket.Connected);
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
				Debug.Log("切断されました。");
				aSocket.Close();
				return;
			}

			Debug.Log("Received " + System.Text.Encoding.Default.GetString(this.socketBuffer, 0, len));
			this.socket.BeginReceive (socketBuffer, 0, this.socketBuffer.Length,
				SocketFlags.None, this.OnReceiveData, socket);
		}
	}

	public void send(string message) {
		byte[] byteMessage = System.Text.Encoding.UTF8.GetBytes(message);
		this.socket.Send(byteMessage);
	}

	public void close() {
		this.socket.Shutdown(SocketShutdown.Both);
		this.socket.Close();
	}
}