"""W29 18 adversarial original-source safe-intake controls, not Unity runtime G1."""
import hashlib, io, json, pathlib, sys, tempfile, unittest, zipfile
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
import composite_g0 as c

class W29OriginalIntakeTests(unittest.TestCase):
    def test_01_exact_current_game_and_one_new_writer(self):
        self.assertEqual(c.P["game_824"],"ed8cf4bcc131688b9a93968a89683d2496586683")
        self.assertEqual(c.P["save826"],"99fb08619c78b3834bb4c0450499babf991f218a")
    def test_02_previous_797_not_selected(self):
        self.assertEqual(len(c.PLAN["audit_only"]),4)
        self.assertEqual({x["ref"] for x in c.PLAN["audit_only"]},{"r3_787","old_save797"})
    def test_03_exact_original_producer_pins(self):
        self.assertEqual(set(c.P),c.SELECTED)
        for sha in c.P.values():self.assertRegex(sha,r"^[0-9a-f]{40}$")
    def test_04_exact_unity_scope_with_original_meta_roots(self):
        self.assertEqual(len(c.PLAN["scopes"]),7)
        for scope in c.PLAN["scopes"]:
            self.assertIn(scope["prefix"][:-1]+".meta",c.files(scope["ref"],scope["prefix"]))
    def test_05_only_one_existing_R5_fix_allowed(self):
        overlap=[x for x in c.PLAN["selected_files"] if "replaces_from" in x]
        self.assertEqual(len(overlap),1)
        self.assertEqual(overlap[0]["ref"],"cold750")
        self.assertEqual(overlap[0]["replaces_from"],"host690")
    def test_06_two_V3_original_files_not_old_V2(self):
        new=[x["path"] for x in c.PLAN["selected_files"] if x["ref"]=="save826"]
        self.assertEqual(len(new),2)
        self.assertTrue(all("P17W28UnityCompatibleWholeGameV3/" in x and "V3.cs" in x for x in new))
    def test_07_git_blobs_are_real_git_hashes(self):
        data=b"the selected C# source bytes\n"
        self.assertEqual(c.blob(data),hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest())
        self.assertNotEqual(c.blob(data),hashlib.sha1(data).hexdigest())
    def test_08_archive_byte_determinism_across_source_order(self):
        a=c.make_zip({"SOURCE/z.cs":b"Z","SOURCE/a.cs":b"A"})
        b=c.make_zip({"SOURCE/a.cs":b"A","SOURCE/z.cs":b"Z"})
        self.assertEqual(a,b)
        with zipfile.ZipFile(io.BytesIO(a)) as z:
            self.assertEqual(z.read("SOURCE/a.cs"),b"A")
    def test_09_archive_prevents_path_traversal(self):
        with self.assertRaises(AssertionError):c.make_zip({"SOURCE/../../bad":b"X"})
    def test_10_duplicate_unity_guid_fail_closed(self):
        g=b"guid: "+b"a"*32+b"\n"
        sample={"Assets/a.cs":("x",b"a"),"Assets/a.cs.meta":("x",g),
                "Assets/b.cs":("y",b"b"),"Assets/b.cs.meta":("y",g)}
        with self.assertRaisesRegex(AssertionError,"DUPLICATE_GUID"):
            c.inspect_unity(sample)
    def test_11_missing_asset_meta_denied(self):
        with self.assertRaisesRegex(AssertionError,"MISSING_REAL_UNITY_ASSET_META"):
            c.inspect_unity({"Assets/a.cs":("x",b"code")})
    def test_12_bad_unity_meta_denied(self):
        with self.assertRaisesRegex(AssertionError,"MALFORMED_OR_MISSING_META_GUID"):
            c.inspect_unity({"Assets/a.meta":("x",b"guid: INVALID\n")})
    def test_13_duplicate_asmdef_denied(self):
        g1=b"guid: "+b"a"*32+b"\n";g2=b"guid: "+b"b"*32+b"\n"
        src=json.dumps({"name":"SameAssembly","references":[]}).encode()
        sample={"Assets/a.asmdef":("x",src),"Assets/a.asmdef.meta":("x",g1),
                "Assets/b.asmdef":("x",src),"Assets/b.asmdef.meta":("x",g2)}
        with self.assertRaisesRegex(AssertionError,"DUPLICATE_ASSEMBLY_NAME"):
            c.inspect_unity(sample)
    def test_14_existing_even_empty_foreign_destination_denied(self):
        with tempfile.TemporaryDirectory() as t:
            p=pathlib.Path(t)/"ci41-w29-owned-foreign"
            p.mkdir()
            with self.assertRaisesRegex(AssertionError,"FOREIGN_EVEN_EMPTY_DESTINATION"):
                c.unique_destination(p)
            self.assertTrue(p.is_dir())
    def test_15_output_name_not_scratch_denied(self):
        with tempfile.TemporaryDirectory() as t:
            with self.assertRaisesRegex(AssertionError,"FOREIGN_SCRATCH_NAME"):
                c.unique_destination(pathlib.Path(t)/"UnityProject")
    def test_16_linked_parent_denied_without_deleting_foreign(self):
        with tempfile.TemporaryDirectory() as t:
            existing=pathlib.Path(t)/"foreign";existing.mkdir()
            alias=pathlib.Path(t)/"link"
            try:alias.symlink_to(existing,target_is_directory=True)
            except (OSError,NotImplementedError):self.skipTest("symlink creation restricted by OS")
            with self.assertRaisesRegex(AssertionError,"SYMLINK_OR_ABSENT_PARENT"):
                c.unique_destination(alias/"ci41-w29-owned-fresh")
            self.assertTrue(existing.exists())
    def test_17_exact_original_826_negative_ABI_reference(self):
        source=(c.read("save826",c.PLAN["selected_files"][1]["path"])+
                c.read("save826",c.PLAN["selected_files"][2]["path"]))
        for old in c.BAD_826:
            self.assertNotIn(old.encode(),source)
        for required in c.GOOD_826:
            self.assertIn(required.encode(),source)
    def test_18_real_git_source_original_not_mock_signature(self):
        src=c.read("econ780","Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs")
        self.assertTrue(src and b"CanonicalEconomySectionV7" in src)
        self.assertEqual(len(c.PLAN["abi_project"]),3)

if __name__=="__main__":unittest.main(verbosity=2)
