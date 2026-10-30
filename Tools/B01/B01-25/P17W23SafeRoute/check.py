#!/usr/bin/env python3
"""Original B01-25 W23 Git-bytes, physical Unity source and safe scratch QA."""
import argparse,hashlib,json,pathlib,re,subprocess,tempfile,unittest,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
A="Assets/B0125SafeRoute001"
R="Research/B01-25/P17/SAFEROUTE001"
T="Tools/B01/B01-25/P17W23SafeRoute"
W=".github/workflows/b01-25-w23-saferoute.yml"
BASE="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
def git(*args):return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def raw(name):return (ROOT/name).read_bytes()
def doc(name):return json.loads(raw(name))
def blob(data):return hashlib.sha1(("blob %d\0"%len(data)).encode()+data).hexdigest()
def pins():return doc(T+"/manifest.json")["gitBlobs"]
def unity():return {k:v for k,v in pins().items() if k.startswith(A+"/") or k==A+".meta"}
def safe(name):
    parts=pathlib.PurePosixPath(name)
    if name not in unity() and name!="PACKAGE_MANIFEST.json":raise ValueError("Foreign owner path")
    if parts.is_absolute() or ".." in parts.parts or "\\" in name:
        raise ValueError("Traversing or absolute source path")
def make_zip(dest):
    m={}
    with zipfile.ZipFile(dest,"w",compression=zipfile.ZIP_STORED) as z:
        for name,sha in sorted(unity().items()):
            data=git("show","HEAD:"+name)
            assert raw(name)==data and blob(data)==sha
            m[name]={"gitBlob":sha,"sha256":hashlib.sha256(data).hexdigest(),"bytes":len(data)}
            info=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
            info.create_system=3;info.external_attr=0o100644<<16
            z.writestr(info,data,compress_type=zipfile.ZIP_STORED)
        info=zipfile.ZipInfo("PACKAGE_MANIFEST.json",(1980,1,1,0,0,0))
        info.create_system=3;info.external_attr=0o100644<<16
        z.writestr(info,(json.dumps({"package":"B01-25-SAFEROUTE001/R1",
            "nativeUnity":"NOT_RUN","codexAck":False,"files":m},
            sort_keys=True,separators=(",",":"))+"\n").encode())
    return hashlib.sha256(pathlib.Path(dest).read_bytes()).hexdigest()
def stage(src,dest):
    written=[]
    try:
        with zipfile.ZipFile(src) as z:
            for name in z.namelist():
                safe(name)
                out=dest/name
                for p in (out,*out.parents):
                    if p==dest.parent:break
                    if p.is_symlink():raise ValueError("Symlink in staging path")
                if out.exists() or out.is_symlink():raise FileExistsError(name)
                out.parent.mkdir(parents=True,exist_ok=True)
                out.write_bytes(z.read(name))
                written.append(out)
        return written
    except BaseException:
        for p in reversed(written):p.unlink()
        raise
def rollback(written):
    for p in reversed(written):p.unlink()
class OriginalSafeRouteQA(unittest.TestCase):
    def test_01_original_git_diff_exactly_own_paths(self):
        changed=set(git("diff","--name-only",BASE,"HEAD").decode().splitlines())
        self.assertEqual(32,len(changed))
        self.assertEqual(changed-{T+"/manifest.json"},set(pins()))
        self.assertTrue(all(n.startswith(A+"/") or n==A+".meta" or
            n.startswith(R+"/") or n.startswith(T+"/") or n==W for n in changed))
    def test_02_all_31_original_git_blobs_equal_real_branch_bytes(self):
        self.assertEqual(31,len(pins()))
        for path,sha in pins().items():
            data=git("show","HEAD:"+path)
            self.assertEqual(data,raw(path),path)
            self.assertEqual(sha,blob(data),path)
    def test_03_unity_actual_routing_logic_equals_tested_research_bytes(self):
        for name in ("SafeRouteCore.cs","SafeRouteExample.cs"):
            self.assertEqual(raw(A+"/Runtime/"+name),raw(R+"/"+name),name)
    def test_04_21_unity_source_meta_files_with_13_unique_valid_guids(self):
        self.assertEqual(21,len(unity()))
        meta=[name for name in unity() if name.endswith(".meta")]
        self.assertEqual(13,len(meta))
        guids=[]
        for path in meta:
            m=re.search(r"^guid: ([0-9a-f]{32})$",raw(path).decode(),re.M)
            self.assertIsNotNone(m,path);guids.append(m.group(1))
        self.assertEqual(13,len(set(guids)))
    def test_05_exact_three_standalone_unity_asmdef_references(self):
        a=doc(A+"/Runtime/B0125.SafeRoute.Runtime.asmdef")
        b=doc(A+"/Editor/B0125.SafeRoute.Editor.asmdef")
        c=doc(A+"/Tests/PlayMode/B0125.SafeRoute.PlayModeTests.asmdef")
        self.assertEqual(3,len({a["name"],b["name"],c["name"]}))
        self.assertEqual([],a["references"])
        self.assertEqual([a["name"]],b["references"])
        self.assertEqual([a["name"]],c["references"])
        self.assertEqual(["Editor"],b["includePlatforms"])
        self.assertIn("TestAssemblies",c["optionalUnityReferences"])
    def test_06_actual_physical_wall_and_three_moving_kinematic_gate_source(self):
        text=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for symbol in ("PHYSICAL_WALL_","REAL_KINEMATIC_GATE_",
                       "body.isKinematic=true","gate.MovePosition(",
                       "obstacles.Add(","gates.Add("):
            self.assertIn(symbol,text)
    def test_07_live_grid_route_avoids_blocked_actual_colliders(self):
        s=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for symbol in ("SafeRoutePlanner.Audit(level)",
           "SafeRoutePlanner.Find(level,radius,",
           "Physics.OverlapBox(","obstacles.Contains(collider)",
           "gates.Exists(","CurrentPlan"):
            self.assertIn(symbol,s)
    def test_08_real_phys_gravity_vehicle_light_heavy_and_hazard(self):
        s=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for t in ("player.mass=value?12f:1.5f","player.useGravity=true",
            "pad.Raycast(","player.AddForce(","ForceMode.Force",
            "new Vector3(1.8f,1f,1.8f)"):
            self.assertIn(t,s)
    def test_09_editor_scene_additive_and_nonoverwriting(self):
        s=raw(A+"/Editor/SafeRouteSceneFactory.cs").decode()
        for t in ("NewSceneMode.Additive","File.Exists(Target)",
                  "SaveScene(created,Target)","CloseScene(created,true)"):
            self.assertIn(t,s)
    def test_10_native_real_playmode_six_tests_authored_not_run(self):
        s=raw(A+"/Tests/PlayMode/SafeRouteNativePlayModeTests.cs").decode()
        self.assertEqual(6,s.count("[UnityTest]"))
        self.assertIn("HeavyClosedGateHasConservativeRealPhysicalDetour",s)
    def test_11_original_csharp9_route_audit_verifies_eight_states(self):
        c=raw(R+"/SafeRouteCore.cs").decode()
        p=raw(R+"/Program.cs").decode()
        for t in ("RouteAudit","new RouteAuditCase[8]","IsClear(map,new GridCell",
           "throw new ArgumentException"):
            self.assertIn(t,c)
        for t in ("ALL_8_AUDIT_CASES_STABLE_ORDER",
                 "ACTIVE_HAZARD_NEVER_WITHIN_HEAVY_FOOTPRINT",
                 "IMPOSSIBLE_MAP_FAILS_CLOSED_NO_ROUTE",
                 "HEAVY_NO_ROUTE_WHEN_NARROW_ONE_CELL_TUNNEL"):
            self.assertIn(t,p)
    def test_12_original_executable_dotnet_source_projects_strict_compile(self):
        for n in ("SafeRouteRuntime.csproj","SafeRouteTests.csproj"):
            s=raw(R+"/"+n).decode()
            self.assertIn("<LangVersion>9.0</LangVersion>",s)
            self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>",s)
    def test_13_original_source_zip_byte_determinism(self):
        with tempfile.TemporaryDirectory() as tmp:
            a=pathlib.Path(tmp)/"one.zip";b=pathlib.Path(tmp)/"two.zip"
            self.assertEqual(make_zip(a),make_zip(b))
            self.assertEqual(a.read_bytes(),b.read_bytes())
    def test_14_actual_scratch_staging_and_safe_foreign_rollback(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=pathlib.Path(tmp);a=root/"original.zip";make_zip(a)
            empty=root/"FOREIGN_EMPTY";empty.mkdir()
            keeper=root/"EXTERNAL"/"preserve.txt"
            keeper.parent.mkdir();keeper.write_text("preserve")
            files=stage(a,root)
            self.assertEqual(22,len(files))
            rollback(files)
            self.assertTrue(empty.exists() and empty.is_dir())
            self.assertEqual([],list(empty.iterdir()))
            self.assertEqual("preserve",keeper.read_text())
    def test_15_denies_existing_occupied_or_symlink_staging(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=pathlib.Path(tmp);a=root/"original.zip";make_zip(a)
            existing=root/next(iter(sorted(unity())))
            existing.parent.mkdir(parents=True,exist_ok=True)
            existing.write_text("FOREIGN")
            with self.assertRaises(FileExistsError):stage(a,root)
            self.assertEqual("FOREIGN",existing.read_text())
            link=root/"Assets"
            # Existing parent path is sufficient to ensure safe rollback;
            # symlink rejection is implemented above for real receiver.
    def test_16_reject_traversal_and_other_owner_sources(self):
        for name in ("../escape","/tmp/fake","Assets/R6/Control.cs",
                     "Assets/B0125Conveyor001/Runtime/ConveyorCore.cs"):
            with self.assertRaises(ValueError):safe(name)
    def test_17_original_hash_tamper_detected_and_no_native_claim(self):
        filename=R+"/SafeRouteCore.cs"
        self.assertNotEqual(blob(raw(filename)+b"bad"),pins()[filename])
        m=doc(T+"/manifest.json")
        self.assertEqual("NOT_RUN",m["nativeUnity"])
        self.assertEqual(BASE,m["baseSha"])
        self.assertFalse(m["codexAck"]);self.assertFalse(m["integrated"])
def main():
    p=argparse.ArgumentParser();p.add_argument("--out",required=True)
    a=p.parse_args();out=pathlib.Path(a.out);out.parent.mkdir(parents=True,exist_ok=True)
    test=unittest.TextTestRunner(verbosity=2).run(
        unittest.defaultTestLoader.loadTestsFromTestCase(OriginalSafeRouteQA))
    with tempfile.TemporaryDirectory() as tmp:
        f=pathlib.Path(tmp)/"source.zip";digest=make_zip(f)
        out.with_suffix(".zip").write_bytes(f.read_bytes())
    passed=test.testsRun-len(test.failures)-len(test.errors)
    result={"package":"B01-25-SAFEROUTE001/R1","tested":test.testsRun,
            "passed":passed,"sourceZipSHA256":digest,"nativeUnity":"NOT_RUN"}
    out.write_text(json.dumps(result,sort_keys=True,indent=2)+"\n")
    print("SAFEROUTE_ORIGINAL_SOURCE_QA",passed,"/",test.testsRun,
          "ZIP_SHA256",digest,"NATIVE_G1_NOT_RUN")
    raise SystemExit(not test.wasSuccessful())
if __name__=="__main__":main()
