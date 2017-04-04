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
		return "(" + this.getGroupId() + ", " + this.getTimestamp().ToString() + ")";
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