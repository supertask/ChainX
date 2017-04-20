//javac -cp .:modules/gson-2.6.2.jar Operation.java
//java -cp .:modules/gson-2.6.2.jar Operation
import java.lang.IllegalStateException;
import java.util.Map;
import java.util.HashMap;
import com.google.gson.*;

/**
 * 操作を表すクラス．<br>
 * Operationクラスを利用して，ChainVoxelクラスの操作を実行する．<br>
 * Operationクラスは内部状態の変更をされてはいけないため，setterを実装しない．
 * @author kengo92i
 */
public class Operation {
    /**
     * insert操作を示す定数
     */
    public static final int INSERT = 0;

    /**
     * delete操作を示す定数
     */
    public static final int DELETE = 1;

    /**
     * create操作を示す定数
     */
    public static final int CREATE = 2;

    /**
     * join操作を示す定数
     */
    public static final int JOIN = 3;

    /**
     * leave操作を示す定数 
     */
    public static final int LEAVE = 4;

    /**
     * appendEntriesを示す定数（Raftのために使用）
     * @see Site#runBehaviorOfRaft
     */
    public static final int APPEND_ENTRIES = 124;

    /**
     * requestVoteを示す定数（Raftのために使用）
     * @see Site#runBehaviorOfRaft
     */
    public static final int REQUEST_VOTE = 125;

    /**
     * VOTEを示す定数（Raftのために使用） 
     * @see Site#runBehaviorOfRaft
     */
    public static final int VOTE = 126;

    /**
     * requestを示す定数（２層コミットのために使用）
     * @see Site#runBehaviorOfTwoPhaseCommit
     */
    public static final int REQUEST = 127;

    /**
     * 確認応答を示す定数（２層コミットのために使用） 
     * @see Site#runBehaviorOfTwoPhaseCommit
     */
    public static final int ACK = 128;

    /**
     * 操作を行なったSiteの識別子
     */
    private int id = -1;

    /**
     * 操作オブジェクトが表す操作を指定する整数．<br>
     * プリミティブ層の操作は{@link Operation#INSERT INSERT}と{@link Operation#DELETE DELETE}が存在．
     * 構造層の操作は{@link Operation#CREATE CREATE}，{@link Operation#JOIN JOIN}と{@link Operation#LEAVE LEAVE}が存在．
     */
    private int opType; // 0:insert, 1:delete, 2:create, 3:join, 4:leave

    /**
     * voxelの識別子を示す文字列（形式: "X:Y:Z"）
     */
    private String posID;

    /**
     * groupの識別子を示す文字列（形式: v4 UUID）
     */
    private String gid;

    /**
     * 操作のタイムスタンプ（作成時に自動的に設定される）
     */
    private long timestamp;


    /**
     * 操作のパラメータを保持するマップ
     */
    private Map<String, Object> params;

    /**
     * 指定されたタイプの操作オブジェクトを作成する．
     * @deprecated プリミティブ操作以外にも対応したコンストラクタ {@link #Operation(int, java.util.Map)}
     * @param id 操作を作成したSiteの識別子
     * @param opType 操作のタイプ
     * @param posID voxelの識別子
     */
    public Operation(int id, int opType, String posID) {
        this.id = id;
        this.opType = opType;
        this.timestamp = System.currentTimeMillis();
        this.posID = posID;
        this.params = null;
    }

    /**
     * 指定されたタイプの操作オブジェクトを作成する．<br>
     * 操作を作成する場合は，操作の種類とパラメータ値を引数に与える．
     * 操作に必要なパラメータを満たしていない場合は異常終了させる．
     * @param opType 操作のタイプ
     * @param params パラメータを保持するマップ
     * @see Operation#satisfyRequirements
     */
    public Operation(int opType, Map<String, Object> params) {
        this.opType = opType;
        this.timestamp = System.currentTimeMillis();
        params.put("ts", this.timestamp);
        this.params = params;
        if (!this.satisfyRequirements()) {
            throw new IllegalStateException("Insufficient parameters for operation.");
        }
    }

    /**
     * 必要なパラメータを満たしているか判定する． <br>
     * 新しい操作を定義する場合は，操作に必要なパラメータ条件を追加する．
     * 操作に必要なパラメータを満たしている場合はtrueを返す．満たしていない場合はfalseを返す．
     * @return 操作に必要なパラメータを満たしているかの真偽値
     */
    private boolean satisfyRequirements() {
        String[][] requirements = {
            {"sid", "ts", "posID"}, // insert
            {"sid", "ts", "posID"}, // delete
            {"ts", "gid"}, // create
            {"ts", "posID", "gid"}, // join
            {"sid", "ts", "posID", "gid"} // leave
        };

        for (String requirement : requirements[this.opType]) {
            if (!this.params.containsKey(requirement)) {
               return false;
            } 
        }

        return true;
    }

    /* Not exist setter method. Because, class field should not be changed since init. */

    /**
     * 操作を行なったSiteの識別子を返す．
     * @return Siteの識別子
     */
    public int getId() {
        return this.id != -1 ? this.id : (int) this.params.get("sid");
    }

    /**
     * 操作のタイプを返す．
     * @return 操作のタイプを示す整数
     */
    public int getOpType() {
        return this.opType;
    }

    /**
     * voxelの識別子を返す．
     * @return voxelの識別子
     */
    public String getPosID() {
        return this.posID != null ? this.posID : (String) this.params.get("posID");
    }

    /**
     * 操作のタイムスタンプを返す．
     * @return 操作のタイムスタンプ
     */
    public long getTimestamp() {
        return this.timestamp;
    }

    /**
     * 指定したパラメータの値を取得する
     * @param name パラメータ名
     * @return パラメータの値
     */
    public Object getParam(String name) {
        return this.params.get(name);
    }

    public static void main(String[] args)
    {
        Gson gson = new Gson();
        Operation o1 = new Operation(99, Operation.ACK, "1:1:1");

        Map<String, Object> params = new HashMap<String, Object>();
        params.put("sid", 52);
        params.put("posID", "2:2:1");
        /*
            {"sid", "ts", "posID"}, // insert
            {"sid", "ts", "posID"}, // delete
            {"ts", "gid"}, // create
            {"ts", "posID", "gid"}, // join
            {"sid", "ts", "posID", "gid"} // leave
        */
        Operation o2 = new Operation(Operation.DELETE, params);
        System.out.println(gson.toJson(o1));
        System.out.println(gson.toJson(o2));
    }
}
