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

			bool isGrouping;
			switch (op.getOpType ()) {
			case Operation.INSERT:
				posID = op.getPosID();
				if (this.stt.isGrouping (posID))
					break;
				this.insert (op, op.getTimestamp(), posID, op.getTextureType());
				break;
			case Operation.DELETE:
				posID = op.getPosID ();
				if (this.stt.isGrouping(posID))
					break;
				this.delete (op, op.getTimestamp(), posID);
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
				destPosID = op.getDestPosID ();
				if (this.stt.isGrouping (posID)) break;
				if (this.stt.isGrouping (destPosID)) break;
				if (this.getVoxel(posID) == null) break; //For protecting to an operate empty voxel.
				if (this.getVoxel(destPosID) != null) break; //If a voxel already exists
				this.move(op, op.getTimestamp(), posID, destPosID);
				break;

			case Operation.INSERT_ALL:
				posIDs = op.getPosIDs ();
				isGrouping = false;
				for (int i = 0; i < posIDs.Length; ++i) {
					if (this.stt.isGrouping (posIDs [i])) {
						isGrouping = true;
					}
				}
				if (isGrouping) { break; }
				this.insertAll (op, op.getTimestamp (), op.getGID (), posIDs, op.getTextureTypes ());
				break;
			case Operation.DELETE_ALL:
				isGrouping = false;
				foreach(string p in this.stt.getPosIDs(op.getGID())) {
					if (this.stt.isGrouping (p)) {
						isGrouping = true;
						break;
					}
				}
				if (isGrouping) { this.deleteAll (op, op.getTimestamp (), op.getGID (), op.getPosIDs()); }
				break;
			case Operation.JOIN_ALL:
				this.joinAll (op, op.getTimestamp(), op.getGID(), op.getPosIDs());
				break;
			case Operation.LEAVE_ALL:
				this.leaveAll (op, op.getTimestamp(), op.getGID(), op.getPosIDs());
				break;
			case Operation.MOVE_ALL:

				posIDs = op.getPosIDs();
				isGrouping = false;
				for (int i = 0; i < posIDs.Length; ++i) {
					if (this.stt.isGrouping (posIDs [i])) {
						isGrouping = true;
						break;
					}
				}
				if (isGrouping) {
					this.moveAll (op);
				}

				//foreach (GameObject anObj in this.controller.selectedObjects) { Debug.Log ("xxx" + anObj); }

				/*
				Voxel[] voxels = this.getVoxelBlock(posIDs);
				for (int i = 0; i < voxels.Length; ++i) {
					Debug.Log (posIDs[i] + ", " + voxels[i]);
				}
				*/
				break;
			default:
				Debug.Assert (false);
				break;
			}
			this.controller.log = this.getStatusString ();
		}
		return;
	}

	/**
     * ChainVoxel内にvoxelを挿入するメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void insert(Operation op, long timestamp, string posID, int textureType) {
		int id = op.getSID();
		Voxel insertVoxel = new Voxel(id, textureType, timestamp);

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
			//!posIDs.Contains(posID((destPosID)) );
			this.insertedPosIDs.Add (posID);

			//Debug.Log ("insert():" + posID);	//動いている！！
		}

		return;
	}

	/**
     * ChainVoxel内の指定したvoxelを削除するメソッド
     * @param op 操作オブジェクト
     * @return textureType 削除するVoxelのテクスチャ番号
     * @see Operation
     */
	public int delete(Operation op, long timestamp, string posID) {
		// step1: 負のvoxelをnegativeVoxelsに追加・更新
		if (!this.negativeVoxels.ContainsKey(posID) || this.negativeVoxels[posID].getTimestamp() < timestamp) {
			this.negativeVoxels[posID] = new Voxel(timestamp);
		}

		/*
		//DEBUG!!!!!!
		foreach (KeyValuePair<string,Voxel> aVoxel in this.negativeVoxels) {
			Debug.Log(aVoxel.ToString());
		}
		*/

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

		//Hereバグ: ここが原因！！！
		if (this.getVoxel (posID) == null) {
			Debug.Log ("delete():" + posID);	//動いている！！

			this.deletedPosIDs.Add (posID);
		}

		return textureType;
	}

	public void move(Operation op, long timestamp, string posID, string destPosID) {
		int textureType = this.delete(op, timestamp, posID);
		//Debug.Log ("deleted" + posID + " " + timestamp);
		//timestamp++;
		this.insert(op, timestamp+1L, destPosID, textureType);
		//Debug.Log("inserted" + destPosID + " " + (timestamp+1L));
		this.movedPosIDs[posID] = destPosID;
	}


	//編集の必要がある
	public long insertAll(Operation op, long timestamp,
		string gid, string[] posIDs, int[] textureTypes) {
		//insertAll時にtextureoTypesをInputする必要がある
		for (int i = 0; i < posIDs.Length; ++i) {
			this.insert (op, timestamp, posIDs [i], textureTypes[i]);
			//Debug.Log ("inserted " + timestamp + " " + posIDs[i]);
			timestamp++;
		}
        this.joinAll(op, timestamp, gid, posIDs);
		timestamp++;

		return timestamp;
	}

	public long deleteAll(Operation op, long timestamp, string gid, string[] posIDs) {
		timestamp = this.leaveAll(op, timestamp, gid, posIDs);
		timestamp++;
		foreach(string posID in posIDs) {
			this.delete(op, timestamp, posID);
			//Debug.Log ("deleted " + timestamp + " " + posID);
			timestamp++;
		}
		return timestamp;
	}

	public void moveAll (Operation op)
	{
		string[] posIDs = op.getPosIDs();
		posIDs = this.arrangePosIDs(posIDs, op.getTransMatrix());
		string[] destPosIDs = op.getDestPosIDs(posIDs);

		//移動時の衝突回避
		for (int i = 0; i < posIDs.Length; ++i) { if (this.getVoxel (posIDs [i]) == null) return; }
		for (int i = 0; i < posIDs.Length; ++i) {
			Voxel aDestVoxel = this.getVoxel(destPosIDs[i]);
			if (aDestVoxel != null) { if (! posIDs.Contains(destPosIDs[i])) return; }
		}
		Debug.Log ("MOVE_ALL");
		int[] textureTypes = this.getTextureTypesFrom(posIDs);
		long timestamp = op.getTimestamp();

		timestamp = this.deleteAll (op, timestamp, op.getGID(), posIDs);
		timestamp++;
		timestamp = this.insertAll(op, timestamp, op.getGID(), destPosIDs, textureTypes); //2Dは衝突を回避するため
		timestamp++;

		for (int i = 0; i < posIDs.Length; ++i) {
			this.movedPosIDs[posIDs[i]] = destPosIDs[i];
			//Debug.Log (posIDs [i] + " " + destPosIDs [i]); //ここにもしっかり入っている
		}
		//Here(バグ)
		//データ構造にはしっかり入っているよう、表示だけがおかしい!!
		//foreach(string posID in posIDs) { Debug.Log (posID + " " + this.getVoxel(posID)); }
		//foreach(string posID in destPosIDs) { Debug.Log (posID + " " + this.getVoxel(posID)); }
	}

	/*
	public void moveAll (Operation op)
	{
		string[] posIDs = op.getPosIDs();
		posIDs = this.arrangePosIDs(posIDs, op.getTransMatrix());
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
		this.joinedGIDs.Add(gid);//最新のタイムスタンプのグループをとる
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
			//Debug.Log ("voxel:" + voxels[i] + ", posID: " + posIDs[i]);
			this.insert (op, timestamp, posIDs[i], voxels[i].getTextureType());
			timestamp++;
		}
		this.leftGIDs.Add(gid);//最新のタイムスタンプのグループをとる

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


    public string[] arrangePosIDs(string[] posIDs, string transMatrix) {
        List<Vector3> vs = new List<Vector3>();
        Vector3 tM = Util.SplitPosID(transMatrix);
        foreach(string posID in posIDs) { vs.Add(Util.SplitPosID(posID)); }

		IEnumerable<Vector3> sortedVs = null;
		if (tM.x > 0) sortedVs = vs.OrderBy (v => v.x);
		else if (tM.x < 0) sortedVs = vs.OrderByDescending (v => v.x);

		else if (tM.y > 0) sortedVs = vs.OrderBy (v => v.y);
		else if (tM.y < 0) sortedVs = vs.OrderByDescending (v => v.y);

		else if (tM.z > 0) sortedVs = vs.OrderBy (v => v.z);
		else if (tM.z < 0) sortedVs = vs.OrderByDescending (v => v.z);
			
		int i = 0;
		foreach (Vector3 v in sortedVs) {
			posIDs[i] = Util.CreatePosID (v);
			i++;
		}
		return posIDs;
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
				List<string> posIDs = new List<string>();
				Operation op;

				while(line != null) {
					if (line[0] == 'o') {
						string[] entries = line.Split(' ');
						string group_name = entries[1];
						if (posIDs.Count > 0 ) {
							//Here
							/*
							op = new Operation(0, Operation.JOIN,
								"{\"posID\": \"" + posID +
								"\", \"gid\":\"" + group_name + "\"}"
							);
							this.apply(op);
							*/
						}
						posIDs = new List<string>();

						op = new Operation(0, Operation.INSERT,
							"{\"gid\": \"" + group_name + "\"}"
						);
					}
					else if (line[0] == 'v') {
						string[] entries = line.Split(' ');
						int textureType = int.Parse(entries[1]);
						string posID = entries[2];
						posIDs.Add(posID);
						op = new Operation(0, Operation.INSERT,
							"{\"posID\": \"" + posID +
							"\", \"textureType\":\"" + textureType.ToString() + "\"}"
						);
						this.apply(op);
					}
					else if (line[0] == 'e') {
						if (posIDs.Count > 0 ) { }
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
				foreach (KeyValuePair<string, List<Voxel>> p in this.atoms) {
					string posID = p.Key;
					Voxel aVoxel = this.getVoxel(posID);
					if (aVoxel == null) { continue; }

					int textureType = this.getVoxel(posID).getTextureType();
					string line = String.Format("v {0} {1}", textureType, posID);
					//Debug.Log(line);
					writer.WriteLine(line);
					writer.Flush();
				}
				writer.WriteLine("e");
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


	//
	// Tests
	//----------------------------------------------------
	// TODO(Tasuku): gidからグループを辿り、それを移動、削除、グループ脱出を交互に実行するテストを追加
	//
	public static void TestGroupOperations(ChainVoxel cv)
	{
		//ChainVoxel cv = new ChainVoxel (new ChainXController ());
		Operation o;
		int numberOfPosIDs = UnityEngine.Random.Range (1, 3);
		Vector3 transMatrix = Util.CreateRandomVector3 (-1, 2);
		string[] posIDs = new string[numberOfPosIDs];
		string[] destPosIDs = new string[numberOfPosIDs];
		string textureTypes = "";
		string gid = ChainXModel.PAINT_TOOL_GROUP_ID + Util.GetGUID ();

		for (int p = 0; p < numberOfPosIDs; ++p) {
			Vector3 v = Util.CreateRandomVector3 (-1000, 1000);
			Vector3 destV = v + transMatrix;
			posIDs [p] = Util.CreatePosID (v);
			destPosIDs [p] = Util.CreatePosID (destV);
			textureTypes += UnityEngine.Random.Range (0, 8).ToString () + Const.SPLIT_CHAR;
		}
		textureTypes = textureTypes.TrimEnd (Const.SPLIT_CHAR);

		//複数Voxelをinsertした後、それらをjoinする
		o = new Operation (0, Operation.CREATE,
			"{\"gid\": \"" + gid + "\"}");
		cv.apply (o);

		o = new Operation (0, Operation.INSERT_ALL,
			"{\"gid\": \"" + gid +
			"\", \"posIDs\": \"" + Util.GetCommaLineFrom (posIDs) +
			"\", \"textureTypes\": \"" + textureTypes + "\"}");
		cv.apply (o);
		Debug.Assert (cv.isIncludingAll (posIDs));
		Debug.Assert (cv.stt.isGroupingAll (posIDs));
		//Debug.Log ("Inserted posIDs: " + Util.GetCommaLineFrom(posIDs));
		//cv.show();
		//cv.stt.show();

		switch(UnityEngine.Random.Range(1, 3)) {
			case 1:
				o = new Operation(0, Operation.MOVE_ALL,
		            "{\"gid\": \"" + gid +
					"\", \"transMatrix\": \"" + Util.CreatePosID(transMatrix) + "\"}");
				cv.apply(o);
				Debug.Assert(cv.isIncludingAll(destPosIDs));
				Debug.Assert(cv.stt.isGroupingAll(destPosIDs));
				break;
			case 2:
			/*
				o = new Operation(0, Operation.DELETE_ALL,
		            "{\"gid\": \"" + gid + "\"}");
				cv.apply(o);
				Debug.Assert(!cv.isIncludingAll(posIDs));
				Debug.Assert(!cv.stt.isGroupingAll(posIDs));
			*/
				break;
			case 3:
				o = new Operation(0, Operation.LEAVE_ALL,
		            "{\"gid\": \"" + gid + "\"}");
				cv.apply(o);
				Debug.Assert(cv.isIncludingAll(posIDs));
				Debug.Assert(! cv.stt.isGroupingAll(posIDs));
				break;
			default:
				break;
		}

	}

	public static void UTestFunction()
	{
		ChainVoxel cv = new ChainVoxel (new ChainXController ());
		//int numberOfPosIDs = UnityEngine.Random.Range (1, 3);
		string[] posIDs = new string[3];
		posIDs[0] = "1:1:2";
		posIDs[1] = "1:1:3";
		posIDs[2] = "1:1:1";
		foreach (string posID in cv.arrangePosIDs(posIDs, "0:0:-1")) {
			Debug.Log (posID);
		}
		/*
		for (int p = 0; p < posIDs.Length; ++p) {
			Vector3 v = Util.CreateRandomVector3 (-1000, 1000);
			posIDs[p] = Util.CreatePosID(v);
		}
		*/
	}


	/**
	 * Test a ChainVoxel class.
	 */
	public static void Test ()
	{
		/*
		//
		// 単体Voxel操作（挿入、削除、移動）をテスト
		//
		string posID = "1:1:1";
		string transMatrix = "0:0:1";
		int textureType = 2;
		Operation o;
		//INSERT, MOVE
		o = new Operation (0, Operation.INSERT, "{\"posID\": \"" + posID + "\", \"textureType\": \"" + textureType + "\"}");
		cv.apply(o);
		cv.show();
		o = new Operation (0, Operation.MOVE, "{\"posID\": \"" + posID + "\", \"transMatrix\": \"" + transMatrix + "\"}");
		cv.apply(o);
		cv.show();

		transMatrix = "1:0:0";
		o = new Operation (0, Operation.MOVE, "{\"posID\": \"" + posID + "\", \"transMatrix\": \"" + transMatrix + "\"}");
		cv.apply(o);
		cv.show();
		*/
		ChainVoxel.UTestFunction();
			
		//
		// グループVoxel（参加、離脱、移動）のテスト
		//
		int numberOfTest = Const.TEST_QUALITY;

		ChainVoxel cv = new ChainVoxel (new ChainXController ());
		for (int t = 0; t < numberOfTest; t++) {
			ChainVoxel.TestGroupOperations(cv);
		}
		Debug.Log("End a ChainXVoxel class test");
	}
}
