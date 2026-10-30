import copy
import json
from pathlib import Path
import tempfile
import unittest
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import assetfootprint as m


class FootprintTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / 'Assets'
        self.root.mkdir()

    def add(self, name, content):
        p = self.root / name
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_bytes(content)
        return p

    def test_real_bytes_and_extensions(self):
        self.add('A.PNG', b'abc')
        self.add('folder/b.fbx', b'hello')
        self.add('folder/c', b'')
        doc = m.scan(self.root)
        self.assertEqual(doc['summary']['source_bytes'], 8)
        self.assertEqual(doc['summary']['file_count'], 3)
        self.assertEqual(doc['summary']['by_extension']['.png']['bytes'], 3)
        self.assertEqual(doc['summary']['by_extension']['[none]']['files'], 1)
        self.assertEqual(m.validate(doc), doc)

    def test_ignores_meta_files(self):
        self.add('model.fbx', b'f')
        self.add('model.fbx.meta', b'hidden')
        self.assertEqual(len(m.scan(self.root)['entries']), 1)

    def test_duplicates_count_only_extra_copies(self):
        self.add('a.png', b'aaa')
        self.add('sub/b.png', b'aaa')
        self.add('c.png', b'bbb')
        self.assertEqual(m.scan(self.root)['summary']['exact_duplicate_groups'][0]['potential_redundant_bytes'], 3)

    def test_deterministic(self):
        self.add('Z/b', b'one')
        self.add('A/a', b'two')
        self.assertEqual(m.scan(self.root), m.scan(self.root))

    def test_symlink_file_rejected(self):
        t = self.add('one', b'one')
        (self.root/'link').symlink_to(t)
        with self.assertRaisesRegex(ValueError, 'symlink'):
            m.scan(self.root)

    def test_symlink_dir_rejected(self):
        self.add('sub/a', b'a')
        (self.root/'link').symlink_to(self.root/'sub', target_is_directory=True)
        with self.assertRaisesRegex(ValueError, 'symlink'):
            m.scan(self.root)

    def test_symlink_root_rejected(self):
        (self.root.parent/'alias').symlink_to(self.root, target_is_directory=True)
        with self.assertRaises(ValueError):
            m.scan(self.root.parent/'alias')

    def test_nested_ignored_library(self):
        self.add('Library/temp.dat', b'hidden')
        self.add('one', b'a')
        self.assertEqual(m.scan(self.root)['summary']['source_bytes'], 1)

    def test_file_mutation_detected_validation(self):
        self.add('one', b'one')
        d = m.scan(self.root)
        d['entries'][0]['bytes'] += 1
        with self.assertRaises(ValueError):
            m.validate(d)

    def test_invalid_hash_and_bool_bytes(self):
        self.add('one', b'one')
        d = m.scan(self.root)
        d['entries'][0]['sha256'] = 'not-a-sha'
        with self.assertRaises(ValueError):
            m.validate(d)
        d = m.scan(self.root)
        d['entries'][0]['bytes'] = True
        with self.assertRaises(ValueError):
            m.validate(d)

    def test_path_traversal_and_backslashes_rejected(self):
        for p in ('../secret', 'a/../b', 'a\\b', '/root', 'a//b', 'a/./b'):
            with self.subTest(path=p), self.assertRaises(ValueError):
                m.checked_path(p)

    def test_duplicate_paths_rejected(self):
        self.add('one', b'a')
        d = m.scan(self.root)
        d['entries'].append(copy.deepcopy(d['entries'][0]))
        with self.assertRaises(ValueError):
            m.validate(d)

    def test_unsorted_rejected(self):
        self.add('a', b'a'); self.add('b', b'b')
        d = m.scan(self.root)
        d['entries'].reverse()
        with self.assertRaises(ValueError):
            m.validate(d)

    def test_manifest_kind_required(self):
        self.add('a', b'a')
        d = m.scan(self.root)
        d['input_kind'] = 'PLAYER_BUILD'
        with self.assertRaises(ValueError):
            m.validate(d)

    def test_delta_add_modify_delete_and_same_size_hash(self):
        self.add('a', b'AA'); self.add('b', b'x'); self.add('c', b'old')
        before = m.scan(self.root)
        self.add('a', b'BB'); self.add('c', b'longer')
        (self.root/'b').unlink()
        self.add('d', b'n')
        after = m.scan(self.root)
        report = m.delta(before, after)
        self.assertEqual(report['delta_source_bytes'], 3)
        self.assertEqual([x['path'] for x in report['changed']], ['a','c'])
        self.assertTrue(report['changed'][0]['content_changed'])
        self.assertEqual([x['path'] for x in report['removed']], ['b'])
        self.assertEqual([x['path'] for x in report['added']], ['d'])

    def test_exclusive_output_and_source_safety(self):
        self.add('one', b'a')
        p = self.root.parent/'snapshot.json'
        m.save_exclusive(m.scan(self.root), p)
        with self.assertRaises(FileExistsError):
            m.save_exclusive({}, p)
        self.assertEqual(m.main(['scan','--assets',str(self.root),'--out',str(self.root/'inside.json')]), 1)

    def test_cli_source_scan_compare_budget(self):
        self.add('a', b'a')
        a = self.root.parent/'a.json'
        b = self.root.parent/'b.json'
        out = self.root.parent/'delta.json'
        self.assertEqual(m.main(['scan','--assets',str(self.root),'--out',str(a)]), 0)
        self.add('b', b'bbbb')
        self.assertEqual(m.main(['scan','--assets',str(self.root),'--out',str(b)]), 0)
        self.assertEqual(m.main(['compare','--base',str(a),'--candidate',str(b),'--out',str(out),'--max-growth-bytes','3']), 2)
        self.assertEqual(json.loads(out.read_text())['delta_source_bytes'], 4)

    def test_cli_invalid_growth_refused(self):
        self.assertEqual(m.main(['compare', '--base','missing', '--candidate','missing', '--out','x', '--max-growth-bytes','-1']), 1)


if __name__ == '__main__':
    unittest.main()
