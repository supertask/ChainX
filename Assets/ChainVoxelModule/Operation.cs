using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
     * appendEntriesを示す定数（Raftのために使用）
     * @see Site#runBehaviorOfRaft
     */
	public const int APPEND_ENTRIES = 124;

	/**
     * requestVoteを示す定数（Raftのために使用）
     * @see Site#runBehaviorOfRaft
     */
	public const int REQUEST_VOTE = 125;

	/**
     * VOTEを示す定数（Raftのために使用） 
     * @see Site#runBehaviorOfRaft
     */
	public const int VOTE = 126;

	/**
     * requestを示す定数（２層コミットのために使用）
     * @see Site#runBehaviorOfTwoPhaseCommit
     */
	public const int REQUEST = 127;

	/**
     * 確認応答を示す定数（２層コミットのために使用） 
     * @see Site#runBehaviorOfTwoPhaseCommit
     */
	public const int ACK = 128;

	/**
     * 操作を行なったSiteの識別子
     */
	private int id = -1;

	/**
     * 操作オブジェクトが表す操作を指定する整数．<br>
     * プリミティブ層の操作は{@link Operation#INSERT INSERT}と{@link Operation#DELETE DELETE}が存在．
     * 構造層の操作は{@link Operation#CREATE CREATE}，{@link Operation#JOIN JOIN}と{@link Operation#LEAVE LEAVE}が存在．
     */
	private int opType; // 0:insert, 1:delete, 2:create, 3:join, 4:leave, 5:move

	/**
     * voxelの識別子を示す文字列（形式: "X:Y:Z"）
     */
	private string posID;

	/**
     * voxelの宛先識別子を示す文字列（形式: "X:Y:Z"）
     */
	private string destPosID;

	/**
     * groupの識別子を示す文字列（形式: v4 UUID）
     */
	private string gid;

	/**
     * 操作のタイムスタンプ（作成時に自動的に設定される）
     */
	private long timestamp;


	/**
     * 操作のパラメータを保持するマップ
     */
	private SortedDictionary<string, object> opParams;

	/**
     * 指定されたタイプの操作オブジェクトを作成する．
     * @deprecated プリミティブ操作以外にも対応したコンストラクタ {@link #Operation(int, java.util.Map)}
     * @param id 操作を作成したSiteの識別子
     * @param opType 操作のタイプ
     * @param posID voxelの識別子
     * @param destPosID 移動先voxelの識別子
     */
	public Operation(int id, int opType, string posID): this(id, opType, posID, "") { }


	/**
     * 指定されたタイプの操作オブジェクトを作成する．
     * @deprecated プリミティブ操作以外にも対応したコンストラクタ {@link #Operation(int, java.util.Map)}
     * @param id 操作を作成したSiteの識別子
     * @param opType 操作のタイプ
     * @param posID voxelの識別子
     * @param destPosID 移動先voxelの識別子
     */
	public Operation(int id, int opType, string posID, string destPosID) {
		this.id = id;
		this.opType = opType;
		this.timestamp = Util.currentTimeNanos();
		this.posID = posID;
		this.destPosID = destPosID;
	}

	/**
     * 指定されたタイプの操作オブジェクトを作成する．<br>
     * 操作を作成する場合は，操作の種類とパラメータ値を引数に与える．
     * 操作に必要なパラメータを満たしていない場合は異常終了させる．
     * @param opType 操作のタイプ
     * @param opParams パラメータを保持するマップ
     * @see Operation#satisfyRequirements
     */
	public Operation(int opType, SortedDictionary<string, object> opParams) {
		this.opType = opType;
		this.timestamp = Util.currentTimeNanos();
		opParams["ts"] = this.timestamp;
		this.opParams = opParams;
		if (!this.satisfyRequirements()) {
			throw new System.InvalidOperationException("Insufficient parameters for operation.");
		}
	}

	/**
     * 必要なパラメータを満たしているか判定する． <br>
     * 新しい操作を定義する場合は，操作に必要なパラメータ条件を追加する．
     * 操作に必要なパラメータを満たしている場合はtrueを返す．満たしていない場合はfalseを返す．
     * @return 操作に必要なパラメータを満たしているかの真偽値
     */
	private bool satisfyRequirements() {
		List<List<string>> requirements = new List<List<string>>() {
			new List<string>() {"sid", "ts", "posID"}, // insert
			new List<string>() {"sid", "ts", "posID"}, // delete
			new List<string>() {"ts", "gid"}, // create
			new List<string>() {"ts", "posID", "gid"}, // join
			new List<string>() {"sid", "ts", "posID", "gid"}, // leave
			new List<string>() {"sid", "ts", "posID","destPosID"} // move
		};

		foreach (string requirement in requirements[this.opType]) {
			if (!this.opParams.ContainsKey(requirement)) { return false; }
		}

		return true;
	}

	/* Not exist setter method. Because, class field should not be changed since init. */

	/**
	* 操作を行なったSiteの識別子を返す．
	* @return Siteの識別子
	*/
	public int getId() { return this.id != -1 ? this.id : (int) this.opParams["sid"]; }

	/**
     * 操作のタイプを返す．
     * @return 操作のタイプを示す整数
     */
	public int getOpType() { return this.opType; }

	/**
     * voxelの識別子を返す．
     * @return voxelの識別子
     */
	public string getPosID() { return this.posID != null ? this.posID : (string) this.opParams["posID"]; }

	/**
     * 移動先voxelの識別子を返す．
     * @return voxelの識別子
     */
	public string getDestPosID() { return this.destPosID != null ? this.destPosID : (string) this.opParams["destPosID"]; }

	/**
     * 操作のタイムスタンプを返す．
     * @return 操作のタイムスタンプ
     */
	public long getTimestamp() { return this.timestamp; }

	/**
     * 指定したパラメータの値を取得する
     * @param name パラメータ名
     * @return パラメータの値
     */
	public object getParam(string name) { return this.opParams[name]; }
}