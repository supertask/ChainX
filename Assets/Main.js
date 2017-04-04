#pragma strict

function Start () {
    var P = new GameObject("Wall");
	for (var y = 0; y < 5; y++) {
       	for (var x = 0; x < 5; x++) {
	        var voxel = GameObject.CreatePrimitive(PrimitiveType.Cube);
			voxel.transform.parent = P.transform;
	        //voxel.AddComponent.<Rigidbody>();
         		voxel.transform.position = Vector3 (x, 1, y);
       	}
   	}
}

function Update () {

}

function OnGUI () {
 	//GUI.Box(Rect(0,0,Screen.width,Screen.height), "Hello World!");
	//GUI.Label (Rect (10,10,100,100), "MenuWindow");
}
