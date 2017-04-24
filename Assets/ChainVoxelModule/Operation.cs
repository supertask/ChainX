using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
 * 操作を表すクラス．<br>
 * Operationクラスを利用して，ChainVoxelクラスの操作を実行する．<br>
 * Operationクラスは内部状態の変更をされてはいけないため，setterを実装しない．
 * @author kengo92i
 */
//[JsonObject("Operation")]
//[System.Serializable]
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

	/**
     * 操作のパラメータを保持するマップ
     */
	public JSONObject opParams;

	/**
     * 指定されたタイプの操作オブジェクトを作成する．<br>
     * 操作を作成する場合は，操作の種類とパラメータ値を引数に与える．
     * 操作に必要なパラメータを満たしていない場合は異常終了させる．
     * @param opType 操作のタイプ
     * @param opParams パラメータを保持するマップ
     * @see Operation#satisfyRequirements
     */
	public Operation(int sid, int opType, string opParamsJson) {
		this.sid = sid;
		this.opType = opType;
		this.timestamp = Util.currentTimeNanos();
		this.opParams = new JSONObject(opParamsJson);
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
			new List<string>() {"posID"}, // insert
			new List<string>() {"posID"}, // delete
			new List<string>() {"gid"}, // create
			new List<string>() {"posID", "gid"}, // join
			new List<string>() {"posID", "gid"}, // leave
			new List<string>() {"posID","destPosID"} // move
		};
		return this.opParams.HasFields(requirements[this.opType].ToArray() );
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
	public string getPosID() {
		return this.opParams.HasField("posID") ? this.opParams.GetField("posID").str : "";
	}

	/**
     * 移動先voxelの識別子を返す．
     * @return voxelの識別子
     */
	public string getDestPosID() {
		return this.opParams.HasField ("destPosID") ? this.opParams.GetField ("destPosID").str : ""; 
	}

	/**
     * Group IDを返す．
     * @return voxelの識別子
     */
	public string getGID() {
		return this.opParams.HasField("gid") ? this.opParams.GetField("gid").str : "";
	}

	/**
     * パラメータのインスタンスを取得する
     * @return パラメータのインスタンス
     */
	public JSONObject getParams() { return this.opParams; }

	/**
     * Jsonの文字列を元にOperationのインスタンスを作成する。
     * @param jsonMessage
     */
	public static Operation FromJson(string jsonMessage) {
		JSONObject j = new JSONObject(jsonMessage);
		int sid = int.Parse(j.GetField ("sid").str);
		int opType = int.Parse(j.GetField ("opType").str);
		JSONObject opParams = j.GetField ("opParams");
		long ts = long.Parse(j.GetField ("ts").str);
		Operation op = new Operation (sid, opType, opParams.Print());
		op.setTimestamp(ts);

		return op;
	}
	public static string ToJson(Operation op) {
		JSONObject j = new JSONObject ();
		j.AddField ("sid", op.getSID().ToString());
		j.AddField ("ts", op.getTimestamp().ToString());
		j.AddField ("opType", op.getOpType().ToString());
		j.AddField("opParams", op.getParams());
		return j.Print();
	}


	private static List<int> operation_types = new List<int>() {
		Operation.INSERT, Operation.DELETE, Operation.CREATE, Operation.JOIN,
		Operation.LEAVE, Operation.MOVE
	};

	public static string createRandomPosID() {
		int x = UnityEngine.Random.Range (0, int.MaxValue);
		int y = UnityEngine.Random.Range (0, int.MaxValue);
		int z = UnityEngine.Random.Range (0, int.MaxValue);
		return  String.Format("{0}:{1}:{2}", x, y, z);	
	}

	public static Operation CreateRandomOperation() {
		int sid = UnityEngine.Random.Range (0, int.MaxValue);
		int opIndex = UnityEngine.Random.Range (0, Operation.operation_types.Count);
		string posID = Operation.createRandomPosID();
		string destPosID = Operation.createRandomPosID();
		string gid = Guid.NewGuid().ToString ("N");

		JSONObject j = new JSONObject();
		switch (Operation.operation_types [opIndex]) {
			case Operation.INSERT:
			case Operation.DELETE:
				j.AddField ("posID", posID);
				break;
			case Operation.MOVE:
				j.AddField ("posID", posID);
				j.AddField ("destPosID", destPosID);
				break;
			case Operation.CREATE:
				j.AddField ("gid", gid);
				break;
			case Operation.JOIN:
			case Operation.LEAVE:
				j.AddField ("posID", posID);
				j.AddField ("gid", gid);
				break;
			default:
				throw new System.InvalidOperationException (
					String.Format("Operation{0} at CreateRandomOperation()",
					Operation.operation_types[opIndex])
				);
		}
		Operation op = new Operation (sid, Operation.operation_types [opIndex], j.Print () );
		Debug.Assert(op.getSID() == sid);
		Debug.Assert(op.getOpType() == Operation.operation_types[opIndex]);
		Debug.Assert(op.getTimestamp() > 0);
		if (op.getGID() != "") { Debug.Assert(op.getGID() == gid); }
		if (op.getPosID() != "") { Debug.Assert(op.getPosID() == posID); }
		if (op.getDestPosID() != "") { Debug.Assert(op.getDestPosID() == destPosID); }

		return op;
	}

	/**
	 * Test an Operation class
	 */
	public static void Test() {
		Debug.Assert(Operation.INSERT == 0);
		Debug.Assert(Operation.DELETE == 1);
		Debug.Assert(Operation.CREATE == 2);
		Debug.Assert(Operation.JOIN == 3);
		Debug.Assert(Operation.LEAVE == 4);
		Debug.Assert(Operation.MOVE == 5);

		/*
		 * Jsonへの変換とその逆ができているかをチェック
		 * Operationへの初期値の値の変化がないかをチェック at CreateRandomOperation()
		 */
		string json = "";
		Operation o1, o2;
		for (int i = 0; i < 1000; ++i) {
			o1 = Operation.CreateRandomOperation ();
			json = Operation.ToJson (o1);
			o2 = Operation.FromJson (json);
			Debug.Assert (o1.getSID () == o2.getSID ());
			Debug.Assert (o1.getOpType () == o2.getOpType ());
			Debug.Assert (o1.getTimestamp () == o2.getTimestamp ());

			switch (o1.getOpType ()) {
				case Operation.INSERT:
				case Operation.DELETE:
					Debug.Assert (o1.getPosID () == o2.getPosID ());
					break;
				case Operation.MOVE:
					Debug.Assert (o1.getPosID () == o2.getPosID ());
					Debug.Assert (o1.getDestPosID () == o2.getDestPosID ());
					break;
				case Operation.CREATE:
					Debug.Assert (o1.getGID () == o2.getGID ());
					break;
				case Operation.JOIN:
				case Operation.LEAVE:
					Debug.Assert (o1.getGID () == o2.getGID ());
					Debug.Assert (o1.getPosID () == o2.getPosID ());
					break;
				default:
					throw new System.InvalidOperationException (
						String.Format("Operation{0} at CreateRandomOperation()",
						o1.getOpType())
					);
			}
		}
	}
}

