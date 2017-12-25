#!/usr/bin/env python
# -*- coding:utf-8 -*-

import sys
import os
import json
SPLIT_CHAR = '#'
MSG_SPLIT_CHAR = '@'
UNIT = 1 #100ナノ秒単位 -> 100ナノ秒単位で扱う

if len(sys.argv) < 3:
    print "引数が足りない(<infile1> <infile2>)"
    sys.exit(1)

in_file_1 = sys.argv[1]
in_file_2 = sys.argv[2]
out_file = "combined_file.txt"

wf = open(out_file, 'w')

last_ts = 0
with open(in_file_1) as rf:
    last_line = ""
    for line in rf:
        wf.write(line)
        last_line = line
    _,op_line = last_line.split(MSG_SPLIT_CHAR)
    op_dict = json.loads(op_line) 
    last_ts = long(op_dict['ts'])

rf = open(in_file_2, 'r')

# 1秒(2 * 10^7 * 100ナノ秒)の間隔をあける
currentTs = 0
exCurrentTs = 0
newCurrentTs = last_ts + 1 * (10 ** 7)
firstOperation = True
for line in rf:
    line = line.replace('\n', '')
    if line == '': continue
    #print line

    op_name,param = line.split(MSG_SPLIT_CHAR)
    if op_name == "OPERATION":
        op_dict = json.loads(param) 
        ts_str = op_dict["ts"]
        currentTs = long(op_dict["ts"])
        diffTs = currentTs - exCurrentTs
        if firstOperation:
            diffTs = 0
            firstOperation = False
        newCurrentTs += diffTs
        line = line.replace(ts_str, str(newCurrentTs))
        wf.write(line + "\n")
        exCurrentTs = currentTs
    else:
        pass

rf.close()
wf.close()
