using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * ChainVoxelを実装したクラス．<br>
 * <br>
 * negativeVoxelのvoxelチェインへの追加処理を簡略化するために，<br>
 * negativeVoxelをposIDに対応するvoxelチェインとは独立して管理する実装になっています．<br>
 * voxelチェインはこのクラスではvoxelのリストとして実装されています．<br>
 * <br>
 * K. Imae and N. Hayashibara, 
 * “ChainVoxel: A Data Structure for Scalable Distributed Collaborative Editing for 3D Models” 
 * The 14th IEEE International Conference on Dependable, Autonomic and Secure Computing, 8-12 Aug. 2016.
 *
 * @author kengo92i, supertask
 */
public class ChainVoxel {
	/**
     * posIDに対応するvoxelのリストを管理するSortedDictionary
     */
	private SortedDictionary<string, List<Voxel>> atoms;

	/**
     * posIDに対応する負のvoxelを管理するSortedDictionary
     */
	private SortedDictionary<string, Voxel> negativeVoxels;

	/**
     * 構造管理のためのStrutureTable
     */
	public StructureTable stt;

	private ChainXController controller;

	public List<string> insertedPosIDs;
	public List<string> deletedPosIDs;
	public Dictionary<string,string> movedPosIDs;
	public List<string> joinedGIDs;
	public List<string> leftGIDs;
	public string textPath;


	/**
     * ChainVoxelのコンストラクタ
     */
	public ChainVoxel(ChainXController controller) {
		this.atoms = new SortedDictionary<string, List<Voxel>>();
		this.negativeVoxels = new SortedDictionary<string, Voxel>();
		this.stt = new StructureTable();
		this.controller = controller;
		this.insertedPosIDs = new List<string>();
		this.deletedPosIDs = new List<string>();
		this.movedPosIDs = new Dictionary<string,string>();
		this.joinedGIDs = new List<string>();
		this.leftGIDs = new List<string>();
		if (!Directory.Exists (Const.SAVED_DIR)) Directory.CreateDirectory(Const.SAVED_DIR);
	}

	/**
     * 操作オブジェクトに対応する操作を実行するメソッド．<br>
     * ChainVoxelに対する操作はapplyメソッドを用いて実行することを推奨しています．
     * @param op 操作オブジェクト
     * @param textureType テクスチャ番号
     * @see Operation
     */
	public void apply (Operation op)
	{

		lock (ChainXController.thisLock) {

			string posID, destPosID;
			string[] posIDs;

			switch (op.getOpType ()) {
			case Operation.INSERT:
				if (this.stt.isGrouping (op.getPosID())) break;
				this.insert(op);
				break;
			case Operation.DELETE:
				if (this.stt.isGrouping(op.getPosID())) break;
				this.delete(op);
				break;
			case Operation.CREATE:
				this.create (op);
				break;
			case Operation.JOIN:
				this.join (op);
				break;
			case Operation.LEAVE:
				this.leave (op);
				break;
			case Operation.MOVE:
				posID = op.getPosID ();
				destPosID = op.getDestPosID(); //Here
				if (this.stt.isGrouping (posID)) break;
				if (this.stt.isGrouping (destPosID)) break;
				if (this.getVoxel(posID) == null) break; //For protecting to an operate empty voxel.
				if (this.getVoxel(destPosID) != null) break; //If a voxel already exists
				this.move(op, op.getTimestamp(), posID, destPosID);
				break;

			case Operation.INSERT_ALL:
				posIDs = op.getPosIDs ();
				if(this.isGroupingAll(posIDs)) break;
				this.insertAll(op);
				break;
			case Operation.INSERT_POLYGON:
				posIDs = op.getPosIDs ();
				if(this.isGroupingAll(posIDs)) break;
				this.insertPolygon(op);
				break;
				
			case Operation.DELETE_ALL:
				if (this.isGroupingAll (this.stt.getPosIDs(op.getGID ()) ))
					this.deleteAll (op, op.getTimestamp (), op.getGID (), op.getPosIDs());
				break;
			case Operation.DELETE_POLYGON:
				//Here
				posIDs = op.getPosIDs ();
				if(this.isGroupingAll(posIDs)) this.deletePolygon(op);
				break;

			case Operation.JOIN_ALL:
				this.joinAll (op, op.getTimestamp(), op.getGID(), op.getPosIDs());
				break;
			case Operation.LEAVE_ALL:
				this.leaveAll (op, op.getTimestamp(), op.getGID(), op.getPosIDs());
				break;
			case Operation.MOVE_ALL:
				posIDs = op.getPosIDs();
				if (this.isGroupingAll(posIDs)) this.moveAll (op);
				//foreach (GameObject anObj in this.controller.selectedObjects) { Debug.Log ("xxx" + anObj); }
				break;
			case Operation.MOVE_POLYGON:
				posIDs = op.getPosIDs();
				if (this.isGroupingAll(posIDs)) this.movePolygon(op);
				//foreach (GameObject anObj in this.controller.selectedObjects) { Debug.Log ("xxx" + anObj); }
				break;
			default:
				Debug.Assert (false);
				break;
			}
			this.controller.log = this.getStatusString ();
		}
		return;
	}

	private bool isGroupingAll(string[] posIDs) {
		for (int i = 0; i < posIDs.Length; ++i) {
			if (this.stt.isGrouping(posIDs[i])) {
				return true;
			}
		}
		return false;
	}


	public void insert(Operation op) {
		this.insert(op, op.getTimestamp(), op.getPosID(), op.getTextureType());
	}

	public void insert(Operation op, long timestamp, string posID, int textureType){
		this.insert(op, timestamp, posID, textureType, "");
	}

	public void insert(Operation op, long timestamp, string posID, string objPath) {
		this.insert(op, timestamp, posID, -1, objPath);
	}

	/**
     * ChainVoxel内にvoxelを挿入するメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void insert(Operation op, long timestamp, string posID, int textureType, string texturePath) {
		int id = op.getSID();
		Voxel insertVoxel;
		if (textureType < 0) {
			insertVoxel = new Voxel(id, texturePath, timestamp);
		}
		else {
			insertVoxel = new Voxel(id, textureType, timestamp);
		}

		List<Voxel> voxelList = this.getVoxelList(posID);

		// step1: 負のvoxelの影響があるか調べる
		// 負のvoxelより新しいtsの場合は以降の処理に進む，そうではない場合は，ここで終了
		if (this.negativeVoxels.ContainsKey(posID)) {
			//Debug.Log (this.negativeVoxels[posID]);

			if (this.negativeVoxels[posID].getTimestamp() >= timestamp) {
				return; // 負のvoxelより前に挿入する操作は無駄な操作であるため
			}
		}

		// step2: insertVoxelを挿入する
		voxelList.Add(insertVoxel);
		voxelList.Sort(Voxel.Compare);
		//Hereバグ: ここが原因！！！
		if (this.getVoxel (posID) != null) {
			if (  !(op.getOpType() == Operation.MOVE_ALL ||
					op.getOpType() == Operation.MOVE_POLYGON ||
					op.getOpType() == Operation.MOVE)) {
				this.insertedPosIDs.Add (posID);
				//Debug.Log ("insert():" + posID);	//動いている！！
			}
		}

		return;
	}


	public void delete(Operation op) {
		this.delete(op, op.getTimestamp(), op.getPosID());
	}

	/**
     * ChainVoxel内の指定したvoxelを削除するメソッド
     * @param op 操作オブジェクト
     * @return textureType 削除するVoxelのテクスチャ番号
     * @see Operation
     */
	public void delete(Operation op, long timestamp, string posID) {
		// step1: 負のvoxelをnegativeVoxelsに追加・更新
		if (!this.negativeVoxels.ContainsKey(posID) || this.negativeVoxels[posID].getTimestamp() < timestamp) {
			this.negativeVoxels[posID] = new Voxel(timestamp);
		}

		List<Voxel> voxelList = this.getVoxelList(posID);
		Voxel tmpVoxel = this.getVoxel (posID); //NULL(06/03/2017)
		int textureType = tmpVoxel.getTextureType ();

		// step2: 負のvoxelより古いvoxelを削除する
		for (int i = voxelList.Count - 1; i >= 0; --i) { // 先頭から削除するとイテレータがおかしくなる
			//Debug.Log(voxelList[i]);
			if (this.negativeVoxels[posID].getTimestamp() >= voxelList[i].getTimestamp()) {
				voxelList.RemoveAt(i); 
			}
		}

		voxelList.Sort(Voxel.Compare);
		if (this.getVoxel (posID) == null) {
			//
			// MOVE系Operation以外の時，deleteを実行する
			//
			if (  !(op.getOpType() == Operation.MOVE_ALL ||
					op.getOpType() == Operation.MOVE_POLYGON ||
					op.getOpType() == Operation.MOVE)) {
				this.deletedPosIDs.Add (posID);
			}
		}
		return;
	}

	//!!moveは，polygon（1つのvoxelに収まるpolygon）を移動させないこと前提!!
	public void move(Operation op, long timestamp, string posID, string destPosID) {
		Voxel aVoxel = this.getVoxel(posID);
		this.delete(op, timestamp, posID);
		timestamp++;
		this.insert(op, timestamp, destPosID, aVoxel.getTextureType());
		this.movedPosIDs[posID] = destPosID;
	}



	public long insertAll(Operation op) {
		return this.insertAll(op, op.getTimestamp(), op.getGID(), op.getPosIDs(), op.getTextureTypes() );
	}

	//編集の必要がある
	public long insertAll(Operation op, long timestamp,
		string gid, string[] posIDs, int[] textureTypes) {
		//insertAll時にtextureoTypesをInputする必要がある
		for (int i = 0; i < posIDs.Length; ++i) {
			this.insert(op, timestamp, posIDs [i], textureTypes[i]);
			timestamp++;
		}
        this.joinAll(op, timestamp, gid, posIDs);
		timestamp++;

		return timestamp;
	}

	public long insertPolygon(Operation op) {
		string[] posIDs = op.getPosIDs ();
		return this.insertPolygon(op, op.getTimestamp(), op.getGID(),
			posIDs, posIDs[posIDs.Length-1], op.getObjPath());
	}
			
	public long insertPolygon(Operation op, long timestamp,
		string gid, string[] posIDs, string polygonPosID,  string objPath) {
		//Debug.Log ("insertPolygon: " + Util.CreatePosID(posIDs));
		this.insert(op, timestamp, polygonPosID, objPath);
		//Debug.Log ("voxel: " + this.getVoxel(posIDs[polyIndex]));
		timestamp++;
		this.joinAll(op, timestamp, gid, posIDs);
		timestamp++;

		return timestamp;
	}

	public long deleteAll(Operation op, long timestamp, string gid, string[] posIDs) {
		timestamp = this.leaveAll(op, timestamp, gid, posIDs);
		timestamp++;
		foreach(string posID in posIDs) {
			this.delete(op, timestamp, posID);
			timestamp++;
		}
		return timestamp;
	}

	public long deletePolygon(Operation op) {
		string[] posIDs = op.getPosIDs();
		return this.deletePolygon(op, op.getTimestamp(), op.getGID(),
			op.getPosIDs(), posIDs[posIDs.Length - 1]);
	}
	public long deletePolygon(Operation op, long timestamp, string gid,
			string[] posIDs, string polygonPosID) {
		timestamp = this.leavePolygon(op, timestamp, gid, posIDs, polygonPosID);
		timestamp++;
		this.delete(op, timestamp, polygonPosID);
		timestamp++;
		return timestamp;
	}

	public void moveAll(Operation op) {
		string[] posIDs = op.getPosIDs();
		posIDs = Util.ArrangePosIDs(posIDs, op.getTransMatrix());
		string[] destPosIDs = op.getDestPosIDs(posIDs);

		//移動時の衝突回避
		for (int i = 0; i < posIDs.Length; ++i) { if (this.getVoxel(posIDs [i]) == null) return; //移動前のVoxelが削除されていればreturn
		}
		for (int i = 0; i < posIDs.Length; ++i) {
			Voxel aDestVoxel = this.getVoxel(destPosIDs[i]);
			if (aDestVoxel != null) {

				//Debug.Log("Grouping check: " + destPosIDs[i] + this.getVoxel(destPosIDs[i]));
                //基本的には移動先Voxelに何かあれば処理せずreturnする
                //＊ただし移動してるVoxel郡が移動先と被っている状態を除く
				if (! posIDs.Contains(destPosIDs[i])) return;
			}
		}
		int[] textureTypes = this.getTextureTypesFrom(posIDs);
		long timestamp = op.getTimestamp();

		timestamp = this.deleteAll(op, timestamp, op.getGID(), posIDs);
		timestamp++;
		timestamp = this.insertAll(op, timestamp, op.getGID(), destPosIDs, textureTypes);
		timestamp++;

		for (int i = 0; i < posIDs.Length; ++i) {
			this.movedPosIDs[posIDs[i]] = destPosIDs[i];
		}
	}


	public void movePolygon(Operation op) {
		string[] posIDs = op.getPosIDs();
		//
		// ポリゴンの存在している座標(polygonPosID)
		// ボクセル群の右上（posIDsの最後）座標のことであるが，下記のArrangePosIDsによる
		// 座標移動により，polygonPosIDを一時格納しておく必要がある
		//
		string polygonPosID = posIDs[posIDs.Length - 1];

		//
		// 動かす方向に従って，timestampをずらすためにposIDsを整理する．
		//
		posIDs = Util.ArrangePosIDs(posIDs, op.getTransMatrix()); //TODO(Tasuku): Here原因!!!!!
		int polygonIndex = Array.IndexOf(posIDs, polygonPosID);
		string[] destPosIDs = op.getDestPosIDs(posIDs);

		//this.stt.show();
		//this.show();
		//Debug.Log (polygonPosID);

		//
		//移動時の衝突回避
		//
		//移動前のVoxelが削除されていればreturn
		if (this.getVoxel(polygonPosID) == null) {
			//ここが動けない原因!!!!!!!!!!!!!!!!!!!!!!!!!!!
			Debug.Log ("first one: " + polygonPosID);
			return;
		}
		for (int i = 0; i < destPosIDs.Length; ++i) {
            //
			//join操作によってグループに参加しているが，
			//チェインハッシュテーブルの中には入ってない場合もあるためこのような条件となる
            //つまり，移動先にvoxelがあるまたは，空だがグループposIDがある場合
            //
			if (this.stt.isGrouping(destPosIDs[i]) ||
				this.getVoxel(destPosIDs[i]) != null) {
                //基本的には移動先Voxelに何かあれば処理せずreturnする
                //＊ただし移動してるVoxel郡が移動先と被っている状態を除く
				if (! posIDs.Contains(destPosIDs[i])) {
					//Debug.Log("Grouping check: " + destPosIDs[i] + " " + this.stt.isGrouping(destPosIDs[i]));
					//this.stt.show();

                    return;
                }
			}
		}
		string objPath = this.getVoxel(polygonPosID).getObjPath();
		long timestamp = op.getTimestamp();

		//Debug.Log ("current timestamp: " + timestamp);
		timestamp = this.deletePolygon(op, timestamp, op.getGID(), posIDs, polygonPosID);
		timestamp++;
		//Debug.Log ("posIDs=" + Util.GetCommaLineFrom(posIDs) + ", destPosIDs=" + Util.GetCommaLineFrom(destPosIDs));
		timestamp = this.insertPolygon(op, timestamp, op.getGID(), destPosIDs,
			op.getDestPosID(polygonPosID), objPath);
		timestamp++;

		this.movedPosIDs[polygonPosID] = destPosIDs[polygonIndex];
	}

	/*
	public void moveAll (Operation op)
	{
		string[] posIDs = op.getPosIDs();
		posIDs = ChainVoxel.ArrangePosIDs(posIDs, op.getTransMatrix());
		string[] destPosIDs = op.getDestPosIDs(posIDs);

		for (int i = 0; i < posIDs.Length; ++i) { if (this.getVoxel (posIDs [i]) == null) return; }
		for (int i = 0; i < posIDs.Length; ++i) {
			Voxel aDestVoxel = this.getVoxel(destPosIDs[i]);
			if (aDestVoxel != null) { if (! posIDs.Contains(destPosIDs[i])) return; }
		}
		Debug.Log ("MOVE_ALL");

		long timestamp = op.getTimestamp ();
		this.leaveAll(op, timestamp, op.getGID(), posIDs);

		timestamp++;
		//バグの原因：一斉に削除した後、一斉に挿入する必要がある
		for (int i = 0; i < posIDs.Length; ++i) {
			this.move(op, timestamp, posIDs[i], destPosIDs[i]);
			timestamp+=2L;
		}
		this.joinAll(op, timestamp, op.getGID(), destPosIDs);
	}
	*/
	
	/**
     * 指定したグループを作成するメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void create(Operation op) {
		this.stt.create(op.getGID());
	}

	/**
     * 指定したグループにvoxelを参加させるメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void join(Operation op) {
		this.stt.join(op.getTimestamp(), op.getPosID(), op.getGID());
	}

	/**
     * 指定したグループにvoxelたちを全て同時刻に参加させるメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void joinAll(Operation op, long timestamp, string gid, string[] posIDs) {
		this.stt.joinAll(timestamp, posIDs, gid);
		if (op.getOpType() != Operation.MOVE_ALL && op.getOpType() != Operation.MOVE_POLYGON) {
			this.joinedGIDs.Add(gid);//最新のタイムスタンプのグループをとる
		}
	}

	/**
     * 指定したグループからvoxelを脱退させるメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void leave(Operation op) {
		Voxel aVoxel = this.getVoxel (op.getPosID());
		this.stt.leave(op.getSID(), op.getTimestamp(), op.getPosID(), op.getGID());
		this.insert(op, op.getTimestamp(), op.getPosID(), aVoxel.getTextureType());
	}

	/**
     * 指定したグループにvoxelたちを全て同時刻に離脱させるメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public long leaveAll(Operation op, long timestamp, string gid, string[] posIDs) {
		//座標を与えよう！！！！
		//移動する前のGIDまで汲み取ってしまうため、NULL Exceptionが発生する
		Voxel[] voxels = this.getVoxelBlock(posIDs);
		this.stt.leaveAll(op.getSID(), timestamp, gid); 

		timestamp++;
		for(int i = 0; i < voxels.Length; ++i) {
			this.insert(op, timestamp, posIDs[i], voxels[i].getTextureType());
			timestamp++;
		}
		if (op.getOpType() != Operation.MOVE_ALL) {
			this.leftGIDs.Add(gid);//最新のタイムスタンプのグループをとる
		}

		return timestamp;
	}

	private long leavePolygon(Operation op, long timestamp, string gid,
			string[] posIDs, string polygonPosID) {
		Voxel[] voxels = this.getVoxelBlock(posIDs);
		this.stt.leaveAll(op.getSID(), timestamp, gid); 
		timestamp++;
		int polygonIndex = Array.IndexOf(posIDs, polygonPosID);

		this.insert(op, timestamp, polygonPosID, voxels[polygonIndex].getObjPath());
		timestamp++;

		if (op.getOpType() != Operation.MOVE_POLYGON) {
			this.leftGIDs.Add(gid);//最新のタイムスタンプのグループをとる
		}
		return timestamp;
	}

	/**
     * 指定したposIDに対応するprimaryVoxelを返すメソッド
     * @param posID voxelの識別子
     * @return posIDに対応するvoxel，posIDに対応するものがない場合はnullを返す．
     * @see Voxel
     */
	public Voxel getVoxel(string posID) {
		List<Voxel> voxelList = this.getVoxelList(posID);
		if (voxelList.Count == 0) { return null; }

		return voxelList[0]; // 先頭のvoxelがprimaryVoxel
	}

	private Voxel[] getVoxelBlock(string[] posIDs) {
		Voxel[] voxels = new Voxel[posIDs.Length];
		for(int i = 0; i < voxels.Length; ++i) {
			voxels[i] = this.getVoxel (posIDs[i]);
		}
		return voxels;
	}

	private int[] getTextureTypesFrom(string[] posIDs) {
		Voxel[] voxels = this.getVoxelBlock (posIDs);
		int[] textureTypes = new int[voxels.Length];
		for(int i = 0; i < voxels.Length; ++i) {
			textureTypes[i] = voxels[i].getTextureType();
		}
		return textureTypes;
	}		


	public bool isIncludingAll(string[] posIDs) {
		Voxel[] voxels = this.getVoxelBlock (posIDs);	
		for (int i = 0; i < voxels.Length; ++i) {
			if (voxels [i] == null) { return false; }
		}
		return true;
	}

	/**
     * 指定したposIDに対応するvoxelのリストを返すメソッド
     * @param posID voxelの識別子
     * @return posIDに対応するvoxelのリスト
     * @see Voxel
     */
	public List<Voxel> getVoxelList(string posID){
		if (!this.atoms.ContainsKey (posID)) {
			this.atoms [posID] = new List<Voxel> ();
		}
		return this.atoms [posID];
	}

	/**
     * ChainVoxelの総容量を返すメソッド * @return ChainVoxelの総容量
     */
	public int size() {
		int totalSize = 0;
		foreach (KeyValuePair<string, List<Voxel>> p in this.atoms) {
			totalSize += p.Value.Count; 
		}
		return totalSize;
	}

	/**
     * 指定されたposIDのvoxel数を返すメソッド
     * @param posID voxelの識別子
     * @return posIDに対応するvoxel数
     */
	public int size(string posID) {
		return this.atoms[posID].Count;
	}




	/**
     * ChainVoxelの状態を返す。
     */
	public string getStatusString() {
		string res="ChainVoxel table\n";
		foreach (KeyValuePair<string, List<Voxel>> p in this.atoms)
		{
			if (p.Value.Count == 0) continue;
			res += "|" + p.Key + "|\n";

			List<Voxel> voxelList = p.Value;
			int n = voxelList.Count;
			foreach (Voxel voxel in voxelList) {
				string id = voxel.getId().ToString();
				string timestamp = voxel.getTimestamp().ToString();
				res += " -> (" + id + "," + timestamp + ")\n";
			}
			res += "\n";
		}
		res += "\n";

		return res;
	}

	/**
     * ChainVoxelの状態を表示する
     */
	public void show() {
		Debug.Log(this.getStatusString());
	}	

	/**
     * StructureTableの状態を表示する
     */
	public void showStructureTable() {
		this.stt.show();	
	}

	/*
	 * 
	 */
	public void LoadSavedData(string saved_path)
	{
		try {
			//StringReader reader = new StringReader(saved_path)
			using (StreamReader reader = new StreamReader(saved_path)) {

				string line =reader.ReadLine();
				string gid = "";
				List<string> posIDs = null;
				List<string> textureTypes = null; 

				while(line != null) {
					if (line[0] == 'g') {
						string[] entries = line.Split(' ');
						gid = entries[1];
						posIDs = new List<string>();
						textureTypes = new List<string>();
						//Debug.Log(gid);
					}
					else if (line[0] == 'v') {
						string[] entries = line.Split(' ');
						int textureType = int.Parse(entries[1]);
						string posID = entries[2];
						if (posIDs == null) {
							Operation op = new Operation(0, Operation.INSERT,
								"{\"posID\": \"" + posID +
								"\", \"textureType\":\"" + textureType.ToString() + "\"}"
							);
							this.apply(op);
						}
						else {
							posIDs.Add(posID);
							textureTypes.Add(textureType.ToString());
						}
					}
					else if (line[0] == 'e') {
						Operation op = new Operation(0, Operation.INSERT_ALL,
							"{\"posIDs\": \"" + Util.GetCommaLineFrom(posIDs) +
							"\", \"gid\": \"" + gid +
							"\", \"textureTypes\":\"" + Util.GetCommaLineFrom(textureTypes) + "\"}"
						);
						this.apply(op);
							
						gid = "";
						posIDs = null;
						textureTypes = null;
					}
					else if (line[0] == '#') {
						//pass
					}
					line = reader.ReadLine();
				}
				reader.Close();
			}
		} catch (Exception e) {
			// Let the user know what went wrong.
			Debug.LogError("The file could not be read:");
			Debug.LogError(e.Message);
		}
	}

	/*
	 * 
	 */
	public string SaveData(string saving_path)
	{
		try {
			//using (StringWriter writer = new StringWriter()) {
			using (StreamWriter writer = new StreamWriter(saving_path)) {

				List<string> saved_posIDs = new List<string>();
				foreach (KeyValuePair<string, HashSet<string>> p in this.stt.getGroupMembersTable()) {
					string gid = p.Key;
					HashSet<string> posIDs = p.Value;
					string line = String.Format("g {0}", gid);
					writer.WriteLine(line);
					foreach (string posID in posIDs) {
						Voxel aVoxel = this.getVoxel(posID);
						line = String.Format("v {0} {1}", aVoxel.getTextureType(), posID);
						writer.WriteLine(line);
						saved_posIDs.Add(posID);
					}
					writer.WriteLine("e");
					writer.Flush();
				}

				foreach (KeyValuePair<string, List<Voxel>> p in this.atoms) {
					string posID = p.Key;
					if (saved_posIDs.Contains(posID)) { continue; }
				
					Voxel aVoxel = this.getVoxel(posID);
					if (aVoxel == null) { continue; }

					int textureType = this.getVoxel(posID).getTextureType();
					string line = String.Format("v {0} {1}", textureType, posID);
					//Debug.Log(line);
					writer.WriteLine(line);
					writer.Flush();
				}
				writer.Flush();
				writer.Close();
				return writer.ToString();
			}
		} catch (Exception e) {
			// Let the user know what went wrong.
			Debug.LogError(e.Message);
			return "";
		}
	}

	public byte[] GetBinaryFromFile(string filepath) {
		return File.ReadAllBytes (filepath);	
	}



}
