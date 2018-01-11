#!/usr/bin/env python
# -*- coding:utf-8 -*-

import sys
import os
import json
SPLIT_CHAR = '#'
MSG_SPLIT_CHAR = '@'

if len(sys.argv) < 2:
    print "引数が足りない"
    print "(<infile1(*.txt)> or <infile1(*.txt)> <interval seconds(any int number)>)"
    sys.exit(1)


in_file_1 = sys.argv[1]
out_file = "interval.txt"

# デフォルト: 3秒(3 * 10^7 * 100ナノ秒)
LIMIT_INTERVAL = 3 * (10 ** 7)
if len(sys.argv) == 3:
    # N秒(N * 10^7 * 100ナノ秒)
    LIMIT_INTERVAL = int(sys.argv[2]) * (10 ** 7)


wf = open(out_file, 'w')
rf = open(in_file_1, 'r')

# 1秒(2 * 10^7 * 100ナノ秒)の間隔をあける
currentTs = 0
exCurrentTs = 0
newCurrentTs = 0
ex_line = ""
#1 * (10 ** 7)
firstOperation = True
for line in rf:
    line = line.replace('\n', '')
    if line == '': continue

    op_name,param = line.split(MSG_SPLIT_CHAR)
    if op_name == "OPERATION":
        op_dict = json.loads(param) 
        ts_str = op_dict["ts"]
        currentTs = long(op_dict["ts"])
        diffTs = currentTs - exCurrentTs
        if firstOperation:
            newCurrentTs = currentTs
            diffTs = 0
            firstOperation = False
        if diffTs > LIMIT_INTERVAL:
            #print diffTs / (10**7)
            diffTs = LIMIT_INTERVAL
        newCurrentTs += diffTs
        line = line.replace(ts_str, str(newCurrentTs))
        wf.write(line + "\n")
    exCurrentTs = currentTs
    ex_line = line

rf.close()
wf.close()

print 'Output file: "%s"' % out_file
