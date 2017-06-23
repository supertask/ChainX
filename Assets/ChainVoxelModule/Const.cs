using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class Const {
	public static int MOUSE_LEFT_CLICK = 0;
	public static int MOUSE_RIGHT_CLICK = 1;
	public static int MOUSE_BOTH_CLICK = 2;

	public static string UI_SELECTING_POINTER_NAME = "MouseCursor";
	public static string UI_SELECTING_VOXEL_NAME = "SelectingVoxel";

	public static string PAINT_TOOL_PATH = "PaintTool/";

	public static Shader TOON_SHADER = Shader.Find ("Toon/Basic Outline");
	public static Shader DIFFUSE_SHADER = Shader.Find ("Diffuse");
	public static char SPLIT_CHAR = ',';
	public static string SAVED_DIR = Application.persistentDataPath;
	public static string SAVED_FILE = Application.persistentDataPath + "/worked3D.txt";
	public static int UI_LAYER = 8;
	public static Regex REGEX_POSID = new Regex( @"[-]?[\d]+:[-]?[\d]+:[-]?[\d]+");
	public static int NUMBER_OF_TEXTURE = 8;
	public static Vector3 PAINT_TOOL_PLATE_POSITION = Camera.main.ScreenToWorldPoint (new Vector3(-5,-30,5));

	public static float VOXEL_PLATE_DIAMETER = GameObject.Find(Const.PAINT_TOOL_PATH + "VoxelPlate").GetComponent<Renderer>().bounds.size.x - 2.2f;

}

public class MessageType {
	public static string OPERATION = "OPERATION";
	public static string TEXT_FILE = "TEXT_FILE"; //no used
	public static string ERROR = "ERROR"; //no used
	public static string EXIT = "EXIT";
}

public class MessageHeader {
	public static string OPERATION = MessageType.OPERATION + ":";
	public static string TEXT_FILE = MessageType.TEXT_FILE + ":"; //no used
	public static string ERROR = MessageType.ERROR + ":"; //no used
	public static string EXIT = MessageType.EXIT; //no used
}