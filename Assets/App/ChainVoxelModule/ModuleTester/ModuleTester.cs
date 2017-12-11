using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ModuleTester : MonoBehaviour
{
	void Start () {
		this.Test();
	}
	void Update () { }

	private void Test() {
		//Voxel.Test();	
		Operation.Test();
		//ChainVoxelTester.Test();	
		//Group.Test();
		//StructureTable.Test();	
		//EmulatedWebSocket.Test();

	}
}