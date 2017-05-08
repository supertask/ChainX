using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class EmulatedSocket
{
    private Socket socket;
    private byte[] socketBuffer;
    private Example controller;

    public EmulatedSocket(Example aController) {
        this.controller = aController;
        this.socketBuffer = new byte[1024];
        this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPAddress ServerIP = IPAddress.Parse("127.0.0.1");
        IPEndPoint remoteEP = new IPEndPoint(ServerIP, 18080);
        try {
            this.socket.Connect(remoteEP);
        }
        catch(Exception e) {
            Console.WriteLine (e);
            Environment.Exit(1);

            //UnityEditor.EditorApplication.isPlaying = false;
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
            Console.WriteLine("閉じました。");
            return;
        }
        if (len <= 0) {
            Console.WriteLine("サーバーが切断されました。");
            aSocket.Close(); //ここがバグってる
            return;
        }


        lock(Example.thisLock) {
            string receivedMessage = System.Text.Encoding.Default.GetString (this.socketBuffer, 0, len);
            receivedMessage = receivedMessage.Trim();
            if (receivedMessage.IndexOf("OPERATION_FAIL") == 0) {
                Console.WriteLine(receivedMessage); //TODO(Tasuku): Think about this errors
            }

            Operation op = Operation.FromJson(receivedMessage);
            //Console.WriteLine ("受信したJson: " + Operation.ToJson(op));

            //Console.WriteLine ("始まり at OnReceiveData()");
            //ChainVoxel cv = this.controller.getChainVoxel ();
            this.controller.cv.apply(op);
        }

        //Console.WriteLine ("終わり at OnReceiveData()");
        //Console.WriteLine ("--------------------");

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
