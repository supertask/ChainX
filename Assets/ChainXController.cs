using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ChainXController : MonoBehaviour
{
	private EmulatedSocket socket;
	private GameObject selectedObject;
	public ChainVoxel cv;
	public string log;
	public static object thisLock = new object();

	void Start() {
		this.cv = new ChainVoxel(this);
		//StartCoroutine (this.Run());
		this.socket = new EmulatedSocket (this);
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
					"{\"posID\": \"" + Operation.CreateRandomPosID(5) +
					"\", \"textureType\":\"" + UnityEngine.Random.Range(0,8) + "\"}" );
			}
			else if (Input.GetMouseButtonDown(Const.MOUSE_LEFT_CLICK))
			{
				Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
				RaycastHit hit = new RaycastHit ();

				if (selectedObject != null) {
					this.selectedObject.GetComponent<Renderer> ().material.shader = Shader.Find ("Diffuse");
				}
				if (Physics.Raycast (ray, out hit)) {
					Regex r = new Regex( @"[-]?[\d]+:[-]?[\d]+:[-]?[\d]+");
					GameObject hitObj = hit.collider.gameObject;
					if (r.IsMatch (hitObj.name)) {
						this.selectedObject = hitObj;
						this.selectedObject.GetComponent<Renderer> ().material.shader = Shader.Find ("Toon/Basic Outline");
					}
					else { this.selectedObject = null; }
				}
				else { this.selectedObject = null; }
			}


			if (this.selectedObject != null) {
				if (Input.GetKeyUp (KeyCode.UpArrow)) {
					o = this.CreateMoveOperation (1, 0, 0);
				} else if (Input.GetKeyUp (KeyCode.DownArrow)) {
					o = this.CreateMoveOperation (-1, 0, 0);
				} else if (Input.GetKeyUp (KeyCode.RightArrow)) {
					o = this.CreateMoveOperation (0, 0, -1);
				} else if (Input.GetKeyUp (KeyCode.LeftArrow)) {
					o = this.CreateMoveOperation (0, 0, 1);
				} else if (Input.GetKeyUp (KeyCode.U)) {
					o = this.CreateMoveOperation (0, 1, 0);
				} else if (Input.GetKeyUp (KeyCode.D)) {
					o = this.CreateMoveOperation (0, -1, 0);
				}
			}
			if (o != null) {
				string json = Operation.ToJson (o);
				this.socket.Send (json + "\r\n");
			}
		//}

		//lock (ChainXController.thisLock) {
		}
	}

	private Operation CreateMoveOperation(int dx, int dy, int dz)
	{
		Vector3 xyz;
		xyz = this.selectedObject.transform.position;
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
				if (anObj == this.selectedObject){
					/*
					 * For changing selectedObject.
					 */
					if (this.cv.movedPosIDs.ContainsKey(posID)) {
						string destPosID = this.cv.movedPosIDs[posID];	
						GameObject destObj = GameObject.Find (destPosID);
						if (destObj != null) {
							this.selectedObject = destObj;
							this.selectedObject.GetComponent<Renderer> ().material.shader = Shader.Find ("Toon/Basic Outline");
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
	}
}
