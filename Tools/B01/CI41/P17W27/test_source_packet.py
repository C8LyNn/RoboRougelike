"""Adversarial unit tests for real original Git-source packet transport and safe staging."""
import hashlib,json,os,pathlib,tempfile,unittest,zipfile,sys
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
import source_packet as p

class SourcePacketGuards(unittest.TestCase):
    def test_01_exact_selected_source_roles(self):
        self.assertEqual(set(p.PLAN["producers"]),p.REQUIRED)
        self.assertEqual(p.PLAN["producers"]["main"],"5f06af81e2dd9d877e4300259bb3dd008cb9c9d1")
    def test_02_no_two_installed_stores(self):
        self.assertEqual(p.PLAN["selected_runtime"],"whole_game_797")
        self.assertEqual(len(p.PLAN["audit_only"]),2)
        self.assertEqual({x["ref"] for x in p.PLAN["audit_only"]},{"selected_strict_path_787"})
    def test_03_r5_only_authorized_existing_override(self):
        self.assertEqual(len([x for x in p.PLAN["explicit_files"] if "replaces_from" in x]),1)
        self.assertEqual([x["replaces_from"] for x in p.PLAN["explicit_files"] if "replaces_from" in x],["r5_parent_690"])
    def test_04_blobs_git_sha1_not_plain_file_hash(self):
        raw=b"hello actual file bytes\n"
        want=hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
        self.assertEqual(p.hash_git(raw),want)
        self.assertNotEqual(p.hash_git(raw),hashlib.sha1(raw).hexdigest())
    def test_05_zip_deterministic_with_original_bytes(self):
        a=p.deterministic_zip({"SOURCE/z.cs":b"abc","SOURCE/a.meta":b"guid: "+"a".encode()*32})
        b=p.deterministic_zip({"SOURCE/a.meta":b"guid: "+b"a"*32,"SOURCE/z.cs":b"abc"})
        self.assertEqual(a,b)
        with zipfile.ZipFile(__import__("io").BytesIO(a)) as z:
            self.assertEqual(z.read("SOURCE/z.cs"),b"abc")
    def test_06_duplicate_guid_fails_closed(self):
        vals={"Assets/x.cs":("a",b"x"),"Assets/x.cs.meta":("a",b"guid: "+b"a"*32),
              "Assets/y.cs":("a",b"y"),"Assets/y.cs.meta":("a",b"guid: "+b"a"*32)}
        with self.assertRaisesRegex(AssertionError,"DUPLICATE_UNITY_GUID"):p.inspect(vals)
    def test_07_missing_meta_fails_closed(self):
        with self.assertRaisesRegex(AssertionError,"UNPAIRED_UNITY_ASSET_META"):
            p.inspect({"Assets/x.cs":("owner",b"real")})
    def test_08_malformed_guid_fails_closed(self):
        with self.assertRaisesRegex(AssertionError,"MISSING_OR_AMBIGUOUS_UNITY_GUID"):
            p.inspect({"Assets/a.cs.meta":("owner",b"guid: not-32-hex")})
    def test_09_duplicate_assembly_name_fails_closed(self):
        b=json.dumps({"name":"duplicate","references":[]}).encode()
        entries={"Assets/a.asmdef":("q",b),"Assets/a.asmdef.meta":("q",b"guid: "+b"a"*32),
                 "Assets/b.asmdef":("q",b),"Assets/b.asmdef.meta":("q",b"guid: "+b"b"*32)}
        with self.assertRaisesRegex(AssertionError,"DUPLICATE_ASMDEF_NAME"):p.inspect(entries)
    def test_10_unresolved_assembly_reference_is_visible(self):
        b=json.dumps({"name":"real","references":["Missing.Real.Assembly"]}).encode()
        entries={"Assets/a.asmdef":("q",b),"Assets/a.asmdef.meta":("q",b"guid: "+b"a"*32)}
        self.assertEqual(p.inspect(entries)["unresolved_asmdef_names"],[("real","Missing.Real.Assembly")])
    def test_11_foreign_even_empty_output_rejected(self):
        with tempfile.TemporaryDirectory() as td:
            dest=pathlib.Path(td)/"ci41-w27-owned-foreign"
            dest.mkdir()
            with self.assertRaisesRegex(AssertionError,"EXISTING_OR_FOREIGN"):
                p.scan_dir_for_foreign(dest)
            self.assertTrue(dest.is_dir())
    def test_12_symlink_parent_rejected_without_touching_foreign(self):
        with tempfile.TemporaryDirectory() as td:
            real=pathlib.Path(td)/"owned";real.mkdir()
            link=pathlib.Path(td)/"alias"
            try:link.symlink_to(real,target_is_directory=True)
            except (OSError,NotImplementedError):self.skipTest("OS lacks symlink creation rights")
            with self.assertRaisesRegex(AssertionError,"SYMLINK_PARENT"):
                p.scan_dir_for_foreign(link/"ci41-w27-owned-any")
            self.assertTrue(real.exists())
    def test_13_foreign_destination_name_rejected(self):
        with tempfile.TemporaryDirectory() as td:
            with self.assertRaisesRegex(AssertionError,"DISPOSABLE_UNIQUE_OUTPUT"):
                p.scan_dir_for_foreign(pathlib.Path(td)/"UnityProject")
    def test_14_selected_sha_present_not_moving_branch(self):
        for role in p.SELECTED:
            self.assertRegex(p.PLAN["producers"][role],r"^[0-9a-f]{40}$")
    def test_15_old_and_alternate_save_never_explicitly_packaged(self):
        forbidden=("P17W25StrictCanonicalPathR3/","P17W24CanonicalPathFixR2/","P17W23OneAuthorityV3/")
        for x in p.PLAN["explicit_files"]:
            self.assertFalse(any(z in x["path"] for z in forbidden))
    def test_16_cross_owner_shadows_not_silent(self):
        a={"Assets/fixture.cs":("r5_parent_690",b"old")}
        self.assertNotEqual(a["Assets/fixture.cs"][0],"r5_fixture_750")
        self.assertEqual(p.PLAN["explicit_files"][0]["replaces_from"],"r5_parent_690")
    def test_17_no_unverified_native_claim(self):
        self.assertEqual(p.PLAN["native_unity_g1"],"NOT_RUN")
        self.assertEqual(p.PLAN["codex_ack"],"NONE")
    def test_18_original_unity_root_metas_not_omitted(self):
        self.assertEqual(len(p.PLAN["asset_sources"]),7)
        self.assertEqual(len(p.PLAN["explicit_files"]),3)
        # Real original frozen Git trees, not manually fabricated test GUIDs.
        for scope in p.PLAN["asset_sources"]:
            paths=p.list_source(scope["ref"],scope["prefix"])
            meta=scope["prefix"].rstrip("/")+".meta"
            self.assertIn(meta,paths)
            raw=p.read_source(scope["ref"],meta)
            self.assertEqual(len(p.GUID_RE.findall(raw)),1)

if __name__=="__main__":unittest.main(verbosity=2)
