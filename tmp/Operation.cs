using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using MinimalJson;

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
    public const int INSERT = 0;

    /**
     * delete操作を示す定数
     */
    public const int DELETE = 1;

    /**
     * create操作を示す定数
     */
    public const int CREATE = 2;

    /**
     * join操作を示す定数
     */
    public const int JOIN = 3;

    /**
     * leave操作を示す定数 
     */
    public const int LEAVE = 4;

    /**
     * move操作を示す定数
     */
    public const int MOVE = 5;

    /**
     * 操作を行なったSiteの識別子
     */
    private int sid;

    /**
     * 操作オブジェクトが表す操作を指定する整数．<br>
     * プリミティブ層の操作は{@link Operation#INSERT INSERT}と{@link Operation#DELETE DELETE}が存在．
     * 構造層の操作は{@link Operation#CREATE CREATE}，{@link Operation#JOIN JOIN}と{@link Operation#LEAVE LEAVE}が存在．
     */
    private int opType; // 0:insert, 1:delete, 2:create, 3:join, 4:leave, 5:move

    /**
     * 操作のタイムスタンプ（作成時に自動的に設定される）
     */
    private long timestamp;

    private string posID;

    private string destPosID;

    private string gid;

    /**
     * 指定されたタイプの操作オブジェクトを作成する．<br>
     * 操作を作成する場合は，操作の種類とパラメータ値を引数に与える．
     * 操作に必要なパラメータを満たしていない場合は異常終了させる．
     * @param opType 操作のタイプ
     * @param opParams パラメータを保持するマップ
     * @see Operation#satisfyRequirements
     */
    public Operation(int sid, int opType, string posID, string destPosID, string gid) {
        this.sid = sid;
        this.opType = opType;
        this.timestamp = Util.currentTimeNanos();
        this.posID = posID;
        this.destPosID = destPosID;
        this.gid = gid;
    }

    public void setTimestamp(long timestamp) { this.timestamp = timestamp; }

    /* Not exist setter method as much as possible. Because, class field should not be changed since init. */

    /**
    * 操作を行なったSiteの識別子を返す．
    * @return Siteの識別子
    */
    public int getSID() { return this.sid; }

    /**
     * 操作のタイプを返す．
     * @return 操作のタイプを示す整数
     */
    public int getOpType() { return this.opType; }

    /**
     * 操作のタイムスタンプを返す．
     * @return 操作のタイムスタンプ
     */
    public long getTimestamp() { return this.timestamp; }

    /**
     * voxelの識別子を返す．
     * @return voxelの識別子
     */
    public string getPosID() { return this.posID; } 

    /**
     * 移動先voxelの識別子を返す．
     * @return voxelの識別子
     */
    public string getDestPosID() { return this.destPosID; }

    /**
     * Group IDを返す．
     * @return voxelの識別子
     */
    public string getGID() { return this.gid; }

    /*
     * フィールドの状態を可視化
     */
    public void show() {
        Console.WriteLine(Operation.ToJson(this));
    }


    public static Operation FromJson(string jsonMessage)
    {
        JsonObject jsonObject = JsonObject.readFrom(jsonMessage);
        int sid = jsonObject.get("sid").asInt();
        int opType = jsonObject.get("opType").asInt();
        long timestamp = jsonObject.get("timestamp").asLong();
        string posID = jsonObject.get("posID").asString();
        string destPosID = jsonObject.get("destPosID").asString();
        string gid = jsonObject.get("gid").asString();
        Operation op = new Operation(sid, opType, posID, destPosID, gid);
        op.setTimestamp(timestamp);

        return op;
    }

    public static string ToJson(Operation op) {
        string res = "{";
        res += "\"sid\":" + op.getSID() + ",";
        res += "\"opType\":" + op.getOpType() + ",";
        res += "\"timestamp\":" + op.getTimestamp() + ",";
        res += "\"posID\":" + op.getPosID() + ",";
        res += "\"destPosID\":" + op.getDestPosID() + ",";
        res += "\"gid\":" + op.getGID() + "}";

        return res;
    }
}

