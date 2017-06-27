#!/bin/bash
SRCS="./Assets/ChainVoxelModule/ChainVoxel.cs \
    ./Assets/ChainVoxelModule/ChainVoxelTester.cs \
    ./Assets/ChainVoxelModule/Const.cs \
    ./Assets/ChainVoxelModule/EmulatedSocket.cs \
    ./Assets/ChainVoxelModule/EmulatedWebSocket.cs \
    ./Assets/ChainVoxelModule/Group.cs \
    ./Assets/ChainVoxelModule/Operation.cs \
    ./Assets/ChainVoxelModule/RealSocket.cs \
    ./Assets/ChainVoxelModule/StructureTable.cs \
    ./Assets/ChainVoxelModule/Util.cs \
    ./Assets/ChainVoxelModule/Voxel.cs \
    ./Assets/ChainXController.cs \
    ./Assets/ChainXModel.cs \
    ./Assets/ChainXTester.cs \
    ./Assets/Example/EchoTest.cs \
    ./Assets/Plugins/MaxCamera.cs
    ./server/websocket/server.js"

wc -l ${SRCS}
