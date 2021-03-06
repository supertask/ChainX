import java.net.URI;

import java.util.*;
import java.io.*;
import java.util.regex.*;
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
    public static final String JOIN_HEADER = "JOIN" + MSG_SPLIT_CHAR;

    //双方向
    public static final String OPERATION_HEADER = "OPERATION" + MSG_SPLIT_CHAR;

    public static String algorithm;

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
    //public static WaitingTimer waitingTimer; //外部から設定, スレッド同士の共有資源

    private BufferedReader reader;

    public VSite(BufferedReader reader) {
        this.reader = reader;
    }

    public int getLeaderID() { return this.leaderID; }
    public int getID() { return this.id; }
    public int getNumberOfSteps() { return this.numberOfSteps; }
    public int getNumberOfMessages() { return this.numberOfMessages; }

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
        else if (msg.startsWith(VSite.JOIN_HEADER)) {
            String strID = msg.substring(msg.indexOf(VSite.MSG_SPLIT_CHAR)+1, msg.length());
            int intID = Integer.parseInt(strID);
            if (! this.siteIDs.contains(intID)) { this.siteIDs.add(intID); }
        }
        else if (msg.startsWith(VSite.REQUEST_VOTE_HEADER) ||
                 msg.startsWith(VSite.OPERATION_HEADER) ||
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

        System.out.println("\u001b[00;33m" + "[受信(on " + this.id + ")]:" + msg + " \u001b[00m");
    }


    // アプリケーション側（キーボード入力）
    //----------------------------------------
    public void increaseNumberOfSteps() { this.numberOfSteps++; }
    public void increaseNumberOfMessages(int num) { this.numberOfMessages+=num; }
    public void increaseNumberOfMessages() { this.increaseNumberOfMessages(1); }

    /**
     * メッセージを送信する
     */
    private void send(int destId, String msg) {
        try {
            msg = msg + VSite.MSG_SPLIT_CHAR + destId;
            System.out.println("\u001b[00;32m" + "[送信(" + this.id + " to " + destId + ")]: " + msg + " \u001b[00m");
            this.serverSession.getBasicRemote().sendBinary(
                ByteBuffer.wrap(msg.getBytes(Charset.forName("US-ASCII") ))
            );
        }
        catch(Exception e) { e.printStackTrace(); }
    }

    /**
     * メッセージを送信する
     * 基本的にはリーダーのみが使う
     */
    private void sendInLocal(String msg) {
        String msgType = msg.substring(0, msg.indexOf(VSite.MSG_SPLIT_CHAR));
        System.out.println("\u001b[00;32m" +  "[ローカル送信(" + this.id + " to " + this.id + ")]: " + msg + " \u001b[00m");
        messageQueue.enqueue(msgType, msg); //受信したメッセージを1つ貯める
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
        else { return ""; }
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


    private String[] getSIDandTS(String msg)
    {
        Matcher m = null;
        Pattern sidP = Pattern.compile("\"sid\":\"(\\d+)\"");
        Pattern tsP = Pattern.compile("\"ts\":\"(\\d+)\"");
        m = sidP.matcher(msg);
        String[] res = new String[2];
        if (m.find()) {
            res[0] = m.group(1);
        }
        m = tsP.matcher(msg);
        if (m.find()) {
            res[1] = m.group(1);
        }
        assert(res[0] != null);
        assert(res[1] != null);
        return res;
    }

    private String replaceSIDandTS(String msg, int sid, long ts) {
        msg = msg.replaceFirst("\"sid\":\"(\\d+)\"", "\"sid\":" + "\"" + sid + "\"");
        msg = msg.replaceFirst("\"ts\":\"(\\d+)\"", "\"ts\":" + "\"" + ts + "\"");
        return msg;
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
            //this.send(this.leaderID, VSite.VOTE_HEADER);
            this.sendInLocal(VSite.VOTE_HEADER);
            this.broadcast(VSite.REQUEST_VOTE_HEADER);
            this.increaseNumberOfSteps();
            this.increaseNumberOfMessages(this.siteIDs.size());

            // step2: Followerからの投票を待つ
            //ここで待ち状態
            this.waitMessagesForLeaderSelect(VSite.VOTE_HEADER, this.siteIDs.size()); //loop wait
            this.increaseNumberOfSteps();
            this.increaseNumberOfMessages(this.siteIDs.size()); //(Leaderになるためには過半数の合意が必要)

            // step3: Followerに対してLeaderになったことを報告
            //this.send(this.leaderID, VSite.APPEND_ENTRIES_HEADER);
            this.sendInLocal(VSite.APPEND_ENTRIES_HEADER);
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
        if (recordLine == null) { return; }
        long opDiffTime = Long.parseLong(recordLine[0]);
        String opLine = recordLine[1];

        Ticks ticks = new Ticks();
        ticks.start();
        long startSystemTime = ticks.end();
        long exCurrentVirtualTime = 0L;
        long execTime = opDiffTime;
        //List<Long> waitedTimes = new ArrayList<Long>();
        long totalWaitedTime = 0L;
        //waitedTimes.add(0L);
        while(true) {
            if (recordLine == null) { break; }
            long currentSystemTime = ticks.end(); //System.nanoTime();
            long currentVirtualTime = currentSystemTime - startSystemTime;
            long diffTime = currentVirtualTime - exCurrentVirtualTime;

            while(execTime <= currentVirtualTime) {
                //if (waitedTimes.isEmpty()) { break; }
                //execTime += Util.sumFrom(waitedTimes);
                //waitedTimes.clear();

                // step0: 操作をLeaderに送信する
                //（レコードされた操作を実行し，送信！）
                //System.out.println(this.id + ": currentVirtualTime = " + this.getSecondTime(currentVirtualTime));
                //System.out.println(this.id + ": execTime = " + this.getSecondTime(execTime));
                long ts = ticks.end();
                opLine = this.replaceSIDandTS(opLine, this.id, ts);
                System.out.println(opLine);
                //String key = VSite.waitingTimer.startWaitingTime(this.id, ts, ts); //タグ付け引数
                //System.out.println("StartWait! " + key);
                if (this.id == this.leaderID) {
                    this.sendInLocal(opLine);
                }
                else {
                    this.send(this.leaderID, opLine);
                }
                this.increaseNumberOfSteps();
                this.increaseNumberOfMessages(this.siteIDs.size() - 1);

                recordLine = this.nextRecord();
                if (recordLine == null) { break; }
                opDiffTime = Long.parseLong(recordLine[0]); //操作前後の差分の時間（ユーザのPause時間）
                opLine = recordLine[1]; 
                execTime += opDiffTime;
            }
            
            // メッセージ受信
            if (this.id == this.leaderID) { // Leaderの動作
                //自分と他のユーザの操作を受け取る
                String opMsg = this.waitOperation(VSite.OPERATION_HEADER);
                if (opMsg != "") {
                    // step1: 送信された操作を受け取る
                    //waiting終了，自分の操作が自分に返ってくるまでの時間
                    //long waitedTime = this.showWaitingTime(ticks, opMsg);
                    //waitedTimes.add(waitedTime);
                    //totalWaitedTime += waitedTime;

                    this.increaseNumberOfSteps();
                    this.increaseNumberOfMessages();

                    // step2: 操作をFollowerに共有する
                    // local operation は省略
                    this.broadcast(opMsg); 
                    this.increaseNumberOfSteps();
                    this.increaseNumberOfMessages(this.siteIDs.size());
                }
            }
            else { // Followerの動作
                //リーダーから他のユーザの操作を受け取る
                String opMsg = this.waitOperation(VSite.OPERATION_HEADER);
                if (opMsg != "") {
                    //waiting終了，自分の操作が自分に返ってくるまでの時間
                    //long waitedTime = this.showWaitingTime(ticks, opMsg);
                    //waitedTimes.add(waitedTime);
                    //totalWaitedTime += waitedTime;

                    this.increaseNumberOfSteps();
                    this.increaseNumberOfMessages();
                }
            }
            exCurrentVirtualTime = currentVirtualTime;
            try { Thread.sleep(10); } //10ミリ秒
            catch (InterruptedException e) { e.printStackTrace(); }
        }

        /*
        System.out.println("site id:" + this.id +
            ", totalWaitedTime:" + totalWaitedTime  + "nano100sec, totalWaitedTime:" +
            Util.convert100NanoSecToSec(totalWaitedTime) + "sec.");
        */
    }


    /**
     * ChainVoxel時のSiteの振る舞いを実行する．<br>
     * <br>
     * シミュレーション実行中のメッセージ総数は，「site毎のメッセージ総数 * site数」で求める
     */
    public void runChainVoxel()
    {
        //操作の実行を行う（ユーザ入力レコードを実行する）
        String[] recordLine = this.nextRecord();
        if (recordLine == null) { return; }
        long opDiffTime = Long.parseLong(recordLine[0]);
        String opLine = recordLine[1];

        Ticks ticks = new Ticks();
        ticks.start();
        long startSystemTime = ticks.end(); //System.nanoTime();
        long exCurrentVirtualTime = 0L;
        long execTime = opDiffTime;
        //currentVirtualTimeは0から始まる
        while(true) {
            if (recordLine == null) { break; }
            long currentSystemTime = ticks.end(); //System.nanoTime();
            long currentVirtualTime = currentSystemTime - startSystemTime;
            long diffTime = currentVirtualTime - exCurrentVirtualTime;

            while(execTime <= currentVirtualTime) {
                //System.out.println(this.id + ": currentVirtualTime = " + this.getSecondTime(currentVirtualTime));
                //System.out.println(this.id + ": execTime = " + this.getSecondTime(execTime));
                //ここでレコードされた操作を実行し，送信！
                long ts = ticks.end();
                opLine = this.replaceSIDandTS(opLine, this.id, ts);
                System.out.println(opLine);

                this.broadcast(opLine);
                this.increaseNumberOfSteps();
                this.increaseNumberOfMessages(this.siteIDs.size() - 1);

                recordLine = this.nextRecord();
                if (recordLine == null) { break; }
                opDiffTime = Long.parseLong(recordLine[0]); //操作前後の差分の時間（ユーザのPause時間）
                opLine = recordLine[1];
                execTime += opDiffTime;
            }
            
            exCurrentVirtualTime = currentVirtualTime;
            try { Thread.sleep(10); } //10ミリ秒
            catch (InterruptedException e) { e.printStackTrace(); }
        }
        return;
    }

    private long showWaitingTime(Ticks ticks, String opMsg) {
        String[] res = getSIDandTS(opMsg);
        int sid = Integer.parseInt(res[0]);
        long ts = Long.parseLong(res[1]);
        long current_ts = ticks.end();
        /*
        long waitedTime = VSite.waitingTimer.endWaitingTime(sid, ts, current_ts);
        if (waitedTime > 0 && sid == this.id) {
            //自分自身が送信したメッセージが帰ってきたら
            //本来はtsがちゃんと設定されるべきだが自作tsなのでマイナスが出たりする．
            System.out.println("EndWait! key="+ WaitingTimer.getOpStr(sid,ts) +
                ", waitedTime=" + waitedTime + "nano100sec, waitedTime="
                + Util.convert100NanoSecToSec(waitedTime) + "sec. ");
            return waitedTime;
        }
        else { return 0L; }
        */
        return 0L;
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
        if (this.algorithm.equals("RAFT")) {
            this.runRaft();
        }
        else if (this.algorithm.equals("CHAINVOXEL")) {
            this.runChainVoxel();
        }
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

    public static void main(String[] args) throws Exception
    {
        if (args.length < 2) {
            System.err.println("エラー: 引数が足りません!");
            System.exit(1);
        }

        File[] recordedFiles = new File("./modified_recorded_operations/").listFiles();
        List<File> opFiles = new ArrayList<File>();

        //ファイル名は0オリジン
        for(int i = 0; i < 100; i++) {
            String name = i + ".txt";
            for(File aFile : recordedFiles) {
                if (aFile.getName().equals(name)) {
                    opFiles.add(aFile);
                    break;
                }
            }
        }

        //引数2つが通常(site数ベース)，3つ目はadditional(操作数ベース)
        VSite.algorithm = args[0];
        int numOfSites = Integer.parseInt(args[1]);
        int numOfOperations = -1;
        if (args.length == 3) { numOfOperations = Integer.parseInt(args[2]); }

        if (opFiles.size() < numOfSites) {
            System.err.println("エラー: レコード数が足りません!");
            System.exit(1);
        }
        Session[] sessions = new Session[numOfSites];
        VSite[] sites = new VSite[numOfSites];
        //VSite.waitingTimer = new WaitingTimer();

        for(int i = 0; i < numOfSites; i++) {
            WebSocketContainer container = ContainerProvider.getWebSocketContainer();
            File aFile = opFiles.get(i);
            BufferedReader reader = null;
            try {
                reader = new BufferedReader(new FileReader(aFile));
            }
            catch(FileNotFoundException e) { e.printStackTrace(); }

            sites[i] = new VSite(reader);
            sessions[i] = container.connectToServer(sites[i], URI.create("ws://localhost:18080"));
            //System.out.println(sites[i].id);
        }
        Thread.sleep(2000); 

        for(VSite aSite : sites) {
            aSite.start();
        }
        for(VSite aSite : sites) {
            aSite.join();
        }
        for(int i = 0; i < numOfSites; i++) {
            if (sessions[i].isOpen()) {
                sites[i].closeBefore();
                sessions[i].close();
            }
        }

        //性能評価
        String EVALUATED_DATA_DIR = "evaluated_data/";
        String filename_messages = "";
        String filename_steps = "";
        if (VSite.algorithm.equals("CHAINVOXEL")) {
            //サイト数ベースの時
            if (numOfOperations == -1) {
                // 評価3. サイト数 vs ステップ数
                filename_steps = EVALUATED_DATA_DIR + "chainvoxel_sites_vs_steps.txt";
                // 評価4. サイト数 vs メッセージ数
                filename_messages = EVALUATED_DATA_DIR + "chainvoxel_sites_vs_messages.txt";
            }
            //操作数ベースの時
            else {
                // 評価1. 操作数 vs ステップ数
                filename_steps = EVALUATED_DATA_DIR + "chainvoxel_operations_vs_steps.txt";
                // 評価2. 操作数 vs メッセージ数
                filename_messages = EVALUATED_DATA_DIR + "chainvoxel_operations_vs_messages.txt";
            }
        }
        else if (VSite.algorithm.equals("RAFT")) {
            if (numOfOperations == -1) {
                // 評価3. サイト数 vs ステップ数
                filename_steps = EVALUATED_DATA_DIR + "raft_sites_vs_steps.txt";
                // 評価4. サイト数 vs メッセージ数
                filename_messages = EVALUATED_DATA_DIR + "raft_sites_vs_messages.txt";
            }
            else {
                // 評価1. 操作数 vs ステップ数
                filename_steps = EVALUATED_DATA_DIR + "raft_operations_vs_steps.txt";
                // 評価2. 操作数 vs メッセージ数
                filename_messages = EVALUATED_DATA_DIR + "raft_operations_vs_messages.txt";
            }
        }

        //リーダーsiteを見つける
        VSite leaderSite = null;
        for(int i = 0; i < sites.length; i++) {
            if (sites[i].getLeaderID() == sites[i].getID()) {
                leaderSite = sites[i];
            }
        }

        //System.out.println("leader: " + leaderSite);
        //System.out.println("numOfSites: " + numOfSites);

        //
        // Unity上で実行している場合，リーダーはUnity上のユーザになるので，パス
        //
        if (leaderSite == null) { return; }

        //性能評価結果
        int totalNumOfMessages = 0;
        if (VSite.algorithm.equals("CHAINVOXEL")) {
            //totalNumOfMessages = leaderSite.getNumberOfMessages() * numOfSites; //????
            for(VSite aSite : sites) {
                totalNumOfMessages += aSite.getNumberOfMessages();
            }
        }
        else if (VSite.algorithm.equals("RAFT")) {
            totalNumOfMessages = leaderSite.getNumberOfMessages();
        }
        int numOfSteps = leaderSite.getNumberOfSteps();
        

        //性能評価結果をfilename_stepsとfilename_messagesに保存
        String stepsLine = "", messagesLine = "";
        if (numOfOperations == -1) {
            //サイト数に対するのを調べるとき
            stepsLine = numOfSites + " " + numOfSteps;
            messagesLine = numOfSites + " " + totalNumOfMessages;
        }
        else {
            //操作数に対するのを調べるとき
            int totalNumOfOperations = numOfOperations * numOfSites;
            stepsLine = totalNumOfOperations + " " + numOfSteps;
            messagesLine = totalNumOfOperations + " " + totalNumOfMessages;
        }
        System.out.println("==========================================");
        System.out.println("stepsLine: " + stepsLine);
        System.out.println("messagesLine: " + messagesLine);
        System.out.println("==========================================");

        try {
            FileWriter wfSteps = new FileWriter(new File(filename_steps), true);
            FileWriter wfMessages = new FileWriter(new File(filename_messages), true);
            wfSteps.write(stepsLine + "\r\n");
            wfMessages.write(messagesLine + "\r\n");
            wfSteps.flush();
            wfMessages.flush();
            wfSteps.close();
            wfMessages.close();
        }
        catch (IOException e) { e.printStackTrace(); }

    }

}
