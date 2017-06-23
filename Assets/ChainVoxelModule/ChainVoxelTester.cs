using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ChainVoxelTester : MonoBehaviour
{
	void Start () {
		this.Test();
	}
	void Update () { }

	private void Test() {
		Voxel.Test();	
		//Operation.Test();
		//Group.Test();
		//ChainVoxel.Test();	
		//StructureTable.Test();	
		//EmulatedWebSocket.Test();

	}
}