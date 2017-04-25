using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChainXController : MonoBehaviour
{
	private ChainXModel model;
	private EmulatedSocket socket;
	private GameObject selectedObject;

	void Start() {
		this.model = new ChainXModel();
		this.model.SetController(this);
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
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit = new RaycastHit ();
			if (Physics.Raycast (ray, out hit)) {
				this.selectedObject = hit.collider.gameObject;
				//this.selectedObject.renderer.material.shader = Shader.Find( "Self-Illumin/Diffuse" ); 
			}
		}

		if (this.selectedObject != null) {
			if (Input.GetKeyUp(KeyCode.UpArrow)) { o = this.CreateMoveOperation(1,0,0); }
			else if (Input.GetKeyUp(KeyCode.DownArrow)) { o = this.CreateMoveOperation(-1,0,0); }
			else if (Input.GetKeyUp(KeyCode.RightArrow)) { o = this.CreateMoveOperation(0,0,1); }
			else if (Input.GetKeyUp(KeyCode.LeftArrow)) { o = this.CreateMoveOperation(0,0,-1); }
			else if (Input.GetKeyUp(KeyCode.U)) { o = this.CreateMoveOperation(0,1,0); }
			else if (Input.GetKeyUp(KeyCode.D)) { o = this.CreateMoveOperation(0,-1,0); }
		}
		if (o != null) {
			string json = Operation.ToJson (o);
			//Debug.Log ("送信したJson: " + json);
			this.socket.Send (json + "\r\n");
		}
			
		this.UpdateVoxels();
		this.SetUpGUICompornets();
	}

	private Operation CreateMoveOperation(int dx, int dy, int dz) {
		Vector3 xyz = this.selectedObject.transform.position;
		string posID = xyz.x + ":" + xyz.y + ":" + xyz.z;
		string destPosID = (xyz.x+dx) + ":" + (xyz.y+dy) + ":" + (xyz.z+dz);
		return new Operation (0, Operation.MOVE,"{\"posID\": \"" + posID
				+ "\", \"destPosID\": \"" + destPosID + "\"}"); 
	}

	/*
	 * 
	 */
	public void UpdateVoxels() {
		ChainVoxel cv = this.model.getChainVoxel ();
		foreach (String posID in cv.insertedPosIDs) {
			if (GameObject.Find (posID) == null) {
				string[] xyzs = posID.Split (':');
				GameObject voxelObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
				Material material = new Material(Shader.Find("Diffuse"));
				material.color = Color.red;
				//voxelObj.renderer.material = material;
				//voxelObj.renderer = new Material(Shader.Find("Unlit/Color"));
				//マテリアルを使って、テクスチャまたは色を付け加える
				voxelObj.name = posID;
				voxelObj.transform.position = new Vector3(int.Parse(xyzs[0]), int.Parse(xyzs[1]), int.Parse(xyzs[2]) );
			}
		}
		cv.insertedPosIDs.Clear ();

		foreach (String posID in cv.deletedPosIDs) {
			GameObject.Destroy(GameObject.Find (posID));
		}
		cv.deletedPosIDs.Clear ();
	}

	public void SetUpGUICompornets() {
		GameObject anObject = GameObject.Find("DebugLog/Viewport/Content");
		Text log = anObject.GetComponent<Text>();
		log.text = this.model.GetLog(); //this.showStructureTable();
	}

	private void OnApplicationQuit() {
		this.socket.Close();
	}

	IEnumerator Run()
	{
		/*
		cv.apply(new Operation(Operation.CREATE,
			new SortedDictionary<string, object>() {{"gid", "a_table"}} ));
		cv.apply(new Operation(Operation.JOIN,
			new SortedDictionary<string, object>() {{"gid", "a_table"}, {"posID", "1:1:1"}} ));
		*/
		ChainVoxel cv = this.model.getChainVoxel ();
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

	public void OperateVoxelOnLocal(Operation op) {
		ChainVoxel cv = this.model.getChainVoxel ();
		cv.apply(op);
	}

	public ChainXModel getModel() { return this.model; }
}
