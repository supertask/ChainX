TARGET_EMU=EmulatedServer
TARGET_REAL=EmulatedServer
SRC=\
	Site.java \
	EmulatedServer.java
MODULES=\
	modules/gson-2.6.2.jar:modules/json-lib-2.4-jdk15.jar
all:
	javac -cp .:${MODULES} ${SRC}
emu: all
	java -cp .:${MODULES} ${TARGET_EMU}

real: all
	java -cp .:${MODULES} ${TARGET_REAL}
