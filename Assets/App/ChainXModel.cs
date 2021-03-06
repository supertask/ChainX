﻿using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

struct Point
{
	public int x, y;
}

public class ChainXModel
{
	private ChainXController controller;
	private Vector3 paintTool3DPos;
	private Vector3 paintToolPlate3DPos;
	private Point paintToolLocation;
	private List<List<string>> paintTools;
	private ObjLoadHelper objLoadHelper;

	public static string PAINT_TOOL_POINTER_ID = "pointer";
	public static string PAINT_TOOL_VOXEL_ID = "voxel";
	public static string PAINT_TOOL_GROUP_ID = "group";
	public static string PAINT_TOOL_POLYGON_ID = "groupPolygon";
	public float VOXEL_PLATE_DIAMETER = 0.0f;

	public ChainXModel() {
		this.VOXEL_PLATE_DIAMETER = GameObject.Find(Const.PAINT_TOOL_PATH + "VoxelPlate").GetComponent<Renderer>().bounds.size.x - 2.2f;
		this.initPaintTool();
		this.objLoadHelper = new ObjLoadHelper();
	}

	public void SetController(ChainXController controller) {
		this.controller = controller;
	}

	/*
	 * 比率を出す
	 * diameter=円の直径
	 */
	public float GetScale(GameObject target, float diameter) {
		Vector3 maxVector, minVector, distanceVector;
		this.GetMaxMinPositions(target, out maxVector, out minVector);
		maxVector.Set(maxVector.x+0.5f, maxVector.y+0.5f, maxVector.z+0.5f);
		minVector.Set(minVector.x-0.5f, minVector.y-0.5f, minVector.z-0.5f);
		distanceVector = maxVector - minVector;
		//Debug.Log(distanceVector.x + " " + distanceVector.z);
		float biggestDistance = (float)Math.Sqrt((double)(distanceVector.x * distanceVector.x
			+ distanceVector.z * distanceVector.z));
		return diameter / biggestDistance;
	}

	/*
	 * 引数で渡されたグループゲームオブジェクトのY座標一番下でかつ、X座標とZ座標のセンター座標を割り出す。
	 * PaintToolのセンター座標を調節するために使われる。
	 */
	public Vector3 GetBottomCenterPosition(GameObject aParent, float y_margin) {
		Vector3 maxVector, minVector;
		this.GetMaxMinPositions(aParent, out maxVector, out minVector);
		Vector3 res = new Vector3(0, minVector.y - y_margin, 0);
		res.x = this.GetCenterPoint(maxVector.x + 0.5f, minVector.x - 0.5f);
		res.z = this.GetCenterPoint(maxVector.z + 0.5f, minVector.z - 0.5f);
		return res;
	}

	private float GetCenterPoint(float p, float q) {
		return q + ((p-q)/2.0f);
	}

	public void GetMaxMinPositions(GameObject aParent, out Vector3 maxVector, out Vector3 minVector)
	{
		minVector = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
		maxVector = new Vector3(int.MinValue, int.MinValue, int.MinValue);
		foreach(Transform aChildTransform in aParent.transform) {
			minVector = Vector3.Min(minVector, aChildTransform.position);
			maxVector = Vector3.Max(maxVector, aChildTransform.position);
		}
		return;
	}


	private void initPaintTool() {
		this.paintToolLocation.x = 0;
		this.paintToolLocation.y = 0;
		this.paintTools = new List<List<string>>();

		this.paintTools.Add(new List<string>() {ChainXModel.PAINT_TOOL_POINTER_ID});
		List<string> voxels = new List<string>();
		for(int i = 0; i < Const.NUMBER_OF_TEXTURE; ++i) {
			voxels.Add(ChainXModel.PAINT_TOOL_VOXEL_ID + i.ToString());
		}
		this.paintTools.Add(voxels);
		//this.paintTools.Add(); //Here
	}

	public string getPaintTool(int dx, int dy) {
		Point p = this.paintToolLocation;
		//this.paintTools[p.y + dy]; //Here
		p.y = Util.mod(p.y + dy, this.paintTools.Count);
		p.x = Util.mod(p.x + dx, this.paintTools[p.y].Count);

		this.paintToolLocation = p;
		//Debug.Log(p.x + " " + p.y);

		return this.paintTools[p.y][p.x]; //バグってる
	}

	public string getCurrentPaintTool() {
		return this.getPaintTool(0,0);
	}

	public void AddPolygonToUI(string gid) {

	}

	public void RemovePolygonToUI(string gid) {
	}

	public void AddGroupToUI(string gid) {
		if (this.paintTools.Count <= 2) { this.paintTools.Add(new List<string>()); }
		if (this.paintTools[2].Contains(gid)) { return; }
		this.paintTools[2].Add(gid);
	}

	public void RemoveGroupFromUI(string gid) {
		if (this.paintTools.Count <= 2) { return; }
		this.paintTools[2].Remove(gid);
		if (this.paintTools[2].Count == 0) {
			this.paintTools.RemoveAt(2);
		}
	}

	/*
	 * 各パラメータが0.5未満なら、切り捨てる。各パラメータが0.5以上なら繰り上げる。
	 */
	public static Vector3 GetRoundIntPoint(Vector3 point) {
		point.x = Mathf.RoundToInt(point.x);
		point.y = Mathf.RoundToInt(point.y);
		point.z = Mathf.RoundToInt(point.z);
		return point;
	}

	/*
	 * 
	 */
	public List<Operation> CreateMoveOperations(List<GameObject> objects, Vector3 transMatrix)
    {
		List<Operation> operations = new List<Operation>();
		foreach (GameObject anObj in objects) {
			if (anObj.transform.childCount > 0) {
				//
				//When selecting multiple voxels or polygon
				//
				if (anObj.transform.childCount == 1) {
					//When selecting polygon
					/*
					foreach (Transform child in anObj.transform) {
						string[] posIDs = ObjLoadHelper.GetEmptyVoxels(child.gameObject);
						//Debug.Log ("posID(emptyVoxel)" + posIDs [posIDs.Length-1]);
						operations.Add (new Operation (this.controller.socket.getID(), Operation.MOVE_POLYGON, "{\"gid\": \"" + anObj.name +
							"\", \"posIDs\": \"" + Util.GetCommaLineFrom (posIDs) +
							"\", \"transMatrix\": \"" + ChainXModel.CreatePosID (transMatrix) + "\"}")
						);
					}
					*/
				}
				else {
					//When selecting multiple voxels
					List<string> posIDs = new List<string> ();	
					List<string> destPosIDs = new List<string> ();	
					List<string> destTextureTypes = new List<string> ();	
					foreach (Transform child in anObj.transform)
					{
						//Debug.Assert (Util.CreatePosID (child.position) == child.name);
						Vector3 destPosition = Util.SplitPosID(child.name) + transMatrix;
						posIDs.Add(child.name);
						destPosIDs.Add(Util.CreatePosID(destPosition));
						Voxel aVoxel = this.controller.cv.getVoxel(child.name);
						destTextureTypes.Add(aVoxel.getTextureType().ToString());
					}

					string posIDsLine = Util.GetCommaLineFrom (posIDs);
					string destPosIDsLine = Util.GetCommaLineFrom (destPosIDs);
					Operation[] ops = new Operation[4];
					ops[0] = new Operation(
		               this.controller.socket.getID (), Operation.LEAVE_ALL,
		               "{\"gid\": \"" + anObj.name + "\", \"posIDs\": \"" + posIDsLine + "\"}"
					);
					ops[1] = new Operation(
		               this.controller.socket.getID (), Operation.DELETE_ALL,
		               "{\"gid\": \"" + anObj.name + "\", \"posIDs\": \"" + posIDsLine + "\"}"
					);
					ops[2] = new Operation (
						this.controller.socket.getID(), Operation.INSERT_ALL,
						"{\"gid\": \"" + anObj.name +
						"\", \"textureTypes\": \"" + Util.GetCommaLineFrom (destTextureTypes) +
						"\", \"posIDs\": \"" + destPosIDsLine + "\"}"
					);
					ops[3] = new Operation (
						this.controller.socket.getID(), Operation.JOIN_ALL,
						"{\"gid\": \"" + anObj.name + "\", \"posIDs\": \"" + destPosIDsLine + "\"}"
					);
					long ts = ops[0].getTimestamp ();
					for(int i = 0; i < ops.Length; i++) {
						ops[i].setTimestamp(ts + i); //できる限り1に近づけないと間に入り込まれる
						operations.Add(ops[i]);
					}
				}
			}
			else {
				//Debug.Log (anObj.name);
				//Debug.Log (anObj.transform.childCount);
				operations.Add(new Operation(
					this.controller.socket.getID(),
					Operation.MOVE,
					"{\"posID\": \"" + anObj.name +
					"\", \"transMatrix\": \"" + ChainXModel.CreatePosID(transMatrix) + "\"}")
				);
			}
		}
		return operations;
    }

	public List<Operation> CreateDeleteOperation(List<GameObject> objects) {
		List<Operation> operations = new List<Operation>();
		foreach (GameObject anObj in objects) {
			if (anObj.transform.childCount > 0) {
				if (anObj.transform.childCount == 1) {
					//For polygon
				}
				else {
					List<string> posIDs = new List<string> ();	
					foreach (Transform child in anObj.transform)
					{
						Debug.Assert (Util.CreatePosID (child.position) == child.name);
						posIDs.Add(child.name);
					}

					string posIDsLine = Util.GetCommaLineFrom (posIDs);
					Operation[] ops = new Operation[2];
					ops[0] = new Operation (
						this.controller.socket.getID (), Operation.LEAVE_ALL,
						"{\"gid\": \"" + anObj.name + "\", \"posIDs\": \"" + posIDsLine + "\"}"
					);

					ops[1] = new Operation (
						this.controller.socket.getID (), Operation.DELETE_ALL,
						"{\"gid\": \"" + anObj.name + "\", \"posIDs\": \"" + posIDsLine + "\"}"
					);
					long ts = ops[0].getTimestamp ();
					for (int i = 0; i < ops.Length; i++) {
						ops[i].setTimestamp(ts + i); //できる限り1に近づけないと間に入り込まれる
						operations.Add(ops[i]);
					}
				}
			}
			else {
				Operation op = new Operation (
					this.controller.socket.getID (),
					Operation.DELETE,
					"{\"posID\": \"" + anObj.name + "\"}"
				);
				operations.Add(op);
			}
		}
		return operations;
	}


    public static string CreatePosID(Vector3 pos) { return pos.x + ":" + pos.y + ":" + pos.z; }

	public string getPosIDsFromObj(GameObject anObj) {
		string res = "";
		foreach (Transform child in anObj.transform) {
			res += child.name + Const.SPLIT_CHAR;
		}
		return res.TrimEnd(Const.SPLIT_CHAR);
	}

	public string getPosIDsFromObjects(List<GameObject> selectedObjects) {
        string res = "";
        foreach (GameObject anObj in selectedObjects) {
			res += anObj.name + Const.SPLIT_CHAR;
        }
		return res.TrimEnd(Const.SPLIT_CHAR);
    }

	public static string CreateGID() {
		return ChainXModel.PAINT_TOOL_GROUP_ID + Util.GetGUID ();
	}


	public static void Test() {
		ChainXModel model = new ChainXModel();

		/*
		 * グループボクセルの中心座標を求める
		 */
		GameObject aParent = new GameObject("P");
		GameObject[] children = new GameObject[5];
		for(int i = 0; i < children.Length; ++i) {
			children[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
			children[i].SetActive(true);
			children[i].name = "c" + i.ToString();	
			children[i].transform.SetParent(aParent.transform);
		}

		//パターン1
		children[0].transform.position = new Vector3(0,0,0);
		children[1].transform.position = new Vector3(0,1,0);
		children[2].transform.position = new Vector3(0,2,0);
		children[3].transform.position = new Vector3(0,2,1);
		children[4].transform.position = new Vector3(1,2,0);
		Vector3 a,b;
		model.GetMaxMinPositions(aParent, out a, out b);
		Debug.Assert(a == new Vector3(1,2,1));
		Debug.Assert(b == new Vector3(0,0,0));
		Vector3 res = model.GetBottomCenterPosition(aParent, 0.0f);
		Debug.Assert(res == new Vector3(0.5f,0,0.5f));

		//パターン2
		children[0].transform.position = new Vector3(5,5,5);
		children[1].transform.position = new Vector3(5,5,6);
		children[2].transform.position = new Vector3(6,5,5);
		children[3].transform.position = new Vector3(5,0,5);
		children[4].transform.position = new Vector3(5,5,8);
		model.GetMaxMinPositions(aParent, out a, out b);
		Debug.Assert(a == new Vector3(6,5,8));
		Debug.Assert(b == new Vector3(5,0,5));
		res = model.GetBottomCenterPosition(aParent, 0.0f);
		Debug.Assert(res == new Vector3(5.5f, 0, 6.5f));

		//パターン3
		children[0].transform.position = new Vector3(-2,0,0);
		children[1].transform.position = new Vector3(-1,1,0);
		children[2].transform.position = new Vector3(-2,2,0);
		children[3].transform.position = new Vector3(-2,3,0);
		children[4].transform.position = new Vector3(-2,4,0);
		model.GetMaxMinPositions(aParent, out a, out b);
		Debug.Assert(a == new Vector3(-1,4,0));
		Debug.Assert(b == new Vector3(-2,0,0));
		res = model.GetBottomCenterPosition(aParent, 0.0f);
		Debug.Assert(res == new Vector3(-1.5f,0,0));
		//GameObject.Destroy(aParent); //ここで作成した可視化をしないようにしている

		/*
		 * Tests for PaintTool
		 */
		model.getPaintTool(0,1);
		Debug.Assert(model.getPaintTool(0,0) == "voxel0");
		Debug.Assert(model.getPaintTool(1,0) == "voxel1");
		Debug.Assert(model.getPaintTool(1,0) == "voxel2");
		Debug.Assert(model.getPaintTool(-1,0) == "voxel1");
		Debug.Assert(model.getPaintTool(-1,0) == "voxel0");
		Debug.Assert(model.getPaintTool(-1,0) == "voxel7");
		Debug.Assert(model.getPaintTool(0,1) == "pointer");
		model.AddGroupToUI("group1");
		Debug.Assert(model.getPaintTool(0,1) == "voxel0");
		Debug.Assert(model.getPaintTool(0,1) == "group1");
		model.AddGroupToUI("group1");
		Debug.Assert(model.getPaintTool(1,0) == "group1");
		model.RemoveGroupFromUI("group1");
		Debug.Assert(model.getCurrentPaintTool() == "pointer");
		Debug.Assert(model.getPaintTool(-1,0) == "pointer"); 
		Debug.Assert(model.getPaintTool(-1,0) == "pointer");

		Debug.Log("End a ChainXModel class test");
	}
}