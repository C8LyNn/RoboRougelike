#!/usr/bin/env python3
"""Source-original B01-25 W21 physical Unity ZIP and non-destructive receive QA. Native NOT_RUN."""
import argparse,hashlib,json,pathlib,re,subprocess,tempfile,unittest,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
A="Assets/B0125Airlock001";R="Research/B01-25/P17/AIRLOCK001"
T="Tools/B01/B01-25/P17W21Airlock";WF=".github/workflows/b01-25-w21-airlock.yml"
BASE="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
def git(*args):return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def source(path):return (ROOT/path).read_bytes()
def parsed(path):return json.loads(source(path))
def blob(buf):return hashlib.sha1(("blob %d\0"%len(buf)).encode()+buf).hexdigest()
def pins():return parsed(T+"/manifest.json")["gitBlobs"]
def unity_pins():return {p:h for p,h in pins().items() if p.startswith(A+"/") or p==A+".meta"}
def source_archive(path):
    manifest={}
    with zipfile.ZipFile(path,"w",compression=zipfile.ZIP_STORED) as z:
        for name,sha in sorted(unity_pins().items()):
            buf=git("show","HEAD:"+name)
            assert blob(buf)==sha and source(name)==buf
            manifest[name]={"blob":sha,"sha256":hashlib.sha256(buf).hexdigest(),"bytes":len(buf)}
            item=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
            item.create_system=3;item.external_attr=0o100644<<16
            z.writestr(item,buf,compress_type=zipfile.ZIP_STORED)
        item=zipfile.ZipInfo("PACKAGE_MANIFEST.json",date_time=(1980,1,1,0,0,0))
        item.create_system=3;item.external_attr=0o100644<<16
        z.writestr(item,(json.dumps({"package":"B01-25-AIRLOCK001/R1","files":manifest,
            "nativeUnity":"NOT_RUN","codexAck":False},sort_keys=True,separators=(",",":"))+"\n").encode())
    return hashlib.sha256(pathlib.Path(path).read_bytes()).hexdigest()
def allow(name):
    p=pathlib.PurePosixPath(name)
    assert name=="PACKAGE_MANIFEST.json" or name in unity_pins()
    assert not p.is_absolute() and ".." not in p.parts and "\\" not in name
class OriginalQA(unittest.TestCase):
    def test_01_strict_only_brand_new_owner_paths(self):
        changed=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
        self.assertTrue(changed)
        self.assertTrue(all(p.startswith(A+"/") or p==A+".meta" or
           p.startswith(R+"/") or p.startswith(T+"/") or p==WF for p in changed),changed)
        self.assertEqual(set(changed)-{T+"/manifest.json"},set(pins()))
        self.assertFalse(any(x.startswith("Assets/R6/") for x in changed))
    def test_02_all_original_git_blobs_are_exactly_pinned(self):
        for path,h in pins().items():
            b=git("show","HEAD:"+path)
            self.assertEqual(blob(b),h,path);self.assertEqual(source(path),b,path)
    def test_03_original_unity_runtime_core_equals_source_game_core(self):
        self.assertEqual(source(R+"/AirlockCore.cs"),source(A+"/Runtime/AirlockCore.cs"))
    def test_04_unity_meta_twelve_unique_valid_32_character_guids(self):
        self.assertEqual(19,len(unity_pins()))
        paths=[name for name in unity_pins() if name.endswith(".meta")]
        self.assertEqual(12,len(paths))
        guids=[]
        for name in paths:
            m=re.search(r"^guid: ([0-9a-f]{32})$",source(name).decode(),re.M)
            self.assertIsNotNone(m,name);guids.append(m.group(1))
        self.assertEqual(12,len(set(guids)))
    def test_05_three_unique_asmdef_import_dependencies(self):
        r=parsed(A+"/Runtime/B0125.Airlock.Runtime.asmdef")
        e=parsed(A+"/Editor/B0125.Airlock.Editor.asmdef")
        t=parsed(A+"/Tests/PlayMode/B0125.Airlock.PlayModeTests.asmdef")
        self.assertEqual(3,len({r["name"],e["name"],t["name"]}))
        self.assertEqual([],r["references"])
        self.assertEqual(["Editor"],e["includePlatforms"])
        self.assertEqual([r["name"]],e["references"])
        self.assertEqual([r["name"]],t["references"])
        self.assertIn("TestAssemblies",t["optionalUnityReferences"])
        self.assertFalse(r["autoReferenced"])
    def test_06_two_actual_distinct_physical_gate_rigidbodies(self):
        s=source(A+"/Runtime/AirlockUnityDemo.cs").decode()
        for word in ("entryGate.isKinematic=true","exitGate.isKinematic=true",
            "entryGate.MovePosition(","exitGate.MovePosition(",
            "Block(\"DOOR A","Block(\"DOOR B"):
            self.assertIn(word,s)
    def test_07_physical_sensor_probes_and_body_not_fake_flags(self):
        s=source(A+"/Runtime/AirlockUnityDemo.cs").decode()
        for word in ("Physics.OverlapBoxNonAlloc(","QueryTriggerInteraction.Collide",
                     "probeHits[i]==rigCollider","new AirlockInput(",
                     "entryThreshold","exitThreshold"):
            self.assertIn(word,s)
    def test_08_vehicle_two_masses_and_physical_bypass_corridor(self):
        s=source(A+"/Runtime/AirlockUnityDemo.cs").decode()
        for word in ("vehicle.mass=value?12f:1.5f","new Vector3(4,-.25f,0)",
                     "new Vector3(2.5f,.5f,19)","vehicle.AddForce(",
                     "ForceMode.Force"):
            self.assertIn(word,s)
    def test_09_no_overwrite_scene_additive_creation_and_cleanup(self):
        s=source(A+"/Editor/AirlockSceneBuilder.cs").decode()
        for word in ("NewSceneMode.Additive","File.Exists(ScenePath)",
                     "SaveScene(scene,ScenePath)","CloseScene(scene,true)"):
            self.assertIn(word,s)
    def test_10_six_native_playmode_tests_authored_not_claimed_run(self):
        s=source(A+"/Tests/PlayMode/AirlockNativePlayModeTests.cs").decode()
        self.assertEqual(6,s.count("[UnityTest]"))
        self.assertIn("EmergencyCannotOpenBothPhysicalDoors",s)
    def test_11_core_invariant_and_actual_negative_test_logic(self):
        c=source(R+"/AirlockCore.cs").decode()
        s=source(R+"/Program.cs").decode()
        for word in ("a>0 && b>0","RecoveryCloseExit","EntryObstructed",
                     "ExitObstructed","EmergencyRequests"):
            self.assertIn(word,c)
        for word in ("NO_DOUBLE_GATE_OPEN_DURING_NORMAL_CYCLE",
                     "EMERGENCY_CANNOT_CRUSH_OCCUPIED_EXIT",
                     "INVALID_PAUSED_FRAME_FAILS_ATOMIC",
                     "EMERGENCY_WITH_OPEN_EXIT_CLOSES_EXIT_FIRST"):
            self.assertIn(word,s)
    def test_12_original_csharp9_source_both_runtime_test_project_warnings_errors(self):
        for path in (R+"/AirlockRuntime.csproj",R+"/AirlockTests.csproj"):
            t=source(path).decode()
            self.assertIn("<LangVersion>9.0</LangVersion>",t)
            self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>",t)
    def test_13_real_original_unity_source_zip_reproducible(self):
        with tempfile.TemporaryDirectory() as d:
            a=pathlib.Path(d)/"one.zip";b=pathlib.Path(d)/"two.zip"
            self.assertEqual(source_archive(a),source_archive(b))
            self.assertEqual(a.read_bytes(),b.read_bytes())
    def test_14_exact_scratch_rollback_preserves_foreign_empty_and_occupied(self):
        with tempfile.TemporaryDirectory() as d:
            root=pathlib.Path(d);arc=root/"original.zip";source_archive(arc)
            other=root/"FOREIGN_EMPTY";other.mkdir()
            keep=root/"external"/"KEEP.txt";keep.parent.mkdir();keep.write_text("keep")
            with zipfile.ZipFile(arc) as z:
                names=z.namelist();self.assertEqual(20,len(names))
                for name in names:
                    allow(name)
                    f=root/name;self.assertFalse(f.exists())
                    f.parent.mkdir(parents=True,exist_ok=True)
                    f.write_bytes(z.read(name));self.assertEqual(f.read_bytes(),z.read(name))
                for name in reversed(names):(root/name).unlink()
            self.assertTrue(other.is_dir());self.assertEqual([],list(other.iterdir()))
            self.assertEqual("keep",keep.read_text())
    def test_15_reject_foreign_owned_and_path_traversal(self):
        for name in ("../outside","/tmp/evil","Assets/R6/foreign.cs",
                     "Assets/B0125WeightLatch001/Runtime/WeightLatchCore.cs"):
            with self.assertRaises(AssertionError):allow(name)
    def test_16_tamper_fails_and_native_gate_truthful(self):
        name=R+"/AirlockCore.cs"
        self.assertNotEqual(blob(source(name)+b"mutated"),pins()[name])
        m=parsed(T+"/manifest.json")
        self.assertEqual("NOT_RUN",m["nativeUnity"])
        self.assertFalse(m["codexAck"]);self.assertFalse(m["integrated"])
        self.assertEqual(BASE,m["baseSha"])
def main():
    ap=argparse.ArgumentParser();ap.add_argument("--out",required=True)
    args=ap.parse_args();out=pathlib.Path(args.out);out.parent.mkdir(parents=True,exist_ok=True)
    r=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(OriginalQA))
    with tempfile.TemporaryDirectory() as root:
        arc=pathlib.Path(root)/"source.zip";sha=source_archive(arc)
        out.with_suffix(".zip").write_bytes(arc.read_bytes())
    receipt={"package":"B01-25-AIRLOCK001/R1","tests":r.testsRun,
       "passed":r.testsRun-len(r.failures)-len(r.errors),"zipSha256":sha,
       "nativeUnity":"NOT_RUN"}
    out.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
    print("AIRLOCK_ORIGINAL_SOURCE_QA",receipt["passed"],"/",receipt["tests"],
          "SOURCE_ZIP_SHA256",sha,"UNITY_NATIVE_NOT_RUN")
    raise SystemExit(not r.wasSuccessful())
if __name__=="__main__":main()
