using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 構造層のためのStructureTableを実装したクラス．<br>
 * <br>
 * ChainVoxelのための構造層を実現したクラス．ChainVoxelクラスと組み合わせて使う．<br>
 * StructureTableへの操作はcreate，join，leaveの3種類をサポートしている．
 * @author kengo92i
 */
public class StructureTable {
	/**
     * グループ(gid)に属するグループメンバー(posID)を管理するためのテーブル
     */
	SortedDictionary<string, HashSet<string>> groupMembersTable; 

	/**
     * voxel(posID)が所属しているグループ(gid, ts)を管理するテーブル
     */
	SortedDictionary<string, HashSet<Group>> groupEntriesTable;

	/**
     * Structure Table のコンストラクタ
     */
	public StructureTable() {
		this.groupMembersTable = new SortedDictionary<string, HashSet<string>>(); 
		this.groupEntriesTable = new SortedDictionary<string, HashSet<Group>>();
	}

	/**
     * Structure Table にグループ(gid)を作成する．
     * 既に作成されたグループのgidの場合は実行されない．
     * @param gid グループ識別子
     * @see Operation
     */
	public void create(string gid) {
		if (groupMembersTable.ContainsKey(gid)) { // 既にグループ(gid)が存在する
			return;
		}
		groupMembersTable.Add(gid, new HashSet<string>());
	}

	/**
     * グループ(gid)にvoxel(posID)を参加させる
     * @param ts タイムスタンプ
     * @param posID voxel識別子
     * @param gid グループ識別子
     * @see Operation
     */
	public void join(long ts, string posID, string gid) {
		Group aGroup = new Group(gid, ts);
		if (!this.groupMembersTable.ContainsKey(gid) || Mathf.Abs(this.getTimestamp(posID, gid)) >= ts) {
			return;
		}

		// groupEntriesTable に Group(gid, ts) を追加
		if (!this.groupEntriesTable.ContainsKey(posID)) {
			this.groupEntriesTable.Add(posID, new HashSet<Group>());
		}
		this.groupEntriesTable[posID].Add(aGroup); 

		// groupMembersTable に posID を追加
		this.groupMembersTable[gid].Add(posID);

		// タイムスタンプの値を最新の値に更新する
		long maxTs = Util.Max(ts, this.getTimestamp(posID, gid));
		this.setTimestamp(maxTs, posID, gid);
	}

	/**
     * グループ(gid)からvoxel(posID)を脱退させる
     * @param sid site識別子
     * @param ts タイムスタンプ
     * @param posID voxel識別子
     * @param gid グループ識別子
     * @see Operation
     */
	public void leave(int sid, long ts, string posID, string gid) {
		Group aGroup = new Group(gid, ts);
		HashSet<Group> groupEntriesSet = this.groupEntriesTable[posID];

		if (groupEntriesSet == null || !groupEntriesSet.Contains(aGroup) || Mathf.Abs(this.getTimestamp(posID, gid)) >= ts) {
			return;
		} 

		// groupMembersTable から posID を削除 (グループからの脱退)
		this.groupMembersTable[gid].Remove(posID);

		// タイムスタンプの更新 + tombstone化
		long minTs = Util.Min(-1L * ts, this.getTimestamp(posID, gid));
		this.setTimestamp(minTs, posID, gid);
	}

	/**
     * posIDに関連したグループ(gid)のタイムスタンプを取得
     * @param posID voxel識別子
     * @param gid グループ識別子
     * @return posIDが関連しているgidのタイムスタンプ，存在しない場合は0を返す．
     */
	private long getTimestamp(string posID, string gid) {
		HashSet<Group> groupEntries = this.groupEntriesTable[posID];
		if (groupEntries == null) { return 0; }

		foreach (Group aGroup in groupEntries) {
			if (!aGroup.getGroupId().Equals(gid)) {
				continue;
			}
			return aGroup.getTimestamp();
		}
		return 0;
	}

	/**
     * posIDに関連したグループ(gid)のタイムスタンプを設定
     * @param ts 更新するタイムスタンプ
     * @param posID voxel識別子
     * @param gid グループ識別子
     * @return 値の更新に成功した場合はtrueを返す．失敗した場合はfalseを返す．
     */
	private bool setTimestamp(long ts, string posID, string gid) {
		HashSet<Group> groupEntries = this.groupEntriesTable[posID];
		if (groupEntries == null) { return false; }

		foreach (Group aGroup in groupEntries) {
			if (aGroup.getGroupId().Equals(gid)) {
				aGroup.setTimestamp(ts); 
				return true;
			} 
		}
		return false;
	}

	/**
     * posIDに関連したグループ(gid)が墓石か判定する
	 * Not used
     * @param posID voxel識別子
     * @param gid グループ識別子
     * @return 墓石ならtrueを返す．それ以外はfalseを返す．
     */
	private bool isTombstone(string posID, string gid) {
		return this.getTimestamp(posID, gid) < 0; 
	}

	/**
     * 指定したvoxelがグループ化中であるか判定する．
     * @param posID voxel識別子
     * @return グループ化中ならばtrue，そうでないならfalseを返す．
     */
	public bool isGrouped(string posID) {
		HashSet<Group> groupEntries = this.groupEntriesTable[posID];
		if (groupEntries == null) { return false; }

		foreach (Group aGroup in groupEntries) {
			if (aGroup.getTimestamp() > 0) {
				return true;
			}
		}
		return false;
	}

	/**
     * StructureTableの状態を確認するための文字列を取得する
     * 各siteのStructureTableの状態が同じであるか確認するために使用する
     * @return StructureTableの状態を示す文字列
     */
	public string getStatusString() {
		string statusString = "";
		foreach (KeyValuePair<string,HashSet<string>> entry in this.groupMembersTable) {
			statusString += entry.Key; 
			foreach (string gid in entry.Value) {
				statusString += gid;
			}
		}

		foreach (KeyValuePair<string,HashSet<Group>> entry in this.groupEntriesTable) {
			statusString += entry.Key;
			foreach (Group g in entry.Value) {
				statusString += g.ToString();
			}
		}

		return statusString;
	}

	/**
     * Structure Table の状態を出力する
     */
	public void show() {
		string res = "groupMembersTable:\n";
		for (Map.Entry<string, TreeSet<string>> entry : this.groupMembersTable.entrySet()) {
			res += "| " + entry.getKey() + " | -> " + entry.getValue();
		}
		res += "\n";

		res += "groupEntriesTable:";
		for (Map.Entry<string, TreeSet<Group<string, long>>> entry : this.groupEntriesTable.entrySet()) {
			res += "| " + entry.getKey() + " | -> " + entry.getValue();
		}
		res += "---\n";
		Debug.Log(res);

		return;
	}

	/**
     * Structure Table の状態を出力する
     * @param dumpMsg ダンプメッセージ
     */
	public void show(string dumpMsg) {
		System.out.println(dumpMsg);
		this.show();
	}
}