var WebSocket = require('ws').Server;
var server;
var SAVED_FILE = 'data/worked3D_1.txt';

var Server = function()
{
    var TEXT_FILE_HEADER = "TEXT_FILE:\n";
    var PORT_NUMBER = 18080;

    function _broadcast(socket, message) {
        this.clients.forEach(function(client, index) {
            if (socket != client) { client.send(message); }
        });
    }

    function _connect(socket) {
        console.log('connected!');
        var fs = require('fs');
        var loading_text = TEXT_FILE_HEADER;
        fs.readFile(SAVED_FILE, 'utf8', function (err, text) {
            if (err == null) {
                loading_text += text;
                socket.send(loading_text);
            }
            else { console.log(err); }
        });

        socket.on('message', function (message) {
            console.log(message.toString());
            //var json_obj = JSON.parse(message);
            socket.send(message); //送信者に返す
            server.broadcast(socket, message); //送信者以外にBroadcast
        });
        socket.on('close', function _onClose(message) {
            console.log('disconnected...');
        });

        //setInterval(function() { socket.send("Hi!"); }, 1000);
    }

    function _run() {
        server = new WebSocket({ port: PORT_NUMBER });
        server.broadcast = _broadcast;
        server.on('connection', _connect);
        console.log("Start a server for WebSocket.");
    }

    return {
        run: _run,
    };
}

//module.exports = ServerHandler;
new Server().run();
