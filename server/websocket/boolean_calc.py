# -*- coding: utf-8 -*-
import os
import bpy
import math
from mathutils import Vector
bpy.context.scene.unit_settings.system='METRIC'

def create_voxel(x,y,z):
    bpy.ops.mesh.primitive_cube_add()
    bpy.context.object.name = '%s:%s:%s' % (x,y,z)
    bpy.context.object.location = Vector([x,y,z])
    bpy.context.object.dimensions = Vector([1,1,1])
    return bpy.context.object

def boolean_modifier(cube, polygon):
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
    

def split_object(path):
    # Delete the default cube
    delete_obj('Cube')

    # Import an obj that you want to split
    imported_object = bpy.ops.import_scene.obj(filepath=path)
    target = bpy.context.selected_objects[0] ####<--Fix
    print('Imported name: ', target.name)
    #mat = bpy.data.materials.get("Material")

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

split_object(path="./monkey.obj")


## 5つの空のオブジェクトを配置
#bpy.ops.object.empty_add(type = 'SPHERE', location = (0, 0, 0 ))
#empty0 = bpy.data.objects["Empty"]
#bpy.ops.object.empty_add(type = 'SPHERE', location = (0.7, 0, 0 ))
#empty1 = bpy.data.objects["Empty.001"]
#bpy.ops.object.empty_add(type = 'SPHERE', location = (0, 0.7, 0 ))
#empty2 = bpy.data.objects["Empty.002"]
#bpy.ops.object.empty_add(type = 'SPHERE', location = (-0.7, 0, 0 ))
#empty3 = bpy.data.objects["Empty.003"]
#bpy.ops.object.empty_add(type = 'SPHERE', location = (0, -0.7, 0 ))
#empty4 = bpy.data.objects["Empty.004"]
#empty = [empty0, empty1, empty2, empty3, empty4]
#
## 空のオブジェクトに力場を設定。なぜかforcefield_toggle()を2回呼ぶ必要がある
#for e in empty:
#  bpy.context.scene.objects.active = e
#  bpy.ops.object.forcefield_toggle()
#  bpy.ops.object.forcefield_toggle()
#  e.field.strength = -300
#
#empty0.field.strength = 250
#
## カメラ
#bpy.data.objects["Camera"].location = ( 2, 2, 5 )
#bpy.data.objects["Camera"].rotation_euler = ( math.pi/6, 0, math.pi*3/4 )
#bpy.data.cameras["Camera"].lens = 30
#
## 照明
#bpy.data.objects["Lamp"].location = (0, 1, 10)
#bpy.data.objects["Lamp"].rotation_euler = ( -math.pi/60, -math.pi/120, math.pi/12 )
#bpy.data.lamps["Lamp"].type = 'SUN'
#
## 物理シミュレーション
#bpy.ops.ptcache.bake_all()
#
## 動画作成
#bpy.context.scene.render.resolution_x = 400
#bpy.context.scene.render.resolution_y = 300
#bpy.context.scene.render.resolution_percentage = 100
#bpy.context.scene.render.image_settings.file_format = 'AVI_JPEG'
#bpy.data.scenes["Scene"].render.filepath = "test.avi"
#bpy.context.scene.frame_start = 0
#bpy.context.scene.frame_end = 250
#bpy.ops.render.render(animation=True)
#
## 保存
#savePath = os.path.abspath(os.path.dirname(__file__))
#bpy.path.relpath(savePath)
#bpy.ops.wm.save_as_mainfile(filepath="test.blend", relative_remap=True)
