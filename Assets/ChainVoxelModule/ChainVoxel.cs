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
		if (!Directory.Exists (Const.SAVED_DIR)) Directory.CreateDirectory(Const.SAVED_DIR);
	}

	/**
     * 操作オブジェクトに対応する操作を実行するメソッド．<br>
     * ChainVoxelに対する操作はapplyメソッドを用いて実行することを推奨しています．
     * @param op 操作オブジェクト
     * @param textureType テクスチャ番号
     * @see Operation
     */
	public void apply(Operation op) {
		lock (ChainXController.thisLock) {
			string posID = op.getPosID();

			switch (op.getOpType()) {
			case Operation.INSERT:
				if (this.stt.isGrouped(posID)) break;
				this.insert(op);
				break;
			case Operation.DELETE:
				if (this.stt.isGrouped(posID)) break;
				this.delete(op);
				break;
			case Operation.CREATE:
				this.create(op);
				break;
			case Operation.JOIN:
				this.join(op);
				break;
			case Operation.LEAVE:
				this.leave(op);
				break;
			case Operation.MOVE:
				this.move(op);
				break;
			case Operation.JOIN_ALL:
				this.joinAll(op);
				break;
			case Operation.LEAVE_ALL:
				this.leaveAll(op);
				break;
			default:
				Debug.Assert (false);
				break;
			}
			this.controller.log = this.show ();
		}
		return;
	}

	/**
     * ChainVoxel内にvoxelを挿入するメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void insert(Operation op) {
		int id = op.getSID();
		int textureType = int.Parse(op.getTextureType ());
		string posID = (op.getOpType() == Operation.MOVE) ? op.getDestPosID() : op.getPosID();
		long timestamp = op.getTimestamp();
		Voxel insertVoxel = new Voxel(id, textureType, timestamp);

		//Debug.Log(op.getOpType());

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
		if (this.getVoxel (posID) != null) { this.insertedPosIDs.Add (posID); }

		return;
	}

	/**
     * ChainVoxel内の指定したvoxelを削除するメソッド
     * @param op 操作オブジェクト
     * @return textureType 削除するVoxelのテクスチャ番号
     * @see Operation
     */
	public int delete(Operation op) {
		//int id = op.getSID();
		string posID = op.getPosID();
		long timestamp = op.getTimestamp();

		// step1: 負のvoxelをnegativeVoxelsに追加・更新
		if (!this.negativeVoxels.ContainsKey(posID) || this.negativeVoxels[posID].getTimestamp() < timestamp) {
			this.negativeVoxels[posID] = new Voxel(timestamp);
		}

		//DEBUG!!!!!!
		foreach (KeyValuePair<string,Voxel> aVoxel in this.negativeVoxels) {
			Debug.Log(aVoxel.ToString());
		}

		List<Voxel> voxelList = this.getVoxelList(posID);
		Voxel tmpVoxel = this.getVoxel (posID); //NULL(06/03/2017)
		int textureType = tmpVoxel.getTextureType ();

		// step2: 負のvoxelより古いvoxelを削除する
		for (int i = voxelList.Count - 1; i >= 0; --i) { // 先頭から削除するとイテレータがおかしくなる
			Debug.Log(voxelList[i]);
			if (this.negativeVoxels[posID].getTimestamp() >= voxelList[i].getTimestamp()) {
				voxelList.RemoveAt(i); 
			}
		}

		voxelList.Sort(Voxel.Compare);
		if (this.getVoxel (posID) == null) { this.deletedPosIDs.Add (posID); }

		return textureType;
	}

	public void move(Operation op) {
		if (this.getVoxel(op.getDestPosID()) != null) { return; }
		int textureType = this.delete (op); //Bug!!
		op.setTextureType(textureType);
		this.insert (op);
		this.movedPosIDs [op.getPosID()] = op.getDestPosID ();
	}

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
	public void joinAll(Operation op) {
		this.stt.joinAll(op.getTimestamp(), op.getPosIDs(), op.getGID());
		this.joinedGIDs.Add(op.getGID());//最新のタイムスタンプのグループをとる
	}

	/**
     * 指定したグループからvoxelを脱退させるメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void leave(Operation op) {
		this.stt.leave(op.getSID(), op.getTimestamp(), op.getPosID(), op.getGID());
		this.insert(op);
	}

	/**
     * 指定したグループにvoxelたちを全て同時刻に離脱させるメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void leaveAll(Operation op) {
		this.stt.leaveAll(op.getSID(), op.getTimestamp(), op.getPosIDs(), op.getGID());
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
     * ChainVoxelの状態を表示する
     */
	public string show() {
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
		//Debug.Log(res);

		return res;
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
	public void LoadSavedData(string saved_data)
	{
		try {
			using (StringReader reader = new StringReader(saved_data)) {

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
	public string GetSavedData()
	{
		//Debug.Log(this.show());
		string saved_data = "";
		try {
			using (StringWriter writer = new StringWriter()) {
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

	/**
	 * Test a ChainVoxel class.
	 */
	public static void Test() {
		//ChainVoxel cv = new ChainVoxel();		
		//SortedDictionary<string, Voxel> s = new SortedDictionary<string, Voxel>();
		//Debug.Log(s);
	}
}
