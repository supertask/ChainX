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
		GameObject anObj = this.CreateVoxel(0, Const.UI_SELECTING_VOXEL_NAME, Const.PAINT_TOOL_VOXEL_POSITION);
		anObj.GetComponent<Renderer>().enabled = false;
		anObj.layer = Const.UI_LAYER;

		this.paintTool = GameObject.Find(Const.UI_SELECTING_POINTER_NAME);
		this.paintTool.layer = Const.UI_LAYER;

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
			this.SetPositionUntilChildren(ggg, Const.PAINT_TOOL_PLATE_POSITION);

			Operation o = null;
			if (Input.GetKeyDown (KeyCode.V)) {
				o = new Operation (0, Operation.INSERT,
					"{\"posID\": \"" + Operation.CreateRandomPosID (5) +
					"\", \"textureType\":\"" + UnityEngine.Random.Range (0, 8) + "\"}");
			}
			else if (Input.GetMouseButtonDown(Const.MOUSE_LEFT_CLICK))
			{
				if (Input.GetKey(KeyCode.LeftAlt)) {
					//Orbit, Zoom, etc..
				}
				else if (this.clickUI()) {
					this.cleanSelectedObjects(); //Voxelの選択解除
				}
				else if (this.paintTool.name == Const.UI_SELECTING_POINTER_NAME) {
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
						string gid = ChainXModel.PAINT_TOOL_GROUP_ID + Util.GetGUID();

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
				this.ApplyChainVoxel(o);
				Debug.Log(this.cv.show());
			}
		}
	}


	private void SetPositionUntilChildren(GameObject anObj, Vector3 pos) {
		anObj.transform.position = pos;
		foreach(Transform aChildTransform in anObj.transform)
		{
			aChildTransform.position = pos;
			aChildTransform.gameObject.layer = Const.UI_LAYER;
		}
		return;
	}


	private void ApplyChainVoxel(Operation o) {
		this.cv.apply(o);
		this.socket.Send (MessageHeader.OPERATION + Operation.ToJson (o) + "\n");
	}

	/*
	 * PaintToolのVoxelまたはグループVoxelが選択されていて、マウスクリックされたときに呼ばれる。
	 * ペイントさせ方は、ぶつかったオブジェクト（Plane）よりも手前のオブジェクトを
	 * 「後ろ側から手前に」辿っていき、Voxelを挿入する。
	 */
	private void paintVoxels() {
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit = new RaycastHit ();
		if (Physics.Raycast (ray, out hit)) {
			float distance = hit.distance - 0.5f;
			Vector3 hitPointShort = ray.GetPoint(distance); //ヒットしたRayより少し手前のPointをたどる
			Vector3 fixedHitPointShort = ChainXModel.GetRoundIntPoint(hitPointShort);
			int textureType = int.Parse(this.paintTool.GetComponent<Text>().text);
			Operation o = new Operation(0, Operation.INSERT,
				"{\"posID\": \"" + this.CreatePosID(fixedHitPointShort) +
				"\", \"textureType\":\"" + textureType + "\"}"
			);
			this.ApplyChainVoxel(o);
			//Debug.DrawLine(ray.origin, hitPointShort, Color.red, 60.0f, true); //レーザービーム
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

			this.hidePaintTool(); //まずこれが動いてない
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
				this.showPaintTool();
				isHitOnUI = false;
			}
			this.paintTool.layer = Const.UI_LAYER;

		}
		return isHitOnUI;
	}

	private void hidePaintTool() { this.visualizePaintTool(false); }
	private void showPaintTool() { this.visualizePaintTool(true); }
	private void visualizePaintTool(bool enabled)
	{
		//Debug.Log(this.paintTool.name);

		if (this.paintTool.transform.childCount > 0) {
			foreach(Transform transform in this.paintTool.transform) {
				transform.gameObject.GetComponent<Renderer>().enabled = enabled;
			}
		}
		else { this.paintTool.GetComponent<Renderer>().enabled = enabled; }
	}


	private GameObject getPaintToolObj(string paintToolStr) {
		GameObject anObj = null;
		if (paintToolStr == ChainXModel.PAINT_TOOL_POINTER_ID) {
			anObj = GameObject.Find(Const.UI_SELECTING_POINTER_NAME);
			anObj.GetComponent<Renderer>().enabled = true;
			anObj.transform.position = Const.PAINT_TOOL_POINTER_POSITION;
		}
		else if (paintToolStr.IndexOf(ChainXModel.PAINT_TOOL_VOXEL_ID) > -1) {
			//TODO(Tasuku): Texture作成の部分をメソッド化する!!!!
			int textureType = int.Parse(paintToolStr.Remove(0, ChainXModel.PAINT_TOOL_VOXEL_ID.Length));
			anObj = GameObject.Find(Const.UI_SELECTING_VOXEL_NAME);
			anObj.GetComponent<Renderer>().enabled = true;
			anObj.transform.position = Const.PAINT_TOOL_VOXEL_POSITION;
			Texture2D texture = Resources.Load<Texture2D> ("Textures/" + textureType.ToString());
			anObj.GetComponent<Text>().text = textureType.ToString();
			anObj.GetComponent<Renderer>().material.mainTexture = texture;
		}
		else if (paintToolStr.IndexOf(ChainXModel.PAINT_TOOL_GROUP_ID) > -1) {
			string gid = paintToolStr;
			anObj = GameObject.Find(Const.PAINT_TOOL_PATH + gid);

			if (anObj == null) {
				GameObject aParent = GameObject.Find(Const.PAINT_TOOL_PATH);
				GameObject aGroupObj = GameObject.Find(gid);
				if (aGroupObj != null) {
					//グロープをまとめたオブジェクト
					anObj = Instantiate(aGroupObj) as GameObject;
					anObj.transform.SetParent(aParent.transform);
					anObj.name = gid;

					float scale = this.model.GetScale(anObj, Const.VOXEL_PLATE_DIAMETER);
					Debug.Log(anObj.transform.localScale + " " + scale);
					if (scale < 1.0) {
						anObj.transform.localScale = anObj.transform.localScale * scale;
					}

					Vector3 bottomCenterPosition = this.model.GetBottomCenterPosition(anObj);
					Vector3 maxVector, minVector;
					this.model.GetMaxMinPositions(anObj, out maxVector, out minVector);
					Vector3 moveVector = new Vector3(0,0,0) - bottomCenterPosition;

					anObj.layer = Const.UI_LAYER;
					foreach(Transform aChildTransform in anObj.transform) {
						aChildTransform.position = aChildTransform.position + moveVector;
						aChildTransform.gameObject.layer = Const.UI_LAYER;
					}
				}
			}
			else {
				this.paintTool = anObj;
				this.showPaintTool();
			}
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
		Vector3 pos;
		pos = this.selectedObjects[0].transform.position;
		string posID = this.CreatePosID(pos);
		pos.Set(pos.x+dx, pos.y+dy, pos.z+dz);
		string destPosID = this.CreatePosID(pos);
		return new Operation (0, Operation.MOVE,"{\"posID\": \"" + posID
				+ "\", \"destPosID\": \"" + destPosID + "\"}"); 
	}

	private string CreatePosID(Vector3 pos) { return pos.x + ":" + pos.y + ":" + pos.z; }


	/*
	 * 
	 */
	public void UpdateVoxels() {
		/*
		 * For INSERT operations.
		 */
		foreach (string posID in this.cv.insertedPosIDs) {
			if (GameObject.Find (posID) == null) {
				this.CreateVoxelFromPosID(this.cv.getVoxel(posID).getTextureType(), posID);
			}
		}
		cv.insertedPosIDs.Clear ();

		/*
		 * For DELETE & MOVE operations.
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
		 * For JOIN ALL operations.
		 */
		foreach (string gid in this.cv.joinedGIDs) {
			GameObject aParent = new GameObject(gid);
			this.model.AddGroup(gid);

			foreach(string posID in this.cv.stt.getPosIDs(gid)) {
				GameObject aChild = GameObject.Find(posID);
				aChild.transform.SetParent(aParent.transform);

			}
		}
		cv.joinedGIDs.Clear ();

		/*
		 * For LEAVE ALL operations.
		 */
		//leave all

		//this.selectedObjects[0].transform.DetachChildren(); //グループ一括解除
		/*
		List<GameObject> groupObjs = new List<GameObject>();
		foreach(GameObject anObj in this.selectedObjects) {
			if (anObj.transform.childCount > 0)
				groupObjs.Add(anObj);	
		}
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
		voxelObj.AddComponent<Text>().text = textureType.ToString();
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
