#!/bin/bash
BLENDER=/Applications/blender.app/Contents/MacOS/blender
TARGET=boolean_calc.py
#${BLENDER} --background -P ${TARGET} 
${BLENDER} -P ${TARGET}
