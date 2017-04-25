TARGET=Server
SRC=\
	Site.java \
	Server.java
MODULES=\
	modules/gson-2.6.2.jar:modules/json-lib-2.4-jdk15.jar
all:
	javac -cp .:${MODULES} ${SRC}
test: all
	java -cp .:${MODULES} ${TARGET}
