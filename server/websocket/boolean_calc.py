# -*- coding: utf-8 -*-
import os
import bpy
import math
bpy.context.scene.unit_settings.system='METRIC'

# TARGETのオブジェクトを設定する
bpy.ops.mesh.primitive_uv_sphere_add(location=(0,0,1))
sphere = bpy.data.objects["Sphere"]
#sphere.scale = (1,1,1)


#
# ここからがメインの処理
# デフォルトで存在する立方体から球をブーリアンで引く
# ================================================
#
cube = bpy.data.objects["Cube"]
cube.scale = (1,1,1)
cube.location = (-10,-10,-10)

bpy.context.scene.objects.active = cube
bpy.ops.object.modifier_add( type = 'BOOLEAN' )
cube.modifiers["Boolean"].operation = 'DIFFERENCE'
cube.modifiers["Boolean"].object = sphere
bpy.ops.object.modifier_apply( modifier = "Boolean" )

# 球を転がす用のサイズに変更
sphere.location = ( 0.9, 0, 1 )
sphere.scale = ( 0.1, 0.1, 0.1 )















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
