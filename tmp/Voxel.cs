using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class Voxel {
    /**
     * Voxelを挿入したSiteID
     */
    private int id;

    /**
     * Voxelのタイムスタンプ
     */
    private long timestamp;

    /**
     * 負のVoxelを作成する．
     * @param timestamp タイムスタンプ
     */
    public Voxel(long timestamp) {
        this.id = -1; // not exists site id.
        this.timestamp = timestamp;
    }

    /**
     * 作成したSiteの識別子を持つVoxelを作成する
     * @param id Siteの識別子
     * @param timestamp タイムスタンプ
     */
    public Voxel(int id, long timestamp) {
        this.id = id;
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


    /**
     * Test a Voxel class.
     */
    public static void Test() {
        //Console.WriteLine("Start Voxel TEST");
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

        voxel = new Voxel(id, timestamp);
        Debug.Assert(voxel.getTimestamp() == timestamp);
        Debug.Assert(voxel.getId() == id);

        List<Voxel> voxels = new List<Voxel>();
        voxels.Add(new Voxel(3, 2400));
        voxels.Add(new Voxel(2, 1000));
        voxels.Add(new Voxel(1, 3200));
        voxels.Add(new Voxel(0, 1000));
        voxels.Sort(Voxel.Compare);
        Debug.Assert(Voxel.Compare(voxels[0],new Voxel(0,1000)) == 0); // "== 0" means same value
        Debug.Assert(Voxel.Compare(voxels[1],new Voxel(2,1000)) == 0);
        Debug.Assert(Voxel.Compare(voxels[2],new Voxel(3,2400)) == 0);
        Debug.Assert(Voxel.Compare(voxels[3],new Voxel(1,3200)) == 0);
        Assert.Check(1,"","Message");

        //foreach (Voxel v in voxels) { Console.WriteLine(v.Tostring()); }
        //Console.WriteLine("End Voxel TEST");
    }
}
