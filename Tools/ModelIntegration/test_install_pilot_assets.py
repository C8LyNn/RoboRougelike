import hashlib
import tempfile
import unittest
import zipfile
from pathlib import Path
from install_pilot_assets import install
NAME = "Assets/RRLImportedModels/W01/example.glb"
DATA = b"glTF" + bytes([0])*50
EXPECTED = {NAME: hashlib.sha256(DATA).hexdigest()}
class InstallerTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        (self.root/"Assets").mkdir()
        (self.root/"ProjectSettings").mkdir()
        (self.root/"ProjectSettings/ProjectVersion.txt").write_text("m_EditorVersion: 6000.3.14f1")
        self.archive = self.root/"model.zip"
        with zipfile.ZipFile(self.archive,"w") as z:
            z.writestr(NAME, DATA)
    def test_verify_only(self):
        self.assertEqual(install(self.archive,self.root,False,EXPECTED)["status"],"PASS")
        self.assertFalse((self.root/NAME).exists())
    def test_install_idempotent(self):
        self.assertEqual(install(self.archive,self.root,True,EXPECTED)["assets"][NAME],"installed")
        self.assertEqual(install(self.archive,self.root,True,EXPECTED)["assets"][NAME],"already-present")
    def test_reject_bad_sha(self):
        with self.assertRaises(ValueError):
            install(self.archive,self.root,True,{NAME:"0"*64})
        self.assertFalse((self.root/NAME).exists())
    def test_reject_overwrite(self):
        target = self.root/NAME
        target.parent.mkdir(parents=True)
        target.write_bytes(b"other")
        with self.assertRaises(FileExistsError):
            install(self.archive,self.root,True,EXPECTED)
        self.assertEqual(target.read_bytes(),b"other")
if __name__ == "__main__":
    unittest.main()
