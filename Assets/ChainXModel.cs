using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ChainXModel
{
	//private ChainXView view;
	private ChainXController controller;
	private string log;
	public static ChainVoxel cv;

	public ChainXModel() {
		//this.cv = new ChainVoxel(this);
	}

	//public ChainVoxel getChainVoxel() { return this.cv; }

	public void SetLog(string log) {
		this.log = log;
		//this.controller.SetUpGUICompornets();
	}

	public string GetLog() { return this.log; } 
}