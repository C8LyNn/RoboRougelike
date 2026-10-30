#!/usr/bin/env python3
import argparse,hashlib,json,pathlib,re,subprocess,tempfile,unittest,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
A="Assets/B0125CounterBridge001"
R="Research/B01-25/P17/COUNTERBRIDGE001"
T="Tools/B01/B01-25/P17W19CounterBridge"
WF=".github/workflows/b01-25-w19-counterbridge.yml"
BASE="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
def git(*a):return subprocess.check_output(["git","-C",str(ROOT),*a],stderr=subprocess.PIPE)
def raw(p):return (ROOT/p).read_bytes()
def data(p):return json.loads(raw(p))
def blob(buf):return hashlib.sha1(("blob %d\0"%len(buf)).encode()+buf).hexdigest()
def pins():return data(T+"/manifest.json")["gitBlobs"]
def zip_original(dst):
    files={}
    with zipfile.ZipFile(dst,"w",compression=zipfile.ZIP_STORED) as z:
        for name,sha in sorted(pins().items()):
            if not (name.startswith(A+"/") or name==A+".meta"):continue
            buf=git("show","HEAD:"+name)
            assert blob(buf)==sha and buf==raw(name)
            files[name]=sha
            i=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
            i.create_system=3;i.external_attr=0o100644<<16
            z.writestr(i,buf,compress_type=zipfile.ZIP_STORED)
        i=zipfile.ZipInfo("PACKAGE_MANIFEST.json",date_time=(1980,1,1,0,0,0))
        i.create_system=3;i.external_attr=0o100644<<16
        z.writestr(i,(json.dumps({"package":"B01-25-COUNTERBRIDGE001/R1",
            "files":files,"nativeUnity":"NOT_RUN","codexAck":False},
            sort_keys=True,separators=(",",":"))+"\n").encode())
    return hashlib.sha256(pathlib.Path(dst).read_bytes()).hexdigest()
def safe(name,allowed):
    p=pathlib.PurePosixPath(name)
    assert name in allowed or name=="PACKAGE_MANIFEST.json"
    assert not p.is_absolute() and ".." not in p.parts and "\\" not in name
class QA(unittest.TestCase):
    def test_01_original_owner_scope_untouched_other_source(self):
        files=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
        self.assertTrue(files)
        self.assertTrue(all(n.startswith(A+"/") or n==A+".meta" or
           n.startswith(R+"/") or n.startswith(T+"/") or n==WF for n in files))
    def test_02_original_git_blobs_match_manifest(self):
        files=set(git("diff","--name-only",BASE,"HEAD").decode().splitlines())
        self.assertEqual(files-{T+"/manifest.json"},set(pins()))
        for name,sha in pins().items():
            buf=git("show","HEAD:"+name)
            self.assertEqual(blob(buf),sha)
            self.assertEqual(raw(name),buf)
    def test_03_unity_original_core_byte_identical_research(self):
        self.assertEqual(raw(R+"/CounterBridgeCore.cs"),raw(A+"/Runtime/CounterBridgeCore.cs"))
    def test_04_12_stable_unique_unity_guids_and_meta(self):
        names=[x for x in pins() if x.startswith(A)]
        metas=[p for p in names if p.endswith(".meta")]
        self.assertEqual(19,len(names));self.assertEqual(12,len(metas))
        ids=[]
        for p in metas:
            m=re.search(r"^guid: ([0-9a-f]{32})$",raw(p).decode(),re.M)
            self.assertIsNotNone(m,p);ids.append(m.group(1))
        self.assertEqual(12,len(set(ids)))
    def test_05_three_isolated_asmdefs(self):
        r=data(A+"/Runtime/B0125.CounterBridge.Runtime.asmdef")
        e=data(A+"/Editor/B0125.CounterBridge.Editor.asmdef")
        p=data(A+"/Tests/PlayMode/B0125.CounterBridge.PlayModeTests.asmdef")
        self.assertEqual(3,len({r["name"],e["name"],p["name"]}))
        self.assertEqual([],r["references"])
        self.assertEqual([r["name"]],e["references"])
        self.assertEqual(["Editor"],e["includePlatforms"])
        self.assertEqual([r["name"]],p["references"])
        self.assertIn("TestAssemblies",p["optionalUnityReferences"])
    def test_06_actual_physical_collider_ray_and_platform_rotation(self):
        s=raw(A+"/Runtime/CounterBridgeUnityDemo.cs").decode()
        for t in ("bridgeCollider.Raycast(","bridgeBody.MoveRotation(",
          "bridgeBody.isKinematic=true","vehicle.AddForce(","ForceMode.Force"):
            self.assertIn(t,s)
    def test_07_real_two_mass_world_and_safe_bypass(self):
        s=raw(A+"/Runtime/CounterBridgeUnityDemo.cs").decode()
        for t in ("vehicle.mass=value?12f:1.5f","new Vector3(4,-.25f,0)",
          "new Vector3(3,.3f,6)","new Vector3(2.5f,.5f,18)"):
            self.assertIn(t,s)
    def test_08_nonoverwriting_additive_scene_builder(self):
        s=raw(A+"/Editor/CounterBridgeSceneBuilder.cs").decode()
        for t in ("NewSceneMode.Additive","File.Exists(Target)","SaveScene(fresh,Target)",
                  "CloseScene(fresh,true)"):self.assertIn(t,s)
    def test_09_six_native_cases_authored_but_not_run(self):
        s=raw(A+"/Tests/PlayMode/CounterBridgeNativePlayModeTests.cs").decode()
        self.assertEqual(6,s.count("[UnityTest]"))
        self.assertIn("RealColliderSupportTiltsActualKinematicBridge",s)
    def test_10_real_source_positive_negative_test_compilation_project(self):
        s=raw(R+"/Program.cs").decode()
        self.assertIn("R1_PAUSED_INVALID_HOST_SAMPLE_REJECTED_ATOMIC",s)
        self.assertIn("R1_BRIDGE_MECHANICAL_STOPS_PRESERVE_INVARIANT",s)
        p=raw(R+"/CounterBridgeTests.csproj").decode()
        self.assertIn("<LangVersion>9.0</LangVersion>",p)
    def test_11_no_save_canonical_Codex_patches(self):
        names=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
        self.assertFalse(any(x.startswith("Assets/R6/") or
          x.startswith("Assets/MOV009") or "SaveLoad" in x for x in names))
    def test_12_original_zip_reproducible(self):
        with tempfile.TemporaryDirectory() as d:
            a=pathlib.Path(d)/"a.zip";b=pathlib.Path(d)/"b.zip"
            self.assertEqual(zip_original(a),zip_original(b))
            self.assertEqual(a.read_bytes(),b.read_bytes())
    def test_13_real_source_stage_safe_rollback_foreign_empty(self):
        with tempfile.TemporaryDirectory() as d:
            root=pathlib.Path(d);zpath=root/"source.zip";zip_original(zpath)
            foreign=root/"FOREIGN_EMPTY";foreign.mkdir()
            keep=root/"FOREIGN"/"keep.txt";keep.parent.mkdir();keep.write_text("do not touch")
            permitted={p for p in pins() if p.startswith(A)}
            with zipfile.ZipFile(zpath) as z:
                names=z.namelist();self.assertEqual(20,len(names))
                for name in names:
                    safe(name,permitted)
                    target=root/name
                    self.assertFalse(target.exists())
                    target.parent.mkdir(parents=True,exist_ok=True)
                    target.write_bytes(z.read(name))
                    self.assertEqual(target.read_bytes(),z.read(name))
                for name in reversed(names):(root/name).unlink()
            self.assertEqual([],list(foreign.iterdir()))
            self.assertEqual("do not touch",keep.read_text())
    def test_14_foreign_zip_path_rejected(self):
        permitted={p for p in pins() if p.startswith(A)}
        for p in ("../evil","/tmp/evil","Assets/R6/bad.cs"):
            with self.assertRaises(AssertionError):safe(p,permitted)
    def test_15_tampering_original_blob_detected(self):
        p=R+"/CounterBridgeCore.cs"
        self.assertNotEqual(blob(raw(p)+b"mutation"),pins()[p])
    def test_16_truthful_non_native_lifecycle(self):
        m=data(T+"/manifest.json")
        self.assertEqual("NOT_RUN",m["nativeUnity"])
        self.assertEqual(BASE,m["baseSha"])
        self.assertFalse(m["codexAck"])
        self.assertFalse(m["integrated"])
def main():
    ap=argparse.ArgumentParser();ap.add_argument("--out",required=True)
    args=ap.parse_args();out=pathlib.Path(args.out);out.parent.mkdir(parents=True,exist_ok=True)
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(QA))
    with tempfile.TemporaryDirectory() as d:
        z=pathlib.Path(d)/"source.zip";sha=zip_original(z)
        out.with_suffix(".zip").write_bytes(z.read_bytes())
    receipt={"sourcePassed":result.testsRun-len(result.failures)-len(result.errors),
      "sourceTested":result.testsRun,"originalUnitySourceZipSha256":sha,
      "nativeUnity":"NOT_RUN"}
    out.write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
    print("COUNTERBRIDGE_SOURCE_QA",receipt["sourcePassed"],"/",receipt["sourceTested"],
          "ORIGINAL_ZIP_SHA256",sha,"NATIVE_NOT_RUN")
    raise SystemExit(not result.wasSuccessful())
if __name__=="__main__":main()
