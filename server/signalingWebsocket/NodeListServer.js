/*
 * Message types:
 * Receiver:
 *      "J"
 *      "L<number>"
 * Sender:
 *      "<number>,<number>,..." (A list of a node id)
 */


var fs = require('fs');
var path = require('path');
var WebSocket = require('ws').Server;
var clog = require('./cclog.js').clog;
const exec = require('child_process').exec;
const execSync = require('child_process').execSync;

var JOIN_TAG = "J";
var LEAVE_TAG = "L"; 
var SPLIT_CHAR = ',';

var latestNodesID = -1;
var nodeIDs = [];

var NodeListServer = function()
{
    var PORT_NUMBER = 18080;

    function _broadcast(socket, message) {
        this.clients.forEach(function(client, index) {
            if (socket != client) { client.send(message); }
        });
    }

    function _connect(socket) {
        clog('connected!');

        socket.on('message', function (msg_binary) {
            var msg = msg_binary.toString();
            //console.log(msg);

            if (msg.indexOf(JOIN_TAG) != -1) {
                latestNodesID++;
                nodeIDs.push(latestNodesID);
                clog("Joined: " + latestNodesID);
                socket.send(nodeIDs.join(SPLIT_CHAR));
            }
            else if (msg.indexOf(LEAVE_TAG) != -1) {
                msg = msg.slice(1);
                if (msg.length > 0) {
                    msg.slice(1);
                    var siteId = Number(msg);
                    if(index != -1) {
                        var index = nodeIDs.indexOf(siteId);
                        nodeIDs.splice(index, 1);
                        clog("Removed: " + siteId);
                    }
                }
            }
        });
        socket.on('close', function _onClose(message) {
            clog('disconnected...');
        });
    }

    function _run() {
        server = new WebSocket({ port: PORT_NUMBER });
        server.broadcast = _broadcast;
        server.on('connection', _connect);
        clog("Listening on " + PORT_NUMBER);
    }

    return {
        run: _run
    };
}
module.exports = NodeListServer;
