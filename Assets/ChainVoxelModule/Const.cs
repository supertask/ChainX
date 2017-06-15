using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Const {
	public static int MOUSE_LEFT_CLICK = 0;
	public static int MOUSE_RIGHT_CLICK = 1;
	public static int MOUSE_BOTH_CLICK = 2;
	public static Shader TOON_SHADER = Shader.Find ("Toon/Basic Outline");
	public static Shader DIFFUSE_SHADER = Shader.Find ("Diffuse");
	public static char SPLIT_CHAR = ',';
	public static string SAVED_DIR = Application.persistentDataPath;
	public static string SAVED_FILE = Application.persistentDataPath + "/worked3D.txt";
	public static int UI_LAYER = 8;

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