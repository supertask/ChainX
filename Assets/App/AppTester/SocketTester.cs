using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class SocketTester
{
	public static void Test() {
		EmulatedWebSocket socket = new EmulatedWebSocket(null);

		string opLine = "OPERATION@{xxfefegege}@24";
		byte[] opBinary = System.Text.Encoding.ASCII.GetBytes(opLine);
		socket.getIdFromEndUntilAt(ref opBinary);
		socket.getOperationFromEndUntilAt(ref opBinary);
	}
}