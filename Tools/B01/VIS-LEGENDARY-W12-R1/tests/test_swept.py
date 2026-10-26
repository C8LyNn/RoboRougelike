"""Positive/negative tests for actual glTF animation pose/math and source QA."""
import importlib.util
import json
import math
import struct
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location("swept",ROOT/"tools/swept_geometry_qa.py")
qa=importlib.util.module_from_spec(spec);spec.loader.exec_module(qa)
class SweptSampleTests(unittest.TestCase):
 def test_identity_transform(self):
  self.assertEqual(qa.local_matrix({},{}),qa.IDENTITY)
 def test_translation_and_scale(self):
  m=qa.local_matrix({"translation":[1,2,3],"scale":[2,3,4]}, {})
  self.assertEqual([m[0],m[5],m[10],m[3],m[7],m[11]],[2,3,4,1,2,3])
 def test_nested_translation(self):
  a=qa.local_matrix({"translation":[1,2,3]},{})
  b=qa.local_matrix({"translation":[4,5,6]},{})
  c=qa.mul(a,b)
  self.assertEqual([c[3],c[7],c[11]],[5,7,9])
 def test_rotation_90_about_y(self):
  q=[0,math.sin(math.pi/4),0,math.cos(math.pi/4)]
  m=qa.local_matrix({"rotation":q},{})
  x,z=m[0]*1+m[2]*0,m[8]*1+m[10]*0
  self.assertAlmostEqual(x,0,places=5)
  self.assertAlmostEqual(z,-1,places=5)
 def test_quaternion_slerp_midpoint(self):
  first=(0,0,0,1)
  last=(0,math.sin(math.pi/4),0,math.cos(math.pi/4))
  mid=qa.slerp(first,last,.5)
  self.assertAlmostEqual(mid[1],math.sin(math.pi/8),places=5)
 def test_interpolated_translation(self):
  track=([0,2],[(0,0,0),(2,4,6)],"LINEAR")
  self.assertEqual(qa.pose_value(track,1,"translation"),(1,2,3))
 def test_refuse_unsupported_interpolation(self):
  with self.assertRaisesRegex(ValueError,"LINEAR"):qa.pose_value(([0,1],[(0,),(1,)],"CUBICSPLINE"),.5,"translation")
 def test_accessor_bounds_reject(self):
  d={"accessors":[{"bufferView":0,"componentType":5126,"type":"VEC3","count":2}],
     "bufferViews":[{"byteOffset":0}]}
  with self.assertRaisesRegex(ValueError,"out-of-bounds"):qa.accessor(d,bytes(8),0)
 def test_source_real_m01_sweep_proof(self):
  f=ROOT/"binaries/M01-018_LEG-WPN-001_R1.glb"
  data=qa.inspect(f,9)
  a=data["animations"]
  self.assertEqual(len(a),1)
  self.assertEqual(a[0]["channels"],1)
  self.assertAlmostEqual(a[0]["sampledSweptSize"][2],3.672,places=3)
 def test_generated_m04_four_axis_sweep_if_present(self):
  f=ROOT/"output/M04-024_LEG-MOV-001_R1.glb"
  if not f.exists():self.skipTest("M04 binary generated only in pinned Actions job")
  data=qa.inspect(f,9)
  self.assertEqual(data["source_sha256"],"c40ce2c090b232833340e3135d75184d01eb7f81c072ab07c80d46508f2af0d8")
  self.assertEqual(len(data["animations"]),5)
  last=data["animations"][-1]
  self.assertEqual(last["clip"],"LEG-MOV-001_ALL_FOUR_GIMBALS")
  self.assertEqual(last["channels"],4)
  self.assertGreater(last["sampledSweptSize"][0],1.5)
  self.assertGreater(last["sampledSweptSize"][2],1.5)
if __name__=="__main__":unittest.main()
