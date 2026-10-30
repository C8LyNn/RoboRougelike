#!/usr/bin/env python3
"""Original physical source gate; NOT native Unity/PhysX."""
import argparse,hashlib,json,pathlib,re,subprocess,tempfile,unittest,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
A=pathlib.Path("Assets/B0125MagnetLane001")
R=pathlib.Path("Research/B01-25/P17/MAGNETLANE001")
T=pathlib.Path("Tools/B01/B01-25/P17W14MagnetLane")
BASE="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
WF=".github/workflows/b01-25-w14-magnetlane.yml"
def git(*args):return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def content(path):return (ROOT/path).read_bytes()
def src(path):return content(path).decode()
def blob(b):return hashlib.sha1(("blob %d\0"%len(b)).encode()+b).hexdigest()
def assetnames():
    return sorted([p.relative_to(ROOT).as_posix() for p in (ROOT/A).rglob("*") if p.is_file()]+[A.as_posix()+".meta"])
def original_core(raw):
    assert raw==git("show","HEAD:"+(R/"MagnetLaneCore.cs").as_posix())
    assert b"public sealed class MagnetLaneController" in raw
    assert b"weightedForce+=take*force" in raw
def guids():
    names=assetnames()
    assert len(names)==19,"19 Unity source/meta original paths"
    metas=[n for n in names if n.endswith(".meta")]
    assert len(metas)==12
    found=[]
    for path in metas:
        match=re.search(r"(?m)^guid:\s*([a-f0-9]{32})$",src(path))
        assert match,path
        found.append(match.group(1))
    assert len(set(found))==12
    for p in (ROOT/"Assets").rglob("*.meta"):
        if p.relative_to(ROOT).as_posix() in metas:continue
        match=re.search(rb"(?m)^guid:\s*([a-f0-9]{32})$",p.read_bytes())
        assert not match or match.group(1).decode() not in found,"Other GUID collided"
def asmdefs():
    d=[]
    for path in ("Runtime","Editor","Tests/PlayMode"):
        files=list((ROOT/A/path).glob("*.asmdef"));assert len(files)==1
        d.append(json.loads(files[0].read_text()))
    assert d[0]["name"]=="B0125.MagnetLane.Runtime" and not d[0]["autoReferenced"]
    assert d[1]["includePlatforms"]==["Editor"] and d[1]["references"]==[d[0]["name"]]
    assert d[2]["references"]==[d[0]["name"]]
    assert "TestAssemblies" in d[2]["optionalUnityReferences"]
def runtime(s):
    for token in ("AddComponent<Rigidbody>()","GameObject.CreatePrimitive(PrimitiveType.Cube)",
                  "vehicle.mass=heavy?12f:1.5f","vehicle.transform.localScale=heavy",
                  "new MagnetSample(dt,pos.x,pos.z,vehicle.mass",
                  "vehicle.AddForce(Vector3.forward*(float)last.ForceZ,ForceMode.Force)",
                  "PERMANENT SAFE BYPASS world Z4","if(dt<=0||dt>.1f)return"):
        assert token in s,"Missing runtime source contract "+token
def scene(s):
    for token in ("NewSceneMode.Additive","NewSceneSetup.EmptyScene",
                  "System.IO.File.Exists(Path)","SaveScene(scene,Path)",
                  "CloseScene(scene,true)","SetActiveScene(before)"):
        assert token in s,"Missing scene protection "+token
def native(s):
    assert s.count("[UnityTest]")==5
    for token in ("ActualPhysicsCapsuleAndFloorExist","HeavyHasDifferentRealBodyEnvelopeAndMass",
                  "ActualRigidbodyReceivesMagnetForceOnlyDuringActivePeriod",
                  "PauseFreezesMagnetClockRestartResets","NorthernBypassNeverReceivesFieldForce"):
        assert token in s
def mapdata():
    d=json.loads(src(R/"arena.json"))
    assert d["schemaVersion"]==1
    assert d["magnetLane"]["center"]==[0,0] and d["magnetLane"]["size"]==[6,3]
    assert d["safeBypass"]["center"]==[0,4] and d["safeBypass"]["permanent"]
    assert d["vehicleMassKg"]["light"]==1.5 and d["vehicleMassKg"]["heavy"]==12
def manifest():
    d=json.loads(src(T/"manifest.json"))
    assert d["packageId"]=="B01-25-MAGNETLANE001" and d["version"]=="R1"
    assert d["baseSha"]==BASE and d["nativeUnity"]=="NOT_RUN"
    assert not d["integrated"] and not d["codexAck"]
    changed=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
    def allowed(p):
        return p.startswith(A.as_posix()+"/") or p==A.as_posix()+".meta" or \
               p.startswith(R.as_posix()+"/") or p.startswith(T.as_posix()+"/") or p==WF
    assert changed and all(allowed(p) for p in changed),"Foreign or canonical path touched"
    assert set(d["gitBlobs"])==set(changed)-{(T/"manifest.json").as_posix()}
    for name,sha in d["gitBlobs"].items():
        b=git("show","HEAD:"+name)
        assert blob(b)==sha and b==content(name),"Git pin mismatch "+name
def safe_name(name):
    assert not name.startswith("/") and ".." not in pathlib.PurePosixPath(name).parts
    assert name=="PACKAGE_MANIFEST.json" or name==A.as_posix()+".meta" or name.startswith(A.as_posix()+"/")
def physical_zip(target):
    files={}
    with zipfile.ZipFile(target,"w",compression=zipfile.ZIP_STORED) as z:
        for name in assetnames():
            b=git("show","HEAD:"+name)
            files[name]={"gitBlob":blob(b),"sha256":hashlib.sha256(b).hexdigest(),"size":len(b)}
            info=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
            info.create_system=3;info.external_attr=0o100644<<16
            z.writestr(info,b,compress_type=zipfile.ZIP_STORED)
        info=zipfile.ZipInfo("PACKAGE_MANIFEST.json",date_time=(1980,1,1,0,0,0))
        info.create_system=3;info.external_attr=0o100644<<16
        meta={"package":"B01-25-MAGNETLANE001/R1","nativeUnity":"NOT_RUN","codexAck":False,"assets":files}
        z.writestr(info,(json.dumps(meta,sort_keys=True,separators=(",",":"))+"\n").encode(),compress_type=zipfile.ZIP_STORED)
    return hashlib.sha256(pathlib.Path(target).read_bytes()).hexdigest()
def rollback(archive):
    with tempfile.TemporaryDirectory() as temp:
        root=pathlib.Path(temp)
        with zipfile.ZipFile(archive) as z:
            paths=z.namelist()
            assert len(paths)==20 and len(set(paths))==20
            assert all(item.create_system==3 for item in z.infolist())
            for name in paths:
                safe_name(name)
                target=root/name;assert not target.exists()
                target.parent.mkdir(parents=True,exist_ok=True)
                target.write_bytes(z.read(name))
                assert target.read_bytes()==z.read(name)
            for name in reversed(paths):
                target=root/name;target.unlink();assert not target.exists()
class QA(unittest.TestCase):
    def test_01_original_byte_exact_engine(self):original_core(content(A/"Runtime/MagnetLaneCore.cs"))
    def test_02_guid_inventory_collision(self):guids()
    def test_03_asmdef_isolated(self):asmdefs()
    def test_04_actual_unity_force_source(self):runtime(src(A/"Runtime/MagnetLaneUnityDemo.cs"))
    def test_05_scene_factory_nonoverwrite(self):scene(src(A/"Editor/MagnetLaneSceneBuilder.cs"))
    def test_06_native_test_source_exists(self):native(src(A/"Tests/PlayMode/MagnetLaneNativePlayModeTests.cs"))
    def test_07_authored_permanent_bypass(self):mapdata()
    def test_08_git_ownership_and_manifest_pins(self):manifest()
    def test_09_mutated_core_rejected(self):
        with self.assertRaises(AssertionError):original_core(content(A/"Runtime/MagnetLaneCore.cs")+b"tampered")
    def test_10_disabled_force_api_rejected(self):
        with self.assertRaises(AssertionError):runtime(src(A/"Runtime/MagnetLaneUnityDemo.cs").replace("ForceMode.Force","ForceMode.VelocityChange"))
    def test_11_missing_heavy_mass_rejected(self):
        with self.assertRaises(AssertionError):runtime(src(A/"Runtime/MagnetLaneUnityDemo.cs").replace("vehicle.mass=heavy?12f:1.5f","vehicle.mass=12f"))
    def test_12_overwrite_scene_rejected(self):
        with self.assertRaises(AssertionError):scene(src(A/"Editor/MagnetLaneSceneBuilder.cs").replace("System.IO.File.Exists(Path)","false"))
    def test_13_nonadditive_scene_rejected(self):
        with self.assertRaises(AssertionError):scene(src(A/"Editor/MagnetLaneSceneBuilder.cs").replace("NewSceneMode.Additive","NewSceneMode.Single"))
    def test_14_deterministic_original_zip_stage_and_rollback(self):
        with tempfile.TemporaryDirectory() as d:
            a=pathlib.Path(d)/"a.zip";b=pathlib.Path(d)/"b.zip"
            self.assertEqual(physical_zip(a),physical_zip(b))
            self.assertEqual(a.read_bytes(),b.read_bytes())
            rollback(a)
    def test_15_reject_unsafe_import(self):
        for n in ("../foreign","/tmp/foreign","Assets/Other/foreign.cs"):
            with self.assertRaises(AssertionError):safe_name(n)
    def test_16_core_has_no_unity_dependency(self):
        self.assertNotIn("UnityEngine",src(A/"Runtime/MagnetLaneCore.cs"))
    def test_17_honest_native_marker(self):
        m=json.loads(src(T/"manifest.json"))
        self.assertEqual(m["nativeUnity"],"NOT_RUN")
        self.assertFalse(m["integrated"])
    def test_18_no_fake_physics_teleport_for_force(self):
        s=src(A/"Runtime/MagnetLaneUnityDemo.cs")
        self.assertIn("ForceMode.Force",s)
        self.assertNotIn("vehicle.transform.Translate(",s)
def main():
    p=argparse.ArgumentParser();p.add_argument("--out",required=True);a=p.parse_args()
    out=pathlib.Path(a.out);out.parent.mkdir(parents=True,exist_ok=True)
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(QA))
    with tempfile.TemporaryDirectory() as d:
        z=pathlib.Path(d)/"inner.zip";digest=physical_zip(z);out.with_suffix(".zip").write_bytes(z.read_bytes())
    receipt={"package":"B01-25-MAGNETLANE001/R1","tested":result.testsRun,
             "passed":result.testsRun-len(result.failures)-len(result.errors),
             "failed":len(result.failures)+len(result.errors),
             "sourceHead":git("rev-parse","HEAD").decode().strip(),
             "innerZipSha256":digest,"nativeUnity":"NOT_RUN"}
    out.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
    print("SOURCE_GATE",receipt["passed"],"/",receipt["tested"],"ORIGINAL_ZIP_SHA256",digest,"NATIVE_NOT_RUN")
    raise SystemExit(not result.wasSuccessful())
if __name__=="__main__":main()
