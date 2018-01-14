#!/usr/bin/env python
# -*- coding:utf-8 -*-

import sys
import os
import json
import re
SRC_DIR = "recorded_operations/"
DEST_DIR = "modified_recorded_operations/"
SPLIT_CHAR = '#'
MSG_SPLIT_CHAR = '@'
#UNIT = 100 #100ナノ秒単位 -> 1ナノ秒単位で扱う
UNIT = 1 #100ナノ秒単位 -> 100ナノ秒単位で扱う
#STRIDE_OPERATIONS = 200000 #200000 * 100ナノ秒= 20ミリ秒
#STRIDE_OPERATIONS = 500000 #500000 * 100ナノ秒= 50ミリ秒
#STRIDE_OPERATIONS = 900000 #1000000 * 100ナノ秒= 90ミリ秒
#STRIDE_OPERATIONS = 1000000 #1000000 * 100ナノ秒= 100ミリ秒 (GREAT!)
#STRIDE_OPERATIONS = 1100000 #1000000 * 100ナノ秒= 110ミリ秒 (GREAT!)
#STRIDE_OPERATIONS = 1200000 #1200000 * 100ナノ秒= 120ミリ秒 (GREAT!)
STRIDE_OPERATIONS = 1300000 #1000000 * 100ナノ秒= 130ミリ秒 (GREAT!)
#STRIDE_OPERATIONS = 2000000 #1000000 * 100ナノ秒= 200ミリ秒 (GREAT!)

if len(sys.argv) >= 3:
    # 操作数とステップ数，メッセージ数の計測時
    IS_FAST = (sys.argv[1] == "fast")
    NUM_OF_OPERATION = int(sys.argv[2])
elif len(sys.argv) == 2:
    # サイト数とステップ数，メッセージ数の計測時
    IS_FAST = (sys.argv[1] == "fast")
    NUM_OF_OPERATION = -1 #無限
else:
    IS_FAST = False
    NUM_OF_OPERATION = -1 #無限

RE_NUM_FILE = re.compile("\d+\.txt")
for i,filename in enumerate(os.listdir(SRC_DIR)):
    matched = RE_NUM_FILE.match(filename)
    if not matched:
        continue
    rf = open(SRC_DIR + filename, 'r')
    wf = open(DEST_DIR + filename, 'w')

    currentTs = 0
    exCurrentTs = 0
    firstOperation = True
    line_i = 1
    for line in rf:
        if NUM_OF_OPERATION >= 0 and line_i > NUM_OF_OPERATION:
            break
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
            if IS_FAST:
                #計測時
                wf.write("%s%s%s\n" % (STRIDE_OPERATIONS, SPLIT_CHAR, line))
            else:
                #通常はこっち
                wf.write("%s%s%s\n" % (diffTs * UNIT, SPLIT_CHAR, line))
            exCurrentTs = currentTs
        elif op_name == "START":
            pass
        line_i += 1

    rf.close()
