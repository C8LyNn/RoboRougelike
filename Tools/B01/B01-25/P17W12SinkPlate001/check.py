#!/usr/bin/env python3
"""Physical original source QA, never native Unity test."""
import argparse,hashlib,json,pathlib,re,subprocess,tempfile,unittest,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
A=pathlib.Path("Assets/B0125SinkPlate001")
R=pathlib.Path("Research/B01-25/P17/SINKPLATE001")
T=pathlib.Path("Tools/B01/B01-25/P17W12SinkPlate001")
BASE="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
def git(*args):return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def read(path):return (ROOT/path).read_bytes()
def txt(path):return read(path).decode()
def blob(data):return hashlib.sha1(("blob %d\0"%len(data)).encode()+data).hexdigest()
def assets():
    return sorted([p.relative_to(ROOT).as_posix() for p in (ROOT/A).rglob("*") if p.is_file()]+[A.as_posix()+".meta"])
def exact_core(raw):
    assert raw==git("show","HEAD:"+(R/"SinkPlateCore.cs").as_posix())
    assert b"public sealed class SinkPlateController" in raw
def guids():
    names=assets()
    assert len(names)==19
    meta=[n for n in names if n.endswith(".meta")]
    assert len(meta)==12
    known=[]
    for n in meta:
        x=re.search(r"(?m)^guid:\s*([0-9a-f]{32})$",txt(n))
        assert x,n
        known.append(x.group(1))
    assert len(set(known))==12
    for file in (ROOT/"Assets").rglob("*.meta"):
        if file.relative_to(ROOT).as_posix() in meta:continue
        m=re.search(rb"(?m)^guid:\s*([0-9a-f]{32})$",file.read_bytes())
        assert not m or m.group(1).decode() not in known
def asmdefs():
    d=[]
    for rel in ("Runtime","Editor","Tests/PlayMode"):
        ps=list((ROOT/A/rel).glob("*.asmdef"));assert len(ps)==1
        d.append(json.loads(ps[0].read_text()))
    assert d[0]["autoReferenced"] is False
    assert d[1]["references"]==["B0125.SinkPlate.Runtime"] and d[1]["includePlatforms"]==["Editor"]
    assert d[2]["references"]==["B0125.SinkPlate.Runtime"]
    assert "TestAssemblies" in d[2]["optionalUnityReferences"]
def actual_demo(s):
    for k in ("AddComponent<Rigidbody>()","CreatePrimitive(PrimitiveType.Cube)",
      "vehicle.mass=heavy?12f:1.5f","vehicle.transform.localScale=heavy",
      "sinkingCollider.enabled=snap.SupportsVehicle","vehicle.AddForce(motion,ForceMode.Force)",
      "new PlateInput(dt,onPlate,vehicle.mass)","PERMANENT SAFE NORTHERN BYPASS",
      "if(pos.y<-5f)","new PlateConfig(4,.8,.35,1.2,.4)"):
        assert k in s,k
def editor(s):
    for k in ("NewSceneMode.Additive","NewSceneSetup.EmptyScene",
      "System.IO.File.Exists(GeneratedScene)","SaveScene(isolated,GeneratedScene)",
      "CloseScene(isolated,true)","SetActiveScene(prior)"):
        assert k in s,k
def arena():
    d=json.loads(txt(R/"arena.json"))
    assert d["plate"]["center"]==[0,0] and d["plate"]["size"]==[6,3]
    assert d["safeBypass"]["center"]==[0,4] and d["safeBypass"]["size"]==[6,2]
    assert d["westApproach"]["center"]==[-7,0] and d["eastExit"]["center"]==[7,0]
    assert d["seconds"]["heavyWarning"]<d["seconds"]["lightWarning"]
def manifest():
    m=json.loads(txt(T/"manifest.json"))
    assert m["packageId"]=="B01-25-SINKPLATE001" and m["version"]=="R1"
    assert m["baseSha"]==BASE and m["unityNative"]=="NOT_RUN"
    assert not m["codexAck"] and not m["integrated"]
    paths=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
    def own(n):
        return n.startswith(A.as_posix()+"/") or n==A.as_posix()+".meta" or \
         n.startswith(R.as_posix()+"/") or n.startswith(T.as_posix()+"/") or \
         n==".github/workflows/b01-25-w12-sinkplate.yml"
    assert paths and all(own(n) for n in paths)
    assert set(m["gitBlobs"])==set(paths)-{(T/"manifest.json").as_posix()}
    for n,d in m["gitBlobs"].items():
        raw=git("show","HEAD:"+n)
        assert blob(raw)==d and raw==read(n),"original source hash mismatch "+n
def safe_name(n):
    assert not n.startswith("/") and ".." not in pathlib.PurePosixPath(n).parts
    assert n=="PACKAGE_MANIFEST.json" or n==A.as_posix()+".meta" or n.startswith(A.as_posix()+"/")
def pack(target):
    ledger={}
    with zipfile.ZipFile(target,"w",compression=zipfile.ZIP_STORED) as z:
        for n in assets():
            raw=git("show","HEAD:"+n)
            ledger[n]={"sha256":hashlib.sha256(raw).hexdigest(),"gitBlob":blob(raw),"bytes":len(raw)}
            info=zipfile.ZipInfo(n,date_time=(1980,1,1,0,0,0))
            info.create_system=3;info.external_attr=0o100644<<16
            z.writestr(info,raw,compress_type=zipfile.ZIP_STORED)
        info=zipfile.ZipInfo("PACKAGE_MANIFEST.json",date_time=(1980,1,1,0,0,0))
        info.create_system=3;info.external_attr=0o100644<<16
        d={"packageId":"B01-25-SINKPLATE001","version":"R1","unityNative":"NOT_RUN","codexAck":False,"files":ledger}
        z.writestr(info,(json.dumps(d,sort_keys=True,separators=(",",":"))+"\n").encode(),compress_type=zipfile.ZIP_STORED)
    return hashlib.sha256(pathlib.Path(target).read_bytes()).hexdigest()
def rollback(src):
    with tempfile.TemporaryDirectory() as temp:
        dest=pathlib.Path(temp)
        with zipfile.ZipFile(src) as z:
            names=z.namelist()
            assert len(names)==20 and len(set(names))==20
            assert all(i.create_system==3 for i in z.infolist())
            for n in names:
                safe_name(n)
                out=dest/n
                assert not out.exists()
                out.parent.mkdir(parents=True,exist_ok=True)
                out.write_bytes(z.read(n))
                assert out.read_bytes()==z.read(n)
            for n in reversed(names):
                out=dest/n;out.unlink();assert not out.exists()
class Checks(unittest.TestCase):
    def test_01_exact_original_core(self):exact_core(read(A/"Runtime/SinkPlateCore.cs"))
    def test_02_stable_unique_guids(self):guids()
    def test_03_isolated_asmdefs(self):asmdefs()
    def test_04_original_physics_demo_source(self):actual_demo(txt(A/"Runtime/SinkPlateUnityDemo.cs"))
    def test_05_original_nonoverwrite_editor(self):editor(txt(A/"Editor/SinkPlateSceneBuilder.cs"))
    def test_06_original_nunit_tests_authored(self):
        s=txt(A/"Tests/PlayMode/SinkPlateNativePlayModeTests.cs")
        self.assertEqual(4,s.count("[UnityTest]"));self.assertIn("WaitForFixedUpdate()",s)
    def test_07_bypass_usable_while_plate_down(self):arena()
    def test_08_original_git_pins_and_scope(self):manifest()
    def test_09_mutated_core_rejected(self):
        with self.assertRaises(AssertionError):exact_core(read(A/"Runtime/SinkPlateCore.cs")+b"BAD")
    def test_10_collider_always_on_rejected(self):
        with self.assertRaises(AssertionError):
            actual_demo(txt(A/"Runtime/SinkPlateUnityDemo.cs").replace("sinkingCollider.enabled=snap.SupportsVehicle","sinkingCollider.enabled=true"))
    def test_11_vehicle_mass_cheat_rejected(self):
        with self.assertRaises(AssertionError):
            actual_demo(txt(A/"Runtime/SinkPlateUnityDemo.cs").replace("vehicle.mass=heavy?12f:1.5f","vehicle.mass=1.5f"))
    def test_12_existing_scene_overwrite_rejected(self):
        with self.assertRaises(AssertionError):
            editor(txt(A/"Editor/SinkPlateSceneBuilder.cs").replace("System.IO.File.Exists(GeneratedScene)","false"))
    def test_13_nonadditive_scene_rejected(self):
        with self.assertRaises(AssertionError):
            editor(txt(A/"Editor/SinkPlateSceneBuilder.cs").replace("NewSceneMode.Additive","NewSceneMode.Single"))
    def test_14_original_deterministic_zip_and_rollback(self):
        with tempfile.TemporaryDirectory() as d:
            a=pathlib.Path(d)/"a.zip";b=pathlib.Path(d)/"b.zip"
            self.assertEqual(pack(a),pack(b))
            self.assertEqual(a.read_bytes(),b.read_bytes())
            rollback(a)
    def test_15_unsafe_zip_paths_rejected(self):
        for n in ("../evil","/etc/evil","Assets/Foreign/thing.cs"):
            with self.assertRaises(AssertionError):safe_name(n)
    def test_16_source_does_not_fake_unity_physics(self):
        self.assertNotIn("UnityEngine",txt(A/"Runtime/SinkPlateCore.cs"))
    def test_17_no_fake_integration(self):
        m=json.loads(txt(T/"manifest.json"))
        self.assertEqual(m["unityNative"],"NOT_RUN");self.assertFalse(m["integrated"])
    def test_18_authoring_safe_route_is_separate(self):
        m=json.loads(txt(R/"arena.json"))
        self.assertGreater(m["safeBypass"]["center"][1]-m["safeBypass"]["size"][1]/2,
                           m["plate"]["center"][1]+m["plate"]["size"][1]/2)
def main():
    p=argparse.ArgumentParser();p.add_argument("--out",required=True);args=p.parse_args()
    out=pathlib.Path(args.out);out.parent.mkdir(parents=True,exist_ok=True)
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(Checks))
    with tempfile.TemporaryDirectory() as d:
        z=pathlib.Path(d)/"source.zip"
        digest=pack(z);out.with_suffix(".zip").write_bytes(z.read_bytes())
    obj={"package":"B01-25-SINKPLATE001/R1","tested":result.testsRun,
         "passed":result.testsRun-len(result.errors)-len(result.failures),
         "failures":len(result.errors)+len(result.failures),"innerZipSha256":digest,
         "headSha":git("rev-parse","HEAD").decode().strip(),"unityNative":"NOT_RUN"}
    out.write_text(json.dumps(obj,sort_keys=True,indent=2)+"\n")
    print("SINKPLATE_SOURCE_G0",obj["passed"],"/",obj["tested"],"ZIP_SHA256",digest,"UNITY_NATIVE_NOT_RUN")
    raise SystemExit(not result.wasSuccessful())
if __name__=="__main__":main()
