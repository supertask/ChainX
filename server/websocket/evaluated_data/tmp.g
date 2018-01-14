set xtics 1; 
set xlabel "Number of sites"
set ylabel "Memory on both layers (KB)"
set key bottom
set grid ls 0
set term aqua title "Sites vs memory (KB)"; set terminal postscript eps enhanced color
set output "./img/total_memory.eps"
plot "./total_memory_KB.txt" with line title "On primary and grouping Layer" lc rgb "red" lw 3
