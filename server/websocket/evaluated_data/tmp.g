set xlabel "Number of operations"
set ylabel "Number of messages"
set grid ls 0
set term aqua title "Operations vs messages"; set terminal postscript eps enhanced color
set output "./img/operations_vs_messages.eps"
plot "./raft_operations_vs_messages.txt" with line title "Lets3D-C" lc rgb "green",         "./chainvoxel_operations_vs_messages.txt" with line title "ChainVoxel" lc rgb "red"
