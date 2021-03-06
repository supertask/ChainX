﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
//using UnityEditor;
using SFB;


struct ScreenSize {
    public int width, height;
}

public class ChainXController : MonoBehaviour
{
    public EmulatedWebSocket socket;
    private ChainXModel model;
    public ChainVoxel cv;
    private GameObject paintTool;
    public string log;
    public static object thisLock = new object();
    private ScreenSize screenSize;
	private GameObject debuggerObj;


    public List<GameObject> selectedObjects;
	//public List<string> removedSelectedGIDs;

    IEnumerator Start() {
        this.model = new ChainXModel();
		this.model.SetController(this);
        GameObject anObj = this.CreateVoxel(0, Const.UI_SELECTING_VOXEL_NAME, Vector3.zero);
        anObj.transform.SetParent(GameObject.Find(Const.PAINT_TOOL_PATH).transform);
        anObj.GetComponent<Renderer>().enabled = false;
        anObj.layer = Const.UI_LAYER;

        this.paintTool = GameObject.Find(Const.PAINT_TOOL_PATH + Const.UI_SELECTING_POINTER_NAME);
        this.paintTool.layer = Const.UI_LAYER;
        this.screenSize.width = Screen.width;
        this.screenSize.height = Screen.height;
        this.SetPositionOfPaintTool();
		this.debuggerObj = GameObject.Find ("DebugLog/Viewport/Content");

        this.cv = new ChainVoxel(this);
        this.selectedObjects = new List<GameObject> ();
		//this.removedSelectedGIDs = new List<string> ();
        this.socket = new EmulatedWebSocket (this);
        StartCoroutine(this.socket.Connect());
		//Util.START_NANO_TIME = System.DateTime.Now.Ticks; //タイムスタンプ開始
        yield return this.socket.Listen();
    }

    void Update ()
	{
		//Debug.Log(DateTime.UtcNow.Ticks);

		lock (ChainXController.thisLock) {
			this.UpdateVoxels ();
			//this.SetUpGUICompornets ();

			//スクリーンサイズが変更された時、実行される
			if (this.screenSize.width != Screen.width ||
			    this.screenSize.height != Screen.height) {
				this.SetPositionOfPaintTool ();
				this.screenSize.width = Screen.width;
				this.screenSize.height = Screen.height;
			}

			//マウスクリックの際の処理
			if (Input.GetMouseButtonDown(Const.MOUSE_LEFT_CLICK)) {
				if (Input.GetKey (KeyCode.LeftAlt)) {
					//Do nothing for Orbit, Zoom, etc..
				} else if (this.clickUI()) {
					this.cleanSelectedObjects(); //Voxelの選択解除
				} else if (this.paintTool.name == Const.UI_SELECTING_POINTER_NAME) {
					this.clickVoxel(); //マウスクリックしてオブジェクトを選択する
				} else {
					//VoxelまたはVoxelsが選択中のとき
					this.paintVoxels(); //VoxelまたはVoxelsをペイントする
				}
			}

			//キーボード操作（移動、グループ作成、解除、削除）
			Operation o = null;
			if (this.selectedObjects.Count > 0) {
				//3. MOVEの移動方向を追加
				Vector3 arrowV = Vector3.zero;
				if (Input.GetKeyUp (KeyCode.UpArrow)) arrowV = new Vector3 (1, 0, 0);
				else if (Input.GetKeyUp (KeyCode.DownArrow)) arrowV = new Vector3 (-1, 0, 0);
				else if (Input.GetKeyUp (KeyCode.RightArrow)) arrowV = new Vector3 (0, 0, -1);
				else if (Input.GetKeyUp (KeyCode.LeftArrow)) arrowV = new Vector3 (0, 0, 1);
				else if (Input.GetKeyUp (KeyCode.K)) arrowV = new Vector3 (0, 1, 0);
				else if (Input.GetKeyUp (KeyCode.J)) { arrowV = new Vector3 (0, -1, 0); }

				if(arrowV == Vector3.zero){
					if (Input.GetKeyUp(KeyCode.D)) {
						//
						// VoxelまたはGroupVoxelを削除する
						//
						if (Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.LeftCommand) ||
						    Input.GetKey (KeyCode.RightControl) || Input.GetKey (KeyCode.RightCommand)) {
							List<Operation> deleteOps = this.model.CreateDeleteOperation (this.selectedObjects);
							foreach (Operation op in deleteOps) {
								this.ApplyChainVoxel (op);
							}
						}
					}
					else if (Input.GetKeyDown (KeyCode.G)) {
						//
						// グループの参加、離脱
						// グループの中にグループは、今は処理しない！
						//
						string gid;
						if (this.selectedObjects.Count == 1) {
							if (this.selectedObjects [0].transform.childCount > 0) {
								//選択したオブジェクトが一つで、それがグループである場合、グループを解除
								gid = this.selectedObjects [0].transform.root.name;
								o = new Operation (this.socket.getID(), Operation.LEAVE_ALL, "{\"posIDs\": \"" +
									this.model.getPosIDsFromObj(this.selectedObjects[0]) + "\", \"gid\": \"" + gid +
									"\"}");
								this.ApplyChainVoxel(o);
							} else { } //何もしない

						} else {
							gid = ChainXModel.CreateGID();
							o = new Operation (this.socket.getID(), Operation.JOIN_ALL, "{\"posIDs\": \"" +
								this.model.getPosIDsFromObjects(this.selectedObjects) + "\", \"gid\": \"" + gid +
							"\"}");
							this.ApplyChainVoxel(o);
						}
                    }
                }

				else  {
					/*
					foreach (GameObject anObj in this.selectedObjects) {
						Debug.Log (anObj.name);
					}
					*/
					//Debug.Log("MOVE");
					this.selectedObjects = Util.ArrangeGameObjects (this.selectedObjects, arrowV);

					List<Operation> moveOps = this.model.CreateMoveOperations(this.selectedObjects, arrowV);
					foreach (Operation moveOp in moveOps) {
						this.ApplyChainVoxel (moveOp);
					}
				}


            }
            if (o != null) {
	            //this.cv.show();
            	//this.cv.stt.show();
            }
        }
    }

	void OnGUI() {
		bool hasButton = GUILayout.Button ("Load obj..", GUILayout.Width (100));
		var extensions = new [] {
			new ExtensionFilter("Obj Files", "obj", "jpg"),
		};
		if (hasButton) {
			//filePaths[0] = EditorUtility.OpenFilePanel("Select an obj", "~/Downloads/", "obj");
			string[] filePaths = StandaloneFileBrowser.OpenFilePanel("Select an Obj", "~/Donwloads/", extensions, false);
			if (filePaths.Length > 0 && filePaths [0].Length > 0) {
				string dir = Path.GetDirectoryName (filePaths [0]);
				string filenameWithoutExt = Path.GetFileNameWithoutExtension (filePaths [0]);
				dir = dir.Replace(@"file:", "");
				dir = dir.Replace(@"///", "/");
				dir = dir.Replace(@"//", "/");
				//dir = dir.Remove (0, 5);
				string[] paths = new string[2];
				paths[0] = dir + "/" + filenameWithoutExt + ".obj";
				paths[1] = dir + "/" + filenameWithoutExt + ".jpg";
				//GUILayout.TextField (paths[0], 100);
				//GUILayout.TextField (paths[1], 100);

				//
				// JPG, MTL, OBJファイルを他のマシーンに転送
				// メッセージ受信する際に、分割されたOBJファイルをローカル受け取り、
				// その時点でオブジェクトが適用され、表示される
				//
				foreach (string path in paths) {
					string filename = Path.GetFileName (path);
					Debug.Log (path);
					byte[] header = Encoding.UTF8.GetBytes (Const.SOME_FILE_HEADER + filename + Const.MSG_SPLIT_CHAR);
					this.socket.SendBinary(-1, Util.CombineBytes (header, File.ReadAllBytes (path)));
				}
			}
		}
	}

    private void SetPositionOfPaintTool() {
        GameObject anObj = GameObject.Find(Const.PAINT_TOOL_PATH);
        Vector3 pos = Const.PAINT_TOOL_PLATE_POSITION;

        anObj.transform.position = pos;
        foreach(Transform aChildTransform in anObj.transform)
        {
            if (aChildTransform.gameObject.name == Const.UI_SELECTING_VOXEL_NAME) {
                aChildTransform.position = pos + new Vector3(0,0.5f,0);
                continue;
            }
            aChildTransform.position = pos;
            aChildTransform.gameObject.layer = Const.UI_LAYER;
        }
        return;
    }


    public void ApplyChainVoxel(Operation o) {
		this.cv.apply(o, ChainVoxel.LOCAL_OPERATION);
		string op = Const.OPERATION_HEADER + Operation.ToJson (o);
		this.socket.SendBinary(-1, Encoding.ASCII.GetBytes(op));
    }

    /*
     * PaintToolのVoxelまたはグループVoxelが選択されていて、マウスクリックされたときに呼ばれる。
     * ペイントさせ方は、ぶつかったオブジェクト（Plane）よりも手前のオブジェクトを
     * 「後ろ側から手前に」辿っていき、Voxelを挿入する。
     */
    private void paintVoxels() {
		string paintToolName = this.model.getCurrentPaintTool();

		List<Operation> ops = new List<Operation>();
        Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
        RaycastHit hit = new RaycastHit ();
        if (Physics.Raycast (ray, out hit)) {

			if (paintToolName.IndexOf(ChainXModel.PAINT_TOOL_VOXEL_ID) > -1) {
				//
				//単位Voxelをペイントする
				//
	            float distance = hit.distance - 0.5f; //ヒットしたRayより少し手前のPointをたどる
				Vector3 hitPointShort = ChainXModel.GetRoundIntPoint(ray.GetPoint(distance));
	            int textureType = int.Parse(this.paintTool.GetComponent<Text>().text);
				ops.Add(new Operation(this.socket.getID(), Operation.INSERT,
					"{\"posID\": \"" + ChainXModel.CreatePosID(hitPointShort) +
	                "\", \"textureType\":\"" + textureType + "\"}"
				));
			}
			else if (paintToolName.IndexOf (ChainXModel.PAINT_TOOL_GROUP_ID) > -1) {
				//
				//グループVoxelsをペイントする
				//				
				float cursor_d = hit.distance - 0.5f;
				Vector3 hitPointShort = ChainXModel.GetRoundIntPoint (ray.GetPoint (cursor_d));

				//もっともヒットポイントに近いオブジェクトをグループVoxelsの中から見つける
				GameObject closeObjToHitPoint = null;
				float minDistance = float.MaxValue;
				GameObject groupObj = GameObject.Find (paintToolName);
				foreach (Transform aPart in groupObj.transform) {
					float d = Vector3.Distance (aPart.gameObject.transform.position, hitPointShort);
					if (d < minDistance) {
						minDistance = d;
						closeObjToHitPoint = aPart.gameObject;
					}
				}

				bool put_enable;
				Vector3 diffV = Vector3.zero;
				string posIDs = "";
				string textureTypes = "";
				while (cursor_d > 0)
				{
					posIDs = "";
					textureTypes = "";
					hitPointShort = ChainXModel.GetRoundIntPoint (ray.GetPoint (cursor_d));
					diffV = hitPointShort - closeObjToHitPoint.transform.position;
					put_enable = true;
					foreach (Transform aPart in groupObj.transform) {
						Vector3 movingV = aPart.gameObject.transform.position + diffV;
						if (GameObject.Find (Util.CreatePosID (movingV)) != null) {
							put_enable = false;	
							break;	
						}
						posIDs += Util.CreatePosID(movingV) + Const.SPLIT_CHAR;
						textureTypes += aPart.gameObject.GetComponent<Text>().text + Const.SPLIT_CHAR;
					}
					if (put_enable) break;
					cursor_d--;
				}
				posIDs = posIDs.TrimEnd (Const.SPLIT_CHAR);
				textureTypes = textureTypes.TrimEnd (Const.SPLIT_CHAR);
				this.selectedObjects.Remove(groupObj);

				string gid = ChainXModel.CreateGID ();
				ops.Add(new Operation(this.socket.getID(), Operation.INSERT_ALL,
					"{\"posIDs\": \"" + posIDs +
					"\", \"gid\": \"" + gid +
					"\", \"textureTypes\":\"" + textureTypes + "\"}")
				);
				ops.Add(new Operation(this.socket.getID(), Operation.JOIN_ALL,
					"{\"posIDs\": \"" + posIDs +
					"\", \"gid\": \"" + gid + "\"}")
				);
			}

			long ts = ops[0].getTimestamp();
			for(int i = 0; i < ops.Count; i++) {
				ops[i].setTimestamp(ts + i); //できる限り1に近づけないと間に入り込まれる
				this.ApplyChainVoxel(ops[i]);
			}
            //Debug.DrawLine(ray.origin, hitPointShort, Color.red, 60.0f, true); //レーザービーム
        }
    }


    /*
     * PaintToolのマウスカーソルが選択されていて、マウスクリックされたときに呼ばれる。
     */
    private void clickVoxel() {
        GameObject hitObj = this.getHitObject();
		if (hitObj != null) {
			while (hitObj.transform.parent != null)
				hitObj = hitObj.transform.parent.gameObject;
		}

		if (hitObj != null && (Const.REGEX_POSID.IsMatch (hitObj.name) ||
				Const.REGEX_GROUP.IsMatch (hitObj.name)) ) {
            if (this.selectedObjects.Contains(hitObj)) {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                    this.RemoveFromSelectedObjects(hitObj);
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

            this.hidePaintTool();
            if (hitObj.name == "ArrowLeft") {
                this.SetPaintToolObj(this.model.getPaintTool(-1,0));
            }
            else if (hitObj.name == "ArrowRight") {
                this.SetPaintToolObj(this.model.getPaintTool(1,0));
            }
            else if (hitObj.name == "ArrowTop") {
                this.SetPaintToolObj(this.model.getPaintTool(0,1));
            }
            else {
                //どれにもヒットしなかった場合
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
		if (this.paintTool == null) { return; }
        if (this.paintTool.transform.childCount > 0) {
            foreach(Transform transform in this.paintTool.transform) {
                transform.gameObject.GetComponent<Renderer>().enabled = enabled;
            }
        }
        else {
			if (this.paintTool.GetComponent<Renderer>() != null) {
				this.paintTool.GetComponent<Renderer>().enabled = enabled;
			}
			//ペイントツールがなければ，無視
		}
    }


	/*
	 * 左方向キー，右方向キー，下方向キーをタッチした時のペイントツールを表示する．
	 */
    private void SetPaintToolObj(string paintToolStr)
    {
        GameObject aPaintToolObj = null;
        if (paintToolStr == ChainXModel.PAINT_TOOL_POINTER_ID) {
            aPaintToolObj = GameObject.Find(Const.PAINT_TOOL_PATH + Const.UI_SELECTING_POINTER_NAME);
            aPaintToolObj.GetComponent<Renderer>().enabled = true;
            this.paintTool = aPaintToolObj;
        }
        else if (paintToolStr.IndexOf(ChainXModel.PAINT_TOOL_VOXEL_ID) > -1) {
			//
			//単一Voxelをペイントツールに表示する
			//
            //TODO(Tasuku): Texture作成の部分をメソッド化する!!!!
            int textureType = int.Parse(paintToolStr.Remove(0, ChainXModel.PAINT_TOOL_VOXEL_ID.Length));
            aPaintToolObj = GameObject.Find(Const.PAINT_TOOL_PATH + Const.UI_SELECTING_VOXEL_NAME);
            aPaintToolObj.GetComponent<Renderer>().enabled = true;
            ///aPaintToolObj.transform.localPosition = new Vector3(0, 0, 0);

            Texture2D texture = Resources.Load<Texture2D> ("Textures/" + textureType.ToString());
            aPaintToolObj.GetComponent<Text>().text = textureType.ToString();
            aPaintToolObj.GetComponent<Renderer>().material.mainTexture = texture;
            this.paintTool = aPaintToolObj;
        }
        else if (paintToolStr.IndexOf(ChainXModel.PAINT_TOOL_GROUP_ID) > -1) {
			//
			//複数Voxelをペイントツールに表示する
			//
            string gid = paintToolStr;
            aPaintToolObj = GameObject.Find(Const.PAINT_TOOL_PATH + gid);

            if (aPaintToolObj == null) {
                GameObject aParent = GameObject.Find(Const.PAINT_TOOL_PATH);
                GameObject aGroupObj = GameObject.Find(gid);

                if (aGroupObj != null) {
                    //グロープをまとめたオブジェクト
                    aPaintToolObj = Instantiate(aGroupObj) as GameObject;
                    aPaintToolObj.transform.SetParent(aParent.transform);
                    aPaintToolObj.name = gid;
					Debug.Log("paint: " + aPaintToolObj);

					float scale = this.model.GetScale(aPaintToolObj, this.model.VOXEL_PLATE_DIAMETER);
                    float y_margin = 0.5f;
                    if (scale < 1.0) {
                        y_margin *= scale;
                        aPaintToolObj.transform.localScale = aPaintToolObj.transform.localScale * scale;
                    }

                    Vector3 bottomCenterPosition = this.model.GetBottomCenterPosition(aPaintToolObj, y_margin);
                    Vector3 maxVector, minVector;
                    this.model.GetMaxMinPositions(aPaintToolObj, out maxVector, out minVector);
                    Vector3 moveVector = Vector3.zero - bottomCenterPosition;

                    aPaintToolObj.layer = Const.UI_LAYER;
                    foreach(Transform aChildTransform in aPaintToolObj.transform) {
                        aChildTransform.position = aChildTransform.position + moveVector;
                        aChildTransform.gameObject.layer = Const.UI_LAYER;
                    }
                    this.paintTool = aPaintToolObj;
                }
            }
            else {
                this.paintTool = aPaintToolObj;
                this.showPaintTool();
            }
            this.SetPositionOfPaintTool();
        }
        return;
    }



    /*
     * 
     */
    private void applyShader(GameObject anObj, Shader aShader) {
        if (anObj.transform.childCount == 0) {
			if (anObj.GetComponent<Renderer>() != null) {
	            anObj.GetComponent<Renderer>().material.shader = aShader;
			}
        }
        else { //childCount > 0
            foreach(Transform aChildTransform in anObj.transform) {
				applyShader(aChildTransform.gameObject, aShader);
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

    private void RemoveFromSelectedObjects(GameObject hitObj)
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


    /*
     * 
     */
    public void UpdateVoxels() {
		/*
         * For DELETE operations.
         */
		if (this.cv.deletedPosIDs.Count > 0) {
			foreach (string posID in this.cv.deletedPosIDs) {
				GameObject deletingObj = GameObject.Find(posID);
				if (deletingObj != null) {
              		// Remove following a ChainVoxel.
					this.selectedObjects.Remove(deletingObj);
					GameObject.DestroyImmediate(deletingObj);
					//Debug.Log ("deletedGUI:" + posID);

				}
			}
			cv.deletedPosIDs.Clear ();
		}

        /*
         * For INSERT operations.
         */
		if (this.cv.insertedPosIDs.Count > 0) {
			foreach (string posID in this.cv.insertedPosIDs) {
				//Debug.Log ("候補，insertGUI:" + posID);
				GameObject foundObj = GameObject.Find(posID);

				if (foundObj == null) {
					Voxel aVoxel = this.cv.getVoxel (posID);
					if (aVoxel != null) {
						//Debug.Log ("insertedPosID:" + posID);
						this.CreateVoxelFromPosID(aVoxel.getTextureType (), posID);
					}
					//Debug.Log(aVoxel.getObjPath()); // /Users/tasuku/Library/Application Support/supertask/ChainX/

					//string[] filepaths = new string[]{ aVoxel.getObjPath(), aVoxel.getTexturePath()};
					//GameObject target = ObjLoadHelper.LoadObj(filepaths, Util.SplitPosID(posID));
					//Debug.Log ("target name: " + target.name);
					/*
					HashSet<Group> groups = this.cv.stt.getGroups(posID);
					GameObject aGroupObj = null;
					foreach (Group aGroup in groups) {
						aGroupObj = GameObject.Find(aGroup.getGroupId());
					}

					if (aGroupObj != null) {
						target.transform.SetParent(aGroupObj.transform);
					}
					*/
				}
			}
			cv.insertedPosIDs.Clear ();
		}

        /*
         * For MOVE operations.
         */
		if (this.cv.movedPosIDs.Count > 0) {
			foreach (KeyValuePair<string,string> aPair in this.cv.movedPosIDs) {
				string posID = aPair.Key;
				string destPosID = aPair.Value;
				GameObject anObj = GameObject.Find (posID);
				if (anObj != null) {

					Vector3 v = Util.SplitPosID (destPosID);	
					anObj.name = destPosID;
					anObj.transform.position = v;
				}
			}
			this.cv.movedPosIDs.Clear ();
		}


        /*
         * For LEAVE ALL operations.
         */
		if (this.cv.leftGIDs.Count > 0) {
			foreach (string gid in this.cv.leftGIDs) {
				this.model.RemoveGroupFromUI (gid);
				GameObject aParent = GameObject.Find (gid);
				if (aParent != null) {
					//Debug.Log("LEAVE");
					if (aParent.transform.childCount > 0) {
						//ローカル操作のみのグループを選択可能にする
						if (this.cv.selectedLeftGIDs.Contains(gid)) {
							this.RemoveFromSelectedObjects(aParent);
						}
					}
					this.selectedObjects.Remove (aParent);
					aParent.transform.DetachChildren ();
					GameObject.DestroyImmediate(aParent);
				}
			}
			this.cv.selectedLeftGIDs.Clear();
			this.cv.leftGIDs.Clear ();
		}

        /*
         * For JOIN ALL operations.
         */
		if (this.cv.joinedGIDs.Count > 0) {
			foreach (string gid in this.cv.joinedGIDs) {
				//グループ（親）を作り、子供を追加
				GameObject aParent = new GameObject (gid);
				foreach (string posID in this.cv.stt.getPosIDs(gid))
				{
					GameObject aChild = GameObject.Find (posID);
					//過去のバグ：NULLになってかつ、無限ループし、グループが無限に作られる
					if (aChild != null) {
						//Debug.Log ("ここ:" + aChild.name);
						//Polygonの場合、一つだけ挿入されそれ以外（NULL）は挿入されず、グループのみ追加される
						aChild.transform.SetParent(aParent.transform);
						this.RemoveFromSelectedObjects(aChild);
					}
				}
				//UIにこのグループオブジェクトを登録
				//TODO(Tasuku): グループオブジェクトが2重に登録されないようにする
				this.model.AddGroupToUI(gid);

				//ローカル操作のみのグループを選択可能にする
				if (this.cv.selectedJoinedGIDs.Contains(gid)) {
					this.AddToSelectedObjects(aParent); //ここでバグが起こるだけで無限ループが起こる
				}
			}
			this.cv.selectedJoinedGIDs.Clear();
			this.cv.joinedGIDs.Clear ();
		}

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
			Text log = this.debuggerObj.GetComponent<Text>();
            log.text = this.log; //this.showStructureTable();
        }
    }

    private void OnApplicationQuit() {
		//Util.GetSerializedLength(this.cv);
		//Debug.Log("Memory on Primary Layer memory: " + this.cv.getMemory());
		//Debug.Log("Memory on Grouping Layer: " + this.cv.stt.getMemory());
		long memorySize = this.cv.getMemory();
		memorySize += this.cv.stt.getMemory();
		Debug.Log("Memory on Primary and Grouping Layer: " + memorySize);
		//this.cv.show();
		//this.cv.stt.show();

		string path = Const.SAVED_FILE;
		string filename = Path.GetFileName(path);
		this.cv.SaveData(Const.SAVED_FILE);
		string headerStr = Const.SOME_FILE_HEADER + filename + Const.MSG_SPLIT_CHAR;
		//Debug.Log(headerStr);
		byte[] header = Encoding.UTF8.GetBytes (headerStr);
		this.socket.SendBinary(-1, Util.CombineBytes (header, File.ReadAllBytes (path)) );
        this.socket.Close(); //It sends EXIT_HEADER
        //TODO(Tasuku): SaveしましたのWindowを表示して終わる!!
    }

	public ChainXModel getModel() { return this.model; }
}