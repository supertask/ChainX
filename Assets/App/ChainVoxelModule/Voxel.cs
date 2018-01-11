using System.IO;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel {
	/**
     * Voxelを挿入したSiteID
     */
	private int id;

	/**
     * Voxelのテクスチャ番号
     */
	private int textureType;

	/**
     * Voxelのオブジェクトパス
     * 実際には、オブジェクトパス＋中心座標から割り出したマージン
     */
	private string objPath;

	/**
     * Voxelのタイムスタンプ
     */
	private long timestamp;

	/**
	 * 
     * 負のVoxelを作成するためのもの．
     * @param timestamp タイムスタンプ
     */
	public Voxel(long timestamp):
		this(-1, -1, "", timestamp) { }

	public Voxel(int id, string texturePath, long timestamp):
		this(id, -1, texturePath, timestamp) { }

	public Voxel(int id, int textureType, long timestamp):
		this(id, textureType, "", timestamp) { } 

	/**
     * 作成したSiteの識別子を持つVoxelを作成する
     * @param id Siteの識別子
     * @param textureType テクスチャ番号
     * @param texturePath テクスチャパス
     * @param timestamp タイムスタンプ
     */
	public Voxel(int id, int textureType, string objPath, long timestamp) {
		this.id = id;
		this.textureType = textureType;
		this.objPath = objPath;
		this.timestamp = timestamp;
	}

	/**
     * Voxelを作成したSiteの識別子を返す．
     * @return Voxelを作成したSiteの識別子
     */
	public int getId() { return this.id; }

	/**
     * Voxelを作成したSiteの識別子を設定する．
     * @param id SiteID
     */
	public void setId(int id) { this.id = id; }

	/**
     * Voxelのテクスチャ番号子を返す．
     * @return テクスチャ番号
     */
	public int getTextureType() { return this.textureType; }

	/**
     * Voxelのテクスチャ番号を設定する．
     * @param textureType テクスチャ番号
     */
	public void setTextureType(int textureType) { this.textureType = textureType; }

	/**
     * Voxelのテクスチャパスを返す．
     * @return テクスチャパス
     */
	public string getObjPath() { return this.objPath; }

	/*
	 * 
	 */
	public string getTexturePath() {
		string objPath = this.getObjPath();
		string dir = Path.GetDirectoryName (objPath);
		string filenameWithoutExt = Path.GetFileNameWithoutExtension (objPath);
		string texturePath = dir + "/" + filenameWithoutExt + ".jpg";
		return texturePath;
	}

	/**
     * Voxelのテクスチャパスを設定する．
     * @param textureType テクスチャパス
     */
	public void setObjPath(string texturePath) { this.objPath = objPath; }


	/**
     * Voxelのタイムスタンプを返す．
     * @return タイムスタンプ
     */
	public long getTimestamp() { return this.timestamp; }

	/**
     * Voxelのタイムスタンプを設定する．
     * @param timestamp タイムスタンプ
     */
	public void setTimestamp(long timestamp) { this.timestamp = timestamp; }

	/**
	 * TODO(Tasuku): 書き直し！！！
     * 自分と他のVoxelをタイムスタンプの昇順にソートする．同じタイムスタンプの場合は識別子の昇順で順位付けをする． 
     * @param left 自分と比較するVoxel
     * @param right 自分と比較する他のVoxel
     */
	public static int Compare(Voxel left, Voxel right) { 
		if (left.getTimestamp() < right.getTimestamp()) { return -1; }
		if (left.getTimestamp() > right.getTimestamp()) { return 1; }
		// timestampが同じ場合
		if (left.getId() < right.getId()) { return -1; }
		if (left.getId() > right.getId()) { return 1; }
		return 0;
	}

	/*
	 * 
	 */
	public override string ToString() {
		return "id=" + this.getId() + ", textureType=" + this.getTextureType() +
			", objPath=" + this.getObjPath() + ", timestamp=" + this.getTimestamp();
	}

	public long getMemory() {
		//id, textureType, timestamp, objPath
		return sizeof(int) + sizeof(int) + sizeof(long) + this.objPath.Length * 1;
	}


	/**
	 * Test a Voxel class.
	 */
	public static void Test() {
		//Debug.Log("Start Voxel TEST");
		Voxel voxel;
		long timestamp = Util.currentTimeNanos();
		int id = 2000000;

		voxel = new Voxel(timestamp);
		Debug.Assert(voxel.getTimestamp() == timestamp);
		Debug.Assert(voxel.getId() == -1);
		voxel.setId(id);
		Debug.Assert(voxel.getId() == id);
		timestamp += 52;
		voxel.setTimestamp(timestamp);
		Debug.Assert(voxel.getTimestamp() == timestamp);

		voxel = new Voxel(id, 3, timestamp);
		Debug.Assert(voxel.getTimestamp() == timestamp);
		Debug.Assert(voxel.getId() == id);

		List<Voxel> voxels = new List<Voxel>();
		voxels.Add(new Voxel(3, 6, 2400));
		voxels.Add(new Voxel(2, 5, 1000));
		voxels.Add(new Voxel(1, 5, 3200));
		voxels.Add(new Voxel(0, 2, 1000));
		voxels.Sort(Voxel.Compare);
		Debug.Assert(Voxel.Compare(voxels[0],new Voxel(0, 1, 1000)) == 0); // "== 0" means same value
		Debug.Assert(Voxel.Compare(voxels[1],new Voxel(2, 2,1000)) == 0);
		Debug.Assert(Voxel.Compare(voxels[2],new Voxel(3, 2, 2400)) == 0);
		Debug.Assert(Voxel.Compare(voxels[3],new Voxel(1, 4, 3200)) == 0);


		//foreach (Voxel v in voxels) { Debug.Log(v.Tostring()); }
		Debug.Log("End a Voxel class test");
	}
}
