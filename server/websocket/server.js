var fs = require('fs');
var path = require('path');
var WebSocket = require('ws').Server;
const exec = require('child_process').exec;
const execSync = require('child_process').execSync;

var server;

var TMP_DIR = "data/";
var SAVE_FILE = 'data/worked3D.txt';
//var SAVE_FILE = 'data/workedEmpty.txt';
//var OBJ_FILE = 'data/monkey.obj';
//var IMG_FILE = 'data/monkey.jpg';

var MSG_SPLIT_CHAR = '@';
var SPLIT_CHAR = ',';
var SOME_FILE_HEADER = "SOME_FILE" + MSG_SPLIT_CHAR;
var OPERATION_HEADER = "OPERATION" + MSG_SPLIT_CHAR;
var START_HEADER = "START" + MSG_SPLIT_CHAR;
var EXIT_HEADER = "EXIT" + MSG_SPLIT_CHAR;
var ID_LIST_HEADER = "ID_LIST" + MSG_SPLIT_CHAR;
var REQUEST_VOTE_HEADER = "REQUEST_VOTE" + MSG_SPLIT_CHAR;
var APPEND_ENTRIES_HEADER = "APPEND_ENTRIES" + MSG_SPLIT_CHAR;
var VOTE_HEADER = "VOTE" + MSG_SPLIT_CHAR;
var JOINED_HEADER = "JOINED" + MSG_SPLIT_CHAR;

var SOME_FILE_BINARY_HEADER = new Buffer(SOME_FILE_HEADER);
var OPERATION_BINARY_HEADER = new Buffer(OPERATION_HEADER);
var START_BINARY_HEADER = new Buffer(START_HEADER);
var EXIT_BINARY_HEADER = new Buffer(EXIT_HEADER);
var ID_LIST_BINARY_HEADER = new Buffer(ID_LIST_HEADER);
var REQUEST_VOTE_BINARY_HEADER = new Buffer(REQUEST_VOTE_HEADER);
var APPEND_ENTRIES_BINARY_HEADER = new Buffer(APPEND_ENTRIES_HEADER);
var VOTE_BINARY_HEADER = new Buffer(VOTE_HEADER);
var RE_ID = /@(\d*)$/;

var latestNodesID = -1;
var nodeIDs = [];


var Server = function()
{
    var PORT_NUMBER = 18080;

    function _connect(socket) {
        console.log('connected!');
        server.send_file(socket, SAVE_FILE);

        var filepaths = ["", ""];
        socket.on('message', function (msg) {
            //
            // destIdを取り出す
            //
            var msges = _getIdFromEndUntilAt(msg); //メッセージとIDに切り分ける
            msg = msges[0];
            var destId = parseInt(msges[1].toString());
            //console.log(msg.toString());
            
            if (_partEqual(msg, OPERATION_BINARY_HEADER)) {
                console.log(msg.toString());
                server.send(destId, msg); //指定されたIDへ送る
            }
            else if (_partEqual(msg, REQUEST_VOTE_BINARY_HEADER) ||
                     _partEqual(msg, VOTE_BINARY_HEADER) ||
                     _partEqual(msg, APPEND_ENTRIES_BINARY_HEADER)) {
                console.log(msg.toString());
                server.send(destId, msg); //指定されたIDへ送る
            }
            else if (_partEqual(msg, START_BINARY_HEADER)) {
                //console.log(msg);
                latestNodesID++;
                nodeIDs.push(latestNodesID);
                socket.id = latestNodesID;
                console.log("Joined: " + latestNodesID);
                var b = new Buffer(ID_LIST_HEADER + nodeIDs.join(SPLIT_CHAR));
                socket.send(b); //送信者に返信
                b = new Buffer(JOINED_HEADER + latestNodesID);
                server.broadcast(socket, b); //送信者以外にブロードキャスト
                //console.log("my socket:" + socket.id);
            }
            else if (_partEqual(msg, EXIT_BINARY_HEADER)) {
                slice_index = EXIT_BINARY_HEADER.length;
                msg = msg.slice(slice_index);
                if (msg.length > 0) {
                    msg.slice(slice_index);
                    var siteId = Number(msg);
                    if(index != -1) {
                        var index = nodeIDs.indexOf(siteId);
                        nodeIDs.splice(index, 1);
                        console.log("Removed: " + siteId);
                    }
                }
            }
            else if (_partEqual(msg, ID_LIST_BINARY_HEADER)) {
                //pass
            }
            else if (_partEqual(msg, SOME_FILE_BINARY_HEADER)) {
                var filename = _getPathUntilAt(msg);
                var filepath = TMP_DIR + filename;
                var exif_len = SOME_FILE_HEADER.length + filename.length + 1;
                file_binary = msg.slice(exif_len, msg.length);

                var ext = path.extname(filename);
                if (ext == ".obj") { filepaths[0] = filepath; }
                else if(ext == ".jpg") { filepaths[1] = filepath; }
                fs.writeFile(filepath, new Buffer(file_binary), function (err) { });
                console.log("Wrote data into \"" + filepath + "\".");

                if (filepaths[0] != "" && filepaths[1] != "") {
                    //全てのファイル情報送信
                    for(var i = 0; i < filepaths.length; i++) {
                        console.log("Send \"" + filepaths[i] + "\" to clients.");
                        server.send_file(socket, filepaths[i]);
                    }
                    filepaths = ["", ""];
                }

            }
            //server.broadcast(socket, msg); //送信者以外にBroadcast
        });
        socket.on('close', function _onClose(msg) {
            console.log('disconnected...');
        });
    }


    function _partEqual(buf_all, buf_part) {
        if (buf_part.length <= buf_all.length) {
            var i = 0;
            while(i < buf_part.length && (buf_all[i] == buf_part[i])) {
                i++;
            }
            return i == buf_part.length;
        }
        return false;
    }

	function _getPathUntilAt(buf_all) {
		var part = MSG_SPLIT_CHAR.charCodeAt(0);
		var i = 0;
		while(i < buf_all.length && (buf_all[i] != part)) { i++; }
		i++;
		var start_i = i;
		while(i < buf_all.length && (buf_all[i] != part)) { i++; }
		var end_i = i;
		//console.log (String.fromCharCode(buf_all[i]));
        var filepathBinary = buf_all.slice(start_i, end_i);

		return filepathBinary.toString('utf8');
	}

	function _getIdFromEndUntilAt(buf_all) {
		var part = MSG_SPLIT_CHAR.charCodeAt(0);
		var i = buf_all.length - 1;
        var end_i = i;
		while(i >= 0 && (buf_all[i] != part)) { i--; }
        var start_i = i + 1; //@の後のindex
        var idBinary = buf_all.slice(start_i, end_i + 1); //start_iは含む，end_iは含まれない
        var messageBinary = buf_all.slice(0, start_i-1); //start_iは含む，end_iは含まれない
		return [messageBinary, idBinary];
	}

    function _send_file(socket, filepath) {
        var header = new Buffer(SOME_FILE_HEADER + path.basename(filepath) + MSG_SPLIT_CHAR);

        fs.readFile(filepath, function (err, data) {
            if (err == null) {
                socket.send(Buffer.concat([header, data])); //or new Buffer(data)
            }
            else { console.log(err); }
        });
    }

    function _send(destId, msg) {
        this.clients.forEach(function(socket, index) {
            if (socket.id == destId) {
                socket.send(msg);
            }
        });
    }

    function _broadcast(this_socket, msg) {
        this.clients.forEach(function(socket, index) {
            if (this_socket != socket) {
                console.log("socket id: " + socket.id);
                socket.send(msg);
            }
        });
    }


    function _run() {
        server = new WebSocket({ port: PORT_NUMBER });
        server.send = _send;
        server.broadcast = _broadcast;
        server.send_file = _send_file;
        server.on('connection', _connect);
        console.log("Start a server for WebSocket.");
    }

    return {
        run: _run
    };
}

//module.exports = ServerHandler;
new Server().run();
