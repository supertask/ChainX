using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ChainXController : MonoBehaviour
{
	private EmulatedSocket socket;
	private static GameObject selectedObject;
	public static ChainVoxel cv;
	public static string log;

	void Start() {
		ChainXController.cv = new ChainVoxel(this);
		//StartCoroutine (this.Run());
		this.socket = new EmulatedSocket (this);
	}

	void Update()
	{
		Operation o = null;
		if (Input.GetKeyDown (KeyCode.S)) {
		}
		else if (Input.GetKeyDown (KeyCode.C)) {
			o = new Operation (0, Operation.INSERT, "{\"posID\": \"" + Operation.CreateRandomPosID(5) + "\"}" );
		}
		else if (Input.GetMouseButtonDown(Const.MOUSE_LEFT_CLICK))
		{
			/*
			 * Here バグってるー
			 */
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit = new RaycastHit ();

			if (selectedObject != null) {
				ChainXController.selectedObject.GetComponent<Renderer> ().material.shader = Shader.Find ("Diffuse");
			}
			if (Physics.Raycast (ray, out hit)) {
				Regex r = new Regex( @"[-]?[\d]+:[-]?[\d]+:[-]?[\d]+");
				GameObject hitObj = hit.collider.gameObject;
				if (r.IsMatch (hitObj.name)) {
					ChainXController.selectedObject = hitObj;
					ChainXController.selectedObject.GetComponent<Renderer> ().material.shader = Shader.Find ("Toon/Basic Outline");
				}
				else { ChainXController.selectedObject = null; }
			}
			else { ChainXController.selectedObject = null; }
		}


		if (ChainXController.selectedObject != null) {
			if (Input.GetKeyUp(KeyCode.UpArrow)) { o = this.CreateMoveOperation(1,0,0); }
			else if (Input.GetKeyUp(KeyCode.DownArrow)) { o = this.CreateMoveOperation(-1,0,0); }
			else if (Input.GetKeyUp(KeyCode.RightArrow)) { o = this.CreateMoveOperation(0,0,-1); }
			else if (Input.GetKeyUp(KeyCode.LeftArrow)) { o = this.CreateMoveOperation(0,0,1); }
			else if (Input.GetKeyUp(KeyCode.U)) { o = this.CreateMoveOperation(0,1,0); }
			else if (Input.GetKeyUp(KeyCode.D)) { o = this.CreateMoveOperation(0,-1,0); }
		}
		if (o != null) {
			string json = Operation.ToJson (o);
			this.socket.Send (json + "\r\n");
		}
			
		this.UpdateVoxels();
		this.SetUpGUICompornets();
	}

	private Operation CreateMoveOperation(int dx, int dy, int dz)
	{
		Vector3 xyz = ChainXController.selectedObject.transform.position;
		string posID = xyz.x + ":" + xyz.y + ":" + xyz.z;
		string destPosID = (xyz.x+dx) + ":" + (xyz.y+dy) + ":" + (xyz.z+dz);
		return new Operation (0, Operation.MOVE,"{\"posID\": \"" + posID
				+ "\", \"destPosID\": \"" + destPosID + "\"}"); 
	}

	/*
	 * 
	 */
	public void UpdateVoxels() {
		ChainVoxel cv = ChainXController.cv;

		/*
		 * For MOVE operation.
		 * 編集の必要あり！！！
		 */
		if (ChainXController.selectedObject) {
			lock(ChainXController.selectedObject) {
				foreach (KeyValuePair<string,string> aPair in ChainVoxel.movedPosIDs) { //InvalidOperationException: out of sync(lockをする。ループを回している最中に書き込みがある時のエラー)
					string posID = aPair.Key;
					string destPosID = aPair.Value;
					GameObject voxelObj = ChainXController.selectedObject; //GameObject.Find (posID); //見つからない

					voxelObj.name = destPosID; //NullReferenceException: Object reference not set to an instance of an object
					voxelObj.transform.position = this.ConvertPosID (destPosID);
				}
				ChainVoxel.movedPosIDs.Clear();
			}
		}

		/*
		 * For INSERT operation.
		 */
		foreach (string posID in cv.insertedPosIDs) {
			if (GameObject.Find (posID) == null)
			{
				GameObject voxelObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

				//色とテクスチャを加える
				Texture2D texture = Resources.Load<Texture2D> ("Textures/A");
				Material material = new Material(Shader.Find("Diffuse"));
				//Material material = new Material(Shader.Find("Toon/Lit Outline"));
				material.mainTexture = texture;
				//material.color = Color.red;
				voxelObj.GetComponent<Renderer> ().material = material;

				voxelObj.name = posID;
				voxelObj.transform.position = this.ConvertPosID (posID);
			}
		}
		cv.insertedPosIDs.Clear ();

		/*
		 * For DELETE operation.
		 */
		foreach (string posID in cv.deletedPosIDs) {
			GameObject.Destroy(GameObject.Find (posID));
		}
		cv.deletedPosIDs.Clear ();
	}

	public Vector3 ConvertPosID(string posID) {
		string[] xyzs = posID.Split (':');
		return new Vector3(int.Parse(xyzs[0]), int.Parse(xyzs[1]), int.Parse(xyzs[2]) );
	}

	public void SetUpGUICompornets() {
		GameObject anObject = GameObject.Find("DebugLog/Viewport/Content");
		Text log = anObject.GetComponent<Text>();
		log.text = ChainXController.log; //this.showStructureTable();
	}

	private void OnApplicationQuit() {
		this.socket.Close();
	}

		/*
	IEnumerator Run()
	{
		cv.apply(new Operation(Operation.CREATE,
			new SortedDictionary<string, object>() {{"gid", "a_table"}} ));
		cv.apply(new Operation(Operation.JOIN,
			new SortedDictionary<string, object>() {{"gid", "a_table"}, {"posID", "1:1:1"}} ));
		*/
		/*
		ChainVoxel cv = ChainXController.cv;
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (3, Operation.INSERT, "{\"posID\": \"1:1:1\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (3, Operation.INSERT, "{\"posID\": \"1:1:2\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (5, Operation.INSERT, "{\"posID\": \"1:2:1\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (2, Operation.INSERT, "{\"posID\": \"1:1:1\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (4, Operation.INSERT, "{\"posID\": \"1:1:1\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (3, Operation.DELETE, "{\"posID\": \"1:1:1\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (4, Operation.INSERT, "{\"posID\": \"0:1:1\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (4, Operation.INSERT, "{\"posID\": \"0:0:0\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (4, Operation.INSERT, "{\"posID\": \"1:1:1\"}"));

		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (5, Operation.MOVE, "{\"posID\": \"1:1:1\", \"destPosID\": \"2:2:2\"}"));
	}
		*/

}
