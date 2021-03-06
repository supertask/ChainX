﻿using System;
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
     * insertPolygon操作を示す定数
     */
	public const int INSERT_POLYGON = 11;

	/**
     * deletePolygon操作を示す定数
     */
	public const int DELETE_POLYGON = 12;

	/**
     * movePolygon操作を示す定数
     */
	public const int MOVE_POLYGON = 13;

	private static List<int> operation_types = new List<int>() {
		Operation.INSERT, Operation.DELETE, Operation.CREATE, Operation.JOIN,
		Operation.LEAVE, Operation.MOVE, 
		Operation.INSERT_ALL, Operation.DELETE_ALL, Operation.JOIN_ALL,
		Operation.LEAVE_ALL, Operation.MOVE_ALL, 
		Operation.INSERT_POLYGON, Operation.DELETE_POLYGON, Operation.MOVE_POLYGON
	};

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
			new List<string>() {"posID", "textureType"}, // insert(INSERT)
			new List<string>() {"posID"}, // delete(DELETE)
			new List<string>() {"gid"}, // create(CREATE)
			new List<string>() {"posID", "gid"}, // join(JOIN)
			new List<string>() {"posID", "gid"}, // leave(LEAVE)
			new List<string>() {"posID","transMatrix"}, // move(MOVE)

			new List<string>() {"posIDs", "textureTypes", "gid"}, // insertAll(INSERT_ALL)
			new List<string>() {"posIDs", "gid"}, // deleteAll(DELETE_ALL)
			new List<string>() {"posIDs", "gid"}, // joinAll(JOIN_ALL)
			new List<string>() {"posIDs", "gid"}, // leaveAll(LEAVE_ALL)
			new List<string>() {"posIDs", "transMatrix", "gid"}, // moveAll((MOVE_ALL)，廃止

			//重要！ posIDsの最後の要素だけがobjPathをもち，insertされる．それ以外は空要素
			new List<string>() {"posIDs", "objPath", "gid"}, // insertPolygon(INSERT_POLYGON)
			new List<string>() {"posIDs", "gid"}, // deletePolygon(DELETE_POLYGON)
			new List<string>() {"posIDs", "transMatrix", "gid"} // movePolygon((MOVE_POLYGON)，廃止
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
		Vector3 c = Util.SplitPosID(posID) + Util.SplitPosID(transMatrix);
		return c.x.ToString() + ':' + c.y.ToString() + ':' + c.z.ToString();
	}

	/**
     * 移動先voxelの識別子を返す．
     * @return voxelの識別子
     */
	public string getDestPosID() {
		return this.getDestPosID(this.getPosID());
	}

	public string getDestPosID(string posID) {
		string transMatrix = this.getTransMatrix();
		if (transMatrix == string.Empty) return string.Empty;
		if (posID == string.Empty) return string.Empty;

		return Operation.CombinePosition(posID, this.getTransMatrix());
	}


	public string[] getDestPosIDs() {
		return this.getDestPosIDs (this.getPosIDs ());
	}

	/**
     * 移動先voxelの識別子のリストを返す．
     * @return voxelの識別子
     */
	public string[] getDestPosIDs(string[] posIDs) {
		string transMatrix = this.getTransMatrix();
		if (transMatrix == string.Empty) return null;
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
	public int getTextureType() {
		return this.opParams.HasField ("textureType") ? int.Parse(this.opParams.GetField ("textureType").str) : -1; 
	}

	/**
     * Voxelのテクスチャ番号のリストを返す．
     * @return テクスチャ番号
     */
	public int[] getTextureTypes() {
		if (this.opParams.HasField("textureTypes")) {
			string[] textureTypes = this.opParams.GetField("textureTypes").str.Split(Const.SPLIT_CHAR);
			int[] intTextureTypes = new int[textureTypes.Length];
			//Debug.Log (this.opParams.GetField("textureTypes"));

			for (int i = 0; i < textureTypes.Length; ++i) {
				intTextureTypes [i] = int.Parse (textureTypes [i]);
			}
			return intTextureTypes;
		}
		else return null;

	}

	public string getObjPath() {
		return this.opParams.HasField("objPath") ? this.opParams.GetField("objPath").str : "";
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


	public static Operation CreateRandomOperation()
	{
		int intMin = -10000;
		int intMax = 10000;

		int sid = UnityEngine.Random.Range (0, int.MaxValue/2);
		int opIndex = UnityEngine.Random.Range (0, Operation.operation_types.Count); //どのOperationか
		string gid = Guid.NewGuid().ToString ("N");
		int numberOfPosIDs = UnityEngine.Random.Range (1, 10);

		string[] posIDs = new string[numberOfPosIDs];
		string[] destPosIDs = new string[numberOfPosIDs];
		string transMatrix = Util.CreateRandomPosID(-1, 2);
		string[] textureTypes = new string[numberOfPosIDs];
		string objPath = Guid.NewGuid ().ToString ("N").Substring(0, 12);

		for(int i=0; i<numberOfPosIDs; ++i)
		{
			posIDs[i] = Util.CreateRandomPosID(intMin, intMax);
			destPosIDs[i] = Operation.CombinePosition(posIDs[i], transMatrix);
			textureTypes[i] = UnityEngine.Random.Range (0, Const.NUMBER_OF_TEXTURE-1).ToString();
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
		case Operation.MOVE:
			j.AddField ("posID", posIDs[0]);
			j.AddField ("transMatrix", transMatrix);
			break;
		case Operation.INSERT_ALL:
			j.AddField ("posIDs", Util.GetCommaLineFrom(posIDs));
			j.AddField ("textureTypes", Util.GetCommaLineFrom(textureTypes));
			j.AddField ("gid", gid);
			break;
		case Operation.INSERT_POLYGON:
			j.AddField ("posIDs", Util.GetCommaLineFrom(posIDs));
			j.AddField ("objPath", objPath);
			j.AddField ("gid", gid);
			break;
		case Operation.DELETE_ALL:
			j.AddField ("posIDs", Util.GetCommaLineFrom(posIDs));
			j.AddField ("gid", gid);
			break;
		case Operation.DELETE_POLYGON:
			j.AddField ("posIDs", Util.GetCommaLineFrom(posIDs));
			j.AddField ("gid", gid);
			break;
		case Operation.MOVE_ALL:
			j.AddField ("posIDs", Util.GetCommaLineFrom(posIDs));
			j.AddField ("transMatrix", transMatrix);
			j.AddField ("gid", gid);
			break;
		case Operation.MOVE_POLYGON:
			j.AddField ("posIDs", Util.GetCommaLineFrom (posIDs));
			j.AddField ("transMatrix", transMatrix);
			j.AddField ("gid", gid);
			break;
		case Operation.CREATE:
			j.AddField ("gid", gid);
			break;
		case Operation.JOIN:
		case Operation.LEAVE:
			j.AddField ("posID", posIDs[0]);
			j.AddField ("gid", gid);
			break;
		case Operation.JOIN_ALL:
		case Operation.LEAVE_ALL:
			j.AddField ("posIDs", Util.GetCommaLineFrom(posIDs));
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
		if (op.getGID() != string.Empty) { Debug.Assert(op.getGID() == gid); }
		if (op.getPosID() != string.Empty) { Debug.Assert(op.getPosID() == posIDs[0]); }
		if (op.getDestPosID() != string.Empty) { Debug.Assert(op.getDestPosID() == destPosIDs[0]); }
		if (op.getTransMatrix() != string.Empty) {
			Debug.Assert(op.getTransMatrix() == transMatrix);
		}
		if (op.getDestPosIDs() != null) {
			Debug.Assert(Util.GetCommaLineFrom(op.getDestPosIDs()) == Util.GetCommaLineFrom(destPosIDs));
		}

		return op;
	}

	/**
	 * Test an Operation class
	 */
	public static void Test()
	{
		int numberOfTest = Const.TEST_QUALITY;

		//
		// A test for Operation.CombinePosition().
		//
		for (int t = 0; t < numberOfTest; ++t) {
			Vector3 posIDVector = Util.CreateRandomVector3 (-10000, 10000); 
			Vector3 transMatrixVector = Util.CreateRandomTransMatrix ();
			Debug.Assert (
				ChainXModel.CreatePosID(posIDVector + transMatrixVector)
				== Operation.CombinePosition(
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
		for (int t = 0; t < numberOfTest; ++t) {
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
				Debug.Assert (Util.GetCommaLineFrom(o1.getPosIDs()) == Util.GetCommaLineFrom(o2.getPosIDs()) );
				break;
			case Operation.MOVE_ALL:
				Debug.Assert (Util.GetCommaLineFrom(o1.getPosIDs()) == Util.GetCommaLineFrom(o2.getPosIDs()) );
				Debug.Assert (o1.getTransMatrix() == o2.getTransMatrix());
				Debug.Assert (Util.GetCommaLineFrom(o1.getDestPosIDs())
					== Util.GetCommaLineFrom(o2.getDestPosIDs()) );
				Debug.Assert (o1.getGID () == o2.getGID ());
				break;
			case Operation.INSERT_ALL:
				Debug.Assert (Util.GetCommaLineFrom(o1.getPosIDs()) == Util.GetCommaLineFrom(o2.getPosIDs()) );
				Debug.Assert (o1.getTextureType() == o2.getTextureType());
				Debug.Assert (o1.getGID () == o2.getGID ());
                break;
			case Operation.DELETE_ALL:
				Debug.Assert (Util.GetCommaLineFrom(o1.getPosIDs()) == Util.GetCommaLineFrom(o2.getPosIDs()) );
				Debug.Assert (o1.getGID () == o2.getGID ());
                break;
			case Operation.INSERT_POLYGON:
				Debug.Assert (Util.GetCommaLineFrom(o1.getPosIDs()) == Util.GetCommaLineFrom(o2.getPosIDs()) );
				Debug.Assert (o1.getObjPath() == o2.getObjPath());
				Debug.Assert (o1.getGID () == o2.getGID ());
				break;
			case Operation.DELETE_POLYGON:
				Debug.Assert (Util.GetCommaLineFrom(o1.getPosIDs()) == Util.GetCommaLineFrom(o2.getPosIDs()) );
				Debug.Assert (o1.getGID () == o2.getGID ());
				break;
			case Operation.MOVE_POLYGON:
				Debug.Assert (Util.GetCommaLineFrom (o1.getPosIDs ()) == Util.GetCommaLineFrom (o2.getPosIDs ()));
				Debug.Assert (o1.getTransMatrix () == o2.getTransMatrix ());
				Debug.Assert (Util.GetCommaLineFrom (o1.getDestPosIDs ())
				== Util.GetCommaLineFrom (o2.getDestPosIDs ()));
				Debug.Assert (o1.getGID () == o2.getGID ());
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
