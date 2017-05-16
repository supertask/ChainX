using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

public class ChainXModel
{
	private ChainXController controller;
	private string log;
	private string initPath;
	//public ChainVoxel cv;

	//public ChainVoxel getChainVoxel() { return this.cv; }

	public void SetLog(string log) { this.log = log; }
	public string GetLog() { return this.log; } 
}