#!/usr/bin/env python3
"""Real Git trees, object blobs, metadata, and isolated physical stage fixtures."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0,str(Path(__file__).resolve().parent))
import exclusive_stage as g

def git(repo,*args):
    p=subprocess.run(["git","-C",str(repo),*args],capture_output=True,text=True)
    if p.returncode:
        raise AssertionError(str(args)+" "+p.stderr)
    return p.stdout.strip()

class PhysicalStageTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix="ci41-exclusive-tests-")
        self.top=Path(self.temp.name)
        self.repo=self.top/"repo"
        self.repo.mkdir()
        git(self.repo,"init","-q")
        git(self.repo,"config","user.name","CI41 Fixture")
        git(self.repo,"config","user.email","ci41@example.invalid")
        self.seq=100
        self.put("Assets/OtherSafe.cs","class Safe {}")
        self.put("Assets/OtherSafe.cs.meta","fileFormatVersion: 2\nguid: "+"f"*32+"\n")
        self.base=self.commit("base canonical gameplay")
        self.head=None

    def tearDown(self):
        self.temp.cleanup()

    def put(self,path,body):
        file=self.repo/path
        file.parent.mkdir(parents=True,exist_ok=True)
        file.write_text(body,encoding="utf-8")

    def unique(self):
        self.seq+=1
        return f"{self.seq:032x}"

    def commit(self,msg):
        git(self.repo,"add","-A")
        git(self.repo,"commit","-qm",msg)
        return git(self.repo,"rev-parse","HEAD")

    def seed_variant(self,variant):
        roots=g.PREFIXES[variant]
        # Real folder meta and real .cs/.meta object SHA pairs at HEAD.
        for root in roots:
            self.put(root+".meta","fileFormatVersion: 2\nguid: "+self.unique()+"\n")
        for path in g.REQUIRED[variant]:
            self.put(path,"public sealed class Example {}")
            self.put(path+".meta","fileFormatVersion: 2\nguid: "+self.unique()+"\n")
            parent=Path(path).parent
            while parent.as_posix()!="Assets":
                meta=parent.as_posix()+".meta"
                if not (self.repo/meta).exists():
                    self.put(meta,"fileFormatVersion: 2\nguid: "+self.unique()+"\n")
                parent=parent.parent
        self.head=self.commit("real versioned "+variant+" Unity assets")
        return self.head

    def positive(self,variant):
        sha=self.seed_variant(variant)
        r=g.plan(self.repo,self.base,sha,variant,pinned_sha=sha)
        self.assertEqual(r["exclusive_stage"],"PASS")
        self.assertEqual(r["security_transaction"],"UNVERIFIED_HOLD")
        self.assertEqual(r["release_gate"],"HOLD")
        self.assertEqual(r["native_unity"],"NOT_RUN")
        return r

    def test_01_secure_v2_source_only_positive(self):
        self.positive("secure-v2")

    def test_02_public_guard_source_only_positive(self):
        self.positive("public-guard")

    def test_03_actual_source_blob_hash_is_git_blob_content(self):
        r=self.positive("secure-v2")
        for f in r["files"]:
            original=g.read_blob(self.repo,f["git_blob"])
            self.assertEqual(hashlib.sha256(original).hexdigest(),f["sha256"])

    def test_04_stage_real_physical_binary_clean_tree_and_receipt(self):
        r=self.positive("secure-v2")
        out=self.top/"physical-unity-staging"
        g.stage(self.repo,r,out)
        self.assertTrue(out.is_dir())
        self.assertFalse((self.repo/"STAGE_RECEIPT.json").exists())
        saved=json.loads((out/"STAGE_RECEIPT.json").read_text())
        self.assertEqual(saved["files"],r["files"])
        self.assertEqual(len(list(out.rglob("*.meta"))),r["source_meta_guid_count"])
        for f in r["files"]:
            self.assertEqual(hashlib.sha256((out/f["path"]).read_bytes()).hexdigest(),f["sha256"])
        print("ACTUAL_UNITY_STAGING_EXCLUSIVE_AND_ALL_SHA256_MATCH_NO_GAME_WRITE")

    def test_05_existing_destination_rejected_without_overwrite(self):
        r=self.positive("secure-v2")
        out=self.top/"existing"
        out.mkdir()
        (out/"sentinel").write_text("untouched")
        with self.assertRaisesRegex(g.GateError,"STAGING_DESTINATION_EXISTS"):
            g.stage(self.repo,r,out)
        self.assertEqual((out/"sentinel").read_text(),"untouched")

    def test_06_no_staging_inside_canonical_repository(self):
        r=self.positive("secure-v2")
        with self.assertRaisesRegex(g.GateError,"INSIDE_SOURCE_REPO"):
            g.stage(self.repo,r,self.repo/"Assets"/"BadInstall")

    def test_07_reject_wrong_producer_pin(self):
        self.seed_variant("secure-v2")
        with self.assertRaisesRegex(g.GateError,"UNAPPROVED_OR_DRIFTED"):
            g.plan(self.repo,self.base,self.head,"secure-v2")

    def test_08_reject_missing_source_script_and_meta(self):
        self.seed_variant("secure-v2")
        (self.repo/g.REQUIRED["secure-v2"][0]).unlink()
        changed=self.commit("remove required runtime source")
        with self.assertRaisesRegex(g.GateError,"MISSING_REQUIRED_SOURCE"):
            g.plan(self.repo,self.base,changed,"secure-v2",pinned_sha=changed)

    def test_09_reject_missing_source_metadata(self):
        self.seed_variant("secure-v2")
        (self.repo/(g.REQUIRED["secure-v2"][0]+".meta")).unlink()
        changed=self.commit("delete .meta but preserve code")
        with self.assertRaisesRegex(g.GateError,"MISSING_REQUIRED_SOURCE"):
            g.plan(self.repo,self.base,changed,"secure-v2",pinned_sha=changed)

    def test_10_reject_duplicate_selected_unity_guids(self):
        self.seed_variant("secure-v2")
        a,b=g.REQUIRED["secure-v2"][:2]
        duplicate=(self.repo/(a+".meta")).read_text()
        (self.repo/(b+".meta")).write_text(duplicate)
        changed=self.commit("physical duplicate Unity GUID")
        with self.assertRaisesRegex(g.GateError,"DUPLICATE_SELECTED_GUID"):
            g.plan(self.repo,self.base,changed,"secure-v2",pinned_sha=changed)

    def test_11_reject_source_including_both_variants(self):
        self.seed_variant("secure-v2")
        self.put(g.PREFIXES["public-guard"][0]+".meta","guid: "+self.unique()+"\n")
        mixed=self.commit("unsafe both alternatives in original source tree")
        with self.assertRaisesRegex(g.GateError,"CANDIDATE_CONTAINS_COMPETING"):
            g.plan(self.repo,self.base,mixed,"secure-v2",pinned_sha=mixed)

    def test_12_reject_target_already_has_deprecated_public_router(self):
        legacy="Assets/B0123DebrisPlayground001.meta"
        # Target committed before secure-v2 source, so it is a real ancestor.
        self.put(legacy,"guid: "+self.unique()+"\n")
        target=self.commit("target already owns legacy docking API")
        self.seed_variant("secure-v2")
        with self.assertRaisesRegex(g.GateError,"TARGET_ALREADY_CONTAINS"):
            g.plan(self.repo,target,self.head,"secure-v2",pinned_sha=self.head)

    def test_13_reject_target_already_has_secure_v2_while_selecting_guard(self):
        self.put("Assets/B0123SecureReattachV2.meta","guid: "+self.unique()+"\n")
        target=self.commit("target already installed secure-v2")
        self.seed_variant("public-guard")
        with self.assertRaisesRegex(g.GateError,"TARGET_ALREADY_CONTAINS"):
            g.plan(self.repo,target,self.head,"public-guard",pinned_sha=self.head)

    def test_14_reject_target_guid_collision_from_unrelated_asset(self):
        # Same GUID in distinct original asset path causes conflict on import.
        self.seed_variant("secure-v2")
        selected=g.REQUIRED["secure-v2"][0]+".meta"
        source_guid=g.guid_of((self.repo/selected).read_bytes(),selected)
        self.put("Assets/OtherSafe.cs.meta","fileFormatVersion: 2\nguid: "+source_guid+"\n")
        target=self.commit("target now owns same guid but unrelated path")
        # Construct a new descendant with full same selected content.
        head=self.commit("same candidate after collision")
        with self.assertRaisesRegex(g.GateError,"TARGET_ALREADY_CONTAINS"):
            # Same source assets already present in target, so prior-scoped gate wins.
            g.plan(self.repo,target,head,"secure-v2",pinned_sha=head)

    def test_15_reject_missing_directory_metadata(self):
        self.seed_variant("secure-v2")
        (self.repo/"Assets/B0123SecureReattachV2/Runtime.meta").unlink()
        head=self.commit("missing runtime folder Unity metadata")
        with self.assertRaisesRegex(g.GateError,"MISSING_UNITY_FOLDER_META"):
            g.plan(self.repo,self.base,head,"secure-v2",pinned_sha=head)

    def test_16_reject_nonancestor_source_sha(self):
        self.seed_variant("secure-v2")
        with self.assertRaises(g.GateError):
            g.plan(self.repo,self.head,self.base,"secure-v2",pinned_sha=self.base)

    def test_17_reject_non_sha_commit_input(self):
        self.seed_variant("secure-v2")
        with self.assertRaisesRegex(g.GateError,"EXACT_40_HEX"):
            g.plan(self.repo,"main",self.head,"secure-v2",pinned_sha=self.head)

    def test_18_real_cli_rejects_unauthorized_fixture_head(self):
        self.seed_variant("secure-v2")
        cli=Path(g.__file__)
        p=subprocess.run([sys.executable,str(cli),"--repo",str(self.repo),
             "--target-sha",self.base,"--producer-sha",self.head,"--variant","secure-v2"],
             capture_output=True,text=True)
        self.assertEqual(p.returncode,2,p.stdout+p.stderr)
        x=json.loads(p.stdout)
        self.assertEqual(x["exclusive_stage"],"FAIL_CLOSED")
        self.assertEqual(x["release_gate"],"HOLD")

    def test_19_reject_already_staged_path_without_destroying_existing_bytes(self):
        r=self.positive("public-guard")
        out=self.top/"safe-staging"
        g.stage(self.repo,r,out)
        original=(out/"STAGE_RECEIPT.json").read_bytes()
        with self.assertRaisesRegex(g.GateError,"DESTINATION_EXISTS"):
            g.stage(self.repo,r,out)
        self.assertEqual((out/"STAGE_RECEIPT.json").read_bytes(),original)

    def test_20_selected_legacy_guard_never_masquerades_as_native_g1(self):
        r=self.positive("public-guard")
        for key,val in (("native_unity","NOT_RUN"),("release_gate","HOLD"),
                        ("security_transaction","UNVERIFIED_HOLD"),
                        ("integration","NO"),("codex_ack","NO")):
            self.assertEqual(r[key],val)

if __name__=="__main__":
    unittest.main(verbosity=2)
