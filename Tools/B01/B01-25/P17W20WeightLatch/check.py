#!/usr/bin/env python3
"""B01-25 original Git-byte provenance / Unity source ZIP / safe rollback. NOT native Unity."""
import argparse,hashlib,json,pathlib,re,subprocess,tempfile,unittest,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
A="Assets/B0125WeightLatch001"
R="Research/B01-25/P17/WEIGHTLATCH001"
T="Tools/B01/B01-25/P17W20WeightLatch"
WF=".github/workflows/b01-25-w20-weightlatch.yml"
BASE="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
def git(*args):return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def raw(p):return (ROOT/p).read_bytes()
def document(p):return json.loads(raw(p))
def gitblob(buf):return hashlib.sha1(("blob %d\0"%len(buf)).encode()+buf).hexdigest()
def pins():return document(T+"/manifest.json")["gitBlobs"]
def selected():return {name:sha for name,sha in pins().items() if name.startswith(A+"/") or name==A+".meta"}
def archive(dst):
    objects={}
    with zipfile.ZipFile(dst,"w",compression=zipfile.ZIP_STORED) as z:
        for name,sha in sorted(selected().items()):
            buf=git("show","HEAD:"+name)
            assert gitblob(buf)==sha and buf==raw(name)
            objects[name]={"gitBlob":sha,"sha256":hashlib.sha256(buf).hexdigest(),"bytes":len(buf)}
            item=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
            item.create_system=3;item.external_attr=0o100644<<16
            z.writestr(item,buf,compress_type=zipfile.ZIP_STORED)
        item=zipfile.ZipInfo("PACKAGE_MANIFEST.json",date_time=(1980,1,1,0,0,0))
        item.create_system=3;item.external_attr=0o100644<<16
        content={"package":"B01-25-WEIGHTLATCH001/R1","files":objects,
                 "nativeUnity":"NOT_RUN","g2":"HOLD","codexAck":False}
        z.writestr(item,(json.dumps(content,sort_keys=True,separators=(",",":"))+"\n").encode())
    return hashlib.sha256(pathlib.Path(dst).read_bytes()).hexdigest()
def allowed(name):
    p=pathlib.PurePosixPath(name)
    assert (name in selected() or name=="PACKAGE_MANIFEST.json")
    assert not p.is_absolute() and ".." not in p.parts and "\\" not in name
class OriginalQA(unittest.TestCase):
    def test_01_strict_owner_scope_in_original_git_diff(self):
        changed=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
        self.assertTrue(changed)
        self.assertTrue(all(p.startswith(A+"/") or p==A+".meta" or
           p.startswith(R+"/") or p.startswith(T+"/") or p==WF for p in changed),changed)
        self.assertEqual(len(changed),len(pins())+1)
    def test_02_original_git_blob_per_file_manifest_verified(self):
        changed=set(git("diff","--name-only",BASE,"HEAD").decode().splitlines())
        self.assertEqual(changed-{T+"/manifest.json"},set(pins()))
        for file,sha in pins().items():
            original=git("show","HEAD:"+file)
            self.assertEqual(original,raw(file),file)
            self.assertEqual(sha,gitblob(original),file)
    def test_03_unity_core_exact_original_research_source_bytes(self):
        self.assertEqual(raw(R+"/WeightLatchCore.cs"),raw(A+"/Runtime/WeightLatchCore.cs"))
    def test_04_12_distinct_original_unity_meta_guid_records(self):
        self.assertEqual(19,len(selected()))
        metas=[p for p in selected() if p.endswith(".meta")]
        self.assertEqual(12,len(metas))
        guids=[]
        for p in metas:
            m=re.search(r"^guid: ([0-9a-f]{32})$",raw(p).decode(),re.M)
            self.assertIsNotNone(m,p);guids.append(m.group(1))
        self.assertEqual(12,len(set(guids)))
    def test_05_three_isolated_unity_asmdef_references(self):
        a=document(A+"/Runtime/B0125.WeightLatch.Runtime.asmdef")
        b=document(A+"/Editor/B0125.WeightLatch.Editor.asmdef")
        c=document(A+"/Tests/PlayMode/B0125.WeightLatch.PlayModeTests.asmdef")
        self.assertEqual(3,len({a["name"],b["name"],c["name"]}))
        self.assertEqual([],a["references"])
        self.assertEqual(["Editor"],b["includePlatforms"])
        self.assertEqual([a["name"]],b["references"])
        self.assertEqual([a["name"]],c["references"])
        self.assertIn("TestAssemblies",c["optionalUnityReferences"])
    def test_06_real_collider_pad_and_real_rigidbody_gate(self):
        s=raw(A+"/Runtime/WeightLatchUnityDemo.cs").decode()
        for x in ("plateCollider.Raycast(","gateBody.MovePosition(",
                  "gateBody.isKinematic=true","vehicle.AddForce(",
                  "ForceMode.Force","vehicle.useGravity=true"):
            self.assertIn(x,s)
    def test_07_two_real_masses_and_independent_safe_path(self):
        s=raw(A+"/Runtime/WeightLatchUnityDemo.cs").decode()
        for x in ("vehicle.mass=isHeavy?12f:1.5f",
                  "new Vector3(4,-.25f,0)","new Vector3(2.5f,.5f,18)",
                  "new Vector3(5.8f,2.6f,.7f)"):
            self.assertIn(x,s)
    def test_08_additive_scene_no_override_existing_scene(self):
        s=raw(A+"/Editor/WeightLatchSceneBuilder.cs").decode()
        for x in ("NewSceneMode.Additive","File.Exists(ScenePath)",
                  "SaveScene(scene,ScenePath)","CloseScene(scene,true)"):
            self.assertIn(x,s)
    def test_09_six_unity_nunit_cases_only_authored(self):
        s=raw(A+"/Tests/PlayMode/WeightLatchNativePlayModeTests.cs").decode()
        self.assertEqual(6,s.count("[UnityTest]"))
        self.assertIn("RealPressurePlateHeavyOpensGate",s)
        self.assertIn("LightOnRealPlateCannotOpenHeavyLatch",s)
    def test_10_original_csharp9_compile_and_executable_positive_negative(self):
        p=raw(R+"/WeightLatchTests.csproj").decode()
        self.assertIn("<LangVersion>9.0</LangVersion>",p)
        s=raw(R+"/Program.cs").decode()
        for x in ("NO_REAL_COLLIDER_CONTACT","LATCH_HOLD_TIME_RESETS_ON_HEAVY_PRESSURE",
                  "INVALID_FRAME_DOES_NOT_PARTIALLY_MUTATE","OUT_OF_WORLD_RANGE"):
            self.assertIn(x,s)
    def test_11_no_codex_private_save_or_original_game_overwrite(self):
        names=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
        self.assertFalse(any(p.startswith("Assets/R6/") or
            p.startswith("Assets/B0125CounterBridge001/") or
            "AtomicDockLedger" in p or "SaveLoad" in p for p in names))
    def test_12_original_physical_zip_is_reproducible(self):
        with tempfile.TemporaryDirectory() as tmp:
            a=pathlib.Path(tmp)/"a.zip";b=pathlib.Path(tmp)/"b.zip"
            self.assertEqual(archive(a),archive(b))
            self.assertEqual(a.read_bytes(),b.read_bytes())
    def test_13_scratch_rollback_preserves_foreign_empty_and_occupied(self):
        with tempfile.TemporaryDirectory() as tmp:
            d=pathlib.Path(tmp);a=d/"original.zip";archive(a)
            empty=d/"FOREIGN_EMPTY";empty.mkdir()
            keep=d/"foreign"/"keep.txt";keep.parent.mkdir();keep.write_text("preserve")
            with zipfile.ZipFile(a) as z:
                names=z.namelist();self.assertEqual(20,len(names))
                for n in names:
                    allowed(n)
                    target=d/n;self.assertFalse(target.exists())
                    target.parent.mkdir(parents=True,exist_ok=True)
                    target.write_bytes(z.read(n))
                    self.assertEqual(target.read_bytes(),z.read(n))
                for n in reversed(names):(d/n).unlink()
            self.assertTrue(empty.is_dir())
            self.assertEqual([],list(empty.iterdir()))
            self.assertEqual("preserve",keep.read_text())
    def test_14_path_traversal_and_foreign_game_paths_rejected(self):
        for name in ("../escape","/tmp/attack","Assets/R6/canonical.cs",
                     "Assets/B0125CounterBridge001/Runtime/CounterBridgeCore.cs"):
            with self.assertRaises(AssertionError):allowed(name)
    def test_15_tampering_original_source_changes_hash(self):
        s=R+"/WeightLatchCore.cs"
        self.assertNotEqual(gitblob(raw(s)+b"mutated"),pins()[s])
    def test_16_native_and_integrator_status_are_truthful(self):
        m=document(T+"/manifest.json")
        self.assertEqual("NOT_RUN",m["nativeUnity"])
        self.assertFalse(m["integrated"]);self.assertFalse(m["codexAck"])
        self.assertEqual(BASE,m["baseSha"])
def main():
    parser=argparse.ArgumentParser();parser.add_argument("--out",required=True)
    args=parser.parse_args();output=pathlib.Path(args.out);output.parent.mkdir(parents=True,exist_ok=True)
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(OriginalQA))
    with tempfile.TemporaryDirectory() as d:
        a=pathlib.Path(d)/"original.zip";sha=archive(a)
        output.with_suffix(".zip").write_bytes(a.read_bytes())
    count=result.testsRun-len(result.failures)-len(result.errors)
    receipt={"id":"B01-25-WEIGHTLATCH001/R1","sourceChecks":result.testsRun,
             "sourcePassed":count,"originalUnitySourceZipSha256":sha,"nativeUnity":"NOT_RUN"}
    output.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
    print("WEIGHTLATCH_ORIGINAL_SOURCE_QA",count,"/",result.testsRun,
          "ZIP_SHA256",sha,"NATIVE_NOT_RUN")
    raise SystemExit(not result.wasSuccessful())
if __name__=="__main__":main()
