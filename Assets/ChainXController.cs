using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

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
		//this.socket = new EmulatedSocket (this);
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.S))
		{
			//Debug.Log("Key S!!");
			//this.socket.Send("{\"id\":3, \"ts\":1451515335}\n");
			/*
			Operation o1 = new Operation (200000, Operation.INSERT, "{\"posID\": \"3:3:3\"}");
			string json = Operation.ToJson(o1);
			Debug.Log ("送信したJson: " + json);
			this.socket.Send(json + "\r\n");
			*/
		}
		else if (Input.GetKeyDown(KeyCode.C)) {
		}
		else if (Input.GetKeyDown(KeyCode.B)) {
		}
	}

	private void OnApplicationQuit() {
		//this.socket.Close();
	}

	IEnumerator Run()
	{
		/*
		cv.apply(new Operation(Operation.CREATE,
			new SortedDictionary<string, object>() {{"gid", "a_table"}} ));
		cv.apply(new Operation(Operation.JOIN,
			new SortedDictionary<string, object>() {{"gid", "a_table"}, {"posID", "1:1:1"}} ));
		*/
		ChainVoxel cv = this.model.getChainVoxel ();
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (3, Operation.INSERT, "{\"posID\": \"1:1:1\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (3, Operation.INSERT, "{\"posID\": \"1:1:2\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (5, Operation.INSERT, "{\"posID\": \"1:2:1\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (2, Operation.INSERT, "{\"posID\": \"1:1:1\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (4, Operation.INSERT, "{\"posID\": \"1:1:1\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (3, Operation.DELETE, "{\"posID\": \"1:1:1\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (4, Operation.INSERT, "{\"posID\": \"0:1:1\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (4, Operation.INSERT, "{\"posID\": \"0:0:0\"}"));
		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (4, Operation.INSERT, "{\"posID\": \"1:1:1\"}"));

		yield return new WaitForSeconds(2.0f);
		cv.apply(new Operation (5, Operation.MOVE, "{\"posID\": \"1:1:1\", \"destPosID\": \"2:2:2\"}"));
	}

	public void OperateVoxelOnLocal(Operation op) {
		ChainVoxel cv = this.model.getChainVoxel ();
		cv.apply(op);
	}

	/*
	private string simulateChainVoxel()
	{
			
	}
	*/

	public ChainXView getView() { return this.view; }
	public ChainXModel getModel() { return this.model; }
}
