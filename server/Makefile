TARGET=Server
SRC=\
	Site.java \
	Server.java
MODULES=\
	modules/gson-2.6.2.jar
all:
	javac -cp .:${MODULES} ${SRC}
test: all
	java -cp .:${MODULES} ${TARGET}
