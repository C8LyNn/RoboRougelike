#!/usr/bin/env python3
"""Physical ZIP and manifest mutation tests + fake full transport parity."""
import copy
import hashlib
import io
import json
import pathlib
import unittest
from unittest import mock
import zipfile
import save_artifact_parity as p

def archive(files,mode=None):
    stream=io.BytesIO()
    with zipfile.ZipFile(stream,"w") as out:
        for name,data in files:
            z=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
            z.compress_type=zipfile.ZIP_STORED
            z.create_system=3
            z.external_attr=((0o100644 if mode is None else mode)<<16)
            out.writestr(z,data)
    return stream.getvalue()

def source(variant,override=None):
    s=p.SPECS[variant]
    f="Tools/B01/B01-23/Save.cs"
    b=b"original CSharp source bytes"
    manifest={s["manifest_source_key"]:s["sha"],s["manifest_game_key"]:p.SELECTED_GAME,
              "codex_ack":False,"native_unity":"NOT_RUN",
              s["manifest_members_key"]:[{"path":f,"sha256":p.sha(b),
                    "bytes" if variant=="544" else "size":len(b)}]}
    if variant=="544":manifest["canonical_save_restored"]=False
    else:manifest["savegame_host"]="NOT_INTEGRATED"
    if override:override(manifest)
    return archive([("PACKAGE_MANIFEST.json",json.dumps(manifest).encode()),(f,b)])

class OriginalPhysicalPackageControls(unittest.TestCase):
    def setUp(self):
        self.old=copy.deepcopy(p.SPECS)
        self.inner={v:source(v) for v in ("544","549")}
        for v,raw in self.inner.items():
            p.SPECS[v].update({"inner_bytes":len(raw),"inner_sha256":p.sha(raw),
                                "members":2})
        self.outer={v:archive([("w13/"+v+".zip",raw)]) for v,raw in self.inner.items()}
        self.red=archive([("b01-61-w13-contract-execution.log",
                           b"source PASS but "+p.QA_550["marker"]+b"\n")])

    def tearDown(self):
        p.SPECS.clear()
        p.SPECS.update(self.old)

    def test_01_physical_544_inner_manifest_valid(self):
        v=p.inspect_outer("544",self.outer["544"])
        self.assertEqual(v["verified_source_members"],1)
        self.assertEqual(v["manifest_native_unity"],"NOT_RUN")

    def test_02_physical_549_inner_manifest_valid(self):
        v=p.inspect_outer("549",self.outer["549"])
        self.assertEqual(v["verified_source_members"],1)
        self.assertEqual(v["manifest_native_unity"],"NOT_RUN")

    def test_03_independent_550_red_marker_detected(self):
        self.assertIn("SECURITY_RED",p.inspect_qa_550(self.red))

    def test_04_544_and_549_not_implicitly_selected(self):
        with mock.patch.object(p,"original_artifact",side_effect=self.supply):
            receipt=p.verify_sources("")
        self.assertEqual(receipt["selected_save_contract"],"NONE_DECIDED_BY_CI41")
        self.assertEqual(receipt["save_load_host"],"HOLD")
        self.assertEqual(receipt["native_unity_g1"],"NOT_RUN")
        self.assertEqual(receipt["proposals"]["549"]["security_gate"],
                         "SECURITY_RED_FROM_INDEPENDENT_550")
        self.assertEqual(receipt["proposals"]["544"]["security_gate"],
                         "INDEPENDENT_SECURITY_QA_PENDING")
        print("PHYSICAL_SNAPSHOT_ZIP_PARITY_WITH_549_SECURITY_RED_RELEASE_HOLD")

    def supply(self,run_id,head,artifact,token):
        if artifact==p.QA_550["artifact"]:return self.red
        for variant,spec in p.SPECS.items():
            if artifact in spec["artifacts"].values():
                self.assertEqual(head,spec["sha"])
                self.assertEqual(run_id,spec["run"])
                return self.outer[variant]
        raise AssertionError("unexpected pinned artifact")

    def test_05_cross_os_bit_mismatch_refused(self):
        real=self.supply
        def wrong(run_id,head,artifact,token):
            if artifact==p.SPECS["549"]["artifacts"]["Linux"]:
                return self.outer["544"]
            return real(run_id,head,artifact,token)
        with mock.patch.object(p,"original_artifact",side_effect=wrong):
            with self.assertRaisesRegex(p.Refuse,"INNER_SIZE_MISMATCH|PINNED_INNER"):
                p.verify_sources("")

    def test_06_missing_red_witness_blocks_receipt(self):
        safe=archive([("log.txt",b"ALL GREEN")])
        original=self.supply
        def no_red(run_id,head,artifact,token):
            return safe if artifact==p.QA_550["artifact"] else original(run_id,head,artifact,token)
        with mock.patch.object(p,"original_artifact",side_effect=no_red):
            with self.assertRaisesRegex(p.Refuse,"MISSING_INDEPENDENT_550"):
                p.verify_sources("")

    def test_07_outer_double_zip_refused(self):
        z=archive([("a.zip",self.inner["544"]),("b.zip",self.inner["544"])])
        with self.assertRaisesRegex(p.Refuse,"EXACTLY_ONE"):
            p.inspect_outer("544",z)

    def test_08_outer_no_zip_refused(self):
        with self.assertRaisesRegex(p.Refuse,"EXACTLY_ONE"):
            p.inspect_outer("544",archive([("x.txt",b"a")]))

    def test_09_inner_original_sha_mismatch(self):
        with self.assertRaisesRegex(p.Refuse,"INNER_SIZE|PINNED_INNER_SHA"):
            p.inspect_inner("544",self.inner["544"]+b"!")

    def test_10_inner_original_bit_flip_same_size(self):
        v=bytearray(self.inner["549"])
        v[30]^=1
        with self.assertRaisesRegex(p.Refuse,"PINNED_INNER_SHA"):
            p.inspect_inner("549",bytes(v))

    def test_11_wrong_selected_original_game_pin(self):
        changed=source("544",lambda m:m.update(only_game_candidate_sha="f"*40))
        self.set_target("544",changed)
        with self.assertRaisesRegex(p.Refuse,"SELECTED_GAME_SHA"):
            p.inspect_inner("544",changed)

    def test_12_wrong_549_schema_owner_sha(self):
        changed=source("549",lambda m:m.update(own_head_sha="f"*40))
        self.set_target("549",changed)
        with self.assertRaisesRegex(p.Refuse,"MANIFEST_PRODUCER_SHA"):
            p.inspect_inner("549",changed)

    def set_target(self,v,raw):
        p.SPECS[v].update(inner_sha256=p.sha(raw),inner_bytes=len(raw))

    def test_13_false_codex_ack_refused(self):
        changed=source("549",lambda m:m.update(codex_ack=True))
        self.set_target("549",changed)
        with self.assertRaisesRegex(p.Refuse,"FORGED_CODEX_ACK"):
            p.inspect_inner("549",changed)

    def test_14_false_native_g1_refused(self):
        changed=source("544",lambda m:m.update(native_unity="PASS"))
        self.set_target("544",changed)
        with self.assertRaisesRegex(p.Refuse,"FORGED_UNITY_G1"):
            p.inspect_inner("544",changed)

    def test_15_false_live_restore_refused(self):
        changed=source("544",lambda m:m.update(canonical_save_restored=True))
        self.set_target("544",changed)
        with self.assertRaisesRegex(p.Refuse,"FORGED_GAME_SAVE_RESTORED"):
            p.inspect_inner("544",changed)

    def test_16_false_549_host_integrated_refused(self):
        changed=source("549",lambda m:m.update(savegame_host="INTEGRATED"))
        self.set_target("549",changed)
        with self.assertRaisesRegex(p.Refuse,"FORGED_HOST_INTEGRATION"):
            p.inspect_inner("549",changed)

    def test_17_archive_member_missing_refused(self):
        raw=archive([("PACKAGE_MANIFEST.json",b'{}')])
        self.set_target("544",raw)
        with self.assertRaisesRegex(p.Refuse,"UNEXPECTED_INNER_MEMBERS"):
            p.inspect_inner("544",raw)

    def test_18_archive_sha256_manifest_corruption(self):
        raw=source("544",lambda m:m["members"][0].update(sha256="0"*64))
        self.set_target("544",raw)
        with self.assertRaisesRegex(p.Refuse,"MANIFEST_FILE_SHA256"):
            p.inspect_inner("544",raw)

    def test_19_archive_length_manifest_corruption(self):
        raw=source("549",lambda m:m["files"][0].update(size=999))
        self.set_target("549",raw)
        with self.assertRaisesRegex(p.Refuse,"MANIFEST_FILE_BYTES"):
            p.inspect_inner("549",raw)

    def test_20_invalid_json_inside_manifest(self):
        raw=archive([("PACKAGE_MANIFEST.json",b"{no"),("source.cs",b"x")])
        self.set_target("544",raw)
        with self.assertRaisesRegex(p.Refuse,"INVALID_INNER_MANIFEST"):
            p.inspect_inner("544",raw)

    def test_21_zip_traversal_refused(self):
        with self.assertRaisesRegex(p.Refuse,"UNSAFE_ZIP_MEMBER"):
            p.zip_files(archive([("../steal.txt",b"x")]))

    def test_22_zip_absolute_refused(self):
        with self.assertRaisesRegex(p.Refuse,"UNSAFE_ZIP_MEMBER"):
            p.zip_files(archive([("/etc/shadow",b"x")]))

    def test_23_zip_windows_backslash_refused(self):
        # zipfile.ZipInfo normalizes backslash on Windows before writing.
        # Mutate BOTH actual ZIP header names byte-for-byte, not ZipInfo input.
        raw=archive([("a/b",b"x")])
        self.assertEqual(raw.count(b"a/b"),2)
        raw=raw.replace(b"a/b",b"a\\b")
        with self.assertRaisesRegex(p.Refuse,"UNSAFE_ZIP_MEMBER"):
            p.zip_files(raw)

    def test_24_zip_symlink_refused(self):
        with self.assertRaisesRegex(p.Refuse,"ZIP_SYMLINK"):
            p.zip_files(archive([("symlink",b"target")],mode=0o120777))

    def test_25_zip_duplicate_entry_refused(self):
        with self.assertRaisesRegex(p.Refuse,"DUPLICATE_ZIP_MEMBER"):
            p.zip_files(archive([("a.txt",b"1"),("a.txt",b"2")]))

    def test_26_zip_empty_unsafe_refused(self):
        with self.assertRaisesRegex(p.Refuse,"BAD_ZIP_MEMBER_COUNT"):
            p.zip_files(archive([]))

    def test_27_unapproved_api_refused_without_network(self):
        with self.assertRaisesRegex(p.Refuse,"UNAPPROVED_API_ORIGIN"):
            p.from_api("https://evil.invalid/example","token")

    def test_28_oversized_zip_refused(self):
        with self.assertRaisesRegex(p.Refuse,"OVERSIZED_OR_MISSING_ZIP"):
            p.zip_files(b"a"*(p.MAX_DOWNLOAD+1))

    def test_29_missing_independent_red_marker_refused(self):
        with self.assertRaisesRegex(p.Refuse,"MISSING_INDEPENDENT"):
            p.inspect_qa_550(archive([("a.txt",b"SECURITY GREEN")]))

    def test_30_unknown_variant_refused(self):
        with self.assertRaisesRegex(p.Refuse,"UNKNOWN_VARIANT"):
            p.inspect_inner("550",self.inner["549"])

if __name__=="__main__":
    unittest.main(verbosity=2)
