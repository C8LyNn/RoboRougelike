#!/usr/bin/env python3
"""Independent-of-Unity source/ZIP/rollback checks, NOT native Unity QA."""
import argparse
import hashlib
import json
import pathlib
import re
import subprocess
import tempfile
import unittest
import zipfile

ROOT=pathlib.Path(__file__).resolve().parents[4]
A=pathlib.Path("Assets/B0125SweepOrbR2")
T=pathlib.Path("Tools/B01/B01-25/P17W11SweepOrbUnityR2")
R1="fc09d2d36b1d26d163347c94b978ca7730cf8891"
CORE="Research/B01-25/P17/SWEEPORB001/SweepCore.cs"
CORE_BLOB="ca98d0aa967d9e6267a5901304155a7a684620d7"

def git(*args):
    return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def read(path):
    return (ROOT/path).read_bytes()
def source(path):
    return read(path).decode()
def blob(raw):
    return hashlib.sha1(("blob %d\0"%len(raw)).encode()+raw).hexdigest()
def asset_names():
    return sorted([p.relative_to(ROOT).as_posix() for p in (ROOT/A).rglob("*") if p.is_file()]+[A.as_posix()+".meta"])

def check_core(raw):
    assert raw==git("show",R1+":"+CORE),"Frozen original R1 bytes differ"
    assert blob(raw)==CORE_BLOB,"Wrong original R1 core Git blob"

def check_runtime(txt):
    for token in ["new SweepController(","new SweepSample(delta,","latest.DamageRequest",
                  "vehicle.AddForce(","vehicle.mass = heavy ? 12f : 1.5f",
                  "vehicle.transform.localScale = heavy","SetPaused(bool value)",
                  "Restart()","Time.fixedDeltaTime","collider.enabled = false",
                  "health = Mathf.Max(0, health - damagePerSweep)",
                  "GameObject.CreatePrimitive(PrimitiveType.Capsule)",
                  "CollisionDetectionMode.ContinuousDynamic",
                  "if (delta <= 0 || delta > 0.25) return"]:
        assert token in txt,"Missing runtime safety or physics contract "+token

def check_editor(txt):
    for token in ["NewSceneMode.Additive","NewSceneSetup.EmptyScene",
                  "SceneManager.SetActiveScene","SaveScene(scene, ScenePath)",
                  "CloseScene(scene, true)","System.IO.File.Exists(ScenePath)",
                  "AddComponent<SweepOrbUnityDemo>()"]:
        assert token in txt,"Editor scene gate missing "+token
    assert "OpenScene(" not in txt

def check_metas():
    names=asset_names()
    assert len(names)==19,"Expected 19 Unity original source and metadata paths"
    metas=[n for n in names if n.endswith(".meta")]
    assert len(metas)==12
    ids=[]
    for path in metas:
        m=re.search(r"(?m)^guid:\s*([a-f0-9]{32})$",source(path))
        assert m,"GUID malformed "+path
        ids.append(m.group(1))
    assert len(set(ids))==12,"Own GUID duplication"
    for p in (ROOT/"Assets").rglob("*.meta"):
        rel=p.relative_to(ROOT).as_posix()
        if rel in metas:continue
        m=re.search(rb"(?m)^guid:\s*([a-f0-9]{32})$",p.read_bytes())
        assert m is None or m.group(1).decode() not in ids,"Canonical GUID collision "+rel

def check_asmdefs():
    def obj(s):return json.loads(source(A/s))
    runtime=obj("Runtime/B0125.SweepOrb.R2.Runtime.asmdef")
    editor=obj("Editor/B0125.SweepOrb.R2.Editor.asmdef")
    test=obj("Tests/PlayMode/B0125.SweepOrb.R2.PlayModeTests.asmdef")
    assert runtime["name"]=="B0125.SweepOrb.R2.Runtime"
    assert runtime["autoReferenced"] is False
    assert editor["references"]==[runtime["name"]]
    assert editor["includePlatforms"]==["Editor"]
    assert test["references"]==[runtime["name"]]
    assert "TestAssemblies" in test["optionalUnityReferences"]

def check_manifest():
    m=json.loads(source(T/"manifest.json"))
    assert m["sourceVersion"]=="R2" and m["sourceBaseSha"]==R1
    assert m["unityG1"]=="NOT_RUN" and not m["codexAck"]
    all_names=asset_names()+[
        (T/"verify.py").as_posix(),(T/"README.md").as_posix(),
        (T/".gitignore").as_posix(),
        ".github/workflows/b01-25-w11-sweeporb-unity-r2.yml"]
    assert set(m["gitBlobs"])==set(all_names),"Unpinned or unexpected source"
    for path,sha in m["gitBlobs"].items():
        real=git("show","HEAD:"+path)
        assert blob(real)==sha and real==read(path),"Git-source pin mismatch "+path
    changed=git("diff","--name-only",R1,"HEAD").decode().splitlines()
    assert set(changed)==set(all_names)|{(T/"manifest.json").as_posix()},"Scope pollution"
    assert all(x.startswith(A.as_posix()+"/") or x==A.as_posix()+".meta" or
               x.startswith(T.as_posix()+"/") or
               x==".github/workflows/b01-25-w11-sweeporb-unity-r2.yml"
               for x in changed),"Foreign file overlap"

def build_zip(path):
    ledger={}
    with zipfile.ZipFile(path,"w",compression=zipfile.ZIP_STORED) as z:
        for name in asset_names():
            payload=git("show","HEAD:"+name)
            ledger[name]={"sha256":hashlib.sha256(payload).hexdigest(),
                          "gitBlob":blob(payload),"bytes":len(payload)}
            info=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
            info.external_attr=0o100644<<16
            z.writestr(info,payload,compress_type=zipfile.ZIP_STORED)
        info=zipfile.ZipInfo("PACKAGE_MANIFEST.json",date_time=(1980,1,1,0,0,0))
        info.external_attr=0o100644<<16
        summary={"package":"B01-25-SWEEPORB001/R2","sourceBaseSha":R1,
                 "nativeUnity":"NOT_RUN","codexAck":False,"files":ledger}
        z.writestr(info,(json.dumps(summary,sort_keys=True,separators=(",",":"))+"\n").encode(),
                   compress_type=zipfile.ZIP_STORED)
    return hashlib.sha256(pathlib.Path(path).read_bytes()).hexdigest()

def stage_rollback(path):
    with tempfile.TemporaryDirectory() as d:
        root=pathlib.Path(d)
        with zipfile.ZipFile(path) as z:
            names=z.namelist()
            assert len(names)==20 and len(set(names))==20
            for name in names:
                check_path(name)
                target=root/name
                assert not target.exists(),"Refuse existing file overwrite"
                target.parent.mkdir(parents=True,exist_ok=True)
                target.write_bytes(z.read(name))
                assert target.read_bytes()==z.read(name)
            for name in reversed(names):
                (root/name).unlink()
                assert not (root/name).exists()

def check_path(path):
    assert not path.startswith("/"),"Absolute ZIP path"
    assert ".." not in pathlib.PurePosixPath(path).parts,"Traversal"
    assert path=="PACKAGE_MANIFEST.json" or path==A.as_posix()+".meta" or path.startswith(A.as_posix()+"/"),"Foreign ZIP path"

class Checks(unittest.TestCase):
    def test_01_original_core_unchanged(self):check_core(read(A/"Runtime/SweepCore.cs"))
    def test_02_physical_guid_inventory(self):check_metas()
    def test_03_asmdef_dependencies(self):check_asmdefs()
    def test_04_actual_rigidbody_demo_contract(self):check_runtime(source(A/"Runtime/SweepOrbUnityDemo.cs"))
    def test_05_isolated_nonoverwrite_editor(self):check_editor(source(A/"Editor/SweepOrbSceneBuilder.cs"))
    def test_06_real_nunit_tests_authored(self):
        s=source(A/"Tests/PlayMode/SweepOrbUnityPlayModeTests.cs")
        self.assertEqual(4,s.count("[UnityTest]"))
        self.assertIn("new WaitForFixedUpdate()",s)
        self.assertIn("CapsuleCollider",s)
    def test_07_exact_source_scope_and_git(self):check_manifest()
    def test_08_mutated_core_rejected(self):
        with self.assertRaises(AssertionError):check_core(read(A/"Runtime/SweepCore.cs")+b"\n// tampered\n")
    def test_09_damage_gate_removal_rejected(self):
        with self.assertRaises(AssertionError):
            check_runtime(source(A/"Runtime/SweepOrbUnityDemo.cs").replace("latest.DamageRequest","latest.Contact"))
    def test_10_force_api_removal_rejected(self):
        with self.assertRaises(AssertionError):
            check_runtime(source(A/"Runtime/SweepOrbUnityDemo.cs").replace("vehicle.AddForce(","vehicle.transform.Translate("))
    def test_11_timestep_check_removal_rejected(self):
        with self.assertRaises(AssertionError):
            check_runtime(source(A/"Runtime/SweepOrbUnityDemo.cs").replace("if (delta <= 0 || delta > 0.25) return","if (delta <= 0) return"))
    def test_12_scenebuilder_nonoverwrite_rejected(self):
        with self.assertRaises(AssertionError):
            check_editor(source(A/"Editor/SweepOrbSceneBuilder.cs").replace("System.IO.File.Exists(ScenePath)","false"))
    def test_13_nonadditive_scene_rejected(self):
        with self.assertRaises(AssertionError):
            check_editor(source(A/"Editor/SweepOrbSceneBuilder.cs").replace("NewSceneMode.Additive","NewSceneMode.Single"))
    def test_14_zip_byte_determinism(self):
        with tempfile.TemporaryDirectory() as d:
            a=pathlib.Path(d)/"a.zip"
            b=pathlib.Path(d)/"b.zip"
            self.assertEqual(build_zip(a),build_zip(b))
            self.assertEqual(a.read_bytes(),b.read_bytes())
            stage_rollback(a)
    def test_15_zip_path_traversal_negative(self):
        for name in ["../other.cs","/etc/temp","Assets/Other/Injector.cs"]:
            with self.assertRaises(AssertionError):check_path(name)
    def test_16_r1_core_cannot_mutate_unity_hp(self):
        s=source(A/"Runtime/SweepCore.cs")
        self.assertNotIn("UnityEngine",s)
        self.assertNotIn("GameObject",s)
    def test_17_not_native_playmode_claim(self):
        m=json.loads(source(T/"manifest.json"))
        self.assertEqual(m["unityG1"],"NOT_RUN")
        self.assertFalse(m["integrated"])
    def test_18_source_factory_creates_actual_savable_scene(self):
        s=source(A/"Editor/SweepOrbSceneBuilder.cs")
        self.assertIn("SWEEPORB_R2_Demo.unity",s)
        self.assertIn("new GameObject",s)

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",required=True)
    args=parser.parse_args()
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(Checks))
    out=pathlib.Path(args.output)
    out.parent.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory() as d:
        package=pathlib.Path(d)/"unity-source-inner.zip"
        digest=build_zip(package)
        out.with_suffix(".zip").write_bytes(package.read_bytes())
    report={"package":"B01-25-SWEEPORB001/R2","passed":result.testsRun-len(result.failures)-len(result.errors),
            "total":result.testsRun,"fails":len(result.failures)+len(result.errors),
            "commit":git("rev-parse","HEAD").decode().strip(),
            "originalCoreGitBlob":CORE_BLOB,"innerZipSha256":digest,
            "nativeUnity":"NOT_RUN","codexAck":False}
    out.write_text(json.dumps(report,sort_keys=True,indent=2)+"\n")
    print("ACTUAL_SOURCE_VERIFY",report["passed"],"/",report["total"],"ZIP_SHA256",digest,"UNITY_NATIVE_NOT_RUN")
    raise SystemExit(not result.wasSuccessful())
if __name__=="__main__": main()
