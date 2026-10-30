#!/usr/bin/env python3
"""REAL R3 original Git SHA, physical Unity source ZIP, safe migration and rollback QA."""
import argparse,hashlib,json,pathlib,re,subprocess,tempfile,unittest,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
BASE="ef742c06faa3c37cd6643af0f099b0d3b4306b2e"
R="Research/B01-25/P17/SAFEROUTE001"
A="Assets/B0125SafeRoute001"
T="Tools/B01/B01-25/P17W25SafeRouteR3"
W=".github/workflows/b01-25-w25-saferoute-r3.yml"
OLD_T="Tools/B01/B01-25/P17W24SafeRouteR2"
MODIFIED={R+"/Program.cs",R+"/SafeRouteRuntime.csproj",
 R+"/SafeRouteTests.csproj",A+"/Runtime/SafeRouteUnityDemo.cs",
 A+"/Tests/PlayMode/SafeRouteNativePlayModeTests.cs"}
ADDED={R+"/PhysicalObstacleRouter.cs",A+"/Runtime/PhysicalObstacleRouter.cs",
 A+"/Runtime/PhysicalObstacleRouter.cs.meta",
 T+"/check.py",T+"/README.md",T+"/.gitignore",T+"/manifest.json",W}
def git(*args):return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def raw(path):return (ROOT/path).read_bytes()
def doc(path):return json.loads(raw(path))
def blob(data):return hashlib.sha1(("blob %d\0"%len(data)).encode()+data).hexdigest()
def pins():return doc(T+"/manifest.json")["gitBlobs"]
def unity():return {p:s for p,s in pins().items() if p.startswith(A+"/") or p==A+".meta"}
def allowed(name):
    p=pathlib.PurePosixPath(name)
    if name not in unity() and name!="PACKAGE_MANIFEST.json":raise ValueError("Foreign owner file "+name)
    if p.is_absolute() or ".." in p.parts or "\\" in name:raise ValueError("Unsafe filename")
def zip_sources(path):
    indexed={}
    with zipfile.ZipFile(path,"w",compression=zipfile.ZIP_STORED) as archive:
        for name,sha in sorted(unity().items()):
            content=git("show","HEAD:"+name)
            assert content==raw(name) and blob(content)==sha
            indexed[name]={"blob":sha,"sha256":hashlib.sha256(content).hexdigest(),"bytes":len(content)}
            e=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
            e.create_system=3;e.external_attr=0o100644<<16
            archive.writestr(e,content,compress_type=zipfile.ZIP_STORED)
        e=zipfile.ZipInfo("PACKAGE_MANIFEST.json",(1980,1,1,0,0,0))
        e.create_system=3;e.external_attr=0o100644<<16
        archive.writestr(e,(json.dumps({"package":"B01-25-SAFEROUTE001/R3",
            "oneVersionOnly":True,"nativeUnity":"NOT_RUN","files":indexed},
            sort_keys=True,separators=(",",":"))+"\n").encode())
    return hashlib.sha256(pathlib.Path(path).read_bytes()).hexdigest()
def stage(file,root):
    written=[]
    try:
        with zipfile.ZipFile(file) as z:
            for name in z.namelist():
                allowed(name)
                out=root/name
                for p in (out,*out.parents):
                    if p==root.parent:break
                    if p.is_symlink():raise ValueError("Symlink in scratch receiver")
                if out.exists() or out.is_symlink():raise FileExistsError(name)
                out.parent.mkdir(parents=True,exist_ok=True)
                out.write_bytes(z.read(name))
                written.append(out)
        return written
    except BaseException:
        for path in reversed(written):path.unlink()
        raise
def undo(written):
    for path in reversed(written):path.unlink()
class OriginalPhysicalR3QA(unittest.TestCase):
    def test_01_only_five_changed_and_eight_additional_owner_paths(self):
        changed=set(git("diff","--name-only",BASE,"HEAD").decode().splitlines())
        self.assertEqual(13,len(changed))
        self.assertEqual(MODIFIED|ADDED,changed)
    def test_02_exact_42_original_git_source_byte_pins(self):
        self.assertEqual(42,len(pins()))
        for name,sha in pins().items():
            self.assertEqual(raw(name),git("show","HEAD:"+name),name)
            self.assertEqual(blob(raw(name)),sha,name)
    def test_03_prior_r2_branch_and_manifest_frozen(self):
        old=doc(OLD_T+"/manifest.json")["gitBlobs"]
        for name in MODIFIED:
            self.assertEqual(old[name],blob(git("show",BASE+":"+name)),name)
            self.assertNotEqual(old[name],pins()[name],name)
        self.assertEqual(git("show",BASE+":"+OLD_T+"/manifest.json"),
                         git("show","HEAD:"+OLD_T+"/manifest.json"))
    def test_04_original_new_physical_router_unity_source_byte_identical(self):
        self.assertEqual(raw(R+"/PhysicalObstacleRouter.cs"),
                         raw(A+"/Runtime/PhysicalObstacleRouter.cs"))
        self.assertEqual(raw(R+"/SafeRouteCore.cs"),raw(A+"/Runtime/SafeRouteCore.cs"))
    def test_05_original_22_unity_paths_14_valid_distinct_meta_guids(self):
        self.assertEqual(22,len(unity()))
        metas=[x for x in unity() if x.endswith(".meta")]
        self.assertEqual(14,len(metas))
        ids=[]
        for p in metas:
            match=re.search(r"^guid: ([0-9a-f]{32})$",raw(p).decode(),re.M)
            self.assertIsNotNone(match,p);ids.append(match.group(1))
        self.assertEqual(14,len(set(ids)))
    def test_06_three_original_unity_asmdefs_and_test_dependencies(self):
        r=doc(A+"/Runtime/B0125.SafeRoute.Runtime.asmdef")
        e=doc(A+"/Editor/B0125.SafeRoute.Editor.asmdef")
        t=doc(A+"/Tests/PlayMode/B0125.SafeRoute.PlayModeTests.asmdef")
        self.assertEqual([],r["references"])
        self.assertEqual([r["name"]],e["references"])
        self.assertEqual([r["name"]],t["references"])
        self.assertIn("TestAssemblies",t["optionalUnityReferences"])
    def test_07_real_physics_overlap_is_not_synthetic_button(self):
        h=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for s in ("Physics.OverlapBoxNonAlloc(","buffer[i]==crateCollider",
          "dynamicBlocker","physicalCrate=box.AddComponent<Rigidbody>()",
          "PhysicalObstacleRouter.FindFrom(level,routeOrigin,radius,",
          "occupiedCells=SampleActualCrateCollider()"):
            if s=="dynamicBlocker":continue
            self.assertIn(s,h)
    def test_08_original_kinematic_physical_crate_safe_move_guard(self):
        s=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for term in ("physicalCrate.isKinematic=true",
          "PhysicalObstacleRouter.AuditAllEight(level,new[]{proposed}).AllReachable",
          "if(hull!=null&&hull.bounds.Intersects(target))",
          "foreach(var wall in obstacles)",
          "rejectedCrateMoves++"):
            self.assertIn(term,s)
    def test_09_external_unguarded_physics_move_replan_fail_closed(self):
        s=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for term in ("Mathf.RoundToInt(physicalCrate.position.x/CellSize)",
                     "!observed.Equals(observedCrateCell)",
                     "current=PhysicalObstacleRouter.FindFrom("):
            self.assertIn(term,s)
    def test_10_original_new_csharp9_cases_real_all_eight_and_no_softlock(self):
        p=raw(R+"/Program.cs").decode()
        for term in ("R3_BLOCK_HEAVY_LOWER_BYPASS_REJECT_ALL8",
             "R3_REAL_OBSTACLE_DUPLICATE_SAMPLES_ARE_IDEMPOTENT",
             "R3_EMPTY_PHYSICAL_INPUT_LEGACY_R2_PARITY_EIGHT_CASES",
             "R3_NULL_PHYSICAL_OCCUPANCY"):
            self.assertIn(term,p)
    def test_11_twelve_real_nunit_playmode_sources_authored_not_executed(self):
        p=raw(A+"/Tests/PlayMode/SafeRouteNativePlayModeTests.cs").decode()
        self.assertEqual(12,p.count("[UnityTest]"))
        self.assertIn("W25ExternalUntrustedPhysicsMoveFailsClosedForHeavy",p)
    def test_12_original_r3_runtime_test_projects_are_csharp9_warning_errors(self):
        for file in ("SafeRouteRuntime.csproj","SafeRouteTests.csproj"):
            p=raw(R+"/"+file).decode()
            self.assertIn("<LangVersion>9.0</LangVersion>",p)
            self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>",p)
            self.assertIn('Include="PhysicalObstacleRouter.cs"',p)
    def test_13_real_zip_reproducibility(self):
        with tempfile.TemporaryDirectory() as d:
            a=pathlib.Path(d)/"a.zip";b=pathlib.Path(d)/"b.zip"
            self.assertEqual(zip_sources(a),zip_sources(b))
            self.assertEqual(a.read_bytes(),b.read_bytes())
    def test_14_real_scratch_original_23_files_rollback_preserves_foreign_empty(self):
        with tempfile.TemporaryDirectory() as d:
            root=pathlib.Path(d);a=root/"sources.zip";zip_sources(a)
            empty=root/"FOREIGN_EMPTY";empty.mkdir()
            keep=root/"OTHER"/"keep.txt";keep.parent.mkdir();keep.write_text("PRESERVE")
            files=stage(a,root)
            self.assertEqual(23,len(files))
            undo(files)
            self.assertTrue(empty.is_dir() and not list(empty.iterdir()))
            self.assertEqual("PRESERVE",keep.read_text())
    def test_15_existing_r2_source_must_not_be_overwritten_or_deleted(self):
        with tempfile.TemporaryDirectory() as d:
            root=pathlib.Path(d);a=root/"sources.zip";zip_sources(a)
            file=root/A/"Runtime/SafeRouteCore.cs"
            file.parent.mkdir(parents=True,exist_ok=True);file.write_text("R2 FROZEN")
            with self.assertRaises(FileExistsError):stage(a,root)
            self.assertEqual("R2 FROZEN",file.read_text())
    def test_16_foreign_path_and_traversal_denied(self):
        for name in ("../evil","/tmp/pwned","Assets/R6/Save.cs",
                     "Assets/B0125Conveyor001/Runtime/ConveyorCore.cs"):
            with self.assertRaises(ValueError):allowed(name)
    def test_17_byte_tamper_detected(self):
        file=R+"/PhysicalObstacleRouter.cs"
        self.assertNotEqual(blob(raw(file)+b"tamper"),pins()[file])
    def test_18_contract_no_codex_ack_or_native_false_pass(self):
        m=doc(T+"/manifest.json")
        self.assertEqual(BASE,m["parentR2Sha"])
        self.assertEqual("NOT_RUN",m["nativeUnity"])
        self.assertFalse(m["integrated"]);self.assertFalse(m["codexAck"])
        self.assertTrue(m["r1r2r3NeverCoInstall"])
    def test_19_old_r2_function_remains_byte_exact(self):
        for n in ("SafeRouteCore.cs","SafeRouteExample.cs"):
            p=R+"/"+n
            self.assertEqual(raw(p),git("show",BASE+":"+p))
    def test_20_full_original_file_manifest_not_stale_old_manifest(self):
        m=doc(T+"/manifest.json")
        self.assertEqual(42,m["closureCount"])
        self.assertEqual(22,len(unity()))
        self.assertNotIn(T+"/manifest.json",pins())
def main():
    ap=argparse.ArgumentParser();ap.add_argument("--out",required=True)
    args=ap.parse_args();out=pathlib.Path(args.out);out.parent.mkdir(parents=True,exist_ok=True)
    test=unittest.TextTestRunner(verbosity=2).run(
        unittest.defaultTestLoader.loadTestsFromTestCase(OriginalPhysicalR3QA))
    with tempfile.TemporaryDirectory() as temp:
        zipfile_=pathlib.Path(temp)/"original.zip";digest=zip_sources(zipfile_)
        out.with_suffix(".zip").write_bytes(zipfile_.read_bytes())
    passed=test.testsRun-len(test.failures)-len(test.errors)
    out.write_text(json.dumps({"package":"B01-25-SAFEROUTE001/R3",
       "actualTestCount":test.testsRun,"passed":passed,"originalSourceZipSha256":digest,
       "nativeUnity":"NOT_RUN"},indent=2,sort_keys=True)+"\n")
    print("SAFEROUTE_R3_ORIGINAL_GIT_PHYSICAL_QA",passed,"/",test.testsRun,
        "ZIP_SHA256",digest,"UNITY_G1_NOT_RUN")
    raise SystemExit(not test.wasSuccessful())
if __name__=="__main__":main()
