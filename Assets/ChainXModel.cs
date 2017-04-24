using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ChainXModel
{
	private ChainXView view;
	private string log;
	ChainVoxel cv;

	public ChainXModel() {
		this.cv = new ChainVoxel(this);
	}

	public ChainVoxel getChainVoxel() { return this.cv; }

	public void SetLog(string log) {
		this.log = log;
		this.view.Update ();
	}

	public string GetLog() { return this.log; } 

	public void SetView(ChainXView view) {
		this.view = view;
	}

}