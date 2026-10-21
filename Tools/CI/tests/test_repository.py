import json, sys, tempfile, unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import validate_repository as repo

class RepositoryTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(); self.root=Path(self.temp.name)
        self.files={
            'Packages/manifest.json':json.dumps({'dependencies':{'com.unity.test-framework':'1.6.0'}}),
            'Packages/packages-lock.json':json.dumps({'dependencies':{'com.unity.test-framework':{'version':'1.6.0'}}}),
            'ProjectSettings/ProjectVersion.txt':'m_EditorVersion: 6000.3.14f1\n',
            'ProjectSettings/EditorSettings.asset':'  m_SerializationMode: 2\n',
            'ProjectSettings/ProjectSettings.asset':'PlayerSettings:\n',
            'ProjectSettings/GraphicsSettings.asset':'GraphicsSettings:\n',
            'ProjectSettings/EditorBuildSettings.asset':'  m_Scenes:\n  - enabled: 1\n    path: Assets/Main.unity\n    guid: '+'a'*32+'\n',
            'Assets/Main.unity':'%YAML 1.1\n',
            'Assets/Main.unity.meta':'fileFormatVersion: 2\nguid: '+'a'*32+'\n',
            '.gitignore':'/Library/\n','.gitattributes':'* -text\n','README.md':'game\n'}
        for name in repo.REQUIRED_FILES:
            self.files.setdefault(name,'documentation or workflow\n')
    def tearDown(self): self.temp.cleanup()
    def validate(self):
        for rel,s in self.files.items():
            f=self.root/rel;f.parent.mkdir(parents=True,exist_ok=True);f.write_text(s)
        return repo.validate(self.root,list(self.files))
    def test_complete_project_passes(self): self.assertEqual(self.validate(),[])
    def test_missing_meta_fails(self):
        del self.files['Assets/Main.unity.meta']; self.assertTrue(any('meta' in e for e in self.validate()))
    def test_duplicate_guid_fails(self):
        self.files['Assets/Other.cs']='class Other {}'
        self.files['Assets/Other.cs.meta']=self.files['Assets/Main.unity.meta']
        self.assertTrue(any('Duplicate GUID' in e for e in self.validate()))
    def test_orphan_meta_fails(self):
        self.files['Assets/Ghost.cs.meta']='guid: '+'b'*32
        self.assertTrue(any('Orphan' in e for e in self.validate()))
    def test_generated_tracked_files_fail(self):
        self.files['Library/innocent.txt']='data'
        self.assertTrue(any('Generated' in e for e in self.validate()))
    def test_untracked_meta_cannot_mask_missing_meta(self):
        path=self.root/'Assets/Main.unity.meta';path.parent.mkdir(parents=True);path.write_text(self.files.pop('Assets/Main.unity.meta'))
        self.assertTrue(any('meta' in e for e in self.validate()))
    def test_wrong_unity_version_fails(self):
        self.files['ProjectSettings/ProjectVersion.txt']='m_EditorVersion: 6000.4.0f1'
        self.assertTrue(any('Unity version' in e for e in self.validate()))
    def test_missing_scene_fails(self):
        self.files['ProjectSettings/EditorBuildSettings.asset']=self.files['ProjectSettings/EditorBuildSettings.asset'].replace('Assets/Main.unity','Assets/Missing.unity')
        self.assertTrue(any('scene' in e for e in self.validate()))
    def test_lock_mismatch_fails(self):
        self.files['Packages/packages-lock.json']='{"dependencies":{}}'
        self.assertTrue(any('lock' in e for e in self.validate()))
    def test_missing_folder_meta_fails(self):
        self.files['Assets/Scripts/Foo.cs']='class Foo {}'
        self.files['Assets/Scripts/Foo.cs.meta']='guid: '+'c'*32
        self.assertTrue(any('Assets/Scripts.meta' in e for e in self.validate()))

if __name__=='__main__': unittest.main()
