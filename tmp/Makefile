TARGET=Example.exe
SRC=\
	MinimalJson/*.cs \
	Util.cs \
	Voxel.cs \
	Operation.cs \
	EmulatedSocket.cs \
	StructureTable.cs \
	ChainVoxel.cs \
	Group.cs \
	Example.cs

all:
	mcs -d:DEBUG ${SRC} -out:${TARGET}
test: all
	@for x in {1..5}; do \
		echo "test" $$x; \
		mono ${TARGET} > a.log; \
		python readerwriter_debugger.py; \
	done

show: all
	mono ${TARGET}

clean:
	rm *.exe
