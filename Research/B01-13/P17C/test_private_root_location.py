#!/usr/bin/env python3
"""B01-13 AIM-VAULT R2: executable repo/symlink preflight regression, stdlib only."""
import os
from pathlib import Path
import tempfile
import unittest
import aim_evidence_vault as vault


class LocationGuards(unittest.TestCase):
    def test_initialize_refuses_git_worktree_without_creating_vault(self):
        with tempfile.TemporaryDirectory() as d:
            repo = Path(d) / 'project'
            repo.mkdir()
            (repo / '.git').mkdir()
            dest = repo / 'private-evidence'
            with self.assertRaisesRegex(vault.VaultError, 'REFUSE_GIT_WORKTREE'):
                vault.initialize(dest)
            self.assertFalse(dest.exists(), 'rejected init must not create private data directory')

    @unittest.skipIf(os.name == 'nt', 'Windows junction/ACL validation requires separate native host')
    def test_initialize_refuses_symlink_ancestor_without_creating_vault(self):
        with tempfile.TemporaryDirectory() as d:
            parent = Path(d)
            actual = parent / 'storage'
            actual.mkdir()
            alias = parent / 'alias'
            alias.symlink_to(actual, target_is_directory=True)
            with self.assertRaisesRegex(vault.VaultError, 'REFUSE_SYMLINK_ANCESTRY'):
                vault.initialize(alias / 'private-evidence')
            self.assertFalse((actual / 'private-evidence').exists())

    @unittest.skipIf(os.name == 'nt', 'Windows junction/ACL validation requires separate native host')
    def test_existing_vault_refuses_symlink_ancestor(self):
        with tempfile.TemporaryDirectory() as d:
            parent = Path(d)
            actual = parent / 'real'
            actual.mkdir()
            vault.initialize(actual / 'vault')
            alias = parent / 'alias'
            alias.symlink_to(actual, target_is_directory=True)
            with self.assertRaisesRegex(vault.VaultError, 'REFUSE_SYMLINK_ANCESTRY'):
                vault.private_root(alias / 'vault')

    def test_initialize_still_works_outside_repository(self):
        with tempfile.TemporaryDirectory() as d:
            dest = Path(d) / 'private-evidence'
            self.assertEqual(vault.initialize(dest)['status'], 'PRIVATE_VAULT_CREATED')
            self.assertEqual(vault.private_root(dest), dest)
            self.assertTrue((dest / '.aim-vault.json').is_file())
            if os.name != 'nt':
                self.assertEqual(dest.stat().st_mode & 0o777, 0o700)


if __name__ == '__main__':
    unittest.main(verbosity=2)
