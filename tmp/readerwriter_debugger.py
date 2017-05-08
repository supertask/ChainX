# coding: utf-8
import re
import sys
POS_ID_RE = re.compile("([-]?\d+:[-]?\d+:[-]?\d+)")

with open("a.log") as rf:
    back_destPosID = ""
    for i,line in enumerate(rf):
        line = line.rstrip()
        match_list = POS_ID_RE.findall(line)
        if match_list:
            posID, destPosID = match_list
            #print posID, destPosID,
            if back_destPosID:
                if back_destPosID == posID:
                    pass                
                else:
                    print "BUG(line %s): %s" % (i+1, line)
                    #sys.exit(1)
            back_destPosID = destPosID

#print "No bug."
