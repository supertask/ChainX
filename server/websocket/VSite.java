import java.net.URI;

import java.util.*;
import java.io.*;
import javax.websocket.ClientEndpoint;
import javax.websocket.ContainerProvider;
import javax.websocket.OnClose;
import javax.websocket.OnError;
import javax.websocket.OnMessage;
import javax.websocket.OnOpen;
import javax.websocket.Session;
import javax.websocket.WebSocketContainer;
import java.nio.ByteBuffer;
import java.nio.charset.Charset;

/**
 * Websocket Endpoint implementation class VSite
 */

@ClientEndpoint
public class VSite extends Thread
{
    //スプリット識別子
    public static final String MSG_SPLIT_CHAR = "@";
    public static final String SPLIT_CHAR = ",";

    //クライアントからサーバへ
    public static final String START_HEADER = "START" + MSG_SPLIT_CHAR;
    public static final String EXIT_HEADER = "EXIT" + MSG_SPLIT_CHAR;
    public static final String REQUEST_VOTE_HEADER = "REQUEST_VOTE" + MSG_SPLIT_CHAR;
    public static final String VOTE_HEADER = "VOTE" + MSG_SPLIT_CHAR;
    public static final String APPEND_ENTRIES_HEADER = "APPEND_ENTRIES" + MSG_SPLIT_CHAR;

    //サーバからクライアントへ
    public static final String SOME_FILE_HEADER = "SOME_FILE" + MSG_SPLIT_CHAR;
    public static final String ID_LIST_HEADER = "ID_LIST" + MSG_SPLIT_CHAR;
    public static final String JOINED_HEADER = "JOINED" + MSG_SPLIT_CHAR;

    //双方向
    public static final String OPERATION_HEADER = "OPERATION" + MSG_SPLIT_CHAR;

    private int leaderID = -1;
    private int id = -1;
    private List<Integer> siteIDs = new ArrayList<Integer>();
    private Session serverSession = null;
    
    /** 操作の実行回数 */
    //private int numberOfOperations;    

    /** ステップ数 */
    private int numberOfSteps = 0;

    /** メッセージ総数 */
    private int numberOfMessages = 0;

    /**
     * メッセージの待ち行列（メッセージキュー）
     * ネットワーク層とアプリケーション層の２つの共有変数
     */
    private MessageQueue messageQueue = new MessageQueue();

    private BufferedReader reader;

    public synchronized void increaseNumberOfSteps() { this.numberOfSteps++; }
    public synchronized void increaseNumberOfMessages(int num) { this.numberOfMessages+=num; }
    public synchronized void increaseNumberOfMessages() { this.increaseNumberOfMessages(1); }
    //public synchronized void increaseNumberOfOperations(int num) { this.numberOfOperations+=num; }

    public VSite() {
        File[] op_files = new File("./modified_recorded_operations/").listFiles();
        try {
            this.reader = new BufferedReader(new FileReader(op_files[0]));
        }
        catch(FileNotFoundException e) { e.printStackTrace(); }
    }

    // ネットワーク側（メッセージ受信）
    //----------------------------------------
    @OnOpen
    public void onOpen(Session session) {
        System.out.println("[セッション確立]");
        this.serverSession = session;
        this.send(this.id, VSite.START_HEADER);
        //this.broadcast(VSite.START_HEADER);
    }

    //public void onMessage(String msg) {
    @OnMessage
    public void onMessage(byte[] binaryMessage) {
        String msg = "";
        try {
            msg = new String(binaryMessage, "UTF-8");
        }
        catch(IOException e) { e.printStackTrace(); }
        //System.out.println("受信: " + msg);
        //System.out.println(msg.startsWith(VSite.VOTE_HEADER));

        if (msg.startsWith(VSite.ID_LIST_HEADER)) {
            //
            //開始時にのみ呼ばれる（接続リストがもらえる）
            //
            String ids_line = msg.substring(msg.indexOf(VSite.MSG_SPLIT_CHAR)+1, msg.length());
            String[] strIDs = ids_line.split(VSite.SPLIT_CHAR);
            for(String strID : strIDs) {
                int intID = Integer.parseInt(strID);
                if (! this.siteIDs.contains(intID)) { this.siteIDs.add(intID); }
            }
            this.leaderID = this.siteIDs.get(0);
            this.id = this.siteIDs.get(this.siteIDs.size() - 1);
        }
        else if (msg.startsWith(VSite.SOME_FILE_HEADER)) {
            //pass
        }
        else if (msg.startsWith(VSite.JOINED_HEADER)) {
            String strID = msg.substring(msg.indexOf(VSite.MSG_SPLIT_CHAR)+1, msg.length());
            int intID = Integer.parseInt(strID);
            if (! this.siteIDs.contains(intID)) { this.siteIDs.add(intID); }
        }
        else if (msg.startsWith(VSite.REQUEST_VOTE_HEADER) ||
                 msg.startsWith(VSite.VOTE_HEADER)) {
            String msgType = msg.substring(0, msg.indexOf(VSite.MSG_SPLIT_CHAR));
            //System.out.println(msgType + ": " + msg);
            messageQueue.enqueue(msgType, msg); //受信したメッセージを1つ貯める
            //ここで何もできなくなる
            //System.out.println("queueSize: " + messageQueue.queueSize(msgType));
        }
        else if (msg.startsWith(VSite.APPEND_ENTRIES_HEADER)) {
            String msgType = msg.substring(0, msg.indexOf(VSite.MSG_SPLIT_CHAR));
            //何かする(APPEND_ENTRIES)
            messageQueue.enqueue(msgType, msg); //受信したメッセージを1つ貯める
        }
        System.out.println("[受信(on " + this.id + ")]:" + msg);
    }


    // アプリケーション側（キーボード入力）
    //----------------------------------------
    /**
     * メッセージを送信する
     */
    private void send(int destId, String msg) {
        try {
            msg = msg + VSite.MSG_SPLIT_CHAR + destId;
            System.out.println("[送信(" + this.id + " to " + destId + ")]: " + msg);
            this.serverSession.getBasicRemote().sendBinary(
                ByteBuffer.wrap(msg.getBytes(Charset.forName("US-ASCII") ))
            );
        }
        catch(Exception e) { e.printStackTrace(); }
    }

    /**
     / 操作を他のSiteに共有するメソッド
     * @param op 操作オブジェクト
     * @see Site#send
     */
    public void broadcast(String msg) {
        System.out.print("Broadcast from " + this.id + " to ");
        for (int intId : this.siteIDs) {
            if (this.id == intId) continue;
            System.out.print(intId + " ");
        }
        System.out.println(".");

        for (int intId : this.siteIDs) {
            if (this.id == intId) continue;
            // this.delay();
            this.send(intId, msg); // remote operation
        }
    }


    private String waitOperation(String msgHeader)
    {
        String msgType = msgHeader.substring(0, msgHeader.indexOf(VSite.MSG_SPLIT_CHAR));
        if (this.messageQueue.queueSize(msgType) > 0) {
            return this.messageQueue.dequeue(msgType); //溜まったメッセージをdequeue
        }
        else { return null; }
    }

    /**
     * 引数の数だけ，指定したメッセージを待つ
     * 例msgHeader=OPERATION,numberOfMessages=1の時，OPERATIONのメッセージを1つだけ待つ
     */
    private void waitMessagesForLeaderSelect(String msgHeader, int numberOfMessages)
    {
        String msgType = msgHeader.substring(0, msgHeader.indexOf(VSite.MSG_SPLIT_CHAR));
        while(true) {
            //引数の数だけ，指定したメッセージを待つ
            int size = this.messageQueue.queueSize(msgType);
            //System.out.println(msgType + ": " + size);
            if (size >= numberOfMessages) {
                this.messageQueue.clearQueue(msgType); //溜まったらメッセージ削除
                return;
            }
            try {
                Thread.sleep(10); //10ミリ秒
            }
            catch (InterruptedException e) { e.printStackTrace(); }
        }
    }


    /**
     * Raft 時のsiteの振る舞いを実行する<br>
     * <br>
     * 全ての操作をRaft に基づいて実行する．siteの故障は起きないためLeaderの選出は１度しか行わない．<br>
     * また，一貫性の収束にかかるステップ数とメッセージ数の評価が目的のため，ログレプリケーションやハートビートといった操作も考えない．<br>
     * Raftの場合は全ての操作をLeaderを介して行うため，Leaderのメッセージ数を測定することで総メッセージ数が測定できる．<br>
     * <br>
     * シミュレーション実行中のメッセージ総数は，「Leaderのメッセージ総数」で求めることができる (id=0のsite)．
     */
    public void runRaft()
    {
        //
        //リーダー選出
        //
        if (this.id == this.leaderID) { 
            // idが0の人がCandidateになる
            // step1: FollowerにrequestVoteを送信する
            this.send(this.leaderID, VSite.VOTE_HEADER);
            this.broadcast(VSite.REQUEST_VOTE_HEADER);
            this.increaseNumberOfSteps();
            this.increaseNumberOfMessages(this.siteIDs.size());

            // step2: Followerからの投票を待つ
            //ここで待ち状態
            this.waitMessagesForLeaderSelect(VSite.VOTE_HEADER, this.siteIDs.size()); //loop wait
            this.increaseNumberOfSteps();
            this.increaseNumberOfMessages(this.siteIDs.size()); //(Leaderになるためには過半数の合意が必要)

            // step3: FollowerにLeaderになったことを報告
            this.send(this.id, VSite.APPEND_ENTRIES_HEADER);
            this.broadcast(VSite.APPEND_ENTRIES_HEADER);
            this.increaseNumberOfSteps();
            this.increaseNumberOfMessages(this.siteIDs.size() - 1);
        }
        else {
            // step1: CandidateからのrequestVoteを待つ
            this.waitMessagesForLeaderSelect(VSite.REQUEST_VOTE_HEADER, 1); //loop wait
            this.increaseNumberOfSteps();
            this.increaseNumberOfMessages();

            // step2: 送信元，Candidateに投票する
            this.send(this.leaderID, VSite.VOTE_HEADER);
            this.increaseNumberOfSteps();
            this.increaseNumberOfMessages();

            // step3: LeaderからのAppendEntriesを待つ
            this.waitMessagesForLeaderSelect(VSite.APPEND_ENTRIES_HEADER, 1); //loop wait
            this.increaseNumberOfSteps();
            this.increaseNumberOfMessages();
        }
        System.out.println("Join!: " + this.id);

        //操作の実行を行う（ユーザ入力レコードを実行する）
        String[] recordLine = this.nextRecord();
        long opDiffTime = Long.parseLong(recordLine[0]);
        String opLine = recordLine[1];

        long startTime = System.nanoTime();
        long exCurrentTime = 0L;
        long waitingTime = 0L; //後で編集
        long execTime = opDiffTime + waitingTime;
        while(true) {
            if (recordLine == null) { break; }
            long currentTime = System.nanoTime() - startTime;

            if (exCurrentTime <= execTime) {
                while(execTime <= currentTime) {
                    //System.out.println(this.id + ": currentTime = " + this.getSecondTime(currentTime));
                    //System.out.println(this.id + ": execTime = " + this.getSecondTime(execTime));
                    //ここでレコードされた操作を実行し，送信！
                    System.out.println(opLine);
                    //this.send(this.leaderID, opLine);

                    recordLine = this.nextRecord();
                    if (recordLine == null) { break; }
                    opDiffTime = Long.parseLong(recordLine[0]);
                    opLine = recordLine[1];
                    execTime += opDiffTime;
                }
            }
            
            /*
            // メッセージ受信
            //ここで受信確認し，次のステップへ進める
            if (this.id == this.leaderID) { // Leaderの動作
                String opMsg = this.waitOperation(VSite.OPERATION_HEADER);
                if (opMsg != "") {
                    // step1: 送信された操作を受け取る
                    this.increaseNumberOfSteps();
                    this.increaseNumberOfMessages();

                    // step2: 操作をFollowerに共有する
                    //this.send(this.leaderID, opMsg); // local operation は省略
                    this.broadcast(opMsg); 
                    this.increaseNumberOfSteps();
                    this.increaseNumberOfMessages(this.siteIDs.size());
                }
            }
            else { // Followerの動作
                String opMsg = this.waitOperation(VSite.OPERATION_HEADER);
                if (opMsg != "") {
                    // local operation は省略
                    this.increaseNumberOfSteps();
                    this.increaseNumberOfMessages();
                }
            }
            */

            exCurrentTime = currentTime;
            try {
                Thread.sleep(10); //10ミリ秒
            }
            catch (InterruptedException e) { e.printStackTrace(); }
        }
    }

    private String[] nextRecord() {
        String line = "";
        try {
            line = this.reader.readLine();
            if (line == null) return null;
        }
        catch(IOException e) { e.printStackTrace(); }
        String[] opRecord = line.split("#");

        return opRecord;
    }


    public float getSecondTime(long nanoTime) {
        return nanoTime / (float)1000000000;
    }

    /*
     * UnityのUpdate関数を再現したもの
     *
     * FixedUpdate関数は20ms(=0.02s)で呼ばれ，
     * Update関数は10ms(0.01s) ~ 40ms(0.04s)で呼ばれる．
     * そのため，
     * 
     */
    public void run() {
        this.runRaft();
    }


    @OnError
    public void onError(Throwable th) { }

    @OnClose
    public void onClose(Session session) {
        //String msg = VSite.EXIT_HEADER + this.id;
        //System.out.println(msg);
    }

    public void closeBefore() {
        String msg = VSite.EXIT_HEADER + this.id;
        this.send(-1, msg);
    }


    static public void main(String[] args) throws Exception
    {
        int numOfSites = 2;
        Session[] sessions = new Session[numOfSites];
        VSite[] sites = new VSite[numOfSites];
        for(int i = 0; i < numOfSites; i++) {
            WebSocketContainer container = ContainerProvider.getWebSocketContainer();
            sites[i] = new VSite();
            sessions[i] = container.connectToServer(sites[i], URI.create("ws://localhost:18080"));
            //System.out.println(sites[i].id);
        }
        Thread.sleep(3000); 

        for(int i = 0; i < numOfSites; i++) {
            sites[i].start();
        }
        Thread.sleep(30000); 

        for(int i = 0; i < numOfSites; i++) {
            if (sessions[i].isOpen()) {
                sites[i].closeBefore();
                sessions[i].close();
            }
        }

        /*
        String msg = "OPERATION@{'sid': '0','ts':'525225252'}";
        //session.getBasicRemote().sendText(msg);
        session.getBasicRemote().sendBinary(
            ByteBuffer.wrap(msg.getBytes(Charset.forName("UTF-8") ))
        );
        */
    }

}
