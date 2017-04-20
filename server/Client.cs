using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Client {
    // 接続先サーバーのエンドポイント
    private static readonly IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 18080);

    // 受信用のバッファ
    private static readonly byte[] buffer = new byte[0x100];

    static void Main()
    {
        // TCP/IPでの通信を行うソケットを作成する
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) {
            // Ctrl+Cが押された場合はソケットを閉じる
            Console.CancelKeyPress += (sender, args) => socket.Close();

            // 接続する
            socket.Connect(serverEndPoint);

            Console.WriteLine("connected to {0}", socket.RemoteEndPoint);

            // 非同期での受信を開始する
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, socket);

            for (;;) {
                // コンソールからの入力を待機する
                var input = Console.ReadLine();

                if (input == null) {
                    // Ctrl+Z(UNIXではCtrl+D)が押された場合はソケットを閉じて終了する
                    socket.Close();
                    break;
                }

                socket.Send(Encoding.Default.GetBytes(input + Environment.NewLine));
            }
        }
    }

    // 非同期受信のコールバックメソッド
    private static void ReceiveCallback(IAsyncResult ar)
    {
        var socket = ar.AsyncState as Socket;

        // 受信を待機する
        var len = socket.EndReceive(ar);

        // 受信した内容を表示する
        Console.Write("> {0}", Encoding.Default.GetString(buffer, 0, len));

        // 再度非同期での受信を開始する
        socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, socket);
    }
}
