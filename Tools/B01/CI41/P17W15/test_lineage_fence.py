#!/usr/bin/env python3
"""Real synthetic Git-commit lineage / Unity meta GUID/manifest adversarial tests."""
import copy
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0,str(Path(__file__).resolve().parent))
import lineage_fence as c

ROOT=c.SAVE_ROOT
ASM=ROOT+"/Runtime/B0123.DetachedSaveContract.Runtime.asmdef"
SEL="Tools/B01/B01-23/P17W14DetachedSaveContractR2/contract_selection.json"
OWNED_META={
    ROOT+".meta":"b0135001000000000000000000000001",
    ROOT+"/Runtime.meta":"b0135002000000000000000000000002",
    ROOT+"/Runtime/DetachedSnapshotContract.cs.meta":"b0135003000000000000000000000003",
    ASM+".meta":"b0135004000000000000000000000004",
}
def git(repo,*args):
    run=subprocess.run(["git","-C",str(repo),*args],capture_output=True,text=True)
    if run.returncode:
        raise AssertionError("git "+str(args)+" failed: "+run.stderr)
    return run.stdout.strip()

class RealGitLineageTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix="ci41-p17w15-git-")
        self.repo=Path(self.tmp.name)/"git"
        self.repo.mkdir()
        git(self.repo,"init","-q")
        git(self.repo,"config","user.name","CI41 Synthetic Git Fixture")
        git(self.repo,"config","user.email","ci41@invalid.example")
        git(self.repo,"config","core.autocrlf","false")
        self.put(c.GAME_ROOT+"/Runtime/Game.cs","public class FrozenGame {}\n")
        self.put(c.GAME_ROOT+"/Runtime/Game.cs.meta",
                 "fileFormatVersion: 2\nguid: aa000000000000000000000000000001\n")
        self.put("Assets/B0123DebrisRepulse001/Runtime/OriginalW06.cs",
                 "public class W06Ledger {}\n")
        self.game=self.commit("frozen game")
        for i,p in enumerate(c.SOURCE_RELATIVE):
            if "P17W14DetachedSaveContractR2/" in p or
               p.endswith("b01-23-p17w14-detached-save-r2.yml"):
                continue
            self.put(p,self.body(p))
        self.old=self.commit("historical save v1")
        for p in c.SOURCE_RELATIVE:
            if "P17W14DetachedSaveContractR2/" in p or
               p.endswith("b01-23-p17w14-detached-save-r2.yml"):
                self.put(p,self.body(p))
        self.save=self.commit("fixed save v2")
        self.game_original=self.game
        self.old_original=self.old
        self.save_original=self.save

    def tearDown(self):
        self.tmp.cleanup()

    def body(self,p):
        if p in OWNED_META:
            return "fileFormatVersion: 2\nguid: "+OWNED_META[p]+"\n"
        if p==ASM:
            return json.dumps({"name":"B0123.DetachedSaveContract.Runtime",
                               "references":["B0123.DebrisRepulse.Runtime"],
                               "noEngineReferences":True})+"\n"
        if p==SEL:
            return json.dumps({"package":"B01-23-DETACHED-SAVE-CONTRACT-001",
                              "version":"2.0.0","selected_save_lineage":"#549",
                              "supersedes_sha":self.old,
                              "other_save_contract_544":"ALT_HOLD",
                              "frozen_gameplay_pr_510":self.game,
                              "unity_native":"NOT_RUN",
                              "savegame_host":"NOT_INTEGRATED",
                              "codex_ack":False})+"\n"
        return "original source fixture "+p+"\n"

    def put(self,p,value):
        f=self.repo/p
        f.parent.mkdir(parents=True,exist_ok=True)
        f.write_text(value,encoding="utf-8")

    def commit(self,message):
        git(self.repo,"add","-A")
        git(self.repo,"commit","-qm",message)
        return git(self.repo,"rev-parse","HEAD")

    def audit(self,save=None,base=None,history=None):
        return c.audit(self.repo,base or self.game,history or self.old,save or self.save)

    def bad(self,path,content,reason):
        self.put(path,content)
        head=self.commit("malicious Git mutation")
        with self.assertRaisesRegex(c.Refused,reason):
            self.audit(save=head)

    def test_01_real_commit_descendant_closure_positive(self):
        r=self.audit()
        self.assertEqual(r["status"],"IMMUTABLE_SOURCE_LINEAGE_PASS")
        self.assertEqual(r["added_source_count"],16)
        self.assertEqual(r["new_save_guid_count"],4)
        self.assertFalse(r["codex_ack"])
        self.assertEqual(r["release_gate"],"HOLD")
        print("REAL_GIT_SELECTED_510_556_LINEAGE_PASS_BUT_UNITY_HOST_HOLD")

    def test_02_no_target_mutation_from_source_audit(self):
        before=git(self.repo,"status","--porcelain")
        self.audit()
        self.assertEqual(before,git(self.repo,"status","--porcelain"))

    def test_03_all_commit_sha_are_actual_git_objects(self):
        r=self.audit()
        self.assertEqual(r["selected_game_sha"],self.game)
        self.assertEqual(r["historical_parent_sha"],self.old)
        self.assertEqual(r["selected_save_sha"],self.save)
        self.assertEqual(len(r["source_git_blob_sha"]),16)

    def test_04_requires_full_sha_not_branch(self):
        with self.assertRaisesRegex(c.Refused,"EXACT_COMMIT_SHA_REQUIRED"):
            self.audit(save="HEAD")

    def test_05_wrong_40hex_absent_commit_fails(self):
        with self.assertRaisesRegex(c.Refused,"GIT_COMMAND_FAILED"):
            self.audit(save="a"*40)

    def test_06_reject_changed_original_gameplay_file(self):
        self.bad(c.GAME_ROOT+"/Runtime/Game.cs","MUTATED GAMEPLAY\n","SOURCE_CLOSURE_DRIFT")

    def test_07_reject_removed_original_gameplay_file(self):
        (self.repo/c.GAME_ROOT+"/Runtime/Game.cs").unlink()
        head=self.commit("delete original")
        with self.assertRaisesRegex(c.Refused,"SOURCE_CLOSURE_DRIFT"):
            self.audit(save=head)

    def test_08_reject_alternative_544_tools(self):
        self.bad("Tools/B01/B01-23/P17W13SecureV2SaveContract001/StableIdSnapshotCodec.cs",
                 "ALT SAVE\n","SOURCE_CLOSURE_DRIFT")

    def test_09_reject_alternative_494_gameplay(self):
        self.bad("Assets/B0123DockHardening001/Runtime/Legacy.cs",
                 "LEGACY\n","SOURCE_CLOSURE_DRIFT")

    def test_10_reject_unknown_cross_owner_source(self):
        self.bad("Assets/CodexReserved/Main.cs","UNAUTHORIZED\n","SOURCE_CLOSURE_DRIFT")

    def test_11_reject_missing_expected_selected_manifest(self):
        (self.repo/SEL).unlink()
        head=self.commit("delete selection")
        with self.assertRaisesRegex(c.Refused,"SOURCE_CLOSURE_DRIFT"):
            self.audit(save=head)

    def test_12_reject_foreign_preexisting_selected_save_path(self):
        git(self.repo,"reset","--hard",self.game)
        self.put(ROOT+"/Runtime/AlreadyHere.cs","OLD TARGET\n")
        base=self.commit("preexisting save root")
        for p in c.SOURCE_RELATIVE:
            self.put(p,self.body(p) if p!=SEL else json.dumps({
             "package":"B01-23-DETACHED-SAVE-CONTRACT-001","version":"2.0.0",
             "selected_save_lineage":"#549","supersedes_sha":"0"*40,
             "other_save_contract_544":"ALT_HOLD","frozen_gameplay_pr_510":base,
             "unity_native":"NOT_RUN","savegame_host":"NOT_INTEGRATED",
             "codex_ack":False}))
        history=self.commit("history")
        newest=self.commit("v2")
        with self.assertRaisesRegex(c.Refused,"SOURCE_CLOSURE_DRIFT"):
            self.audit(base=base,history=history,save=newest)

    def test_13_reject_missing_unity_asset_meta(self):
        (self.repo/(ROOT+"/Runtime/DetachedSnapshotContract.cs.meta")).unlink()
        head=self.commit("drop meta")
        with self.assertRaisesRegex(c.Refused,"SOURCE_CLOSURE_DRIFT"):
            self.audit(save=head)

    def test_14_reject_duplicate_unity_guid_between_game_and_save(self):
        self.bad(ROOT+"/Runtime/DetachedSnapshotContract.cs.meta",
                 "guid: aa000000000000000000000000000001\n","GUID_COLLISION")

    def test_15_reject_duplicate_unity_guid_inside_save(self):
        self.bad(ROOT+"/Runtime/DetachedSnapshotContract.cs.meta",
                 "guid: "+OWNED_META[ROOT+".meta"]+"\n","GUID_COLLISION")

    def test_16_reject_missing_guid_inside_meta(self):
        self.bad(ROOT+"/Runtime/DetachedSnapshotContract.cs.meta",
                 "fileFormatVersion: 2\n","MISSING_OR_DUPLICATE_UNITY_META_GUID")

    def test_17_reject_two_guids_in_one_meta(self):
        self.bad(ROOT+"/Runtime/DetachedSnapshotContract.cs.meta",
                 "guid: "+OWNED_META[ROOT+".meta"]+"\nguid: "
                 +OWNED_META[ROOT+"/Runtime.meta"]+"\n",
                 "MISSING_OR_DUPLICATE_UNITY_META_GUID")

    def test_18_reject_unapproved_asmdef_dependency(self):
        self.bad(ASM,json.dumps({"name":"B0123.DetachedSaveContract.Runtime",
             "references":["B0123.Unapproved.Runtime"],"noEngineReferences":True}),
             "UNEXPECTED_SAVE_ASMDEF_DEPENDENCY")

    def test_19_reject_missing_noEngineReferences(self):
        self.bad(ASM,json.dumps({"name":"B0123.DetachedSaveContract.Runtime",
             "references":["B0123.DebrisRepulse.Runtime"],"noEngineReferences":False}),
             "UNEXPECTED_SAVE_ASMDEF_DEPENDENCY")

    def selection_tamper(self,changes,reason):
        s=json.loads((self.repo/SEL).read_text())
        s.update(changes)
        self.bad(SEL,json.dumps(s),reason)

    def test_20_reject_fake_codex_ack(self):
        self.selection_tamper({"codex_ack":True},"CONTRACT_SELECTION_IDENTITY")

    def test_21_reject_fake_native_unity_pass(self):
        self.selection_tamper({"unity_native":"PASS"},"CONTRACT_SELECTION_IDENTITY")

    def test_22_reject_fake_live_host_save(self):
        self.selection_tamper({"savegame_host":"INTEGRATED"},"CONTRACT_SELECTION_IDENTITY")

    def test_23_reject_selected_544_lineage(self):
        self.selection_tamper({"selected_save_lineage":"#544"},"CONTRACT_SELECTION_IDENTITY")

    def test_24_reject_wrong_selected_original_gameplay_sha(self):
        self.selection_tamper({"frozen_gameplay_pr_510":"f"*40},"CONTRACT_SELECTION_IDENTITY")

if __name__=="__main__":
    unittest.main(verbosity=2)
