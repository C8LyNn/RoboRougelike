#!/usr/bin/env python3
"""Independent actual original B01-32 PNG+Unity .meta receiver; synthetic test mutations labelled."""
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
import zipfile
from PIL import Image
import sprite_import_bridge as b

class RealSourceUnitySpriteImport(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.artifact=Path(os.environ["B0140_P17_ARTIFACT"])
        cls.contents=cls.artifact.read_bytes()
        with zipfile.ZipFile(io.BytesIO(cls.contents)) as z:
            cls.files={n:z.read(n) for n in z.namelist()}

    def mutate(self,rel,contents):
        files=dict(self.files)
        files[rel]=contents
        return b.package_zip(files)

    def test_01_actual_real_source_pngs_and_unity_metas_pass(self):
        r=b.validate_zip(self.contents)
        self.assertEqual(r["sprites"],12)
        self.assertEqual(r["unity_guid_count"],15)
        self.assertEqual(r["native_unity"],"NOT_RUN")

    def test_02_physical_asset_scope_exactly_12_pairs(self):
        src=[n for n in self.files if n.startswith(b.FRAMES) and n.endswith(".png")]
        self.assertEqual(len(src),12)
        self.assertTrue(all(n+".meta" in self.files for n in src))
        self.assertEqual(len(self.files),30)
        self.assertFalse(any(n.startswith("Assets/R3") or n.startswith("Assets/R6") for n in self.files))

    def test_03_png_is_decodable_rgba_transparent_at_native_resolution(self):
        for n in self.files:
            if n.endswith(".png") and n.startswith(b.FRAMES):
                img=Image.open(io.BytesIO(self.files[n]))
                self.assertEqual(img.size,(256,256))
                self.assertEqual(img.mode,"RGBA")
                if "-clean-" in n:self.assertIsNone(img.getchannel("A").getbbox())
                else:self.assertIsNotNone(img.getchannel("A").getbbox())

    def test_04_original_stage_mapping_no_invented_hp_thresholds(self):
        d=json.loads(self.files["Reference/codex-sprite-binding.json"])
        self.assertEqual(d["texture_stage_thresholds"],"CODEX_POLICY_NOT_ASSUMED")
        self.assertEqual([x["state"] for x in d["sprites"]],list(b.STATES[0:1])*3+
            list(b.STATES[1:2])*3+list(b.STATES[2:3])*3+list(b.STATES[3:4])*3)
        self.assertTrue(all(x["duration_ms"]==160 for x in d["sprites"]))
        self.assertTrue(all(x["atlas_rect_unity_bottom_left"][1]==(3-i//3)*256 for i,x in enumerate(d["sprites"])))

    def test_05_original_artifact_is_byte_deterministic(self):
        self.assertEqual(self.contents,b.package_zip(self.files))
        self.assertEqual(self.contents,b.package_zip(self.files))

    def test_06_modified_meta_guid_is_rejected(self):
        path=next(n for n in self.files if n.endswith(".png.meta"))
        original=self.files[path]
        broken=original.replace(b"guid: ",b"guid: 11111111",1)
        with self.assertRaisesRegex(b.Reject,"NONDETERMINISTIC_SPRITE_META_GUID"):
            b.validate_zip(self.mutate(path,broken))

    def test_07_wrong_sprite_import_mode_is_rejected(self):
        path=next(n for n in self.files if n.endswith(".png.meta"))
        with self.assertRaisesRegex(b.Reject,"WRONG_SPRITE_IMPORT_SETTINGS"):
            b.validate_zip(self.mutate(path,self.files[path].replace(b"spriteMode: 1",b"spriteMode: 0")))

    def test_08_removed_mipmap_protection_is_rejected(self):
        path=next(n for n in self.files if n.endswith(".png.meta"))
        with self.assertRaisesRegex(b.Reject,"WRONG_SPRITE_IMPORT_SETTINGS"):
            b.validate_zip(self.mutate(path,self.files[path].replace(b"enableMipMap: 0",b"enableMipMap: 1")))

    def test_09_corrupt_frame_bytes_rejected(self):
        path=next(n for n in self.files if n.endswith(".png") and n.startswith(b.FRAMES))
        with self.assertRaises(Exception):
            b.validate_zip(self.mutate(path,b"NOT-A-PNG"))

    def test_10_wrong_atlas_row_rejected(self):
        path="Reference/codex-sprite-binding.json"
        d=json.loads(self.files[path]);d["sprites"][1]["atlas_rect_top_left"]=[1,1,256,256]
        with self.assertRaisesRegex(b.Reject,"SOURCE_ATLAS_SPRITE_PIXEL_MISMATCH"):
            b.validate_zip(self.mutate(path,b.canon(d)))

    def test_11_unknown_original_guid_collision_is_rejected(self):
        path=next(n for n in self.files if n.endswith(".png.meta"))
        id=b.stage_guid(path[:-5])
        with self.assertRaisesRegex(b.Reject,"CODEX_EXISTING_UNITY_GUID_COLLISION"):
            b.validate_zip(self.contents,{id})

    def test_12_empty_unity_stage_no_false_ack(self):
        d=json.loads(self.files["Reference/codex-sprite-binding.json"])
        d["native_unity"]="PASS"
        with self.assertRaisesRegex(b.Reject,"FALSE_UNITY_INTEGRATION_CLAIM"):
            b.validate_zip(self.mutate("Reference/codex-sprite-binding.json",b.canon(d)))

    def test_13_escape_archive_member_is_rejected(self):
        with self.assertRaisesRegex(b.Reject,"ZIP_TRAVERSAL_MEMBER"):
            b.package_zip({"../outside.bin":b"malicious"})

    def test_14_missing_second_frame_is_rejected(self):
        f=dict(self.files)
        f.pop(b.FRAMES+"/B0140-abraded-1.png") if b.FRAMES+"/B0140-abraded-1.png" in f else f.pop(b.FRAMES+"/B0140_abraded-1.png")
        with self.assertRaisesRegex(b.Reject,"ZIP_MEMBER_SET_DRIFT"):
            b.validate_zip(b.package_zip(f))

    def test_15_utf8_manifest_rejects_unknown_type(self):
        self.assertNotEqual(b.stage_guid("one"),b.stage_guid("two"))
        self.assertEqual(len(b.stage_guid("one")),32)
        self.assertEqual(len(set(b.stage_guid(n) for n in self.files if n.endswith(".meta"))),15)

    def test_16_consistent_archive_receipt(self):
        info=json.loads((self.artifact.parent/"B0140-original-source-receipt.json").read_bytes())
        self.assertEqual(info["zip_sha256"],b.artifact_sha(self.contents))
        self.assertEqual(len(info["file_sha256"]),30)
        for name,oid in info["file_sha256"].items():
            self.assertEqual(oid,b.artifact_sha(self.files[name]))
        self.assertEqual(info["G1"],"NOT_RUN")
        self.assertEqual(info["G2"],"HOLD")

if __name__=="__main__":
    print("B0140_ORIGINAL_RGBA_SPRITE_IMPORT_AND_GUIDS; NATIVE_UNITY_NOT_RUN",flush=True)
    unittest.main(verbosity=2)
