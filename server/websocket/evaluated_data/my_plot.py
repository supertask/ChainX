import os
import subprocess
import sys
import re

def plot(title, x, y, raft_filepath, chainvoxel_filepath, outpath):
    wf = open('tmp.g','w')
    wf.write('set xlabel "%s"\n' % x)
    wf.write('set ylabel "%s"\n' % y)
    #wf.write('set key bottom\n')
    wf.write('set grid ls 0\n')
    wf.write('set term aqua title "%s"; ' % title)
    #wf.write('set key font "Arial,15"\n')
    wf.write('set terminal postscript eps enhanced color\n') # neccessary to output eps with color
    wf.write('set output "%s"\n' % outpath)
    wf.write('plot "%s" with line title "Lets3D-C" lc rgb "green", \
        "%s" with line title "ChainVoxel" lc rgb "red"\n' % (raft_filepath, chainvoxel_filepath))
    wf.close()
    p = subprocess.Popen('gnuplot -e \'load "tmp.g"\'', shell=True)
    p.wait()

plot(
    title = "Operations vs messages",
    x = "Number of operations", y = "Number of messages",
    raft_filepath = "./raft_operations_vs_messages.txt",
    chainvoxel_filepath = "./chainvoxel_operations_vs_messages.txt",
    outpath = "./img/operations_vs_messages.eps"
)
