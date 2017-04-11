using System.Linq;
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
 * @author kengo92i
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
	private StructureTable stt;

	/*
	 * 
	 */
	private Shower shower;

	/**
     * ChainVoxelのコンストラクタ
     */
	public ChainVoxel(Shower shower) {
		this.atoms = new SortedDictionary<string, List<Voxel>>();
		this.negativeVoxels = new SortedDictionary<string, Voxel>();
		this.stt = new StructureTable();
		this.shower = shower;
	}

	/**
     * 操作オブジェクトに対応する操作を実行するメソッド．<br>
     * ChainVoxelに対する操作はapplyメソッドを用いて実行することを推奨しています．
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void apply(Operation op) {
		string posID = op.getPosID();
		//Debug.Log(op.getTimestamp()); OK(一貫性が保ててる)
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
		default:
			Debug.Assert (false);
			break;
		}
		this.shower.log.text = this.show ();
		return;
	}

	/**
     * ChainVoxel内にvoxelを挿入するメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void insert(Operation op) {
		int id = op.getId();
		string posID = (op.getOpType() == Operation.MOVE) ? op.getDestPosID() : op.getPosID();
		long timestamp = op.getTimestamp();
		Voxel insertVoxel = new Voxel(id, timestamp);

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
		if (GameObject.Find (posID) == null)
		{
			string[] xyzs = posID.Split (':');
			GameObject voxelObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
			voxelObj.name = posID;
			voxelObj.transform.position = new Vector3(int.Parse(xyzs[0]), int.Parse(xyzs[2]), int.Parse(xyzs[1]) );
		}
		return;
	}

	/**
     * ChainVoxel内の指定したvoxelを削除するメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void delete(Operation op) {
		//int id = op.getId();
		string posID = op.getPosID();
		long timestamp = op.getTimestamp();

		// step1: 負のvoxelをnegativeVoxelsに追加・更新
		//Voxel negativeVoxel = negativeVoxels[posID];
		if (!this.negativeVoxels.ContainsKey(posID) || this.negativeVoxels[posID].getTimestamp() < timestamp) {
			this.negativeVoxels[posID] = new Voxel(timestamp);
			//Debug.Log (this.negativeVoxels[posID].getTimestamp());
		}

		List<Voxel> voxelList = this.getVoxelList(posID);

		// step2: 負のvoxelより古いvoxelを削除する
		for (int i = voxelList.Count - 1; i >= 0; --i) { // 先頭から削除するとイテレータがおかしくなる
			if (this.negativeVoxels[posID].getTimestamp() >= voxelList[i].getTimestamp()) {
				voxelList.RemoveAt(i); 
			}
		}

		voxelList.Sort(Voxel.Compare);
		GameObject.Destroy(GameObject.Find (posID));
		return;
	}

	public void move(Operation op) {
		this.delete (op);
		this.insert (op); //posIDの異なるop
	}

	/**
     * 指定したグループを作成するメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void create(Operation op) {
		string gid = (string) op.getParam("gid"); 
		this.stt.create(gid);
	}

	/**
     * 指定したグループにvoxelを参加させるメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void join(Operation op) {
		long ts = op.getTimestamp(); 
		string posID = (string) op.getParam("posID"); 
		string gid = (string) op.getParam("gid"); 

		this.stt.join(ts, posID, gid);
	}

	/**
     * 指定したグループからvoxelを脱退させるメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
	public void leave(Operation op) {
		int sid = (int) op.getParam("sid"); 
		long ts = op.getTimestamp(); 
		string posID = (string) op.getParam("posID"); 
		string gid = (string) op.getParam("gid"); 

		this.stt.leave(sid, ts, posID, gid);
		this.insert(op);
	}

	/**
     * 指定したposIDに対応するprimaryVoxelを返すメソッド
     * @param posID voxelの識別子
     * @return posIDに対応するvoxel，posIDに対応するものがない場合はnullを返す．
     * @see Voxel
     */
	public Voxel getVoxel(string posID) {
		List<Voxel> voxelList = this.atoms[posID];
		if (voxelList == null) {
			return null;
		}
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
		string res="";
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
}
