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
	public static int NUMBER_OF_TEXTURE = 8;
	public static Vector3 PAINT_TOOL_PLATE_POSITION = Camera.main.ScreenToWorldPoint (new Vector3(-5,-30,5));


	public static string TEST_OBJ_PATH = Application.dataPath + "/objects";

	public static int TEST_QUALITY = 80; //テスト回数
}

public class MessageType {
	public static string OPERATION = "OPERATION";
	public static string SOME_FILE = "SOME_FILE";

	public static string EXIT = "EXIT";
	public static string ERROR = "ERROR"; //no used
}

public class MessageHeader {
	public static char SPLIT_CHAR = '@';
	public static string OPERATION = MessageType.OPERATION + MessageHeader.SPLIT_CHAR;
	public static string SOME_FILE = MessageType.SOME_FILE + MessageHeader.SPLIT_CHAR;

	public static byte[] OPERATION_BINARY = Encoding.UTF8.GetBytes(MessageType.OPERATION + MessageHeader.SPLIT_CHAR);
	public static byte[] SOME_FILE_BINARY = Encoding.UTF8.GetBytes(MessageType.SOME_FILE + MessageHeader.SPLIT_CHAR);

	public static byte[] EXIT = Encoding.UTF8.GetBytes(MessageType.EXIT); //no used
	public static byte[] ERROR = Encoding.UTF8.GetBytes(MessageType.ERROR + MessageHeader.SPLIT_CHAR); //no used
}