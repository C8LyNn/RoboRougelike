#!/usr/bin/env python3
"""W27 R4: original two-crate game source, real Git bytes, ZIP and scratch migration QA."""
import argparse,hashlib,json,pathlib,re,subprocess,tempfile,unittest,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
BASE="456186ba3ee25beab6f00905a3f8f114fc477c1b"
R="Research/B01-25/P17/SAFEROUTE001"
A="Assets/B0125SafeRoute001"
T="Tools/B01/B01-25/P17W28SafeRouteR5"
W=".github/workflows/b01-25-w28-saferoute-r5.yml"
OLD_T="Tools/B01/B01-25/P17W27SafeRouteR4"
MODIFIED={"Assets/B0125SafeRoute001/Runtime/SafeRouteUnityDemo.cs","Assets/B0125SafeRoute001/Tests/PlayMode/SafeRouteNativePlayModeTests.cs","Research/B01-25/P17/SAFEROUTE001/Program.cs"}
ADDED={"Tools/B01/B01-25/P17W28SafeRouteR5/check.py","Tools/B01/B01-25/P17W28SafeRouteR5/README.md","Tools/B01/B01-25/P17W28SafeRouteR5/INSTALL_FOR_CODEX.md","Tools/B01/B01-25/P17W28SafeRouteR5/TEST_PLAN.md","Tools/B01/B01-25/P17W28SafeRouteR5/KNOWN_LIMITATIONS.md","Tools/B01/B01-25/P17W28SafeRouteR5/CHANGELOG.md","Tools/B01/B01-25/P17W28SafeRouteR5/QA_B01_60.md","Tools/B01/B01-25/P17W28SafeRouteR5/.gitignore","Tools/B01/B01-25/P17W28SafeRouteR5/manifest.json",".github/workflows/b01-25-w28-saferoute-r5.yml"}
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
        archive.writestr(e,(json.dumps({"package":"B01-25-SAFEROUTE001/R5",
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
class OriginalPhysicalR5QA(unittest.TestCase):
    def test_01_only_three_own_modified_ten_own_new_paths(self):
        listed=set(git("diff","--name-only",BASE,"HEAD").decode().splitlines())
        self.assertEqual(13,len(listed))
        self.assertEqual(MODIFIED|ADDED,listed)
    def test_02_exact_original_source_closure_51_real_git_blob_shas(self):
        self.assertEqual(60,len(pins()))
        for path,sha in pins().items():
            self.assertEqual(git("show","HEAD:"+path),raw(path),path)
            self.assertEqual(blob(raw(path)),sha,path)
    def test_03_original_r3_branch_exact_parent_sha_and_old_manifest_immutable(self):
        m=doc(T+"/manifest.json")
        self.assertEqual(BASE,m["parentR4Sha"])
        self.assertEqual(git("show",BASE+":"+OLD_T+"/manifest.json"),
                         git("show","HEAD:"+OLD_T+"/manifest.json"))
    def test_04_exact_three_existing_r3_code_file_provenance_and_modified_sha(self):
        old=doc(OLD_T+"/manifest.json")["gitBlobs"]
        for name in MODIFIED:
            self.assertEqual(old[name],blob(git("show",BASE+":"+name)),name)
            self.assertNotEqual(old[name],pins()[name],name)
    def test_05_original_23_unity_source_and_meta_paths(self):
        self.assertEqual(23,len(unity()))
        self.assertEqual(14,len([n for n in unity() if n.endswith(".meta")]))
    def test_06_actual_unique_14_unity_guid_and_old_guid_identity(self):
        paths=[p for p in unity() if p.endswith(".meta")]
        ids=[]
        for p in paths:
            found=re.search(r"^guid: ([0-9a-f]{32})$",raw(p).decode(),re.M)
            self.assertIsNotNone(found,p)
            ids.append(found.group(1))
            self.assertEqual(raw(p),git("show",BASE+":"+p),p)
        self.assertEqual(14,len(set(ids)))
    def test_07_original_three_asmdefs_unchanged(self):
        r=doc(A+"/Runtime/B0125.SafeRoute.Runtime.asmdef")
        e=doc(A+"/Editor/B0125.SafeRoute.Editor.asmdef")
        t=doc(A+"/Tests/PlayMode/B0125.SafeRoute.PlayModeTests.asmdef")
        self.assertEqual([],r["references"])
        self.assertEqual([r["name"]],e["references"])
        self.assertEqual([r["name"]],t["references"])
        self.assertIn("TestAssemblies",t["optionalUnityReferences"])
    def test_08_source_and_imported_physical_router_core_byte_parity(self):
        for p in ("SafeRouteCore.cs","SafeRouteExample.cs","PhysicalObstacleRouter.cs"):
            self.assertEqual(raw(R+"/"+p),raw(A+"/Runtime/"+p))
            self.assertEqual(raw(R+"/"+p),git("show",BASE+":"+R+"/"+p))
    def test_09_two_actual_unity_rigidbody_and_boxcollider_proxies(self):
        s=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for x in ("physicalCrate=box.AddComponent<Rigidbody>()",
                  "auxiliaryCrate=second.AddComponent<Rigidbody>()",
                  "crateCollider=box.GetComponent<BoxCollider>()",
                  "auxiliaryCollider=second.GetComponent<BoxCollider>()",
                  "TryMoveAuxiliaryCrate","public Rigidbody AuxiliaryCrate"):
            self.assertIn(x,s)
    def test_10_physical_broadphase_samples_both_and_fails_closed_on_overflow(self):
        s=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for x in ("Physics.OverlapBoxNonAlloc(","buffer[i]==crateCollider",
                  "buffer[i]==auxiliaryCollider","hits>=buffer.Length",
                  "occupancyUncertain=true","new SafeRoutePlan(false,"):
            self.assertIn(x,s)
    def test_11_composite_relocation_audits_eight_conditions_before_real_move(self):
        s=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for x in ("new List<GridCell>(otherCells){proposed}",
            "PhysicalObstacleRouter.AuditAllEight(level,hypothetical).AllReachable",
            "if(otherCollider.bounds.Intersects(target))",
            "if(hull!=null&&hull.bounds.Intersects(target))",
            "mover.position=target.center"):
            self.assertIn(x,s)
    def test_12_physical_pose_change_even_within_one_grid_cell_replans(self):
        s=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for x in ("(physicalCrate.position-observedCratePose).sqrMagnitude>.0004f",
                  "(auxiliaryCrate.position-observedAuxiliaryPose).sqrMagnitude>.0004f",
                  "replanRevision++","observedAuxiliaryPose=auxiliaryCrate.position"):
            self.assertIn(x,s)
    def test_13_visible_route_and_physical_validation_deny_both_crates(self):
        s=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        self.assertIn("collider==auxiliaryCollider",s)
        self.assertIn("PhysicalObstacleRouter.FindFrom(level,routeOrigin,radius,",s)
        self.assertIn("occupiedCells=SamplePhysicalBlockers()",s)
    def test_14_full_17_actual_nunit_unity_playmode_sources_authored_not_run(self):
        t=raw(A+"/Tests/PlayMode/SafeRouteNativePlayModeTests.cs").decode()
        self.assertEqual(22,t.count("[UnityTest]"))
        for name in ("W27TwoOriginalRealPhysicalCratesOccupyDistinctCells",
           "W27TwoSoloSafeCratesCombinedHeavySoftlockIsRejected",
           "W27SafeMoveOfSecondPhysicalCrateKeepsHeavyRoute",
           "W27UnexpectedSecondCrateMoveFailsClosedWithoutFalseGuidance",
           "W27SubcellObstacleMoveTriggersRealPhysicalReplan"):
            self.assertIn(name,t)
    def test_15_genuine_source_cases_include_two_safe_alone_unsafe_together(self):
        p=raw(R+"/Program.cs").decode()
        for k in ("R4_BOTH_INDIVIDUALLY_SAFE_BUT_TOGETHER_SOFTLOCK_HEAVY",
          "R4_COMBINED_LIGHT_ROUTE_MAY_EXIST_WHEN_HEAVY_IS_BLOCKED",
          "R4_ALL_EIGHT_GATE_HAZARD_HEAVY_CASES_AVOID_BOTH_REAL_CRATES",
          "R4_EXTERNAL_PHYSICAL_CRATE_RELOCATION_DETECTED_AS_NO_ROUTE"):
            self.assertIn(k,p)
    def test_16_real_unchanged_netstandard_csharp9_project_compiled_with_warnings_errors(self):
        for f in ("SafeRouteRuntime.csproj","SafeRouteTests.csproj"):
            c=raw(R+"/"+f).decode()
            self.assertIn("<LangVersion>9.0</LangVersion>",c)
            self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>",c)
            self.assertIn('Include="PhysicalObstacleRouter.cs"',c)
    def test_17_original_source_zip_twice_is_byte_deterministic(self):
        with tempfile.TemporaryDirectory() as d:
            a=pathlib.Path(d)/"a.zip";b=pathlib.Path(d)/"b.zip"
            self.assertEqual(zip_sources(a),zip_sources(b))
            self.assertEqual(a.read_bytes(),b.read_bytes())
    def test_18_real_scratch_full_zip_stage_and_rollback_keeps_foreign_dirs(self):
        with tempfile.TemporaryDirectory() as d:
            root=pathlib.Path(d);a=root/"original.zip";zip_sources(a)
            empty=root/"FOREIGN_EMPTY";empty.mkdir()
            occupied=root/"UNRELATED"/"keep.txt"
            occupied.parent.mkdir();occupied.write_text("KEEP")
            written=stage(a,root)
            self.assertEqual(24,len(written))
            undo(written)
            self.assertTrue(empty.is_dir())
            self.assertEqual([],list(empty.iterdir()))
            self.assertEqual("KEEP",occupied.read_text())
    def test_19_existing_r3_source_must_never_be_overwritten(self):
        with tempfile.TemporaryDirectory() as d:
            root=pathlib.Path(d);a=root/"original.zip";zip_sources(a)
            frozen=root/A/"Runtime/SafeRouteUnityDemo.cs"
            frozen.parent.mkdir(parents=True,exist_ok=True)
            frozen.write_text("OLD VERSION MUST STAY")
            with self.assertRaises(FileExistsError):stage(a,root)
            self.assertEqual("OLD VERSION MUST STAY",frozen.read_text())
    def test_20_other_owner_path_and_zip_traversal_rejected(self):
        for x in ("../escape","/tmp/evil","Assets/R6/Save.cs",
                  "Assets/B0125Conveyor001/Runtime/ConveyorCore.cs"):
            with self.assertRaises(ValueError):allowed(x)
    def test_21_tampered_changed_unity_source_does_not_match_git_pin(self):
        p=A+"/Runtime/SafeRouteUnityDemo.cs"
        self.assertNotEqual(blob(raw(p)+b"tampered"),pins()[p])
    def test_22_old_r1_r2_r3_versions_must_never_coinstall(self):
        m=doc(T+"/manifest.json")
        self.assertTrue(m["oneVersionOnly"])
        self.assertFalse(m["integrated"])
        self.assertFalse(m["codexAck"])
        self.assertEqual("NOT_RUN",m["nativeUnity"])
        self.assertEqual(23,m["unitySourceCount"])
        self.assertEqual(24,m["sourceZipEntryCount"])
    def test_23_real_receiver_docs_include_qa_and_install_contracts(self):
        for file in ("README.md","INSTALL_FOR_CODEX.md","TEST_PLAN.md",
                     "KNOWN_LIMITATIONS.md","CHANGELOG.md","QA_B01_60.md"):
            self.assertGreater(len(raw(T+"/"+file)),100,file)
        self.assertIn("NOT_RUN",raw(T+"/QA_B01_60.md").decode())
    def test_24_no_frozen_source_or_codex_active_worktree_changed(self):
        names=set(git("diff","--name-only",BASE,"HEAD").decode().splitlines())
        self.assertTrue(all(p.startswith(A+"/") or
            p.startswith(R+"/") or p.startswith(T+"/") or p==W for p in names))
    def test_25_external_original_world_rigidbody_registration_contract(self):
        h=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for x in ("public bool TryRegisterWorldBlocker(Rigidbody body)",
            "public bool UnregisterWorldBlocker(Rigidbody body)",
            "GetComponentsInChildren<Collider>(true)",
            "shape.attachedRigidbody!=body",
            "RegisteredWorldBlockerCount",
            "MaxWorldBlockers=32",
            "item.Body==body"):
            self.assertIn(x,h)
    def test_26_real_physics_all_child_colliders_observed_not_just_root(self):
        h=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        for x in ("var seen=new bool[externalObstacles.Count][]",
          "seen[k]=new bool[externalObstacles[k].Shapes.Length]",
          "seen[k][m]=true;occupied=true",
          "if(!seen[k][m])allWorldSeen=false",
          "item.ChildWorldPositions",
          "item.ChildWorldRotations",
          "item.ChildWorldScales",
          "ExternalWorldChanged()"):
            self.assertIn(x,h)
    def test_27_original_authoritative_foreign_physics_never_mutated_by_registration(self):
        h=raw(A+"/Runtime/SafeRouteUnityDemo.cs").decode()
        part=h[h.index("public bool TryRegisterWorldBlocker"):h.index("public bool UnregisterWorldBlocker")]
        self.assertNotIn("body.position=",part)
        self.assertNotIn("body.MovePosition(",part)
        self.assertNotIn("body.isKinematic=",part)
        self.assertIn("Replan();",part)
    def test_28_native_world_detached_and_compound_child_tests_authored_not_run(self):
        t=raw(A+"/Tests/PlayMode/SafeRouteNativePlayModeTests.cs").decode()
        self.assertEqual(22,t.count("[UnityTest]"))
        for name in (
            "W28RealDetachedModuleColliderBlocksHeavyWithoutAuthoritativeMutation",
            "W28ExternallyMovingDebrisCausesRealPhysicsResample",
            "W28DisabledRegisteredColliderFailsClosedUntilUnregistered",
            "W28ForeignObstacleDoesNotAllowPlayerOrDuplicateRegistration",
            "W28CompoundDetachedPartRegistersBothPhysicalChildColliders"):
            self.assertIn(name,t)
def main():
    ap=argparse.ArgumentParser();ap.add_argument("--out",required=True)
    args=ap.parse_args();out=pathlib.Path(args.out);out.parent.mkdir(parents=True,exist_ok=True)
    t=unittest.TextTestRunner(verbosity=2).run(
       unittest.defaultTestLoader.loadTestsFromTestCase(OriginalPhysicalR4QA))
    with tempfile.TemporaryDirectory() as d:
        z=pathlib.Path(d)/"original-source.zip"
        digest=zip_sources(z);out.with_suffix(".zip").write_bytes(z.read_bytes())
    good=t.testsRun-len(t.failures)-len(t.errors)
    out.write_text(json.dumps({"package":"B01-25-SAFEROUTE001/R5",
        "sourceTests":t.testsRun,"passed":good,
        "originalSourceZipSHA256":digest,"nativeUnity":"NOT_RUN",
        "formalG0":"PENDING_INDEPENDENT"},sort_keys=True,indent=2)+"\n")
    print("SAFEROUTE_R5_ORIGINAL_GIT_PHYSICAL_QA",good,"/",t.testsRun,
        "ZIP_SHA256",digest,"UNITY_G1_NOT_RUN")
    raise SystemExit(not t.wasSuccessful())
if __name__=="__main__":main()
