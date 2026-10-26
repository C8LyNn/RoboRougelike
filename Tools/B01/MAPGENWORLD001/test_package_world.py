import io
import tempfile
import unittest
import zipfile
from pathlib import Path
import package_world as pkg

class TestPackager(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.files=pkg.gather()
        cls.zip,cls.manifest=pkg.create_zip(cls.files)

    def test_source_archive_contains_no_compiler_products(self):
        self.assertFalse(any(pkg.is_generated(p) for p in self.files))
        self.assertFalse(any("/obj/" in p or "/bin/" in p for p in self.files))
        self.assertTrue(all(not pkg.is_generated(f["path"]) for f in self.manifest["files"]))

    def test_generated_files_in_disposable_tree_are_ignored(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            for p,data in self.files.items():
                dst = root / p
                dst.parent.mkdir(parents=True,exist_ok=True)
                dst.write_bytes(data)
            for p in [
                "Tools/B01/MAPGENWORLD001/obj/project.assets.json",
                "Tools/B01/MAPGENWORLD001/bin/Release/net8.0/app.runtimeconfig.json",
                "Assets/B01Packages/MAPGENWORLD001/Runtime/obj/cached.generated.cs",
                "Tools/B01/MAPGENWORLD001/__pycache__/cache.json"
            ]:
                dst=root / p
                dst.parent.mkdir(parents=True,exist_ok=True)
                dst.write_bytes(b"NONDETERMINISTIC!")
            self.assertEqual(pkg.gather(root),self.files)

    def test_generated_obj_path_cannot_be_forged_into_manifest(self):
        altered=dict(self.files)
        altered["Tools/B01/MAPGENWORLD001/obj/project.assets.json"]=b"FAKE"
        with self.assertRaisesRegex(ValueError,"GENERATED_FILE_NOT_ALLOWED"):
            pkg.validate_assets(altered)

    def test_generated_obj_entry_in_zip_rejected_even_without_manifest(self):
        entries={}
        with zipfile.ZipFile(io.BytesIO(self.zip)) as z:
            for name in z.namelist(): entries[name]=z.read(name)
        entries["Tools/B01/MAPGENWORLD001/obj/project.assets.json"]=b"FAKE"
        buf=io.BytesIO()
        with zipfile.ZipFile(buf,"w") as z:
            for name,v in entries.items():z.writestr(name,v)
        with self.assertRaisesRegex(ValueError,"GENERATED_FILE_NOT_ALLOWED"):
            pkg.verify_zip(buf.getvalue())

    def test_real_package_verifies(self):
        self.assertEqual(len(pkg.verify_zip(self.zip)["files"]),len(self.files))

    def test_deterministic(self):
        self.assertEqual(self.zip,pkg.create_zip(self.files)[0])

    def test_source_meta_pair(self):
        self.assertGreaterEqual(pkg.validate_assets(self.files).__len__(),14)

    def test_tamper_sha_rejected(self):
        entries={}
        with zipfile.ZipFile(io.BytesIO(self.zip)) as z:
            for name in z.namelist(): entries[name]=z.read(name)
        key=next(p for p in entries if p.endswith("MapWorldPolicy.cs"))
        entries[key]+=b"// malicious mutation"
        with io.BytesIO() as b:
            with zipfile.ZipFile(b,"w") as z:
                for name,v in entries.items():z.writestr(name,v)
            with self.assertRaisesRegex(ValueError,"HASH_OR_SIZE_MISMATCH"):
                pkg.verify_zip(b.getvalue())

    def test_duplicate_guid_rejected(self):
        data=dict(self.files)
        one=pkg.META
        other=pkg.ASSET+"/Runtime.meta"
        original=data[one].decode().split("guid: ")[1][:32]
        data[other]=data[other].replace(
            data[other].decode().split("guid: ")[1][:32].encode(),original.encode())
        with self.assertRaisesRegex(ValueError,"DUPLICATE_GUID"):
            pkg.validate_assets(data)

    def test_missing_meta_rejected(self):
        data=dict(self.files); del data[pkg.ASSET+"/Runtime/MapWorldPolicy.cs.meta"]
        with self.assertRaisesRegex(ValueError,"MISSING_META"):
            pkg.validate_assets(data)

    def test_zip_slip_rejected(self):
        entries={}
        with zipfile.ZipFile(io.BytesIO(self.zip)) as z:
            for name in z.namelist():entries[name]=z.read(name)
        entries["../outside.txt"]=b"BAD"
        with io.BytesIO() as b:
            with zipfile.ZipFile(b,"w") as z:
                for name,v in entries.items():z.writestr(name,v)
            with self.assertRaisesRegex(ValueError,"ZIP_TRAVERSAL"):
                pkg.verify_zip(b.getvalue())

    def test_outside_owner_scope_rejected(self):
        data=dict(self.files);data["Assets/R3/LiveHost.cs"]=b"bad"
        with self.assertRaisesRegex(ValueError,"PATH_NOT_OWNED"):
            pkg.validate_assets(data)

if __name__=="__main__": unittest.main()
