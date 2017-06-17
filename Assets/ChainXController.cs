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
	private GameObject paintTool;
	public string log;
	public static object thisLock = new object();

	private List<GameObject> selectedObjects;

	IEnumerator Start() {
		this.model = new ChainXModel();
		this.paintTool = GameObject.Find("MouseCursor");
		this.paintTool.GetComponent<Renderer>().enabled = false;
		this.paintTool.layer = Const.UI_LAYER;
		this.paintTool = this.CreateVoxel(0, "SelectingVoxel", Const.PAINT_TOOL_VOXEL_POSITION);
		this.paintTool.layer = Const.UI_LAYER;

		/*
		*/
		//Vector3 tmp = Camera.main.ScreenToWorldPoint (new Vector3(70,40,8));
		//GameObject voxelObj = GameObject.Find("UI3D/VoxelPlate");
		//voxelObj.transform.position = tmp; 
		//Debug.Log ("ワールド座標" + tmp);

		this.cv = new ChainVoxel(this);
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

			GameObject ggg = GameObject.Find("PaintTool");
			ggg.transform.position = Const.PAINT_TOOL_PLATE_POSITION;
			foreach(Transform aChildTransform in ggg.transform) {
				aChildTransform.position = Const.PAINT_TOOL_PLATE_POSITION;
			}


			Operation o = null;
			if (Input.GetKeyDown (KeyCode.V)) {
				o = new Operation (0, Operation.INSERT,
					"{\"posID\": \"" + Operation.CreateRandomPosID (5) +
					"\", \"textureType\":\"" + UnityEngine.Random.Range (0, 8) + "\"}");
			}
			else if (Input.GetMouseButtonDown(Const.MOUSE_LEFT_CLICK))
			{
				if (this.clickUI()) {
					this.cleanSelectedObjects(); //Voxelの選択解除
				}
				else if (this.paintTool.name == "MouseCursor") {
					this.clickVoxel(); //マウスクリックしてオブジェクトを選択する
				}
				else {
					//VoxelまたはVoxelsが選択中のとき
					this.paintVoxels(); //VoxelまたはVoxelsをペイントする
				}
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
						//TODO(Tasuku): selectedObjectsを編集する
						foreach (GameObject anObj in this.selectedObjects)
							o = new Operation (0, Operation.DELETE, "{\"posID\": \"" + anObj.name + "\"}");
						this.selectedObjects.Clear();
					}
					else if (Input.GetKeyDown (KeyCode.G))
					{
						string gid = "G" + Util.GetGUID();

						if (this.selectedObjects.Count == 1) {
							if (this.selectedObjects[0].transform.childCount > 0) {
								//選択したオブジェクトが一つで、それがグループである場合、グループを解除
								o = new Operation (0, Operation.LEAVE_ALL, "{\"posIDs\": \"" + 
									this.getPosIDsFromSelectedObjects() + "\", \"gid\": \"" + gid +
								"\"}");
							}
							else { o = null; }
						}
						else {
							//グループをそもそも作成し忘れてる
							o = new Operation (0, Operation.JOIN_ALL, "{\"posIDs\": \"" + 
								this.getPosIDsFromSelectedObjects() + "\", \"gid\": \"" + gid +
							"\"}");
						}

					}
				}
			}
			if (o != null) {
				this.socket.Send (MessageHeader.OPERATION + Operation.ToJson (o) + "\n");
				Debug.Log(this.cv.show());
			}
		}
	}

	/*
	 * PaintToolのVoxelまたはグループVoxelが選択されていて、マウスクリックされたときに呼ばれる。
	 * ペイントさせ方は、ぶつかったオブジェクト（Plane）よりも手前のオブジェクトを
	 * 「後ろ側から手前に」辿っていき、Voxelを挿入する。
	 */
	private void paintVoxels() {
		//UIを操作した際に、UIボタンと同時に、paintVoxelsも起動してしまう
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit = new RaycastHit ();
		if (Physics.Raycast (ray, out hit)) {
			float distance = hit.distance - 0.5f;
			Vector3 hitPointShort = ray.GetPoint(distance); //ヒットしたRayより少し手前のPointをたどる
			Vector3 fixedhitPointShort = ChainXModel.GetRoundIntPoint(hitPointShort);
			Debug.Log(hitPointShort.ToString());
			Debug.Log(ChainXModel.GetRoundIntPoint(hitPointShort));
			this.CreateVoxel(0, "xxx", fixedhitPointShort);

			Debug.DrawLine(ray.origin, hitPointShort, Color.red, 60.0f, true);
		}
	}


	/*
	 * PaintToolのマウスカーソルが選択されていて、マウスクリックされたときに呼ばれる。
	 */
	private void clickVoxel() {
		GameObject hitObj = this.getHitObject();

		if (hitObj != null && Const.REGEX_POSID.IsMatch (hitObj.name)) {
			if (hitObj.transform.parent != null) {
				hitObj = hitObj.transform.parent.gameObject;
			}

			if (this.selectedObjects.Contains(hitObj)) {
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
					this.RemoveToSelectedObjects(hitObj);
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
			//ゲームオブジェクトないのものでない時（Voxel）
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) { }
			else { this.cleanSelectedObjects(); }
		}
	}

	/*
	 * PaintToolの左キー、右キー、下キーがマウスクリックされたときに呼ばれる。
	 */
	private bool clickUI() {
		GameObject hitObj = null;
		Ray ray = GameObject.Find("SubCamera").GetComponent<Camera>().ScreenPointToRay (Input.mousePosition);
		RaycastHit hit = new RaycastHit ();
		bool isHitOnUI = true;
		if (Physics.Raycast (ray, out hit)) {
			hitObj = hit.collider.gameObject;

			this.paintTool.GetComponent<Renderer>().enabled = false;
			if (hitObj.name == "ArrowLeft") {
				this.paintTool = this.getPaintToolObj(this.model.getPaintTool(-1,0));
			}
			else if (hitObj.name == "ArrowRight") {
				this.paintTool = this.getPaintToolObj(this.model.getPaintTool(1,0));
			}
			else if (hitObj.name == "ArrowTop") {
				this.paintTool = this.getPaintToolObj(this.model.getPaintTool(0,1));
			}
			else {
				//ヒットしなかった場合
				this.paintTool.GetComponent<Renderer>().enabled = true;
				isHitOnUI = false;
			}
			paintTool.layer = Const.UI_LAYER;

		}
		return isHitOnUI;
	}


	private GameObject getPaintToolObj(string paintToolStr) {
		GameObject anObj = null;
		if (paintToolStr == ChainXModel.PAINT_TOOL_POINTER_ID) {
			anObj = GameObject.Find("MouseCursor");
			anObj.GetComponent<Renderer>().enabled = true;
			anObj.transform.position = Const.PAINT_TOOL_POINTER_POSITION;
		}
		else if (paintToolStr.IndexOf(ChainXModel.PAINT_TOOL_VOXEL_ID) > -1) {
			int textureType = int.Parse(paintToolStr.Remove(0, ChainXModel.PAINT_TOOL_VOXEL_ID.Length));
			anObj = GameObject.Find("SelectingVoxel");
			anObj.GetComponent<Renderer>().enabled = true;
			anObj.transform.position = Const.PAINT_TOOL_VOXEL_POSITION;
			Texture2D texture = Resources.Load<Texture2D> ("Textures/" + textureType.ToString());
			anObj.GetComponent<Renderer>().material.mainTexture = texture;
		}
		else if (paintToolStr == ChainXModel.PAINT_TOOL_GROUP_ID) {
		}

		return anObj;
	}


	private string getPosIDsFromSelectedObjects() {
		string res = "";
		foreach (GameObject anObj in this.selectedObjects) {
			res += anObj.name + ",";
		}
		return res.TrimEnd(',');
	}

	/*
	 * 
	 */
	private void applyShader(GameObject anObj, Shader aShader) {
		if (anObj.transform.childCount == 0) {
			anObj.GetComponent<Renderer>().material.shader = aShader;
		}
		else {
			foreach(Transform aChildTransform in anObj.transform) {
				aChildTransform.gameObject.GetComponent<Renderer>().material.shader = aShader;
			}
		}
	}

	private void cleanSelectedObjects()
	{
		foreach(GameObject anObj in this.selectedObjects) {
			this.applyShader(anObj, Const.DIFFUSE_SHADER);	
		}
		this.selectedObjects.Clear();
	}

	private void AddToSelectedObjects(GameObject hitObj)
	{
		this.applyShader(hitObj, Const.TOON_SHADER);	
		this.selectedObjects.Add(hitObj);
	}

	private void RemoveToSelectedObjects(GameObject hitObj)
	{
		this.applyShader(hitObj, Const.DIFFUSE_SHADER);	
		this.selectedObjects.Remove(hitObj);
	}

	private GameObject getHitObject() {

		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit = new RaycastHit ();
		if (Physics.Raycast (ray, out hit)) {
			return hit.collider.gameObject;
		} else {
			return null;
		}
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
				this.CreateVoxelFromPosID(this.cv.getVoxel(posID).getTextureType(), posID);
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
								this.selectedObjects [0].GetComponent<Renderer> ().material.shader = Const.TOON_SHADER;
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

		/*
		 * Structure Table
		 */
		//this.selectedObjects[0].transform.DetachChildren(); //グループ一括解除

		/*
		List<GameObject> groupObjs = new List<GameObject>();
		foreach(GameObject anObj in this.selectedObjects) {
			if (anObj.transform.childCount > 0)
				groupObjs.Add(anObj);	
		}
		//groupObjs.Count == 0

		GameObject groupObj = new GameObject("G" + Util.GetGUID());
		foreach(GameObject anObj in this.selectedObjects)
			anObj.transform.parent = groupObj.transform;
		*/
	}

	/*
	 * 
	 * textureTypeまたは、colorTypeの一方は必ず0以上、もう一方は0未満にしなくてはならない。
	 */
	private GameObject CreateVoxel(int textureType, string name, Vector3 pos)
	{
		GameObject voxelObj = GameObject.CreatePrimitive (PrimitiveType.Cube);
		Material material = new Material (Const.DIFFUSE_SHADER);
		//Material material = new Material(Shader.Find("Toon/Lit Outline")); //影をつける
		Texture2D texture = Resources.Load<Texture2D> ("Textures/" + textureType.ToString());
		material.mainTexture = texture;
		voxelObj.GetComponent<Renderer> ().material = material;
		voxelObj.name = name;
		voxelObj.transform.position = pos;

		return voxelObj;
	}

	private void CreateVoxelFromPosID(int textureType, string posID)
	{
		Vector3 pos = this.ConvertPosID (posID);
		this.CreateVoxel(textureType, posID, pos);
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
