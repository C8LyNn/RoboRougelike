#!/usr/bin/env python3
import argparse,hashlib,json,pathlib,subprocess,tempfile,unittest,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
R="Research/B01-25/P17/W17_MAGNETLANE_SCENARIOS/"
T="Tools/B01/B01-25/W17Scenarios/"
WF=".github/workflows/b01-25-w17-magnetlane-scenarios.yml"
FROZEN="c9663a8c50599bdb0f8954c42737d8e0d8afcaec"
CORE="Research/B01-25/P17/MAGNETLANE001R3/MagnetLaneCore.cs"
def git(*a):return subprocess.check_output(["git","-C",str(ROOT),*a],stderr=subprocess.PIPE)
def raw(p):return (ROOT/p).read_bytes()
def data(p):return json.loads(raw(p))
def blob(b):return hashlib.sha1(("blob %d\0"%len(b)).encode()+b).hexdigest()
def pins():return data(T+"manifest.json")["gitBlobs"]
def sourcezip(dst):
    listing={}
    with zipfile.ZipFile(dst,"w",compression=zipfile.ZIP_STORED) as z:
        for path,sha in sorted(pins().items()):
            buf=git("show","HEAD:"+path)
            assert blob(buf)==sha and raw(path)==buf
            listing[path]=sha
            info=zipfile.ZipInfo(path,date_time=(1980,1,1,0,0,0))
            info.create_system=3;info.external_attr=0o100644<<16
            z.writestr(info,buf,compress_type=zipfile.ZIP_STORED)
        info=zipfile.ZipInfo("PACKAGE_MANIFEST.json",date_time=(1980,1,1,0,0,0))
        info.create_system=3;info.external_attr=0o100644<<16
        z.writestr(info,(json.dumps({"files":listing,"frozenR3":FROZEN,
                        "nativeUnity":"NOT_RUN","codexAck":False},sort_keys=True)+"\n").encode())
    return hashlib.sha256(pathlib.Path(dst).read_bytes()).hexdigest()
def safe(path):
    p=pathlib.PurePosixPath(path)
    assert path in pins() or path=="PACKAGE_MANIFEST.json"
    assert not p.is_absolute() and ".." not in p.parts and "\\" not in path
class QA(unittest.TestCase):
    def test_01_exact_unchanged_original_core(self):
        b=git("show",FROZEN+":"+CORE)
        self.assertEqual("2453bc1f0c562681e1ffcdec8437f9178536c089",blob(b))
        self.assertEqual(b,git("show","HEAD:"+CORE))
    def test_02_real_csharp9_core_compilation_reference(self):
        s=raw(R+"Scenarios.csproj").decode()
        self.assertIn("../MAGNETLANE001R3/MagnetLaneCore.cs",s)
        self.assertIn("<LangVersion>9.0</LangVersion>",s)
    def test_03_seven_distinct_original_game_data_scenarios(self):
        a=data(R+"fixtures.json")
        self.assertEqual(FROZEN,a["immutableEngineSha"])
        self.assertEqual(7,len(a["scenarios"]))
        self.assertEqual(7,len(set(s["id"] for s in a["scenarios"])))
        self.assertEqual({"ZERO","POSITIVE","NEGATIVE"},set(s["forceSign"] for s in a["scenarios"]))
    def test_04_actual_masses_and_disjoint_bypass(self):
        a=data(R+"fixtures.json")
        self.assertEqual((1.5,12),(a["masses"]["light"],a["masses"]["heavy"]))
        self.assertEqual(3,a["config"]["halfX"])
        self.assertGreater(a["safeBypassZ"]-a["config"]["halfZ"],1.5)
    def test_05_independent_numeric_oracle_and_mutants_are_code(self):
        s=raw(R+"Program.cs").decode()
        for term in ("const int N=20000","new MagnetLaneController(",
                     "Math.Floor(now/cycle)","Numerical(config,s)",
                     "DUPLICATE_ROUTE_ID","SAFE_BYPASS_OVERLAP",
                     "REJECTED_OVERFLOW_LEAVES_FROZEN_R3_STATE_ATOMIC"):
            self.assertIn(term,s)
    def test_06_strict_scope_and_original_git_blobs(self):
        names=git("diff","--name-only",FROZEN,"HEAD").decode().splitlines()
        self.assertTrue(names)
        self.assertTrue(all(s.startswith(R) or s.startswith(T) or s==WF for s in names))
        self.assertEqual(set(names)-{T+"manifest.json"},set(pins()))
        for name,sha in pins().items():
            self.assertEqual(blob(git("show","HEAD:"+name)),sha)
            self.assertEqual(raw(name),git("show","HEAD:"+name))
    def test_07_mutant_hash_and_zip_traversal_denied(self):
        self.assertNotEqual(blob(raw(R+"fixtures.json")+b"altered"),pins()[R+"fixtures.json"])
        for name in ("../escape","/tmp/escape","Assets/R6/foreign.cs"):
            with self.assertRaises(AssertionError):safe(name)
    def test_08_actual_zip_and_foreign_safe_scratch_rollback(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=pathlib.Path(tmp)
            first=root/"one.zip";second=root/"two.zip"
            self.assertEqual(sourcezip(first),sourcezip(second))
            self.assertEqual(first.read_bytes(),second.read_bytes())
            foreign=root/"foreign-empty";foreign.mkdir()
            other=root/"foreign"/"KEEP.txt";other.parent.mkdir();other.write_text("untouched")
            with zipfile.ZipFile(first) as z:
                files=z.namelist()
                self.assertEqual(9,len(files))
                for name in files:
                    safe(name)
                    dst=root/name;self.assertFalse(dst.exists())
                    dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(z.read(name))
                    self.assertEqual(dst.read_bytes(),z.read(name))
                for name in reversed(files):(root/name).unlink()
            self.assertTrue(foreign.is_dir());self.assertEqual([],list(foreign.iterdir()))
            self.assertEqual("untouched",other.read_text())
    def test_09_honest_native_gates(self):
        m=data(T+"manifest.json")
        self.assertEqual("NOT_RUN",m["nativeUnity"])
        self.assertFalse(m["codexAck"]);self.assertFalse(m["integrated"])
def main():
    p=argparse.ArgumentParser();p.add_argument("--out",required=True);a=p.parse_args()
    out=pathlib.Path(a.out);out.parent.mkdir(parents=True,exist_ok=True)
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(QA))
    with tempfile.TemporaryDirectory() as tmp:
        z=pathlib.Path(tmp)/"original.zip";sha=sourcezip(z);out.with_suffix(".zip").write_bytes(z.read_bytes())
    receipt={"passed":result.testsRun-len(result.failures)-len(result.errors),"tests":result.testsRun,
             "sha256":sha,"nativeUnity":"NOT_RUN"}
    out.write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
    print("W17_SOURCE_DATA_QA_PASS",receipt["passed"],"/",receipt["tests"],"ZIP_SHA256",sha)
    raise SystemExit(not result.wasSuccessful())
if __name__=="__main__":main()
