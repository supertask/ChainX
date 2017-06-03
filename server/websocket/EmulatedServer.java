import java.net.InetSocketAddress;

import java.net.*;
import java.io.*;
import java.util.*;

import org.java_websocket.WebSocket;
import org.java_websocket.WebSocketImpl;
import org.java_websocket.handshake.ClientHandshake;
import org.java_websocket.server.WebSocketServer;
import org.java_websocket.framing.CloseFrame;
import com.google.gson.*;

public class EmulatedServer extends WebSocketServer
{
    private List<WebSocket> sockets;
    private Map<WebSocket,Integer> siteIDMap;
    //public static final int NUMBER_OF_LIMITED_SITE = 100;

    public EmulatedServer(InetSocketAddress address) {
        super(address);
        this.sockets = new ArrayList<WebSocket>();
        this.siteIDMap = new HashMap<WebSocket,Integer>();
    }

    @Override
    public void onStart() {
        System.out.println("Server has been started.");
    }

    @Override
    public void onOpen(WebSocket conn, ClientHandshake handshake)
    {
        if (!this.sockets.contains(conn))
            this.sockets.add(conn);
        System.out.println("new connection to " + conn.getRemoteSocketAddress());
    }

    @Override
    public void onClose(WebSocket conn, int code, String reason, boolean remote)
    {
        if (this.sockets.contains(conn))
            this.sockets.remove(conn);
        System.out.println("Closed " + conn.getRemoteSocketAddress() + " with exit code " + code + " additional info: " + reason);
    }

    @Override
    public void onMessage(WebSocket conn, String message)
    {
        System.out.println("received message from "    + conn.getRemoteSocketAddress() + ": " + message);
        if (message.equals("EXIT"))
            this.onClose(conn, CloseFrame.NORMAL, "EXIT SUCCESS", true);

        Gson gson = new Gson();
        Operation op = gson.fromJson(message, Operation.class);
        int sid = Integer.parseInt(op.getSID());

        if (sid < this.sockets.size()) {
            conn.send(message); //本来はsidの指定先に送る, 自分自身にも送る
            this.broadcast(conn, message);
        }
        else
            conn.send("OPERATION_FAIL: sid number has been out of bounds.");
    }

    public void broadcast(WebSocket conn, String message) {
        for(WebSocket aSocket : this.sockets) {
            if (aSocket != conn) {
                aSocket.send(message);
            }
        }
    }

    @Override
    public void onError(WebSocket conn, Exception ex) {
        System.err.println("an error occured on connection " + conn.getRemoteSocketAddress()  + ":" + ex);
    }
    

    public List<String> getUserNames()
    {
        return null;
    }

    /**
     * 指定した宛先に操作オブジェクトを送信する
     * @param dest 宛先Siteの識別子
     * @param op 操作オブジェクト
     * @see Operation
     * @see OperationQueue
     */
    public void send(int dest, String message) {
        //synchronizedされている
        /*
        try {
            this.sites[dest].getWriter().write(message);
            this.sites[dest].getWriter().write("\r\n");
            this.sites[dest].getWriter().flush();
        }
        catch(IOException anException) { anException.printStackTrace(); }
        */
    }
    
    

    public static void main(String[] args) {
        String host = "localhost";
        int port = 18080;

        WebSocketServer server = new EmulatedServer(new InetSocketAddress(host, port));
        server.run();
    }
}
