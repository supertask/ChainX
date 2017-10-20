#!/bin/bash
BLENDER=/Applications/blender.app/Contents/MacOS/blender
TARGET=boolean_calc.py
ARGS=$@
${BLENDER} -P ${TARGET} -- $@
#${BLENDER} --background -P ${TARGET} -- $@
