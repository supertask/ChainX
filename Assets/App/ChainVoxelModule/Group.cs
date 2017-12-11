using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * StructureTableで管理するGroupEntryクラス．
 * (gid, ts)としてgidにタイムスタンプを紐付けるために作成．tsの状態は同値判定に影響しない．
 *
 * @author kengo92i
 */
public class Group {
	/**
     * GroupID
     */
	private string groupId;

	/**
     * タイムスタンプ
     */
	private long timestamp;

	public Group(string groupId, long timestamp) {
		this.groupId = groupId;
		this.timestamp = timestamp;
	}

	/**
     * @return 
     */
	public void setGroupId(string groupId) { this.groupId = groupId; }

	/**
     * @return 
     */
	public string getGroupId() { return this.groupId; }

	/**
     * @return
     */
	public void setTimestamp(long timestamp) { this.timestamp = timestamp; }

	/**
     * @return
     */
	public long getTimestamp() { return this.timestamp; }

	public override string ToString() {
		return "(gid=" + this.getGroupId() + ", ts=" + this.getTimestamp().ToString() + ")";
	}

	/**
	 * Test a Group and GroupComparer class.
	 */
	public static void Test() {
		long timestamp = Util.currentTimeNanos();
		string groupId = "g1";

		Group g = new Group(groupId, timestamp);
		Debug.Assert(g.getTimestamp() == timestamp);
		Debug.Assert(g.getGroupId() == groupId);

		HashSet<Group> set = new HashSet<Group>(new GroupComparer());
		set.Add(new Group("g1", 100));
		set.Add(new Group("g2", 200));
		set.Add(new Group("g1", 200));
		int cnt=0;
		foreach(Group aGroup in set) { cnt++; }
		Debug.Assert(cnt == 2);


		Debug.Assert(set.Contains(new Group("g1", 200)));
		Debug.Assert(set.Contains(new Group("g2", 2)));
	}
}


public class GroupComparer : IEqualityComparer<Group>
{
	public bool Equals(Group left, Group right) { 
		return left.getGroupId() == right.getGroupId();
	}
	public int GetHashCode(Group g)	{
		return g.getGroupId().GetHashCode();
	}
}