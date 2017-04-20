using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainXController : MonoBehaviour
{
	private ChainXModel model;
	private ChainXView view;
	private EmulatedSocket socket;

	void Start() {
		/*
		this.model = new ChainXModel();
		this.view = new ChainXView(this.model);
		this.model.SetView(this.view);
		StartCoroutine (this.Run());
		*/
		this.socket = new EmulatedSocket ();
	}


	void Update()
	{
		if (Input.GetKeyDown(KeyCode.S))
		{
			//Debug.Log("Key S!!");
			this.socket.send("{\"id\":3, \"ts\":1451515335}\n");
		}
		else if (Input.GetKeyDown(KeyCode.C)) {
		}
		else if (Input.GetKeyDown(KeyCode.B)) {
		}
	}

	IEnumerator Run()
	{
		ChainVoxel cv = new ChainVoxel(this.model);
		/*
		cv.apply(new Operation(Operation.CREATE,
			new SortedDictionary<string, object>() {{"gid", "a_table"}} ));
		cv.apply(new Operation(Operation.JOIN,
			new SortedDictionary<string, object>() {{"gid", "a_table"}, {"posID", "1:1:1"}} ));
		*/
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (3, Operation.INSERT, "1:1:1"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (3, Operation.INSERT, "1:1:2"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (5, Operation.INSERT, "1:2:1"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (2, Operation.INSERT, "1:1:1"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (4, Operation.INSERT, "1:1:1"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation(3, Operation.DELETE, "1:1:1"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation(4, Operation.INSERT, "0:1:1"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation(4, Operation.INSERT, "0:0:0"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation(4, Operation.INSERT, "1:1:1"));

		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation(5, Operation.MOVE, "1:1:1", "2:2:2"));
	}

	/*
	private string simulateChainVoxel()
	{
			
	}
	*/
}
