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
     * insertAll操作を示す定数
     */
	public const int INSERT_ALL = 6;

	/**
     * delete操作を示す定数
     */
	public const int DELETE_ALL = 7;

	/**
     * join操作を示す定数
     */
	public const int JOIN_ALL = 8;

	/**
     * leave操作を示す定数 
     */
	public const int LEAVE_ALL = 9;

	/**
     * move操作を示す定数
     */
	public const int MOVE_ALL = 10;

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
			Debug.LogError("Operation type = " + opType);
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
			new List<string>() {"posID", "textureType"}, // insert
			new List<string>() {"posID"}, // delete
			new List<string>() {"gid"}, // create
			new List<string>() {"posID", "gid"}, // join
			new List<string>() {"posID", "gid"}, // leave
			new List<string>() {"posID","transMatrix"}, // move

			new List<string>() {"posIDs", "textureTypes"}, // insertAll
			new List<string>() {"posIDs"}, // deleteAll
			new List<string>() {"posIDs", "gid"}, // joinAll
			new List<string>() {"posIDs", "gid"}, // leaveAll
			new List<string>() {"posIDs","transMatrix"} // moveAll
		};
		/*
		Debug.Log(requirements[this.opType].Count);
		Debug.Log(requirements[this.opType][0]);
		Debug.Log(requirements[this.opType][1]);
		*/
		return this.opParams.HasFields(requirements[this.opType].ToArray() );
	}

	public void setTimestamp(long timestamp) { this.timestamp = timestamp; }

	public void setTextureType(int textureType) {
		this.opParams.AddField ("textureType", textureType.ToString());
	}
	public void setTextureTypes(List<int> textureTypes) {
		Debug.LogError("工事中");
		//this.opParams.AddField ("textureType", textureType.ToString());
	}

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
     * Voxelの識別子を返す．
     * @return voxelの識別子
     */
	public string getPosID() {
		return this.opParams.HasField("posID") ? this.opParams.GetField("posID").str : "";
	}

	/**
     * Voxelの識別子のリストを返す．
     * @return voxelの識別子
     */
	public string[] getPosIDs() {
		if (this.opParams.HasField("posIDs")) {
			return this.opParams.GetField("posIDs").str.Split(Const.SPLIT_CHAR);
		}
		else return null;
	}

	/**
     * Voxelの識別子のリストを返す．
     * @return voxelの識別子
     */
	public string getTransMatrix() {
		return this.opParams.HasField("transMatrix") ? this.opParams.GetField("transMatrix").str : "";
	}

	/*
	 * 
	 */
	public static string CombinePosition(string posID, string transMatrix) {
		string[] posIDStrs = posID.Split(':');
		string[] transMatrixStrs = transMatrix.Split(':');
		int x = int.Parse(posIDStrs[0]) + int.Parse(transMatrixStrs[0]);
		int y = int.Parse(posIDStrs[1]) + int.Parse(transMatrixStrs[1]);
		int z = int.Parse(posIDStrs[2]) + int.Parse(transMatrixStrs[2]);
		string destPosID = x.ToString() + ':' + y.ToString() + ':' + z.ToString();
		return destPosID;
	}

	/**
     * 移動先voxelの識別子を返す．
     * @return voxelの識別子
     */
	public string getDestPosID() {
		string transMatrix = this.getTransMatrix();
		if (transMatrix == string.Empty) return string.Empty;
		string posID = this.getPosID();
		if (posID == string.Empty) return string.Empty;

		return Operation.CombinePosition(this.getPosID(), this.getTransMatrix());
	}


	/**
     * 移動先voxelの識別子のリストを返す．
     * @return voxelの識別子
     */
	public string[] getDestPosIDs() {
		string transMatrix = this.getTransMatrix();
		if (transMatrix == string.Empty) return null;

		string[] posIDs = this.getPosIDs();
		if (posIDs == null) return null;

		List<string> destPosIDs = new List<string>();
		foreach(string posID in posIDs)
		{
			destPosIDs.Add(Operation.CombinePosition(posID, transMatrix));
		}
		return destPosIDs.ToArray();
	}

	/**
     * Voxelのテクスチャ番号を返す．
     * @return テクスチャ番号
     */
	public string getTextureType() {
		return this.opParams.HasField ("textureType") ? this.opParams.GetField ("textureType").str : ""; 
	}

	/**
     * Voxelのテクスチャ番号のリストを返す．
     * @return テクスチャ番号
     */
	public string getTextureTypes() {
		return this.opParams.HasField ("textureTypes") ? this.opParams.GetField ("textureTypes").str : ""; 
	}

	/**
     * Group IDを返す．
     * @return voxelの識別子
     */
	public string getGID() {
		return this.opParams.HasField("gid") ? this.opParams.GetField("gid").str : "";
	}

	/**
     * Group IDのリストを返す．
     * @return voxelの識別子
     */
	public string getGIDs() {
		return this.opParams.HasField("gids") ? this.opParams.GetField("gids").str : "";
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
		Operation.LEAVE, Operation.MOVE , Operation.MOVE_ALL, Operation.JOIN_ALL
	};


	public static string CreateRandomPosID(int minValue, int maxValue) {
		int x = UnityEngine.Random.Range (minValue, maxValue);
		int y = UnityEngine.Random.Range (minValue, maxValue);
		int z = UnityEngine.Random.Range (minValue, maxValue);
		return  String.Format("{0}:{1}:{2}", x, y, z);	
	}

	public static string CreateRandomPosID() {
		return Operation.CreateRandomPosID (int.MinValue, int.MaxValue);
	}

	public static string GetPosIDsFrom(string[] posIDs) {
		string res = "";
		foreach (string posID in posIDs) {
			res += posID + Const.SPLIT_CHAR;
		}
		return res.TrimEnd(Const.SPLIT_CHAR);	
	}

	public static Operation CreateRandomOperation()
	{
		int intMin = int.MinValue/2+1;
		int intMax = int.MaxValue/2-1;

		int sid = UnityEngine.Random.Range (0, int.MaxValue);
		int opIndex = UnityEngine.Random.Range (0, Operation.operation_types.Count); //どのOperationか
		string gid = Guid.NewGuid().ToString ("N");
		int numberOfPosIDs = UnityEngine.Random.Range (1, 10);

		string[] posIDs = new string[numberOfPosIDs];
		string[] destPosIDs = new string[numberOfPosIDs];
		string transMatrix = Operation.CreateRandomPosID(intMin, intMax);
		int[] textureTypes = new int[numberOfPosIDs];
		for(int i=0; i<numberOfPosIDs; ++i)
		{
			posIDs[i] = Operation.CreateRandomPosID(intMin, intMax);
			destPosIDs[i] = Operation.CombinePosition(posIDs[i], transMatrix);
			textureTypes[i] = UnityEngine.Random.Range (0, Const.NUMBER_OF_TEXTURE-1);
		}

		JSONObject j = new JSONObject();
		switch (Operation.operation_types [opIndex]) {
		case Operation.INSERT:
			j.AddField ("posID", posIDs[0]);
			j.AddField ("textureType", textureTypes[0]);
			break;
		case Operation.DELETE:
			j.AddField ("posID", posIDs[0]);
			break;
		case Operation.CREATE:
			j.AddField ("gid", gid);
			break;
		case Operation.JOIN:
		case Operation.LEAVE:
			j.AddField ("posID", posIDs[0]);
			j.AddField ("gid", gid);
			break;
		case Operation.MOVE:
			j.AddField ("posID", posIDs[0]);
			j.AddField ("transMatrix", transMatrix);
			break;
		case Operation.JOIN_ALL:
		case Operation.LEAVE_ALL:
			j.AddField ("posIDs", Operation.GetPosIDsFrom(posIDs));
			j.AddField ("gid", gid);
			break;
		case Operation.MOVE_ALL:
			j.AddField ("posIDs", Operation.GetPosIDsFrom(posIDs));
			j.AddField ("transMatrix", transMatrix);
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
		if (op.getGID() != string.Empty) { Debug.Assert(op.getGID() == gid); }
		if (op.getPosID() != string.Empty) { Debug.Assert(op.getPosID() == posIDs[0]); }
		if (op.getDestPosID() != string.Empty) { Debug.Assert(op.getDestPosID() == destPosIDs[0]); }
		if (op.getTransMatrix() != string.Empty) {
			Debug.Assert(op.getTransMatrix() == transMatrix);
		}
		if (op.getDestPosIDs() != null) { Debug.Assert(Operation.GetPosIDsFrom(op.getDestPosIDs()) == Operation.GetPosIDsFrom(destPosIDs)); }

		return op;
	}

	/**
	 * Test an Operation class
	 */
	public static void Test()
	{
		int numberOfTest = 100;

		//
		// A test for Operation.CombinePosition().
		//
		for (int i = 0; i < numberOfTest; ++i) {
			int minInt = -10000, maxInt = 10000;
			Vector3 posIDVector = new Vector3(
				UnityEngine.Random.Range (minInt, maxInt), 
				UnityEngine.Random.Range (minInt, maxInt), 
				UnityEngine.Random.Range (minInt, maxInt)
			);
			Vector3 transMatrixVector = new Vector3(
				UnityEngine.Random.Range (minInt, maxInt), 
				UnityEngine.Random.Range (minInt, maxInt), 
				UnityEngine.Random.Range (minInt, maxInt)
			);
			string combinedPosID = ChainXModel.CreatePosID(posIDVector + transMatrixVector);
			Debug.Assert (
				combinedPosID == Operation.CombinePosition(
					ChainXModel.CreatePosID(posIDVector), ChainXModel.CreatePosID(transMatrixVector)
				)
			);
		}

		//
		// Jsonへの変換とその逆ができているかをチェック
		// Operationへの初期値の値の変化がないかをチェック at CreateRandomOperation()
		//
		string json = "";
		Operation o1, o2;
		for (int i = 0; i < numberOfTest; ++i) {
			o1 = Operation.CreateRandomOperation ();
			json = Operation.ToJson (o1);
			o2 = Operation.FromJson (json);
			Debug.Assert (o1.getSID () == o2.getSID ());
			Debug.Assert (o1.getOpType () == o2.getOpType ());
			Debug.Assert (o1.getTimestamp () == o2.getTimestamp ());

			switch (o1.getOpType ()) {
			case Operation.INSERT:
				Debug.Assert (o1.getPosID () == o2.getPosID ());
				Debug.Assert (o1.getTextureType() == o2.getTextureType());
				break;
			case Operation.DELETE:
				Debug.Assert (o1.getPosID () == o2.getPosID ());
				break;
			case Operation.MOVE:
				Debug.Assert (o1.getPosID () == o2.getPosID ());
				Debug.Assert (o1.getTransMatrix() == o2.getTransMatrix());
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
			case Operation.JOIN_ALL:
			case Operation.LEAVE_ALL:
				Debug.Assert (o1.getGID () == o2.getGID ());
				Debug.Assert (Operation.GetPosIDsFrom(o1.getPosIDs()) == Operation.GetPosIDsFrom(o2.getPosIDs()) );
				break;
			case Operation.MOVE_ALL:
				Debug.Assert (Operation.GetPosIDsFrom(o1.getPosIDs()) == Operation.GetPosIDsFrom(o2.getPosIDs()) );
				Debug.Assert (o1.getTransMatrix() == o2.getTransMatrix());
				Debug.Assert (Operation.GetPosIDsFrom(o1.getDestPosIDs()) == Operation.GetPosIDsFrom(o2.getDestPosIDs()) );
				break;
			default:
				throw new System.InvalidOperationException (
					String.Format("Operation{0} at CreateRandomOperation()",
					o1.getOpType())
				);
			}
		}
		Debug.Log("End an Operation class test");
	}
}