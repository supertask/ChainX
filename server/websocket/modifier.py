#!/usr/bin/env python
# -*- coding:utf-8 -*-
import os
import json
SRC_DIR = "recorded_operations/"
DEST_DIR = "modified_recorded_operations/"
SPLIT_CHAR = '#'
MSG_SPLIT_CHAR = '@'
UNIT = 100 #100ナノ秒単位 -> 1ナノ秒単位で扱う

for filename in os.listdir(SRC_DIR):
    if filename == ".DS_Store":
        continue
    rf = open(SRC_DIR + filename, 'r')
    wf = open(DEST_DIR + filename, 'w')

    currentTs = 0
    exCurrentTs = 0
    firstOperation = True
    for line in rf:
        line = line.replace('\n', '')
        if line == '':
            continue
        #print line
        op_name,param = line.split(MSG_SPLIT_CHAR)
        if op_name == "OPERATION":
            op_dict = json.loads(param) 
            currentTs = long(op_dict['ts'])
            diffTs = currentTs - exCurrentTs
            if firstOperation:
                diffTs = 0
                firstOperation = False
            #print op_dict['sid']
            wf.write("%s%s%s\n" % (diffTs * UNIT, SPLIT_CHAR, line))
            exCurrentTs = currentTs
        elif op_name == "START":
            pass

    rf.close()
