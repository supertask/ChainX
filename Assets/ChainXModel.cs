using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

struct Point
{
	public int x, y;
}

public class ChainXModel
{
	private ChainXController controller;
	private Vector3 paintTool3DPos;
	private Vector3 paintToolPlate3DPos;
	private Point paintToolLocation;
	private List<List<string>> paintTools;
	public static string PAINT_TOOL_POINTER_ID = "pointer";
	public static string PAINT_TOOL_VOXEL_ID = "voxel";
	public static string PAINT_TOOL_GROUP_ID = "group";

	public ChainXModel() {
		this.initPaintTool();
	}

	private void initPaintTool() {
		this.paintToolLocation.x = 0;
		this.paintToolLocation.y = 1;
		this.paintTools = new List<List<string>>();

		this.paintTools.Add(new List<string>() {ChainXModel.PAINT_TOOL_POINTER_ID});
		List<string> voxels = new List<string>();
		for(int i = 0; i < Const.NUMBER_OF_TEXTURE; ++i) {
			voxels.Add(ChainXModel.PAINT_TOOL_VOXEL_ID + i.ToString());
		}
		this.paintTools.Add(voxels);
	}

	public string getPaintTool(int dx, int dy) {
		Point p = this.paintToolLocation;
		p.y = Util.mod(p.y + dy, this.paintTools.Count);
		p.x = Util.mod(p.x + dx, this.paintTools[p.y].Count);

		this.paintToolLocation = p;
		//Debug.Log(p.x + " " + p.y);

		return this.paintTools[p.y][p.x]; //バグってる
	}

	public string getCurrentPaintTool() {
		return this.getPaintTool(0,0);
	}

	public void AddGroup(string gid) {
		if (this.paintTools.Count <= 2) { this.paintTools.Add(new List<string>()); }
		if (this.paintTools[2].Contains(ChainXModel.PAINT_TOOL_GROUP_ID + gid)) { return; }
		this.paintTools[2].Add(ChainXModel.PAINT_TOOL_GROUP_ID + gid);
	}

	public void RemoveGroup(string gid) {
		if (this.paintTools.Count <= 2) { return; }
		this.paintTools[2].Remove(ChainXModel.PAINT_TOOL_GROUP_ID + gid);
		if (this.paintTools[2].Count == 0) {
			this.paintTools.RemoveAt(2);
		}
	}

	/*
	 * 各パラメータが0.5未満なら、切り捨てる。各パラメータが0.5以上なら繰り上げる。
	 */
	public static Vector3 GetRoundIntPoint(Vector3 point) {
		point.x = Mathf.RoundToInt(point.x);
		point.y = Mathf.RoundToInt(point.y);
		point.z = Mathf.RoundToInt(point.z);
		return point;
	}

	public static void Test() {
		ChainXModel model = new ChainXModel();
		Debug.Assert(model.getPaintTool(0,0) == "voxel0");
		Debug.Assert(model.getPaintTool(1,0) == "voxel1");
		Debug.Assert(model.getPaintTool(1,0) == "voxel2");
		Debug.Assert(model.getPaintTool(-1,0) == "voxel1");
		Debug.Assert(model.getPaintTool(-1,0) == "voxel0");
		Debug.Assert(model.getPaintTool(-1,0) == "voxel7");
		Debug.Assert(model.getPaintTool(0,1) == "pointer");
		model.AddGroup("1");
		Debug.Assert(model.getPaintTool(0,1) == "voxel0");
		Debug.Assert(model.getPaintTool(0,1) == "group1");
		model.AddGroup("1");
		Debug.Assert(model.getPaintTool(1,0) == "group1");
		model.RemoveGroup("1");
		Debug.Assert(model.getCurrentPaintTool() == "pointer");
		Debug.Assert(model.getPaintTool(-1,0) == "pointer"); 
		Debug.Assert(model.getPaintTool(-1,0) == "pointer");
	}
}