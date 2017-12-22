using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using UnityEngine;

public class Const {
	public static string SERVER_ID = "127.0.0.1";

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
	public static Regex REGEX_POSID = new Regex(@"[-]?[\d]+:[-]?[\d]+:[-]?[\d]+");
	public static Regex REGEX_GROUP = new Regex(@"group.+");
	public static int NUMBER_OF_TEXTURE = 9;
	public static Vector3 PAINT_TOOL_PLATE_POSITION = Camera.main.ScreenToWorldPoint (new Vector3(-5,-30,5));

	public static string TEST_OBJ_PATH = Application.dataPath + "/App/ChainVoxelModule/TestObjects/";
	public static int TEST_QUALITY = 60; //テスト回数
	//public static int TEST_QUALITY = 1; //テスト回数

	public static char MSG_SPLIT_CHAR = '@';
	public static string OPERATION_HEADER = "OPERATION" + Const.MSG_SPLIT_CHAR;
	public static string SOME_FILE_HEADER = "SOME_FILE" + Const.MSG_SPLIT_CHAR;
	public static string START_HEADER = "START" + Const.MSG_SPLIT_CHAR;
	public static string EXIT_HEADER = "EXIT" + Const.MSG_SPLIT_CHAR;
	public static string ID_LIST_HEADER = "ID_LIST" + Const.MSG_SPLIT_CHAR;
	public static string JOIN_HEADER = "JOIN" + Const.MSG_SPLIT_CHAR;

	public static byte[] OPERATION_BINARY_HEADER = Encoding.UTF8.GetBytes(Const.OPERATION_HEADER);
	public static byte[] SOME_FILE_BINARY_HEADER = Encoding.UTF8.GetBytes(Const.SOME_FILE_HEADER);

	public static byte[] START_BINARY_HEADER = Encoding.UTF8.GetBytes(Const.START_HEADER);
	public static byte[] EXIT_BINARY_HEADER = Encoding.UTF8.GetBytes(Const.EXIT_HEADER);
	public static byte[] ID_LIST_BINARY_HEADER = Encoding.UTF8.GetBytes(Const.ID_LIST_HEADER);
	public static byte[] JOIN_BINARY_HEADER = Encoding.UTF8.GetBytes(Const.JOIN_HEADER);
}

public class MessageType {
	public static string OPERATION = "OPERATION";
	public static string SOME_FILE = "SOME_FILE";
	public static string START = "START";
	public static string EXIT = "EXIT";
	public static string ID_LIST = "ID_LIST";
}

public class MessageHeader {

}