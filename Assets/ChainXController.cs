using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

struct ScreenSize {
    public int width, height;
}

public class ChainXController : MonoBehaviour
{
    private EmulatedWebSocket socket;
    private ChainXModel model;
    public ChainVoxel cv;
    private GameObject paintTool;
    public string log;
    public static object thisLock = new object();
    private ScreenSize screenSize;

    public List<GameObject> selectedObjects;

    IEnumerator Start() {
        this.model = new ChainXModel();
        GameObject anObj = this.CreateVoxel(0, Const.UI_SELECTING_VOXEL_NAME, Vector3.zero);
        anObj.transform.SetParent(GameObject.Find(Const.PAINT_TOOL_PATH).transform);
        anObj.GetComponent<Renderer>().enabled = false;
        anObj.layer = Const.UI_LAYER;

        this.paintTool = GameObject.Find(Const.PAINT_TOOL_PATH + Const.UI_SELECTING_POINTER_NAME);
        this.paintTool.layer = Const.UI_LAYER;
        this.screenSize.width = Screen.width;
        this.screenSize.height = Screen.height;
        this.SetPositionOfPaintTool();

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

            //スクリーンサイズが変更された時、実行される
            if (this.screenSize.width != Screen.width ||
                this.screenSize.height != Screen.height)
            {
                this.SetPositionOfPaintTool();
                this.screenSize.width = Screen.width;
                this.screenSize.height = Screen.height;
            }


            Operation o = null;
            if (Input.GetMouseButtonDown(Const.MOUSE_LEFT_CLICK))
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
				if (Input.GetKeyUp (KeyCode.UpArrow)) o = this.model.CreateMoveOperation(this.selectedObjects, new Vector3(1, 0, 0));
				else if (Input.GetKeyUp (KeyCode.DownArrow)) o = this.model.CreateMoveOperation(this.selectedObjects, new Vector3(-1, 0, 0));
				else if (Input.GetKeyUp (KeyCode.RightArrow)) o = this.model.CreateMoveOperation(this.selectedObjects,new Vector3(0, 0, -1));
				else if (Input.GetKeyUp (KeyCode.LeftArrow)) o = this.model.CreateMoveOperation(this.selectedObjects, new Vector3(0, 0, 1));
				else if (Input.GetKeyUp (KeyCode.U)) o = this.model.CreateMoveOperation(this.selectedObjects, new Vector3(0, 1, 0));
				else if (Input.GetKeyUp (KeyCode.D)) o = this.model.CreateMoveOperation(this.selectedObjects, new Vector3(0, -1, 0));
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
                                    this.model.getPosIDsFrom(this.selectedObjects) + "\", \"gid\": \"" + gid +
                                "\"}");
                            }
                            else { o = null; }
                        }
                        else {
                            //グループをそもそも作成し忘れてる
                            o = new Operation (0, Operation.JOIN_ALL, "{\"posIDs\": \"" + 
                                this.model.getPosIDsFrom(this.selectedObjects) + "\", \"gid\": \"" + gid +
                            "\"}");
                        }

                    }
                }
            }
            if (o != null) {
                this.ApplyChainVoxel(o);
                this.cv.show();
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
                "{\"posID\": \"" + ChainXModel.CreatePosID(fixedHitPointShort) +
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
        if (this.paintTool.transform.childCount > 0) {
            foreach(Transform transform in this.paintTool.transform) {
                transform.gameObject.GetComponent<Renderer>().enabled = enabled;
            }
        }
        else { this.paintTool.GetComponent<Renderer>().enabled = enabled; }
    }


    private void SetPaintToolObj(string paintToolStr)
    {
        GameObject aPaintToolObj = null;
        if (paintToolStr == ChainXModel.PAINT_TOOL_POINTER_ID) {
            aPaintToolObj = GameObject.Find(Const.PAINT_TOOL_PATH + Const.UI_SELECTING_POINTER_NAME);
            aPaintToolObj.GetComponent<Renderer>().enabled = true;
            this.paintTool = aPaintToolObj;
        }
        else if (paintToolStr.IndexOf(ChainXModel.PAINT_TOOL_VOXEL_ID) > -1) {
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
