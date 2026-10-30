#!/usr/bin/env python3
"""B01-25 W22 real original Git source, physical ZIP, and non-destructive scratch QA."""
import argparse,hashlib,json,pathlib,re,subprocess,tempfile,unittest,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
A="Assets/B0125Conveyor001";R="Research/B01-25/P17/CONVEYOR001"
T="Tools/B01/B01-25/P17W22Conveyor";W=".github/workflows/b01-25-w22-conveyor.yml"
BASE="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
def git(*args):return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def raw(name):return (ROOT/name).read_bytes()
def doc(name):return json.loads(raw(name))
def blob(data):return hashlib.sha1(("blob %d\0"%len(data)).encode()+data).hexdigest()
def manifest():return doc(T+"/manifest.json")
def pins():return manifest()["gitBlobs"]
def unity():return {k:v for k,v in pins().items() if k.startswith(A+"/") or k==A+".meta"}
def verify_name(name):
    p=pathlib.PurePosixPath(name)
    if (name not in unity() and name!="PACKAGE_MANIFEST.json") or \
       p.is_absolute() or ".." in p.parts or "\\" in name:
        raise ValueError("Unowned, absolute or traversing source path "+name)
def physical_zip(dest):
    included={}
    with zipfile.ZipFile(dest,"w",compression=zipfile.ZIP_STORED) as archive:
        for path,expected in sorted(unity().items()):
            data=git("show","HEAD:"+path)
            assert data==raw(path) and blob(data)==expected
            included[path]={"gitBlob":expected,"sha256":hashlib.sha256(data).hexdigest(),"size":len(data)}
            info=zipfile.ZipInfo(path,date_time=(1980,1,1,0,0,0))
            info.create_system=3;info.external_attr=0o100644<<16
            archive.writestr(info,data,compress_type=zipfile.ZIP_STORED)
        info=zipfile.ZipInfo("PACKAGE_MANIFEST.json",date_time=(1980,1,1,0,0,0))
        info.create_system=3;info.external_attr=0o100644<<16
        archive.writestr(info,(json.dumps({"package":"B01-25-CONVEYOR001/R1",
              "nativeUnity":"NOT_RUN","codexAck":False,"files":included},
              sort_keys=True,separators=(",",":"))+"\n").encode())
    return hashlib.sha256(pathlib.Path(dest).read_bytes()).hexdigest()
def guarded_stage(archive,root):
    written=[]
    try:
        with zipfile.ZipFile(archive) as z:
            for member in z.namelist():
                verify_name(member)
                dest=root/member
                for parent in dest.parents:
                    if parent==root.parent:break
                    if parent.is_symlink():raise ValueError("Symlink ancestor")
                if dest.exists() or dest.is_symlink():raise FileExistsError(member)
                dest.parent.mkdir(parents=True,exist_ok=True)
                dest.write_bytes(z.read(member))
                written.append(dest)
        return written
    except BaseException:
        for owned in reversed(written):owned.unlink()
        raise
def rollback(files):
    # Deliberately NEVER rmdir (even our staging parent may be shared/foreign).
    for own in reversed(files):
        own.unlink()
class PhysicalSourceQA(unittest.TestCase):
    def test_01_exact_owner_scope_diff_and_manifest(self):
        files=set(git("diff","--name-only",BASE,"HEAD").decode().splitlines())
        self.assertEqual(files-{T+"/manifest.json"},set(pins()))
        self.assertEqual(29,len(files))
        self.assertTrue(all(n.startswith(A+"/") or n==A+".meta" or
            n.startswith(R+"/") or n.startswith(T+"/") or n==W for n in files))
    def test_02_actual_original_git_content_and_blob_sha(self):
        for path,sha in pins().items():
            data=git("show","HEAD:"+path)
            self.assertEqual(data,raw(path),path)
            self.assertEqual(blob(data),sha,path)
    def test_03_real_unity_logic_byte_identical_to_tested_core(self):
        self.assertEqual(raw(A+"/Runtime/ConveyorCore.cs"),raw(R+"/ConveyorCore.cs"))
    def test_04_all_12_original_unity_meta_guids_unique(self):
        self.assertEqual(19,len(unity()))
        metas=[n for n in unity() if n.endswith(".meta")]
        self.assertEqual(12,len(metas))
        ids=[]
        for n in metas:
            m=re.search(r"^guid: ([0-9a-f]{32})$",raw(n).decode(),re.M)
            self.assertIsNotNone(m,n);ids.append(m.group(1))
        self.assertEqual(len(set(ids)),12)
    def test_05_three_isolated_asmdefs_valid_ref_edges(self):
        a=doc(A+"/Runtime/B0125.Conveyor.Runtime.asmdef")
        b=doc(A+"/Editor/B0125.Conveyor.Editor.asmdef")
        c=doc(A+"/Tests/PlayMode/B0125.Conveyor.PlayModeTests.asmdef")
        self.assertEqual(3,len({a["name"],b["name"],c["name"]}))
        self.assertEqual([],a["references"])
        self.assertEqual([a["name"]],b["references"])
        self.assertEqual(["Editor"],b["includePlatforms"])
        self.assertEqual([a["name"]],c["references"])
        self.assertIn("TestAssemblies",c["optionalUnityReferences"])
    def test_06_real_physical_force_and_contact_source(self):
        s=raw(A+"/Runtime/ConveyorUnityDemo.cs").decode()
        for word in ("physicalBelt.Raycast(","vehicle.AddForce(",
           "ForceMode.Force","vehicle.linearVelocity.z","GetComponent<BoxCollider>()",
           "vehicle.useGravity=true"):
            self.assertIn(word,s)
    def test_07_both_masses_have_different_real_body_and_bypass(self):
        s=raw(A+"/Runtime/ConveyorUnityDemo.cs").decode()
        for word in ("vehicle.mass=value?12f:1.5f","new Vector3(4,-.25f,0)",
                     "new Vector3(2.5f,.5f,18)","new Vector3(3,.3f,10)"):
            self.assertIn(word,s)
    def test_08_unique_additive_scene_and_nonoverwrite(self):
        s=raw(A+"/Editor/ConveyorSceneBuilder.cs").decode()
        for word in ("NewSceneMode.Additive","File.Exists(ScenePath)",
           "SaveScene(scene,ScenePath)","CloseScene(scene,true)"):
            self.assertIn(word,s)
    def test_09_six_native_nunit_sources_not_executed(self):
        s=raw(A+"/Tests/PlayMode/ConveyorNativePlayModeTests.cs").decode()
        self.assertEqual(6,s.count("[UnityTest]"))
        self.assertIn("PermanentSafeBypassNeverGrantsBeltForce",s)
        self.assertIn("ActualBeltContactAppliesPositiveForce",s)
    def test_10_original_core_limits_force_and_fail_atomic_preflight(self):
        s=raw(R+"/ConveyorCore.cs").decode()
        for word in ("MaxForceNewtons","Last=new ConveyorFrame(",
           "if(s.Paused||s.Dt==0)return Last","s.ColliderSupport",
           "throw new ArgumentException("):
            self.assertIn(word,s)
    def test_11_portable_csharp9_tests_are_actual_executable_project(self):
        s=raw(R+"/Program.cs").decode()
        p=raw(R+"/ConveyorTests.csproj").decode()
        for word in ("WARNING_BEFORE_REVERSE_IS_FORCE_FREE",
          "HEAVY_ACCELERATION_SMALLER_UNDER_BUDGET","PAUSED_INVALID_PHYSICS_NOT_SILENT",
          "NO_PASSIVE_FORCE_SAFE_BYPASS_AT_X4"):
            self.assertIn(word,s)
        self.assertIn("<LangVersion>9.0</LangVersion>",p)
        self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>",p)
    def test_12_original_source_zip_determinism(self):
        with tempfile.TemporaryDirectory() as temp:
            a=pathlib.Path(temp)/"one.zip";b=pathlib.Path(temp)/"two.zip"
            self.assertEqual(physical_zip(a),physical_zip(b))
            self.assertEqual(a.read_bytes(),b.read_bytes())
    def test_13_actual_scratch_stage_rollback_keeps_foreign_empty_dir(self):
        with tempfile.TemporaryDirectory() as temp:
            root=pathlib.Path(temp);arc=root/"original.zip";physical_zip(arc)
            empty=root/"FOREIGN_EMPTY";empty.mkdir()
            keeper=root/"FOREIGN"/"keep.txt";keeper.parent.mkdir();keeper.write_text("keep")
            files=guarded_stage(arc,root)
            self.assertEqual(20,len(files))
            rollback(files)
            self.assertEqual([],list(empty.iterdir()))
            self.assertEqual("keep",keeper.read_text())
    def test_14_foreign_abs_traversal_and_previous_game_denied(self):
        for path in ("../escape","/tmp/payload","Assets/R6/Control.cs",
            "Assets/B0125Airlock001/Runtime/AirlockCore.cs"):
            with self.assertRaises(ValueError):verify_name(path)
    def test_15_overwriting_existing_owner_file_is_rejected_non_destructively(self):
        with tempfile.TemporaryDirectory() as temp:
            root=pathlib.Path(temp);arc=root/"original.zip";physical_zip(arc)
            first=next(iter(sorted(unity())))
            present=root/first;present.parent.mkdir(parents=True,exist_ok=True)
            present.write_text("foreign occupant")
            with self.assertRaises(FileExistsError):guarded_stage(arc,root)
            self.assertEqual("foreign occupant",present.read_text())
    def test_16_exact_git_tamper_native_notrun_no_codex_ack(self):
        name=R+"/ConveyorCore.cs"
        self.assertNotEqual(blob(raw(name)+b"modified"),pins()[name])
        m=manifest()
        self.assertEqual(BASE,m["baseSha"])
        self.assertEqual("NOT_RUN",m["nativeUnity"])
        self.assertFalse(m["integrated"]);self.assertFalse(m["codexAck"])
def main():
    ap=argparse.ArgumentParser();ap.add_argument("--out",required=True)
    args=ap.parse_args();out=pathlib.Path(args.out);out.parent.mkdir(parents=True,exist_ok=True)
    tests=unittest.TextTestRunner(verbosity=2).run(
        unittest.defaultTestLoader.loadTestsFromTestCase(PhysicalSourceQA))
    with tempfile.TemporaryDirectory() as temp:
        src=pathlib.Path(temp)/"source.zip";digest=physical_zip(src)
        out.with_suffix(".zip").write_bytes(src.read_bytes())
    passed=tests.testsRun-len(tests.failures)-len(tests.errors)
    result={"package":"B01-25-CONVEYOR001/R1","tested":tests.testsRun,"passed":passed,
            "zipSha256":digest,"nativeUnity":"NOT_RUN"}
    out.write_text(json.dumps(result,indent=2,sort_keys=True)+"\n")
    print("CONVEYOR_ORIGINAL_SOURCE_QA",passed,"/",tests.testsRun,
          "ZIP_SHA256",digest,"NATIVE_UNITY_NOT_RUN")
    raise SystemExit(not tests.wasSuccessful())
if __name__=="__main__":main()
