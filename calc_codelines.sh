#!/bin/bash
SRCS="./Assets/ChainVoxelModule/*.cs \
    ./Assets/ChainVoxelModule/Tester/*.cs \
    ./Assets/ChainXController.cs \
    ./Assets/ChainXModel.cs \
    ./Assets/Plugins/MaxCamera.cs
    ./server/websocket/*.js\
    ./server/websocket/*.java\
    ./server/websocket/*.py\
    ./server/signalingWebsocket/server.js\
"

wc -l ${SRCS}
