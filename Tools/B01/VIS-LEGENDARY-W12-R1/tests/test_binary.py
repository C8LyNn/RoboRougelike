"""Independent stdlib source + committed M01 GLB integrity tests."""
import hashlib,importlib.util,json,struct,unittest
from pathlib import Path
BASE=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location("rework",BASE/"tools/rework_legendary_glb.py")
mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
M01=(BASE/"binaries/M01-018_LEG-WPN-001_R1.glb").read_bytes()
class ActualBinaryTests(unittest.TestCase):
 def test_m01_actual_sha(self):
  self.assertEqual(hashlib.sha256(M01).hexdigest(),"795124613e26cf2f4d7d3d59990542d0d2f4b3a65fef911132318748bae589dd")
 def test_m01_actual_structure(self):
  d,_=mod.parse_glb(M01)
  self.assertEqual((len(d["meshes"]),len(d["materials"]),len(d["animations"])),(53,7,1))
  self.assertEqual(sum(len(a["channels"]) for a in d["animations"]),1)
  self.assertEqual(sum(n.get("name","").startswith("VIS_LEG_") for n in d["nodes"]),3)
 def test_m01_mount_is_not_authority(self):
  d,_=mod.parse_glb(M01)
  anchor=next(n for n in d["nodes"] if n.get("name")=="VIS_LEG_LEG-WPN-001_SOCKET_WEAPONBASE")
  self.assertFalse(anchor["extras"]["isAuthoritativeSocket"])
  self.assertFalse(d["asset"]["extras"]["b01VisualRetrofit"]["footprintFits"])
  self.assertIsNone(d["asset"]["extras"]["b01VisualRetrofit"]["unapprovedMassKg"])
 def test_m01_material_binding(self):
  d,_=mod.parse_glb(M01)
  self.assertTrue(all(m["extras"]["urpBindingPolicy"]=="EXPLICIT_MATERIAL_ASSET_REFERENCE_REQUIRED" for m in d["materials"]))
 def test_reject_truncated_glb(self):
  with self.assertRaises(ValueError):mod.parse_glb(M01[:12])
 def test_reject_modified_header(self):
  with self.assertRaises(ValueError):mod.parse_glb(M01+b"x")
 def test_reject_double_rework(self):
  with self.assertRaisesRegex(ValueError,"already retrofitted"):mod.retrofit(M01,"M01-018")
 def test_reject_unknown_model(self):
  with self.assertRaises(ValueError):mod.retrofit(M01,"M02-001")
 def test_fake_m04_root(self):
  d,ch=mod.parse_glb(M01)
  d= json.loads(json.dumps(d))
  d["nodes"][next(i for i,n in enumerate(d["nodes"]) if n["name"]=="MOUNT_TOP_SOCKET")]["name"]="ROOT_UNDERSIDE_MOUNT_Z0"
  d["nodes"]=[{**n,"name":n["name"].replace("VIS_LEG_","NEUTRAL_TEST_")} if n.get("name","").startswith("VIS_LEG_") else n for n in d["nodes"]]
  raw=mod.write_glb(d,ch)
  out,e=mod.retrofit(raw,"M04-024")
  gltf,chunks=mod.parse_glb(out)
  self.assertEqual(e["animations"],1)
  self.assertFalse(gltf["asset"]["extras"]["b01VisualRetrofit"]["socketAuthority"])
 def test_m04_actual_if_supplied(self):
  f=BASE/"incoming/M04-024.glb"
  if not f.exists():self.skipTest("M04 original is CI artifact; no implicit download")
  raw=f.read_bytes()
  self.assertEqual(hashlib.sha256(raw).hexdigest(),"280bb77e21995b7440fdda5c7551fc135b2b3b5ce6999322708fb670ff979414")
  modified,r=mod.retrofit(raw,"M04-024")
  self.assertEqual(hashlib.sha256(modified).hexdigest(),"ab26d649e31a5ea4d21320480c52b3b9781bd7f3752a687f265c24bbf473df4b")
  d,ch=mod.parse_glb(raw);new,nch=mod.parse_glb(modified)
  self.assertEqual(ch[1:],nch[1:])
  self.assertEqual((len(new["meshes"]),len(new["materials"]),sum(len(a["channels"]) for a in new["animations"])),(105,8,4))
if __name__=="__main__":unittest.main(verbosity=2)
