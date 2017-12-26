TARGET=VSite
SRC=\
	Operation.java \
	VSite.java
MODULES=\
	.:java_modules/gson-2.6.2.jar:java_modules/tyrus-standalone-client-1.9.jar

# 通常実行の際のサイト数，各サイトごとの操作数の設定
SITES=5
OPERATIONS=10

# 計測時の操作数の刻み幅
STRIDE_OPERATIONS=25
EVALUATED_DATA_DIR=evaluated_data

all: cleaneval
	javac -cp ${MODULES} ${SRC}

#
# ゆっくり実行
#
raft: all
	python modifier.py
	java -cp ${MODULES} ${TARGET} RAFT ${SITES}
chain: all
	python modifier.py
	java -cp ${MODULES} ${TARGET} CHAINVOXEL ${SITES}


#
# 最速実行
#
# 性能評価する
eval: all
	python evaluate.py ${MODULES} ${TARGET} ${STRIDE_OPERATIONS}

#性能評価できてるか確かめる
siteR: all
	python modifier.py fast
	java -cp ${MODULES} ${TARGET} RAFT ${SITES}

siteC: all
	python modifier.py fast
	java -cp ${MODULES} ${TARGET} CHAINVOXEL ${SITES}

opR: all
	python modifier.py fast ${OPERATIONS}
	java -cp ${MODULES} ${TARGET} RAFT ${SITES} ${OPERATIONS}

opC: all
	python modifier.py fast ${OPERATIONS}
	java -cp ${MODULES} ${TARGET} CHAINVOXEL ${SITES} ${OPERATIONS}

plot:
	cd evaluated_data/; python my_plot.py

cleaneval:
	rm -f ${EVALUATED_DATA_DIR}/raft_operations_vs_messages.txt
	rm -f ${EVALUATED_DATA_DIR}/raft_operations_vs_steps.txt
	rm -f ${EVALUATED_DATA_DIR}/chainvoxel_operations_vs_steps.txt
	rm -f ${EVALUATED_DATA_DIR}/chainvoxel_operations_vs_messages.txt
	rm -f ${EVALUATED_DATA_DIR}/raft_sites_vs_steps.txt
	rm -f ${EVALUATED_DATA_DIR}/raft_sites_vs_messages.txt
	rm -f ${EVALUATED_DATA_DIR}/chainvoxel_sites_vs_steps.txt
	rm -f ${EVALUATED_DATA_DIR}/chainvoxel_sites_vs_messages.txt

clean:
	rm *.class
