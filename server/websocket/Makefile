TARGET=VSite
SRC=\
	Operation.java \
	VSite.java
MODULES=\
	.:java_modules/gson-2.6.2.jar:java_modules/tyrus-standalone-client-1.9.jar
all:
	javac -cp ${MODULES} ${SRC}
test: all
	java -cp ${MODULES} ${TARGET}

