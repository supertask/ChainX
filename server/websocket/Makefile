TARGET=VSite
SRC=\
	Operation.java \
	VSite.java
MODULES=\
	.:java_modules/gson-2.6.2.jar:java_modules/tyrus-standalone-client-1.9.jar
SITES=2

all:
	python modifier.py
	javac -cp ${MODULES} ${SRC}
raft: all
	java -cp ${MODULES} ${TARGET} RAFT ${SITES}
voxel: all
	java -cp ${MODULES} ${TARGET} CHAINVOXEL ${SITES}


eval: all
	python evaluate.py ${MODULES} ${TARGET}





clean:
	rm *.class
