var fs = require('fs');
var path = require('path');
var WebSocket = require('ws').Server;
const exec = require('child_process').exec;
const execSync = require('child_process').execSync;

var server;

var TMP_DIR = "data/tmp/";
var SAVE_FILE = 'data/worked3D_1.txt';
//var SAVE_FILE = 'data/worked3D.txt';
var OBJ_FILE = 'data/monkey.obj';
var IMG_FILE = 'data/monkey.jpg';
var MTL_FILE = 'data/monkey.mtl';

var SPLIT_CHAR = '@';
var SOME_FILE_HEADER = "SOME_FILE" + SPLIT_CHAR;
var OPERATION_HEADER = "OPERATION" + SPLIT_CHAR;
var EXIT_HEADER = "EXIT" + SPLIT_CHAR;
var ERROR_HEADER = "ERROR" + SPLIT_CHAR;

var SOME_FILE_BINARY_HEADER = new Buffer(SOME_FILE_HEADER);
var OPERATION_BINARY_HEADER = new Buffer(OPERATION_HEADER);
var EXIT_BINARY_HEADER = new Buffer(EXIT_HEADER);
var ERROR_BINARY_HEADER = new Buffer(ERROR_HEADER);

var boolean_calc = "./run_blender.sh"
var boolean_id = "_boolean"

var Server = function()
{
    var PORT_NUMBER = 18080;

    function _partEqual(buf_all, buf_part) {
        if (buf_part.length < buf_all.length) {
            var i = 0;
            while(i < buf_part.length && (buf_all[i] == buf_part[i])) { i++; }
            return i == buf_part.length;
        }
        return false;
    }

	function _getStringUntilAt(buf_all) {
		var part = SPLIT_CHAR.charCodeAt(0);
		var i = 0;
		while(i < buf_all.length && (buf_all[i] != part)) { i++; }
		i++;
		//console.log (String.fromCharCode(buf_all[i]));
		var start_i = i;
		while(i < buf_all.length && (buf_all[i] != part)) { i++; }
		var end_i = i;
		//console.log (String.fromCharCode(buf_all[i]));

		var l = end_i - start_i;
        var filepathBinary = buf_all.slice(start_i, end_i);

		return filepathBinary.toString('utf8');
	}

    function _send_file(socket, filepath) {
        var header = new Buffer(SOME_FILE_HEADER + path.basename(filepath) + SPLIT_CHAR);

        fs.readFile(filepath, function (err, data) {
            if (err == null) {
                socket.send(Buffer.concat([header, data])); //or new Buffer(data)
            }
            else { console.log(err); }
        });
    }

    function _broadcast(socket, message) {
        this.clients.forEach(function(client, index) {
            if (socket != client) { client.send(message); }
        });
    }

    function _connect(socket) {
        console.log('connected!');

        //socket.send(OPERATION_HEADER + "{\"sid\":\"2\", \"opType\":\"0\", \"ts\":\"1\", \"opParams\": {\"posID\":\"1:1:1\", \"textureType\":\"1\"} }");
        server.send_file(socket, SAVE_FILE);

        var filepaths = ["", ""];
        socket.on('message', function (message) {

            if (_partEqual(message, OPERATION_BINARY_HEADER)) {
                console.log(message.toString());
                //var json_obj = JSON.parse(message);
                //socket.send(message); //送信者に返す
                server.broadcast(socket, message); //送信者以外にBroadcast
            }
            else if (_partEqual(message, SOME_FILE_BINARY_HEADER)) {
                var filename = _getStringUntilAt(message);
                var filepath = TMP_DIR + filename;
                var exif_len = SOME_FILE_HEADER.length + filename.length + 1;
                file_binary = message.slice(exif_len, message.length);

                var ext = path.extname(filename);
                if (ext == ".obj") { filepaths[0] = filepath; }
                else if(ext == ".jpg") { filepaths[1] = filepath; }
                fs.writeFile(filepath, new Buffer(file_binary), function (err) { });
                console.log("Wrote data into \"" + filepath + "\".");

                if (filepaths[0] != "" && filepaths[1] != "") {
                    //objファイルをブーリアン演算し、data/tmp/に保存！
                    /*
                    exec_line = boolean_calc + " \"" + filepaths[0]  + "\"";
                    console.log("Exec \'" + exec_line + "\'.");

                    execSync(exec_line, function(err, stdout) {
                        //console.log(stdout);
                    });
                    */

                    //全てのファイル情報送信
                    for(var i = 0; i < filepaths.length; i++) {
                        console.log("Send \"" + filepaths[i] + "\" to clients.");
                        server.send_file(socket, filepaths[i]);
                    }
                    filepaths = ["", ""];
                }

            }

            else if (_partEqual(message, EXIT_BINARY_HEADER)) {
                console.log("EXIT");
            }
            else if (_partEqual(message, ERROR_BINARY_HEADER)) {
                console.log("ERROR");
            }
            //server.broadcast(socket, message); //送信者以外にBroadcast
        });
        socket.on('close', function _onClose(message) {
            console.log('disconnected...');
        });
    }

    function _run() {
        server = new WebSocket({ port: PORT_NUMBER });
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
