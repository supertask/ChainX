using System;
using System.Collections;
using System.Collections.Generic;
using Parabox.CSG;

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
	public static string PAINT_TOOL_POINTER_ID = "pointer";
	public static string PAINT_TOOL_VOXEL_ID = "voxel";
	public static string PAINT_TOOL_GROUP_ID = "group";
	public static string PAINT_TOOL_POLYGON_ID = "groupPolygon";
	public float VOXEL_PLATE_DIAMETER = 0.0f;

	public ChainXModel() {
		this.VOXEL_PLATE_DIAMETER = GameObject.Find(Const.PAINT_TOOL_PATH + "VoxelPlate").GetComponent<Renderer>().bounds.size.x - 2.2f;
		this.initPaintTool();
	}

	public string[] LoadObj(string[] filepaths) {
		GameObject targetObj = OBJLoader.LoadOBJFile(filepaths[0]);
		//Material[] materials = OBJLoader.LoadMTLFile (filepaths[1]);
		Texture2D texture = TextureLoader.LoadTexture(filepaths[2]);

		//TODO(Tasuku): 位置をどう決めるかをそのうち決める必要がある
		targetObj.transform.position = new Vector3 (0,5,0);

		//コリダー，マテリアルの設定
		if (targetObj.transform.childCount > 0) {
			foreach (Transform child in targetObj.transform) {
				child.gameObject.AddComponent<MeshCollider>();
				child.gameObject.GetComponent<Renderer>().material = new Material (Const.DIFFUSE_SHADER);
				child.gameObject.GetComponent<Renderer>().material.mainTexture = texture;
			}
		}

		//ボクセルブロックに合わせるため，メッシュを移動させる
		this.MoveCenterToCorner(targetObj);
		string[] emptyPosIDs = this.getEmptyVoxels(targetObj);
		string polygonPosID = emptyPosIDs[emptyPosIDs.Length - 1];
		return emptyPosIDs;
	}


	/*
	 * 範囲を計算
	 */
	/*
	public string[] CalcMergin(string[] filepaths) {
		GameObject targetObj = OBJLoader.LoadOBJFile(filepaths[0]);
		Vector3 exTargetV = new Vector3 (0,5,0);
		targetObj.transform.position = exTargetV;

		Vector3 meshMerginV = this.MoveCenterToCorner(targetObj);
		Vector3 positionMerginV = targetObj.transform.position - exTargetV;
	}
	*/

	/*
	 * 
	 * targetのポジション情報とtargetのメッシュをどのくらいずらすかのマージン情報を返す．
	 * @return 
	 */
	private Vector3 MoveCenterToCorner(GameObject target)
	{
		Vector3 halfSize = Vector3.zero;
		foreach (Transform t in target.transform) {
			Mesh m = t.gameObject.GetComponent<MeshFilter> ().mesh;
			halfSize = m.bounds.extents;
		}
		//
		//targetのboundsの一番右上の座標(maxV)と一番右下の座標(minV)
		//
		Vector3 maxV = target.transform.position + halfSize;
		Vector3 minV = target.transform.position - halfSize;

		//
		//Update maxV to fixed maxV for fitting into this voxels.
		//
		Vector3 fixedMaxV = Vector3.zero;
		fixedMaxV.x = Mathf.Round (maxV.x) + 0.5f;
		fixedMaxV.y = Mathf.Round (maxV.y) + 0.5f;
		fixedMaxV.z = Mathf.Round (maxV.z) + 0.5f;

		//
		//Move the target position for mergin between masV and fixedMaxV
		//
		Vector3 fixingMergin = Vector3.zero;
		fixingMergin = fixedMaxV - maxV;
		target.transform.position += fixingMergin;

		Vector3 cornerPosition = fixedMaxV - new Vector3 (0.5f, 0.5f, 0.5f);
		Vector3 verticesMergin = cornerPosition - target.transform.position;

		//
		// Move a position of the target
		//
		target.transform.position = cornerPosition;
		target.name = Util.CreatePosID(target.transform.position);

		//
		// Move meshes of the target
		//
		foreach (Transform child in target.transform) {
			child.position = child.position - verticesMergin;
		}

		return verticesMergin;

		//targetポリゴンの位置を可視化しデバッグ
		//GameObject anObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
		//anObj.transform.position = Util.SplitPosID(target.name);
		//Debug.Log ("obj position: " + target.transform.position);
	}

	/*
	 *
	 * @param target 一番右上の角のオブジェクトであり，このオブジェクトのみでポリゴン形成する
	 */
	private string[] getEmptyVoxels(GameObject target)
	{
		Vector3 totalSize = Vector3.zero;
		//TODO(Tasuku): totalSizeを全てのオブジェクトのBound情報から計算する必要がある
		foreach (Transform t in target.transform) {
			Mesh m = t.gameObject.GetComponent<MeshFilter> ().mesh;
			totalSize = m.bounds.size;
			//Debug.Log ("totalSize: " + m.bounds.size);
		}
		//minV, maxVを再計算
		Vector3 halfVoxelSize = new Vector3(0.5f, 0.5f, 0.5f);
		Vector3 maxV = target.transform.position;
		Vector3 minV = target.transform.position - totalSize + new Vector3(1,1,1);
		minV.x = Mathf.Round (minV.x);
		minV.y = Mathf.Round (minV.y);
		minV.z = Mathf.Round (minV.z);
		//Debug.Log("target: " + target.transform.position);
		//Debug.Log("maxV: " + maxV);
		//Debug.Log("minV: " + minV);

		//一番右上の角のボクセルをtargetとしており，リストの最後はtargetボクセルである
		List<string> posIDList = new List<string> ();
		for (int z = (int)minV.z; z <= (int)maxV.z; ++z) {
			for (int y = (int)minV.y; y <= (int)maxV.y; ++y) {
				for (int x = (int)minV.x; x <= (int)maxV.x; ++x) {
					posIDList.Add (Util.CreatePosID(new Vector3(x,y,z)));
					//GameObject aBooleanVoxel = GameObject.CreatePrimitive(PrimitiveType.Cube);
					//aBooleanVoxel.name = "EMPTY";
					//aBooleanVoxel.transform.position = new Vector3 (x,y,z);
				}
			}
		}
		//Debug.Log ("num of posIDs: " + posIDList.Count);
		//Debug.Log ("-----");
		Debug.Assert (posIDList.Count == 12);
		return posIDList.ToArray();
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
					foreach (Transform child in anObj.transform) {
						//Here!!!!!!!!!!!!!
						string[] posIDs = this.getEmptyVoxels(child.gameObject);
						//Debug.Log ("posID(emptyVoxel)" + posIDs [posIDs.Length-1]);
						operations.Add (new Operation (0, Operation.MOVE_POLYGON, "{\"gid\": \"" + anObj.name +
							"\", \"posIDs\": \"" + Util.GetCommaLineFrom (posIDs) +
							"\", \"transMatrix\": \"" + ChainXModel.CreatePosID (transMatrix) + "\"}")
						);
					}
				}
				else {
					//When selecting multiple voxels
					List<string> posIDs = new List<string> ();	
					foreach (Transform child in anObj.transform) {
						posIDs.Add(child.name);
					}
					operations.Add (new Operation (0, Operation.MOVE_ALL, "{\"gid\": \"" + anObj.name +
						"\", \"posIDs\": \"" + Util.GetCommaLineFrom (posIDs) +
						"\", \"transMatrix\": \"" + ChainXModel.CreatePosID (transMatrix) + "\"}")
					);
				}
			}
			else {
				operations.Add(new Operation (0, Operation.MOVE, "{\"posID\": \"" + anObj.name +
					"\", \"transMatrix\": \"" + ChainXModel.CreatePosID(transMatrix) + "\"}")
				);
			}
		}
		return operations;
    }

	public Operation CreateDeleteOperation(GameObject anObj) {
		if (anObj.transform.childCount > 0) {
			List<string> posIDs = new List<string> ();	
			foreach (Transform child in anObj.transform) { posIDs.Add (child.name); }
			return new Operation (0, Operation.DELETE_ALL, "{\"gid\": \"" + anObj.name +
				"\", \"posIDs\": \"" + Util.GetCommaLineFrom(posIDs) + "\"}");
		}
		else { return new Operation (0, Operation.DELETE, "{\"posID\": \"" + anObj.name + "\"}"); }
	}


    public static string CreatePosID(Vector3 pos) { return pos.x + ":" + pos.y + ":" + pos.z; }

	public string getPosIDsFrom(List<GameObject> selectedObjects) {
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