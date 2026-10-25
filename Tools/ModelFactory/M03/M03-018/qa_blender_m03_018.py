#!/usr/bin/env python3
"""Run with blender -b --python qa_blender_m03_018.py; independent repeat/sweep QA."""
import bpy
import importlib.util
import math
import os
from mathutils import Vector

HERE=os.path.dirname(os.path.abspath(__file__))
spec=importlib.util.spec_from_file_location("m03_018_generator",
    os.path.join(HERE,"generate_m03_018.py"))
gen=importlib.util.module_from_spec(spec)
spec.loader.exec_module(gen)

# Foreign object and material must survive both generator invocations.
sentinel=bpy.data.objects.new("M03_018_QA_FOREIGN_SENTINEL",None)
bpy.context.scene.collection.objects.link(sentinel)
counts=[]
for run in (1,2):
    col,nmesh=gen.build_model()
    counts.append((len(col.objects),nmesh,
                   len([m for m in bpy.data.materials if m.name.startswith("M03_018_")])))
    assert sentinel.name in bpy.data.objects
assert counts[0]==counts[1], "repeat changed object/mesh/material counts: "+str(counts)
assert len([o for o in bpy.data.objects if o.name.startswith("M03_018_")
            and o.name!="M03_018_QA_FOREIGN_SENTINEL"])==counts[1][0]

pitch_ring=next(o for o in col.objects if o.name.endswith("Inner_Pitch_Ring"))
yaw=next(o for o in col.objects if o.name.endswith("YAW_AXIS_Z"))
pitch=next(o for o in col.objects if o.name.endswith("PITCH_AXIS_X"))
max_radius=0.0
min_height=1e9
for frame in range(1,122):
    bpy.context.scene.frame_set(frame)
    yaw_inv=yaw.matrix_world.inverted()
    for v in pitch_ring.data.vertices:
        world=pitch_ring.matrix_world @ v.co
        local=yaw_inv @ world
        max_radius=max(max_radius,math.hypot(local.x,local.y))
        min_height=min(min_height,world.z)
    # Animated axes must remain bounded at intermediate interpolated frames.
    assert abs(math.degrees(yaw.rotation_euler.z))<46
    assert abs(math.degrees(pitch.rotation_euler.x))<26
# Ring radial separation to outer annulus inner surface.
assert .55-.047-max_radius>.18, max_radius
# Ring lower tip remains above static bearing and rotating hub top.
assert min_height>.99+.04, min_height
assert len([o for o in col.objects if "SNAP_BASE_" in o.name])==4
assert any("SNAP_PAYLOAD_TOP" in o.name for o in col.objects)
bpy.context.scene.frame_set(1)
bpy.data.objects.remove(sentinel,do_unlink=True)
print("M03-018 BLENDER QA PASS repeat=%s samples=121 ring_max_radius=%.5f min_height=%.5f" %
      (counts,max_radius,min_height))
