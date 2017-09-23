using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Polygon : Voxel {
	/**
     * Voxelのテクスチャ番号
     */
	private string texturePath;

	/**
     * 負のVoxelを作成する．
     * @param timestamp タイムスタンプ
     */
	public Polygon(long timestamp) : base(timestamp) { }

	/**
     * 作成したSiteの識別子を持つVoxelを作成する
     * @param id Siteの識別子
     * @param timestamp タイムスタンプ
     */
	public Polygon(int id, string texturePath, long timestamp) : base(id, -1, timestamp) { }

	/**
     * Voxelのテクスチャパスを返す．
     * @return テクスチャパス
     */
	public string getTexturePath() { return this.texturePath; }

	/**
     * Voxelのテクスチャパスを設定する．
     * @param textureType テクスチャパス
     */
	public void setTexturePath(string texturePath) { this.texturePath = texturePath; }

	/*
	 * 
	 */
	public override string ToString() {
		return "id=" + this.getId() + ", texturePath=" + this.getTexturePath() + ", timestamp=" + this.getTimestamp();
	}

	/**
	 * Test a Voxel class.
	 */
	public static void Test() {
		/*
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
		*/
	}
}
