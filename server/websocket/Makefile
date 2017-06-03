TARGET_EMU=EmulatedServer
TARGET_REAL=EmulatedServer
SRC=\
	Operation.java \
	EmulatedServer.java
MODULES=\
	.:modules/gson-2.6.2.jar:modules/java_websocket.jar
all:
	javac -cp ${MODULES} ${SRC}
test: all
	java -cp ${MODULES} ${TARGET_EMU}

real: all
	java -cp ${MODULES} ${TARGET_REAL}
