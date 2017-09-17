var path = require('path');
var WebSocket = require('ws').Server;
var server;
var SAVE_FILE = 'data/worked3D_1.txt';
var OBJ_FILE = 'data/monkey.obj';
var IMG_FILE = 'data/monkey.jpg';
var MTL_FILE = 'data/monkey.mtl';
var SPLIT_CHAR = "@"
//var TEXT_FILE_HEADER = "TEXT_FILE" + SPLIT_CHAR;
//var IMG_FILE_HEADER = "IMG_FILE" + SPLIT_CHAR;
//var OBJ_FILE_HEADER = "OBJ_FILE" + SPLIT_CHAR;
//var MTL_FILE_HEADER = "MTL_FILE" + SPLIT_CHAR;
var SOME_FILE_HEADER = "SOME_FILE" + SPLIT_CHAR;
var OPERATION_HEADER = "OPERATION" + SPLIT_CHAR;



var Server = function()
{
    var PORT_NUMBER = 18080;

    /*
    function _send_file(socket, filename) {
        var fs = require('fs');
        fs.readFile(filename, 'utf8', function (err, text) {
            if (err == null) {
                socket.send(text);
            }
            else { console.log(err); }
        });
    }
    */

    function _send_file(socket, filepath) {
        var fs = require('fs');
        var header = new Buffer(SOME_FILE_HEADER + path.basename(filepath) + SPLIT_CHAR);

        fs.readFile(filepath, function (err, data) {
            if (err == null) {
                //fs.writeFile("data/x.jpg", new Buffer(data), function (err) { });
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

        socket.send(OPERATION_HEADER + "{\"sid\":\"2\", \"opType\":\"0\", \"ts\":\"1\", \"opParams\": {\"posID\":\"1:1:1\", \"textureType\":\"1\"} }");
        server.send_file(socket, SAVE_FILE);
        server.send_file(socket, OBJ_FILE);
        server.send_file(socket, MTL_FILE);
        server.send_file(socket, IMG_FILE);

        socket.on('message', function (message) {
            strs = message.toString().split(SPLIT_CHAR);
            //var json_obj = JSON.parse(message);
            //socket.send(message); //送信者に返す
            console.log(message.toString());
            server.broadcast(socket, message); //送信者以外にBroadcast
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
