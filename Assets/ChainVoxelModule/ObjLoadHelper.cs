using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * An ObjLoadHelper class
 * This class is used for 
 */
public class ObjLoadHelper
{
	public static string[] LoadObj(string[] filepaths, Vector3 targetV, bool isTest=true)
	{
		GameObject target = OBJLoader.LoadOBJFile(filepaths[0]);
		string[] emptyPosIDs = ObjLoadHelper.ShiftPosition(target, targetV);

		if (target.transform.childCount > 0) {
			Texture2D texture = TextureLoader.LoadTexture(filepaths[2]);
			foreach (Transform child in target.transform) {
				child.gameObject.AddComponent<MeshCollider> ();
				child.gameObject.GetComponent<Renderer> ().material = new Material (Const.DIFFUSE_SHADER);
				child.gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
			}
		}
		return emptyPosIDs;
	}

	public static string[] LoadOnlyObj(string filepath, Vector3 targetV) {
		GameObject target = OBJLoader.LoadOBJFile(filepath);
		string[] emptyPosIDs = ObjLoadHelper.ShiftPosition(target, targetV);
		Object.Destroy(target);
		return emptyPosIDs;
	}

	private static string[] ShiftPosition(GameObject target, Vector3 targetV) {
		//TODO(Tasuku): 位置をどう決めるかをそのうち決める必要がある
		target.transform.position = targetV;

		//ボクセルブロックに合わせるため，メッシュを移動させる
		ObjLoadHelper.MoveCenterToCorner(target);
		string[] emptyPosIDs = ObjLoadHelper.GetEmptyVoxels(target);
		return emptyPosIDs;	
	}


	/*
	 * 
	 * targetのポジション情報とtargetのメッシュをどのくらいずらすかのマージン情報を返す．
	 * @return 
	 */
	private static Vector3 MoveCenterToCorner(GameObject target)
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
	public static string[] GetEmptyVoxels(GameObject target)
	{
		Vector3 totalSize = Vector3.zero;
		//TODO(Tasuku): totalSizeを全てのオブジェクトのBound情報から計算する必要がある
		foreach (Transform t in target.transform) {
			Mesh m = t.gameObject.GetComponent<MeshFilter> ().mesh;
			totalSize = m.bounds.size;
			//Debug.Log ("totalSize: " + m.bounds.size);
		}
		//minV, maxVを再計算
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
	 * Gets object mergins.
	 * @param objPath
	 * @param targetV
	 * @return a string combined position mergin and mesh mergin of a target game object.
	 * TODO(Tasuku): Do not use LoadOBJFile(). This is a bottleneck of this system.
	 */
	/*
	private static string GetObjMergin(string objPath, Vector3 targetV) {
		GameObject targetObj = OBJLoader.LoadOBJFile(objPath);
		targetObj.transform.position = targetV;

		//A position of target object moves from center to the corner.
		Vector3 meshMerginV = ObjLoadHelper.MoveCenterToCorner(targetObj);
		Vector3 positionMerginV = targetObj.transform.position - targetV;
		Object.Destroy(targetObj);

		return Util.CreatePosID(positionMerginV) + Const.SPLIT_CHAR + Util.CreatePosID(meshMerginV);
	}

	public static string GetNewObjPath(string objPath, Vector3 targetV) {
		return ObjLoadHelper.GetObjMergin(objPath, targetV) + Const.SPLIT_CHAR + objPath;
	}
	*/
}
