import java.io.*;
import java.net.*;
import java.util.ArrayList;
import java.util.List;
import java.io.InputStreamReader;

public class ChatClientHandler extends Thread
{
    private static final String commandList = "HELP, NAME, WHOAMI, BYE, USERS";
    private String userName;
    private ChatServer server;
    private Socket socket;
    private BufferedReader aReader;
    private BufferedWriter aWriter;

    /**
     * 初期化する。
     */
    public ChatClientHandler(Socket aSocket)
    {
        this.userName = null;
        this.socket = aSocket;
        InetAddress address = this.socket.getInetAddress();
    }

    /**
     * スレッドをスタートさせる。
     */
    public void run()
    {
        try
        {
            this.open();
            while(true)
            {
                aWriter.write(">>> ");
                aWriter.flush();
                String str = this.receive();
                String[] commands = str.split(" ");
                
                if (commands[0].equals("HELP"))
                {
                    this.useHelpCommand(commands);
                }
                else if (commands[0].equals("NAME"))
                {
                    this.useNameCommand(commands);
                }
                else if (commands[0].equals("WHOAMI"))
                {
                    this.useWhoamiCommand(commands);
                }
                else if (commands[0].equals("BYE"))
                {
                    this.useByeCommand(commands);
                    break;
                }
                else if (commands[0].equals("USERS"))
                {
                    this.useUsersCommand(commands);
                }
                else if (commands[0].equals("POST"))
                {
                    this.usePostCommand(commands);
                }
            }
        }
        catch(IOException anException)
        {
            anException.printStackTrace();
        }
        finally
        {
            this.close();
        }
    }
    
    
    private void useHelpCommand(String[] commands) throws IOException
    {
        if (commands.length == 1)
        {
            this.send(this.commandList);
        }
        else
        {
            String line;
            try
            {
                File aFile = new File("commandDocuments", commands[1] + ".txt");
                FileReader aReader = new FileReader(aFile);
                int aChar = aReader.read();
                String aString = "";
                int count = 0;
                while (aChar != -1)
                {
                    if (count > 24)
                    {
                        aWriter.write(aString);
                        aWriter.write("\r\n\t");
                        aWriter.flush();
                        aString = "";
                        count = 0;
                    }
                    if (aChar == '\n') { count = 0; }
                    aString += String.valueOf((char)aChar);
                    aChar = aReader.read();
                    count++;
                }
                this.send(aString);
            }
            catch(IOException anException) { }
        }
        
        return;
    }
    
    public void useNameCommand(String[] commands) throws IOException
    {
        List userNames = this.server.getUserNames();
        if (commands.length == 1)
        {
            this.send("引数が指定されていません。");
        }
        else if (userNames.contains(commands[1]))
        {
            this.send("既に他の人に使われている名前です。");
        }
        else
        {
            this.setUserName(commands[1]);
        }
        
        return;
    }
    
    private void useWhoamiCommand(String[] commands) throws IOException
    {
        if (this.getUserName() == null)
        {
            this.send("名前が設定されてません。");
        }
        else {
            this.send(this.getUserName());
        }
        
        return;
    }
    

    private void useByeCommand(String[] commands) throws IOException
    {
        this.send("チャット終了");
        return;
    }
    
    private void useUsersCommand(String[] commands) throws IOException
    {
        String str = "";
        List<String> userNames = this.server.getUserNames();
        for (int index=0; index < userNames.size(); index++)
        {
            str += userNames.get(index);
            if (index < userNames.size() - 1)
            {
                str += ", ";
            }
        }
        this.send(str);
    }
    
    private void usePostCommand(String[] commands) throws IOException
    {
        if (commands.length == 1)
        {
            this.send("メッセージがありません");
        }
        else
        {
            this.server.sendAll(this,commands[1]);
        }
    }
    
    
    public void setServer(ChatServer aServer)
    {
        this.server = aServer;
    }
    
    public String getUserName()
    {
        return this.userName;
    }
    
    private void setUserName(String aName)
    {
        this.userName = aName;
    }
    
    
    /**
     * クライアントとのデータのやり取りを行うストリームを開く。
     */
    void open() throws IOException
    {
        InputStream socketIn = this.socket.getInputStream();
        OutputStream socketOut = this.socket.getOutputStream();
        this.aReader = new BufferedReader(new InputStreamReader(socketIn));
        this.aWriter = new BufferedWriter(new OutputStreamWriter(socketOut));
        
        return;
    }

    /**
     * クライアントからデータを受け取る。
     */
    String receive() throws IOException
    {
        return aReader.readLine();
    }

    /**
     * クライアントへデータを送る。
     */
    void send(String message) throws IOException
    {
        aWriter.write(message);
        aWriter.write("\r\n");
        aWriter.flush();
        
        return;
    }

    /**
     * クライアントとの接続を閉じる。
     */
    void close()
    {
        if(aReader != null)
        {
            try {
                aReader.close();
            }
            catch(IOException anException) {}
        }
        if(aWriter != null)
        {
            try {
                aWriter.close();
            }
            catch(IOException anException) {}
        }
        if(this.socket != null)
        {
            try {
                this.socket.close();
            }
            catch(IOException anException){}
        }
        
        return;
    }
}
