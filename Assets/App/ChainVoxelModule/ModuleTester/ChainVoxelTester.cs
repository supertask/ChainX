using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainVoxelTester
{
	ChainVoxel cv;

	public ChainVoxelTester() {
		this.cv = new ChainVoxel(new ChainXController ());
	}

	private void CheckInsertPolygon(string gid, string[] posIDs, string objPath)
	{
		Operation o = new Operation (0, Operation.INSERT_POLYGON,
			"{\"gid\": \"" + gid +
			"\", \"posIDs\": \"" + Util.GetCommaLineFrom (posIDs) +
			"\", \"objPath\": \"" + objPath + "\"}");
		this.cv.apply(o, ChainVoxel.LOCAL_OPERATION);
		Debug.Assert (this.cv.getVoxel(posIDs[posIDs.Length - 1]) != null);
		Debug.Assert (this.cv.stt.isGroupingAll(posIDs));
	}

	private void CheckMovePolygon(string gid, string[] posIDs, string[] destPosIDs, Vector3 transMatrix)
	{
		destPosIDs = Util.ArrangePosIDs(destPosIDs, transMatrix);
		Operation o = new Operation(0, Operation.MOVE_POLYGON,
			"{\"gid\": \"" + gid +
			"\", \"posIDs\": \"" + Util.GetCommaLineFrom (posIDs) +
			"\", \"transMatrix\": \"" + Util.CreatePosID (transMatrix) + "\"}");
		//Debug.Log ("In test: posIDs=" + Util.GetCommaLineFrom(posIDs) + ", destPosIDs=" + Util.GetCommaLineFrom(destPosIDs));
		this.cv.apply (o, ChainVoxel.LOCAL_OPERATION);
		//ArrangeしてからCheck!!!!!
		Debug.Assert (this.cv.getVoxel(destPosIDs[destPosIDs.Length - 1]) != null); //Here
		Debug.Assert (this.cv.stt.isGroupingAll (destPosIDs));
	}

	public void CheckDeletePolygon(string gid, string[] posIDs) {
		Operation o = new Operation (0, Operation.DELETE_POLYGON,
			"{\"gid\": \"" + gid +
			"\", \"posIDs\": \"" + Util.GetCommaLineFrom (posIDs) + "\"}");
		this.cv.apply (o, ChainVoxel.LOCAL_OPERATION);
		Debug.Assert (this.cv.getVoxel(posIDs[posIDs.Length - 1]) == null);

		//timestampは更新されるが削除はされないのisGroupingAll関数を書き換える！！
		Debug.Assert(!this.cv.stt.isGroupingAll(posIDs)); //Here
	}


	public void TestPolygonOperations()
	{
		Vector3 transMatrix = Util.CreateRandomTransMatrix ();
		string gid = ChainXModel.PAINT_TOOL_GROUP_ID + Util.GetGUID ();
		string objPath = Const.TEST_OBJ_PATH + "monkey.obj";
		string[] posIDs = ObjLoadHelper.LoadOnlyObj(objPath, Util.CreateRandomVector3(-100, 100));
		string[] destPosIDs = new string[posIDs.Length];

		for (int p = 0; p < posIDs.Length; ++p) {
			Vector3 v = Util.SplitPosID(posIDs[p]);
			Vector3 destV = v + transMatrix;
			destPosIDs[p] = Util.CreatePosID(destV);
		}
		this.CheckInsertPolygon(gid, posIDs, objPath);

		switch(UnityEngine.Random.Range(1, 2+1))
		{
			case 1:
			{
				this.CheckMovePolygon(gid, posIDs, destPosIDs, transMatrix);
				this.CheckDeletePolygon(gid, destPosIDs); //ここで
				break;
			}
			case 2:
			{
				this.CheckDeletePolygon(gid, posIDs); //ここか
				break;
			}
		}

	}

	public void CheckInsertAll(string gid, string[] posIDs, string textureTypes)
	{
		Operation o = new Operation (0, Operation.INSERT_ALL,
			"{\"gid\": \"" + gid +
			"\", \"posIDs\": \"" + Util.GetCommaLineFrom (posIDs) +
			"\", \"textureTypes\": \"" + textureTypes + "\"}");
		this.cv.apply (o, ChainVoxel.LOCAL_OPERATION);
		Debug.Assert (this.cv.isIncludingAll(posIDs) );
		Debug.Assert (this.cv.stt.isGroupingAll(posIDs) );
	}

	public void CheckDeleteAll(string gid, string[] posIDs)
	{
		Operation o = new Operation (0, Operation.DELETE_ALL,
			"{\"gid\": \"" + gid +
			"\", \"posIDs\": \"" + Util.GetCommaLineFrom (posIDs) + "\"}");
		cv.apply (o, ChainVoxel.LOCAL_OPERATION);
		Debug.Assert (!this.cv.isIncludingAll (posIDs));
		Debug.Assert (!this.cv.stt.isGroupingAll (posIDs)); //バグ
	}

	public void CheckMoveAll(string gid, string[] posIDs,
			string[] destPosIDs, Vector3 transMatrix)
	{
		Operation o = new Operation (0, Operation.MOVE_ALL,
			"{\"gid\": \"" + gid +
			"\", \"posIDs\": \"" + Util.GetCommaLineFrom (posIDs) +
			"\", \"transMatrix\": \"" + Util.CreatePosID (transMatrix) + "\"}");
		this.cv.apply (o, ChainVoxel.LOCAL_OPERATION);
		Debug.Assert (this.cv.isIncludingAll (destPosIDs));
		Debug.Assert (this.cv.stt.isGroupingAll (destPosIDs));
	}

	public void CheckLeaveAll(string gid, string[] posIDs)
	{
		Operation o = new Operation (0, Operation.LEAVE_ALL,
			"{\"gid\": \"" + gid + "\"}");
		this.cv.apply (o, ChainVoxel.LOCAL_OPERATION);
		Debug.Assert (this.cv.isIncludingAll (posIDs));
		Debug.Assert (!this.cv.stt.isGroupingAll (posIDs));
	}


	//
	// Tests
	//----------------------------------------------------
	// TODO(Tasuku): gidからグループを辿り、それを移動、削除、グループ脱出を交互に実行するテストを追加
	//
	public void TestGroupOperations()
	{
		//ChainVoxel cv = new ChainVoxel (new ChainXController ());
		Operation o;
		int numberOfPosIDs = UnityEngine.Random.Range (1, 3+1);
		Vector3 transMatrix = Util.CreateRandomTransMatrix();
		string[] posIDs = new string[numberOfPosIDs];
		string[] destPosIDs = new string[numberOfPosIDs];
		string textureTypes = "";
		string gid = ChainXModel.PAINT_TOOL_GROUP_ID + Util.GetGUID ();

		for (int p = 0; p < numberOfPosIDs; ++p) {
			Vector3 v = Util.CreateRandomVector3 (-1000, 1000);
			Vector3 destV = v + transMatrix;
			posIDs [p] = Util.CreatePosID (v);
			destPosIDs [p] = Util.CreatePosID (destV);
			textureTypes += UnityEngine.Random.Range (0, 8+1).ToString () + Const.SPLIT_CHAR;
		}
		textureTypes = textureTypes.TrimEnd (Const.SPLIT_CHAR);

		//複数Voxelをinsertした後、それらをjoinする
		o = new Operation (0, Operation.CREATE,
			"{\"gid\": \"" + gid + "\"}");
		this.cv.apply (o, ChainVoxel.LOCAL_OPERATION);

		this.CheckInsertAll(gid, posIDs, textureTypes);

		//Debug.Log ("Inserted posIDs: " + Util.GetCommaLineFrom(posIDs));
		//this.cv.show();
		//this.cv.stt.show();

		switch(UnityEngine.Random.Range(1, 3+1)) {
		case 1:
			{
				this.CheckMoveAll(gid, posIDs, destPosIDs, transMatrix);
				this.CheckDeleteAll(gid, destPosIDs);
				break;
			}
		case 2:
			{
				this.CheckDeleteAll(gid, posIDs);
				break;
			}
		case 3:
			{
				this.CheckLeaveAll(gid, posIDs);
				break;
			}
		default: break;
		}
	}

	public void UTestFunction()
	{
		//int numberOfPosIDs = UnityEngine.Random.Range (1, 3);
		string[] posIDs = new string[3];
		posIDs[0] = "1:1:2";
		posIDs[1] = "1:1:3";
		posIDs[2] = "1:1:1";
		/*
		foreach (string posID in Util.ArrangePosIDs(posIDs, "0:0:-1")) {
			Debug.Log (posID);
		}
		*/
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
	public void Run()
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
		this.cv.apply(o);
		this.cv.show();
		o = new Operation (0, Operation.MOVE, "{\"posID\": \"" + posID + "\", \"transMatrix\": \"" + transMatrix + "\"}");
		this.cv.apply(o);
		this.cv.show();

		transMatrix = "1:0:0";
		o = new Operation (0, Operation.MOVE, "{\"posID\": \"" + posID + "\", \"transMatrix\": \"" + transMatrix + "\"}");
		this.cv.apply(o);
		this.cv.show();
		*/
		this.UTestFunction();

		//
		// グループVoxel（参加、離脱、移動）のテスト
		//
		int numberOfTest = Const.TEST_QUALITY;

		for (int t = 0; t < numberOfTest; t++) {
			//this.TestGroupOperations();
			this.TestPolygonOperations();
		}
		Debug.Log("End a ChainXVoxel class test");
	}


	public static void Test() {
		new ChainVoxelTester().Run();
	}
}