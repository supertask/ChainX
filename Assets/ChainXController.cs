using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ChainXController : MonoBehaviour
{
	private EmulatedWebSocket socket;
	private ChainXModel model;
	public ChainVoxel cv;
	public string log;
	public static object thisLock = new object();

	private GameObject selectedObject;
	private List<GameObject> selectedObjects;

	IEnumerator Start() {
		this.cv = new ChainVoxel(this);
		//this.cv.LoadFile();
		this.selectedObjects = new List<GameObject> ();
		this.socket = new EmulatedWebSocket (this);
		StartCoroutine(this.socket.Connect());
		yield return this.socket.Listen();
	}

	void Update()
	{
		lock (ChainXController.thisLock) {

			this.UpdateVoxels ();
			this.SetUpGUICompornets ();

			Operation o = null;
			if (Input.GetKeyDown (KeyCode.B)) {
			}
			else if (Input.GetKeyDown (KeyCode.V)) {
				o = new Operation (0, Operation.INSERT,
					"{\"posID\": \"" + Operation.CreateRandomPosID (5) +
					"\", \"textureType\":\"" + UnityEngine.Random.Range (0, 8) + "\"}");
			}
			else if (Input.GetMouseButtonDown(Const.MOUSE_LEFT_CLICK))
			{
				GameObject hitObj = this.isHitVoxel ();

				if (hitObj) {
					if (this.selectedObjects.Contains(hitObj)) {
						if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
							hitObj.GetComponent<Renderer> ().material.shader = Const.DIFFUSE_SHADER;
							this.selectedObjects.Remove(hitObj);
						} else {
							this.cleanSelectedObjects();
							this.AddToSelectedObjects(hitObj);
						}
					}
					else {
						if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
							this.AddToSelectedObjects(hitObj);
						else {
							if (this.selectedObjects.Count > 0)
								this.cleanSelectedObjects();
							this.AddToSelectedObjects(hitObj);
						}
					}
				}
				else {
					if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) { }
					else
						this.cleanSelectedObjects();
				}
				//Debug.Log(this.selectedObjects.Count);
			}

			if (this.selectedObjects.Count > 0) {
				if (Input.GetKeyUp (KeyCode.UpArrow)) o = this.CreateMoveOperation (1, 0, 0);
				else if (Input.GetKeyUp (KeyCode.DownArrow)) o = this.CreateMoveOperation (-1, 0, 0);
				else if (Input.GetKeyUp (KeyCode.RightArrow)) o = this.CreateMoveOperation (0, 0, -1);
				else if (Input.GetKeyUp (KeyCode.LeftArrow)) o = this.CreateMoveOperation (0, 0, 1);
				else if (Input.GetKeyUp (KeyCode.U)) o = this.CreateMoveOperation (0, 1, 0);
				else if (Input.GetKeyUp (KeyCode.D)) o = this.CreateMoveOperation (0, -1, 0);
				else if (Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.LeftCommand) ||
					Input.GetKey (KeyCode.RightControl) || Input.GetKey(KeyCode.RightCommand))
				{
					if (Input.GetKeyDown (KeyCode.D)) {
						//Here
						//TODO(Tasuku): selectedObjectを編集する
						foreach (GameObject anObj in this.selectedObjects)
							o = new Operation (0, Operation.DELETE, "{\"posID\": \"" + anObj.name + "\"}");
						this.selectedObjects.Clear();
					}
					else if (Input.GetKeyDown (KeyCode.G)) {
						/*
						o = new Operation (0, Operation.JOIN_ALL, "{\"posIDs\": \"" + 
							this.getPosIDsFromSelectedObjects() + "\"}");
						*/
					}
				}
			}
			if (o != null) {
				this.socket.Send (MessageHeader.OPERATION + Operation.ToJson (o) + "\n");
				Debug.Log(this.cv.show());
			}
		}
	}

	private string getPosIDsFromSelectedObjects() {
		string res = "";
		foreach (GameObject anObj in this.selectedObjects) {
			res += anObj.name + ",";
		}
		return res.TrimEnd(',');
	}

	private void cleanSelectedObjects()
	{
		foreach(GameObject anObj in this.selectedObjects)
			anObj.GetComponent<Renderer> ().material.shader = Const.DIFFUSE_SHADER;
		this.selectedObjects.Clear();
	}

	private void AddToSelectedObjects(GameObject hitObj)
	{
		hitObj.GetComponent<Renderer> ().material.shader = Const.TOON_SHADER;
		this.selectedObjects.Add(hitObj);
	}

	private GameObject isHitVoxel()
	{
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit = new RaycastHit ();
		Regex r = new Regex( @"[-]?[\d]+:[-]?[\d]+:[-]?[\d]+");
		if (Physics.Raycast (ray, out hit)) {
			GameObject hitObj = hit.collider.gameObject;
			if (r.IsMatch (hitObj.name)) return hitObj;
		}
		return null;
	}

	private Operation CreateMoveOperation(int dx, int dy, int dz)
	{
		Vector3 xyz;
		xyz = this.selectedObjects[0].transform.position;
		string posID = xyz.x + ":" + xyz.y + ":" + xyz.z;
		string destPosID = (xyz.x+dx) + ":" + (xyz.y+dy) + ":" + (xyz.z+dz);
		return new Operation (0, Operation.MOVE,"{\"posID\": \"" + posID
				+ "\", \"destPosID\": \"" + destPosID + "\"}"); 
	}

	/*
	 * 
	 */
	public void UpdateVoxels() {
		/*
		 * For INSERT operation.
		 */
		foreach (string posID in this.cv.insertedPosIDs) {
			if (GameObject.Find (posID) == null) {
				this.CreateVoxel (this.cv.getVoxel(posID).getTextureType(), posID);
			}
		}
		cv.insertedPosIDs.Clear ();

		/*
		 * For DELETE & MOVE operation.
		 */
		foreach (string posID in this.cv.deletedPosIDs) {
			GameObject anObj = GameObject.Find (posID);
			if (anObj != null) {
				if (this.selectedObjects.Count > 0) {
					if (anObj == this.selectedObjects [0]) { //ここでout of range
						/*
						 * For changing selectedObject.
				 		 */
						if (this.cv.movedPosIDs.ContainsKey (posID)) {
							string destPosID = this.cv.movedPosIDs [posID];	
							GameObject destObj = GameObject.Find (destPosID);
							if (destObj != null) {
								this.selectedObjects [0] = destObj;
								this.selectedObjects [0].GetComponent<Renderer> ().material.shader = Shader.Find ("Toon/Basic Outline");
							}
						}
					}
				}
				/*
				 * Remove following a ChainVoxel.
				 */
				GameObject.Destroy(anObj);
			}
		}
		cv.deletedPosIDs.Clear ();
	}

	/*
	 * 
	 * textureTypeまたは、colorTypeの一方は必ず0以上、もう一方は0未満にしなくてはならない。
	 */
	private void CreateVoxel(int textureType, string posID)
	{
		GameObject voxelObj = GameObject.CreatePrimitive (PrimitiveType.Cube);
		Material material = new Material (Shader.Find ("Diffuse"));
		//Material material = new Material(Shader.Find("Toon/Lit Outline")); //影をつける
		Texture2D texture = Resources.Load<Texture2D> ("Textures/" + textureType.ToString());
		material.mainTexture = texture;
		voxelObj.GetComponent<Renderer> ().material = material;
		voxelObj.name = posID;
		voxelObj.transform.position = this.ConvertPosID (posID);
	}

	public Vector3 ConvertPosID(string posID) {
		string[] xyzs = posID.Split (':');
		return new Vector3(int.Parse(xyzs[0]), int.Parse(xyzs[1]), int.Parse(xyzs[2]) );
	}

	public void SetUpGUICompornets() {
		lock(ChainXController.thisLock) {
			GameObject anObject = GameObject.Find("DebugLog/Viewport/Content");
			Text log = anObject.GetComponent<Text>();
			log.text = this.log; //this.showStructureTable();
		}
	}

	private void OnApplicationQuit() {
		this.socket.Close();
		this.socket.Send(MessageHeader.TEXT_FILE + this.cv.GetSavedData()); //ここ
		//TODO(Tasuku): SaveしましたのWindowを表示して終わる!!
	}

}
