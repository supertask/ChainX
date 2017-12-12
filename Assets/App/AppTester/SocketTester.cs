using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class SocketTester
{
	public static void Test() {
		EmulatedWebSocket socket = new EmulatedWebSocket(null);

		string opLine;
		byte[] opBinary;

		opLine = "OPERATION@{xxfefegege}@24";
		opBinary = System.Text.Encoding.ASCII.GetBytes(opLine);
		Debug.Assert(
			socket.getAtIndexFromEnd(ref opBinary) == ("OPERATION@{xxfefegege}@".Length - 1)
		);

		opLine = "OPERATION@{xxfefegege}@24";
		opBinary = System.Text.Encoding.ASCII.GetBytes(opLine);
		socket.getIdFromEndUntilAt(ref opBinary);
		socket.getOperationFromEndUntilAt(ref opBinary);
	}
}