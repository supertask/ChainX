# -*- coding: utf-8 -*-
import os
import bpy
import math
from mathutils import Vector
from os.path import basename

bpy.context.scene.unit_settings.system='METRIC'
#bpy.context.scene.render.engine = 'BLENDER_RENDER'
bpy.context.scene.render.engine = 'CYCLES'
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces[0].viewport_shade = 'TEXTURED'

def create_voxel(x,y,z):
    bpy.ops.mesh.primitive_cube_add()
    bpy.context.object.name = '%s:%s:%s' % (x,y,z)
    bpy.context.object.location = Vector([x,y,z])
    bpy.context.object.dimensions = Vector([1,1,1])
    return bpy.context.object

def boolean_modifier(cube, polygon):
    cube.active_material = polygon.active_material
    bpy.context.scene.objects.active = cube
    cube.data.use_auto_smooth = 1 #Smoothを解除

    bpy.ops.object.modifier_add( type = 'BOOLEAN' )
    cube.modifiers["Boolean"].operation = 'INTERSECT'
    cube.modifiers["Boolean"].object = polygon
    bpy.ops.object.modifier_apply( modifier = "Boolean" )



def delete_obj(name):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.data.objects[name].select = True
    bpy.ops.object.delete()

def delete_all_objects():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        obj.select = True
        bpy.ops.object.delete()
    bpy.ops.object.delete()


def split_object(path, texture_path):
    delete_all_objects()

    # Import an obj that you want to split
    imported_object = bpy.ops.import_scene.obj(filepath=path)
    target = bpy.context.selected_objects[0] ####<--Fix

    # Create a texture
    texture_name, _ = os.path.splitext(basename("./monkey.jpg"))
    target.active_material.use_nodes = True
    tree = target.active_material.node_tree
    diffuse_node = tree.nodes['Diffuse BSDF']
    texture_node = tree.nodes.new("ShaderNodeTexImage")
    texture_node.image = bpy.data.images.load(texture_path)
    tree.links.new(diffuse_node.inputs['Color'], texture_node.outputs['Color'])

    # Calc bounds
    bpy.data.objects[target.name].select = True
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS') #move to center of bounds
    target.location.x = 0.0
    target.location.y = 0.0
    target.location.z = 0.0
    half_dimensions = target.dimensions / 2
    center = target.location

    print(center)
    print(target.dimensions)
    print(half_dimensions)

    minDZ, maxDZ = round(-half_dimensions.z), round(half_dimensions.z)
    minDY, maxDY = round(-half_dimensions.y), round(half_dimensions.y)
    minDX, maxDX = round(-half_dimensions.x), round(half_dimensions.x)
    print(minDZ, maxDZ)
    print(minDY, maxDY)
    print(minDX, maxDX)
    print()
    for z in range(minDZ, maxDZ+1):
        for y in range(minDY, maxDY+1):
            for x in range(minDX, maxDX+1):
                voxel = create_voxel(x,y,z)
                boolean_modifier(voxel, target)

    delete_obj(target.name)

split_object(path="./monkey.obj", texture_path="./monkey.jpg")

