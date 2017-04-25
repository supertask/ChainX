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
     * 操作を行なったSiteの識別子
     */
    private String sid;

    /**
     * 操作オブジェクトが表す操作を指定する整数．<br>
     * プリミティブ層の操作は{@link Operation#INSERT INSERT}と{@link Operation#DELETE DELETE}が存在．
     * 構造層の操作は{@link Operation#CREATE CREATE}，{@link Operation#JOIN JOIN}と{@link Operation#LEAVE LEAVE}が存在．
     */
    private String opType; // 0:insert, 1:delete, 2:create, 3:join, 4:leave

    /**
     * voxelの識別子を示す文字列（形式: "X:Y:Z"）
     */
    private String posID;

    /**
     * 操作のタイムスタンプ（作成時に自動的に設定される）
     */
    private String ts;


    /**
     * 操作のパラメータを保持するマップ
     */
    private Map<String,String> opParams;

    /**
     * 指定されたタイプの操作オブジェクトを作成する．
     * @deprecated プリミティブ操作以外にも対応したコンストラクタ {@link #Operation(int, java.util.Map)}
     * @param sid 操作を作成したSiteの識別子
     * @param opType 操作のタイプ
     * @param posID voxelの識別子
     */
    public Operation(String sid, String opType, Map<String,String> opParam) {
        this.sid = sid;
        this.opType = opType;
        this.ts = "";
        this.opParams = opParams;
    }

    /* Not exist setter method. Because, class field should not be changed since init. */

    /**
     * 操作を行なったSiteの識別子を返す．
     * @return Siteの識別子
     */
    public String getSID() {
        return this.sid;
    }

    /**
     * 操作のタイプを返す．
     * @return 操作のタイプを示す整数
     */
    public String getOpType() {
        return this.opType;
    }

    /**
     * 操作のタイムスタンプを返す．
     * @return 操作のタイムスタンプ
     */
    public String getTimestamp() {
        return this.ts;
    }

    /**
     * 指定したパラメータの値を取得する
     * @param name パラメータ名
     * @return パラメータの値
     */
    public Map<String,String> getOpParams() {
        return this.opParams;
    }

    public static void main(String[] args)
    {
    }
}
