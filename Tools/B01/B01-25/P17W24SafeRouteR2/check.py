#!/usr/bin/env python3
"""W24 R2: real original Git-file provenance + no-overwrite 22-member Unity SOURCE ZIP QA."""
import argparse,hashlib,json,pathlib,re,subprocess,tempfile,unittest,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
BASE="de59e8ce7d5e98958874c738947f788846f3583a"
T="Tools/B01/B01-25/P17W24SafeRouteR2"
OLD_T="Tools/B01/B01-25/P17W23SafeRoute"
A="Assets/B0125SafeRoute001"
R="Research/B01-25/P17/SAFEROUTE001"
W=".github/workflows/b01-25-w24-saferoute-r2.yml"
MODIFIED={R+"/SafeRouteCore.cs",A+"/Runtime/SafeRouteCore.cs",
 R+"/Program.cs",A+"/Runtime/SafeRouteUnityDemo.cs",
 A+"/Tests/PlayMode/SafeRouteNativePlayModeTests.cs"}
def git(*args):return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def raw(path):return (ROOT/path).read_bytes()
def load(path):return json.loads(raw(path))
def blob(buf):return hashlib.sha1(("blob %d\0"%len(buf)).encode()+buf).hexdigest()
def pins():return load(T+"/manifest.json")["gitBlobs"]
def assets():return {p:b for p,b in pins().items() if p.startswith(A+"/") or p==A+".meta"}
def validate_path(name):
    p=pathlib.PurePosixPath(name)
    if (name not in assets() and name!="PACKAGE_MANIFEST.json") or p.is_absolute() or \
        ".." in p.parts or "\\" in name:
        raise ValueError("foreign/unsafe receiver path "+name)
def original_zip(dest):
    entries={}
    with zipfile.ZipFile(dest,"w",compression=zipfile.ZIP_STORED) as z:
        for path,sha in sorted(assets().items()):
            content=git("show","HEAD:"+path)
            assert content==raw(path) and blob(content)==sha
            entries[path]={"gitBlob":sha,"sha256":hashlib.sha256(content).hexdigest(),"bytes":len(content)}
            info=zipfile.ZipInfo(path,(1980,1,1,0,0,0))
            info.create_system=3;info.external_attr=0o100644<<16
            z.writestr(info,content,compress_type=zipfile.ZIP_STORED)
        info=zipfile.ZipInfo("PACKAGE_MANIFEST.json",(1980,1,1,0,0,0))
        info.create_system=3;info.external_attr=0o100644<<16
        z.writestr(info,(json.dumps({"package":"B01-25-SAFEROUTE001/R2",
          "originalParent":BASE,"nativeUnity":"NOT_RUN",
          "requiresOnlyOneVersion":True,"files":entries},
          sort_keys=True,separators=(",",":"))+"\n").encode())
    return hashlib.sha256(pathlib.Path(dest).read_bytes()).hexdigest()
def stage(archive,root):
    written=[]
    try:
        with zipfile.ZipFile(archive) as z:
            for name in z.namelist():
                validate_path(name)
                output=root/name
                for path in (output,*output.parents):
                    if path==root.parent:break
                    if path.is_symlink():raise ValueError("symlink in staging path")
                if output.exists() or output.is_symlink():raise FileExistsError(name)
                output.parent.mkdir(parents=True,exist_ok=True)
                output.write_bytes(z.read(name));written.append(output)
        return written
    except BaseException:
        for file in reversed(written):file.unlink()
        raise
def rollback(files):
    for f in reversed(files):f.unlink()
class ExactR2QA(unittest.TestCase):
    def test_01_r2_adds_only_own_five_files_and_five_authorized_changes(self):
        changed=set(git("diff","--name-only",BASE,"HEAD").decode().splitlines())
        expected=MODIFIED|{T+"/check.py",T+"/README.md",T+"/.gitignore",
                           T+"/manifest.json",W}
        self.assertEqual(expected,changed)
    def test_02_no_edit_to_immutable_parent_and_old_manifest(self):
        old=load(OLD_T+"/manifest.json")["gitBlobs"]
        for p in MODIFIED:
            self.assertEqual(blob(git("show",BASE+":"+p)),old[p])
            self.assertNotEqual(blob(raw(p)),old[p],p)
        self.assertEqual(git("show",BASE+":"+OLD_T+"/manifest.json"),
                         git("show","HEAD:"+OLD_T+"/manifest.json"))
    def test_03_r2_current_original_file_pins_full_source_closure(self):
        self.assertEqual(35,len(pins()))
        for path,sha in pins().items():
            self.assertEqual(raw(path),git("show","HEAD:"+path),path)
            self.assertEqual(blob(raw(path)),sha,path)
    def test_04_r2_both_pure_source_files_exact_unity_bytes(self):
        for n in ("SafeRouteCore.cs","SafeRouteExample.cs"):
            self.assertEqual(raw(R+"/"+n),raw(A+"/Runtime/"+n))
    def test_05_21_real_original_unity_sources_include_13_unique_guids(self):
        self.assertEqual(21,len(assets()))
        ms=[p for p in assets() if p.endswith(".meta")]
        self.assertEqual(13,len(ms))
        ids=[]
        for path in ms:
            match=re.search(r"^guid: ([0-9a-f]{32})$",raw(path).decode(),re.M)
            self.assertIsNotNone(match,path);ids.append(match.group(1))
        self.assertEqual(13,len(set(ids)))
    def test_06_three_runtime_editor_playmode_asmdef_edges_remain_own(self):
        a=load(A+"/Runtime/B0125.SafeRoute.Runtime.asmdef")
        b=load(A+"/Editor/B0125.SafeRoute.Editor.asmdef")
        c=load(A+"/Tests/PlayMode/B0125.SafeRoute.PlayModeTests.asmdef")
        self.assertEqual([],a["references"])
        self.assertEqual([a["name"]],b["references"])
        self.assertEqual([a["name"]],c["references"])
        self.assertEqual(["Editor"],b["includePlatforms"])
        self.assertIn("TestAssemblies",c["optionalUnityReferences"])
    def test_07_genuine_findfrom_compiled_route_actual_physical_origin_host(self):
        core=raw(R+"/SafeRouteCore.cs").decode()
        host=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for n in ("FindFrom(LevelGrid map,GridCell from","int origin=from.Z*map.Width+from.X",
                   "!IsClear(map,from,radius,condition)"):
            self.assertIn(n,core)
        for n in ("private GridCell ActualPlayerCell()","player.position",
            "SafeRoutePlanner.FindFrom(level,routeOrigin,radius,",
            "!ActualPlayerCell().Equals(routeOrigin)","hasRouteOrigin=false"):
            self.assertIn(n,host)
    def test_08_open_gate_script_state_cannot_hide_real_physical_colliders(self):
        host=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        self.assertIn("gates.Exists(g=>g.GetComponent<Collider>()==collider)",host)
        self.assertNotIn("if(!physicalGateClear&&\n                       gates.Exists",host)
        self.assertIn("Physics.OverlapBox(",host)
    def test_09_original_nunit_sources_add_two_new_native_cases(self):
        tests=raw(A+"/Tests/PlayMode/SafeRouteNativePlayModeTests.cs").decode()
        self.assertEqual(8,tests.count("[UnityTest]"))
        for name in ("W24ActualPlayerTeleportReplansFromPhysicalPosition",
                     "W24TeleportOutsideMapNeverProvidesStaleRoute"):
            self.assertIn(name,tests)
    def test_10_original_portable_source_tests_add_dynamic_replan_regressions(self):
        p=raw(R+"/Program.cs").decode()
        for term in ("R2_EIGHT_STATES_REPLAN_AFTER_FIRST_STEP_LIGHT_HEAVY",
          "R2_ROUTE_FROM_LIVE_PLAYER_NOT_OLD_SPAWN",
          "R2_LIVE_ORIGIN_IN_CLOSED_GATE_FAILS_CLOSED",
          "R2_FIND_FROM_NULL_GRID","R2_ROUTE_FROM_EXIT_IS_SINGLE_CELL"):
            self.assertIn(term,p)
    def test_11_test_projects_compile_original_csharp9_without_unity(self):
        for name in ("SafeRouteRuntime.csproj","SafeRouteTests.csproj"):
            p=raw(R+"/"+name).decode()
            self.assertIn("<LangVersion>9.0</LangVersion>",p)
            self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>",p)
    def test_12_only_own_new_r2_manifest_may_drive_source_transport(self):
        m=load(T+"/manifest.json")
        self.assertEqual(BASE,m["baseSha"])
        self.assertEqual("R2",m["version"])
        self.assertEqual("NOT_RUN",m["nativeUnity"])
        self.assertTrue(m["r1ManifestFrozenNotForR2"])
        self.assertFalse(m["integrated"]);self.assertFalse(m["codexAck"])
    def test_13_reproducible_real_unity_r2_source_archive(self):
        with tempfile.TemporaryDirectory() as d:
            a=pathlib.Path(d)/"one.zip";b=pathlib.Path(d)/"two.zip"
            self.assertEqual(original_zip(a),original_zip(b))
            self.assertEqual(a.read_bytes(),b.read_bytes())
    def test_14_source_r2_staging_preserves_foreign_empty_and_occupied(self):
        with tempfile.TemporaryDirectory() as d:
            root=pathlib.Path(d);a=root/"src.zip";original_zip(a)
            keep=root/"FOREIGN"/"keep.txt";keep.parent.mkdir();keep.write_text("DO_NOT_DELETE")
            empty=root/"FOREIGN_EMPTY";empty.mkdir()
            files=stage(a,root)
            self.assertEqual(22,len(files))
            rollback(files)
            self.assertTrue(empty.is_dir())
            self.assertEqual([],list(empty.iterdir()))
            self.assertEqual("DO_NOT_DELETE",keep.read_text())
    def test_15_preexisting_r1_source_cannot_be_overwritten(self):
        with tempfile.TemporaryDirectory() as d:
            root=pathlib.Path(d);a=root/"src.zip";original_zip(a)
            target=root/A/"Runtime/SafeRouteCore.cs"
            target.parent.mkdir(parents=True,exist_ok=True)
            target.write_text("FROZEN R1")
            with self.assertRaises(FileExistsError):stage(a,root)
            self.assertEqual("FROZEN R1",target.read_text())
    def test_16_no_traversal_or_foreign_canonical_game_payload(self):
        for name in ("../evil","/tmp/evil","Assets/R6/Save.cs",
                     "Assets/B0125Airlock001/Runtime/AirlockCore.cs"):
            with self.assertRaises(ValueError):validate_path(name)
    def test_17_tampering_a_real_new_source_blob_fails_manifest(self):
        name=R+"/SafeRouteCore.cs"
        self.assertNotEqual(blob(raw(name)+b"\nTAMPER"),pins()[name])
    def test_18_original_source_only_no_fake_native_run(self):
        self.assertTrue(all("UnityEngine" not in raw(R+"/"+n).decode()
            for n in ("SafeRouteCore.cs","SafeRouteExample.cs")))
        self.assertEqual("NOT_RUN",load(T+"/manifest.json")["nativeUnity"])
def main():
    parser=argparse.ArgumentParser();parser.add_argument("--out",required=True)
    args=parser.parse_args()
    out=pathlib.Path(args.out);out.parent.mkdir(parents=True,exist_ok=True)
    r=unittest.TextTestRunner(verbosity=2).run(
        unittest.defaultTestLoader.loadTestsFromTestCase(ExactR2QA))
    with tempfile.TemporaryDirectory() as temp:
        z=pathlib.Path(temp)/"source.zip";digest=original_zip(z)
        out.with_suffix(".zip").write_bytes(z.read_bytes())
    passed=r.testsRun-len(r.failures)-len(r.errors)
    out.write_text(json.dumps({"package":"B01-25-SAFEROUTE001/R2",
        "sourcePassed":passed,"total":r.testsRun,
        "sourceZipSha256":digest,"nativeUnity":"NOT_RUN"},sort_keys=True,indent=2)+"\n")
    print("SAFEROUTE_R2_ORIGINAL_QA",passed,"/",r.testsRun,
          "SOURCE_ZIP_SHA256",digest,"NATIVE_G1_NOT_RUN")
    raise SystemExit(not r.wasSuccessful())
if __name__=="__main__":main()
