import java.net.*;
import java.io.*;
import java.util.ArrayList;
import java.util.List;
import java.util.Collections;

public class EmulatedServer
{
    private ServerSocket serverSocket;
    private Site[] sites;
    public static final int NUMBER_OF_LIMITED_SITE = 100;
    
    public EmulatedServer()
    {
        this.serverSocket = null;
        this.sites = new Site[EmulatedServer.NUMBER_OF_LIMITED_SITE];
    }
    
    /*
     *
     */
    public void listen()
    {
        try {
            this.serverSocket = new ServerSocket(18080);
            System.out.println("Echoサーバをポート18080で起動しました。");
            while(true)
            {
                Socket aSocket = this.serverSocket.accept();
                this.cleanGarbageSites();
                for (int i = 0; i < EmulatedServer.NUMBER_OF_LIMITED_SITE; ++i)
                {
                    if (this.sites[i] == null) {
                        this.sites[i] = new Site(i, aSocket);
                        this.sites[i].setServer(this);
                        this.sites[i].start();
                        System.out.println("クライアント[" + i + "]が接続してきました。");
                        break;
                    }
                }
            }
        }
        catch(IOException anException) { anException.printStackTrace(); }
        
        return;
    }

    /*
     *
     */
    public void cleanGarbageSites() {
        for (int i = 0; i < EmulatedServer.NUMBER_OF_LIMITED_SITE; ++i) {
            if (this.sites[i] == null) { continue; }
            if (this.sites[i].isAlive()) { continue; }
            this.sites[i] = null;
            System.out.println("Cleaned garbage sites!!!");
        }
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
        try {
            this.sites[dest].getWriter().write(message);
            this.sites[dest].getWriter().write("\r\n");
            this.sites[dest].getWriter().flush();
        }
        catch(IOException anException) { anException.printStackTrace(); }
    }
    
    
    public static void main(String[] argumets)
    {
        EmulatedServer aServer = new EmulatedServer();
        aServer.listen();
        
        return;
    }
}
