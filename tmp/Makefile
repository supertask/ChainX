TARGET=Example.exe
SRC=\
	Util.cs \
	Voxel.cs \
	Operation.cs \
	StructureTable.cs \
	Group.cs \
	Example.cs
# ChainVoxel.cs 

all:
	mcs ${SRC} -out:${TARGET}
test: all
	mono ${TARGET}
clean:
	rm *.exe
