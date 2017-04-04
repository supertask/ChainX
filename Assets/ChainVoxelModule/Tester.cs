using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Tester : MonoBehaviour
{
	void Start () { Test(); }
	void Update () { }
	
	private void Test() {
		TestVoxel();	
		TestOperation();
		TestGroup();
		TestChainVoxel();	
		TestStructureTable();	
	}

	/**
	 * Test a Voxel class.
	 */
	public void TestVoxel() {
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

		//foreach (Voxel v in voxels) { Debug.Log(v.Tostring()); }
		//Debug.Log("End Voxel TEST");
	}

	/**
	 * Test an Operation class
	 */
	private void TestOperation() {
		Debug.Assert(Operation.INSERT == 0);
		Debug.Assert(Operation.DELETE == 1);
		Debug.Assert(Operation.CREATE == 2);
		Debug.Assert(Operation.JOIN == 3);
		Debug.Assert(Operation.LEAVE == 4);
		Debug.Assert(Operation.APPEND_ENTRIES == 124);
		Debug.Assert(Operation.REQUEST_VOTE == 125);
		Debug.Assert(Operation.VOTE == 126);
		Debug.Assert(Operation.REQUEST == 127);
		Debug.Assert(Operation.ACK == 128);

		Operation o;
		o = new Operation(0, Operation.INSERT, "1:1:1");
		Debug.Assert(o.getId() == 0);
		Debug.Assert(o.getOpType() == Operation.INSERT);
		Debug.Assert(o.getPosID() == "1:1:1");

		o = new Operation(1, Operation.DELETE, "1:1:1");
		Debug.Assert(o.getId() == 1);
		Debug.Assert(o.getOpType() == Operation.DELETE);
		Debug.Assert(o.getPosID() == "1:1:1");

		SortedDictionary<string,object> opParams = new SortedDictionary<string,object>();
		opParams["sid"] = 0;
		opParams["ts"] = 100;
		opParams["posID"] = "1:1:1";
		o = new Operation(0, opParams);
		Debug.Assert(o.getId() == 0);
		Debug.Assert(o.getTimestamp() > 0);
		Debug.Assert(o.getPosID() == "1:1:1");

		Debug.Assert((int) o.getParam("sid") == 0);
		Debug.Assert((long) o.getParam("ts") > 0);
		Debug.Assert((string) o.getParam("posID") == "1:1:1");
	}

	/**
	 * Test an OperationQueueList class.
	 */
	private void TestOperationQueueList() {
		int NumberOfSites = 5;
		OperationQueueList aList = new OperationQueueList(NumberOfSites);
		Debug.Assert(aList.getNumberOfSites() == 5);

		//Thread t1 = new Thread();

		//OperationQueueList aList = new OperationQueueList();
		Debug.Assert(aList.getNumberOfSites() == 0);
	}	

	/**
	 * Test a Group and GroupComparer class.
	 */
	private void TestGroup() {
		long timestamp = Util.currentTimeNanos();
		string groupId = "1:1:1";

		Group g = new Group (groupId, timestamp);
		Debug.Assert(g.getTimestamp() == timestamp);
		Debug.Assert(g.getGroupId() == groupId);

		HashSet<Group> set = new HashSet<Group>(new GroupComparer());
		set.Add(new Group("1:1:1", 100));
		set.Add(new Group("1:2:1", 200));
		set.Add(new Group("1:1:1", 200));
		int cnt=0;
		foreach(Group aGroup in set) { cnt++; }
		Debug.Assert(cnt == 2);
	}

	/**
	 * Test a StructureTable class.
	 */
	private void TestStructureTable() {
		StructureTable aTable = new StructureTable ();	
		aTable.create("");
		StructureTable stt = new StructureTable();



		List<string> gids =  new List<string>();
		List<string> posIDs = Arrays.<string>asList("1:1:1", "1:2:3", "5:1:9", "7:8:0", "9:4:1");
		for (int i = 0; i < 5; ++i) {
			gids.Add(UUID.randomUUID().tostring());
		}

		stt.create(gids.get(0));
		stt.create(gids.get(0)); // 既にあるグループを作成

		stt.join(1L, posIDs.get(0), gids.get(0));
		stt.join(2L, posIDs.get(1), gids.get(0));
		stt.join(3L, posIDs.get(2), gids.get(0));
		stt.leave(1, 4L, posIDs.get(1), gids.get(0));
		stt.show("正常な参加・脱退");

		stt.join(5L, posIDs.get(0), gids.get(1)); // 存在しないグループへの参加
		stt.leave(1, 5L, posIDs.get(1), gids.get(1)); // 参加していないグループからの脱退
		stt.show("不正な参加・脱退");

		stt.create(gids.get(1));
		stt.create(gids.get(2));
		stt.create(gids.get(3));
		stt.join(6L, posIDs.get(0), gids.get(1));
		stt.join(7L, posIDs.get(3), gids.get(3));
		stt.leave(1, 8L, posIDs.get(3), gids.get(3));
		stt.show("グループの追加");

		stt.join(8L, posIDs.get(1), gids.get(2));
		stt.join(9L, posIDs.get(1), gids.get(2));
		stt.join(7L, posIDs.get(1), gids.get(2));
		stt.leave(1, 8L, posIDs.get(0), gids.get(0));
		stt.leave(1, 10L, posIDs.get(0), gids.get(0));
		stt.leave(1, 11L, posIDs.get(0), gids.get(1));
		stt.show("複数のjoin・leaveの収束結果");

		for (int i = 0; i < 5; ++i) {
			System.out.println(posIDs.get(i) + " isGrouped() = " + stt.isGrouped(posIDs.get(i)));
		}
	}

	/**
	 * Test a ChainVoxel class.
	 */
	private void TestChainVoxel() {
		//ChainVoxel cv = new ChainVoxel();		
		//SortedDictionary<string, Voxel> s = new SortedDictionary<string, Voxel>();
		//Debug.Log(s);
	}
}