import os
import subprocess
import sys
import re

def plot(title, x, y, filepath, outpath):
    wf = open('tmp.g','w')
    wf.write('set xtics 1; \n')
    wf.write('set xlabel "%s"\n' % x)
    wf.write('set ylabel "%s"\n' % y)
    wf.write('set key bottom\n')
    wf.write('set grid ls 0\n')
    wf.write('set term aqua title "%s"; ' % title)
    #wf.write('set key font "Arial,15"\n')
    wf.write('set terminal postscript eps enhanced color\n') # neccessary to output eps with color
    wf.write('set output "%s"\n' % outpath)
    wf.write('plot "%s" with line title "On primary and grouping Layer" lc rgb "red" lw 3\n' % (filepath))
    wf.close()
    p = subprocess.Popen('gnuplot -e \'load "tmp.g"\'', shell=True)
    p.wait()


plot(
    title = "Sites vs memory (KB)",
    x = "Number of sites", y = "Memory on both layers (KB)",
    filepath = "./total_memory_KB.txt",
    outpath = "./img/total_memory.eps"
)
