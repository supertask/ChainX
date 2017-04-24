import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;
import java.util.Random;
import java.util.UUID;

import java.net.*;
import java.io.*;
import com.google.gson.*;

/**
 * Siteを表すクラス
 * @author kengo92i
 */
public class Site extends Thread {    
    /**
     * Siteの識別子
     */
    private int id;

    /**
     * ソケットインスタンス
     */
    private Socket socket;

    /**
     * ソケットの読み書きをするためのreaderとwriter
     */
    private BufferedReader reader;
    private BufferedWriter writer;
    
    /**
     * サーバーのインスタンス
     */
    private Server server;

    /**
     * 操作の実行回数
     */
    private int numberOfOperations;    

    /**
     * XYZ座標軸の限界値
     */
    private int limitOfRange;    

    /**
     * ステップ数
     */
    private int numberOfSteps;

    /**
     * メッセージ総数
     */
    private int numberOfMessages;

    /**
     * 指定された操作数を実行するSiteを作成します．
     * @param id Siteの識別子
     * @param opq オペレーションキュー
     * @param numberOfOperations 操作の実行回数
     * @param limitOfRange XYZ座標軸の限界値
     * @see OperationQueue
     */
    public Site(int id, Socket aSocket) {
        this.id = id;
        this.socket = aSocket;
        //this.numberOfOperations = numberOfOperations;
        //this.limitOfRange = limitOfRange;
        this.numberOfSteps = 0;
        this.numberOfMessages = 0;
    }

    public void setServer(Server aServer) {
        this.server = aServer;
    }
    
    /**
     * クライアントとのデータのやり取りを行うストリームを開く。
     */
    private void open() throws IOException
    {
        InputStream socketIn = this.socket.getInputStream();
        OutputStream socketOut = this.socket.getOutputStream();
        this.reader = new BufferedReader(new InputStreamReader(socketIn));
        this.writer = new BufferedWriter(new OutputStreamWriter(socketOut));
        return;
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
        this.server.send(dest, message);
    }

    /**
     * 操作オブジェクトを受信するメソッド <br>
     * receiveメソッドは受信した操作を１つ返します．受信した操作が空の場合は，nullを返す．
     * @return 操作オブジェクト
     * @see Operation
     * @see OperationQueue
     */
    public String receive() throws IOException {
        //synchronizedされている
        return this.reader.readLine();
    }


    /**
     * 操作を他のSiteに共有するメソッド
     * @param op 操作オブジェクト
     * @see Site#send
     */
     /*
     サーバー側に追加
    public void broadcast(Operation op) {
        int n = this.opq.getNumberOfSites();
        for (int i = 0; i < n; ++i) {
            if (this.id == i) continue;
            // this.delay();     
            this.send(i, op); // remote operation
        }
    }
    */
    
    /**
     * Siteの識別子を取得する
     * @return Siteの識別子
     */
    public int getSiteId() { return this.id; }

    /**
     * Siteが保持するメッセージ総数を取得する
     * @return メッセージ総数
     */
    public int getNumberOfMessages() {
        return this.numberOfMessages;
    }

    /**
     * Siteが保持する操作の実行回数を取得する
     * @return 操作の実行回数
     */
    public int getNumberOfOperations() {
        return this.numberOfOperations;
    }

    /**
     * Siteが保持するステップ数を取得する
     * @return ステップ数
     */
    public int getNumberOfSteps() {
        return this.numberOfSteps;
    }
    
    /**
     * Siteに遅延を発生させるメソッド
     */
    public void delay() {
        try {
            Thread.sleep((long) Math.ceil(Math.random()*10));
        } catch (InterruptedException ie) {
            ie.printStackTrace();        
        }
    }

    /**
     * ChainVoxelに操作を適用するメソッド
     * @return ChainVoxelの容量
     * @see ChainVoxel
     * @see Operation
     */
    /*
    public int applyOperation() {
        while (!opq.isEmpty(this.id)) {
            Operation op = receive();
            chainVoxel.apply(op);
        }
        chainVoxel.show();
        chainVoxel.exportCollada(Integer.toString(this.id));
        // System.out.println(chainVoxel.stt.getStatusString());
        return chainVoxel.size();
    }
    */

    /**
     * [-limitOfRange, limitOfRange]の範囲内の整数を返すメソッド
     * @return [-limitOfRange, limitOfRange]の範囲内の整数
     */
    /*
    private int randomIntRange() {
        return (new Random()).nextInt(2 * this.limitOfRange + 1) - this.limitOfRange;
    }
    */

    /**
     * voxel識別子(posID)をランダムに生成するメソッド
     * @return voxel識別子
     */
    /*
    private String generateRandomPosID() {
        String x = Integer.toString(this.randomIntRange()); 
        String y = Integer.toString(this.randomIntRange()); 
        String z = Integer.toString(this.randomIntRange()); 
        String posID = x + ":" + y + ":" + z;        
        return posID;
    }
    */

    /**
     * 操作をランダムに生成するメソッド<br>
     * プリミティブ層の操作にしか対応していない．
     * @deprecated プリミティブ層以外の操作に対応 {@link #randomOperation}
     * @return 操作オブジェクト
     */
    /*
    private Operation generateRandomOperation() {
        int opType = (new Random()).nextInt(2);
        String posID = this.generateRandomPosID();
        Operation op = new Operation(this.id, opType, posID);
        return op;
    }
    */

    /**
     * 操作をランダムに生成するメソッド
     * @return 操作オブジェクト
     */
    /*
    private Operation randomOperation() {
        int opType = (new Random()).nextInt(5); 
        Map<String, Object> params = new HashMap<String, Object>();

        if (opType == Operation.INSERT || opType == Operation.DELETE) {
            params.put("sid", this.id);
            params.put("posID", this.generateRandomPosID());
        }
        else if (opType == Operation.CREATE) {
            params.put("gid", UUID.randomUUID().toString());
        }
        else if (opType == Operation.JOIN) {
            params.put("posID", this.generateRandomPosID()); 
            params.put("gid", UUID.randomUUID().toString());
        }
        else if (opType == Operation.LEAVE) {
            params.put("sid", this.id);
            params.put("posID", this.generateRandomPosID());
            params.put("gid", UUID.randomUUID().toString());
        }
        else {
            assert false;
        }

        Operation op = new Operation(opType, params);
        return op;
    }
    */

    /**
     * 操作を指定数受け取るまで待機するメソッド
     * @param num 操作を受け取る数
     * @return 受信した操作のリスト
     */
    /*
    public ArrayList<Operation> waitReceiveOperation(int num) {
        Operation op = null; int count=0;
        ArrayList<Operation> operationList = new ArrayList<Operation>();
        while (count < num) {
            if ((op = receive()) != null) {
                operationList.add(op);
                count++;
            }
        }
        return operationList;
    }
    */

    /**
     * Raft 時のsiteの振る舞いを実行する<br>
     * <br>
     * 全ての操作をRaft に基づいて実行する．siteの故障は起きないためLeaderの選出は１度しか行わない．<br>
     * また，一貫性の収束にかかるステップ数とメッセージ数の評価が目的のため，ログレプリケーションやハートビートといった操作も考えない．<br>
     * Raftの場合は全ての操作をLeaderを介して行うため，Leaderのメッセージ数を測定することで総メッセージ数が測定できる．<br>
     * <br>
     * シミュレーション実行中のメッセージ総数は，「Leaderのメッセージ総数」で求めることができる (id=0のsite)．
     * @see Operation
     */
    /*
    private void runBehaviorOfRaft() {
        // Leaderの選出 
        int numberOfSites = this.opq.getNumberOfSites();
        if (this.id == 0) { // idが0の人がCandidateになる
            // step1: FollowerにrequestVoteを送信する
            Operation requestVote = new Operation(this.id, Operation.REQUEST_VOTE, "");
            this.send(0, requestVote);
            this.broadcast(requestVote);
            this.numberOfSteps++;
            this.numberOfMessages += numberOfSites;

            // step2: Followerからの投票を待つ
            this.waitReceiveOperation(numberOfSites);
            this.numberOfSteps++;
            this.numberOfMessages += numberOfSites; // (Leaderになるためには過半数の合意が必要)

            // step3: FollowerにLeaderになったことを報告
            Operation appendEntries = new Operation(this.id, Operation.APPEND_ENTRIES, "");
            this.broadcast(appendEntries);
            this.numberOfSteps++;
            this.numberOfMessages += numberOfSites - 1;
        } else {
            // step1: CandidateからのrequestVoteを待つ
            this.waitReceiveOperation(1);
            this.numberOfSteps++;
            this.numberOfMessages++;

            // step2: 送信元，Candidateに投票する
            Operation vote = new Operation(this.id, Operation.VOTE, "");
            this.send(0, vote);
            this.numberOfSteps++;
            this.numberOfMessages++;

            // step3: LeaderからのAppendEntriesを待つ
            this.waitReceiveOperation(1);
            this.numberOfSteps++;
            this.numberOfMessages++;
        }

        // 操作の実行を行う
        int maxTurn = this.numberOfOperations * numberOfSites;
        for (int turn = 0; turn < maxTurn; ++turn) {
            if (turn % numberOfSites == this.id) { // 操作を実行する人 
                // step0: 操作をLeaderに送信する
                Operation op = this.generateRandomOperation();
                this.send(0, op);
            }

            if (this.id == 0) { // Leaderの動作
                // step1: 送信された操作を受け取る
                Operation op = this.waitReceiveOperation(1).get(0);
                this.numberOfSteps++;
                this.numberOfMessages++;

                // step2: 操作をFollowerに共有する
                // this.send(this.id, op); // local operation は省略
                this.broadcast(op); 
                this.numberOfSteps++;
                this.numberOfMessages += numberOfSites;
            } else { // Followerの動作
                this.waitReceiveOperation(1);
                // local operation は省略
                this.numberOfSteps++;
                this.numberOfMessages++;
            }
        }
        return;
    }
    */

    /**
     * two-phase commit 時のsiteの振る舞いを実行する <br>
     * <br>
     * 全ての操作を２層コミットに基づいて実行する．siteが故障することは考えない．<br>
     * <br>
     * siteIDの昇順で操作を実行する．最後のsiteの後は，最初のsiteから操作を実行する．<br>
     * 操作実行を行うsiteが調停者の役割を担う．<br>
     * <br>
     * シミュレーション実行中のメッセージ総数は，「site毎のメッセージ総数 * site数」で求める
     * @see Operation
     */
    /*
    private void runBehaviorOfTwoPhaseCommit() {
        int numberOfSites = this.opq.getNumberOfSites();
        int maxTurn = this.numberOfOperations * numberOfSites;
        for (int turn = 0; turn < maxTurn; ++turn) {
            if (turn % numberOfSites == this.id) { // 調停者の動作
                // step1: 参加者にコミットの準備を求める
                Operation request = new Operation(this.id, Operation.REQUEST, "");
                this.broadcast(request);
                this.numberOfSteps++;
                this.numberOfMessages += numberOfSites - 1;

                // step2: 参加者からの確認応答を待つ
                this.waitReceiveOperation(numberOfSites - 1);
                this.numberOfSteps++;

                // step3: 操作を全員に送信する
                Operation op = this.generateRandomOperation();
                //this.chainVoxel.apply(op); // local operation(ローカルサイトのChainVoxelを更新)
                this.broadcast(op); // remote operation
                this.numberOfSteps++;
                this.numberOfMessages += numberOfSites - 1;

            } else { // 参加者の動作
                // step1: requestを待つ
                this.waitReceiveOperation(1);
                this.numberOfSteps++;

                // step2: 確認応答を返す
                Operation ack = new Operation(this.id, Operation.ACK, "");
                this.send(turn % numberOfSites, ack);
                this.numberOfSteps++;
                this.numberOfMessages += 1;

                // step3: 操作を適用する
                Operation op = null;
                while (true) {
                    op = this.waitReceiveOperation(1).get(0);
                    if (op.getOpType() == Operation.INSERT || op.getOpType() == Operation.DELETE) {
                        break;        
                    }
                    this.send(this.id, op); // 先行した調停者のREQUESTだったので元に戻す。
                }
                //this.chainVoxel.apply(op); //(ローカルサイトのChainVoxelを更新)
                this.numberOfSteps++;
            }
        }

        return;
    }
    */

    /**
     * ChainVoxel時のSiteの振る舞いを実行する．<br>
     * <br>
     * シミュレーション実行中のメッセージ総数は，「site毎のメッセージ総数 * site数」で求める
     * @see ChainVoxel
     * @see Operation
     */
    /*
    private void runBehaviorOfChainVoxel() {
        int numberOfSites = this.opq.getNumberOfSites();
        for (int i = 0; i < this.numberOfOperations; ++i) {
            Operation op = this.generateRandomOperation();
            this.send(this.id, op); // local operation
            this.broadcast(op); // remote operation
            this.numberOfSteps++;
            this.numberOfMessages += numberOfSites - 1;
        }

        return;
    }
    */

    /**
     * ChainVoxelの構造層のテストをする
     * @see ChainVoxel
     * @see Operation
     * @see StructureTable
     */
    /*
    private void runBehaviorOfChainVoxelForStructureLayer() {
        int numberOfSites = this.opq.getNumberOfSites();
        for (int i = 0; i < this.numberOfOperations; ++i) {
            Operation op = this.randomOperation();
            //this.send(this.id, op); // local operation
            //this.chainVoxel.apply(op); // local operation(ローカルサイトのChainVoxelを更新)
            this.broadcast(op); // remote operation
            this.numberOfSteps++;
            this.numberOfMessages += numberOfSites - 1;
        }
        return;
    }
    */

    /**
     * Siteの動作を記述するメソッド．
     * {@inheritDoc}
     */
    @Override
    public void run() {    
        try { this.open(); }
        catch(IOException anException) { anException.printStackTrace(); }

        String res = "";
        Gson gson = new Gson();
        try {
            while(true)
            {
                res = this.receive();

                if (res.indexOf("EXIT") > -1) { break; }
                System.out.println(res);
                Operation op = gson.fromJson(res, Operation.class);
                this.send(op.getSID(), res); //別の宛先へ
            }
        }
        catch(IOException anException) { /*anException.printStackTrace();*/ }
        finally { this.close(); }

        // this.delay();
        
        //this.runBehaviorOfChainVoxel();
        // this.runBehaviorOfTwoPhaseCommit();
        // this.runBehaviorOfRaft();        

        // this.runBehaviorOfChainVoxelForStructureLayer();

        return;
    }

    /**
     * クライアントとの接続を閉じる。
     */
    private void close() {
        if(this.reader != null) {
            try { this.reader.close(); }
            catch(IOException anException){ anException.printStackTrace(); }
        }
        if(this.writer != null) {
            try { this.writer.close(); }
            catch(IOException anException){ anException.printStackTrace(); }
        }
        if(this.socket != null) {
            try { this.socket.close(); }
            catch(IOException anException){ anException.printStackTrace(); }
        }
        return;
    }

    public BufferedReader getReader() { return this.reader; }
    public BufferedWriter getWriter() { return this.writer; }
}
