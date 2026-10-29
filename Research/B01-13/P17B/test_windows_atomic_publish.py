#!/usr/bin/env python3
"""AIM-DELTA R3: atomic HTML publish must survive transient Windows file locks."""
import tempfile
from pathlib import Path
from unittest import TestCase,main,mock
import aim_delta as delta

def sharing_error(code=32):
    e=PermissionError(13,'synthetic Windows file sharing violation')
    e.winerror=code
    return e

class WindowsAtomicPublish(TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        self.target=self.root/'REPORT01.html'

    def test_normal_unique_report(self):
        delta.write_private(self.target,'<html>synthetic</html>')
        self.assertEqual(self.target.read_text(),'<html>synthetic</html>')
        self.assertEqual(list(self.root.glob('.aim-delta-*')),[])

    def test_transient_windows_share_32_retries_and_succeeds(self):
        real=delta.os.replace
        attempts=[]
        def intermittent(src,dst):
            attempts.append((src,dst))
            if len(attempts)<=2: raise sharing_error(32)
            return real(src,dst)
        with mock.patch.object(delta.os,'replace',side_effect=intermittent),mock.patch.object(delta.time,'sleep') as sleep:
            delta.write_private(self.target,'hello')
        self.assertEqual(len(attempts),3)
        self.assertEqual(sleep.call_count,2)
        self.assertEqual(self.target.read_text(),'hello')

    def test_transient_windows_share_33_retries(self):
        real=delta.os.replace
        n=[0]
        def intermittent(src,dst):
            n[0]+=1
            if n[0]==1: raise sharing_error(33)
            return real(src,dst)
        with mock.patch.object(delta.os,'replace',side_effect=intermittent),mock.patch.object(delta.time,'sleep'):
            delta.write_private(self.target,'hello')
        self.assertEqual(n[0],2)

    def test_persistent_busy_fails_closed_and_removes_temporary(self):
        with mock.patch.object(delta.os,'replace',side_effect=sharing_error(32)) as replacing,mock.patch.object(delta.time,'sleep') as sleep:
            with self.assertRaises(PermissionError):
                delta.write_private(self.target,'hello')
        self.assertEqual(replacing.call_count,20)
        self.assertEqual(sleep.call_count,19)
        self.assertFalse(self.target.exists())
        self.assertEqual(list(self.root.glob('.aim-delta-*')),[])

    def test_non_sharing_access_denied_never_retried(self):
        with mock.patch.object(delta.os,'replace',side_effect=sharing_error(5)) as replacing,mock.patch.object(delta.time,'sleep') as sleep:
            with self.assertRaises(PermissionError):
                delta.write_private(self.target,'hello')
        self.assertEqual(replacing.call_count,1)
        self.assertEqual(sleep.call_count,0)
        self.assertFalse(self.target.exists())

    def test_concurrent_report_must_not_be_overwritten(self):
        def concurrent(src,dst):
            self.target.write_text('external actor')
            raise sharing_error(32)
        with mock.patch.object(delta.os,'replace',side_effect=concurrent),mock.patch.object(delta.time,'sleep'):
            with self.assertRaisesRegex(delta.DeltaError,'Output already exists'):
                delta.write_private(self.target,'our data')
        self.assertEqual(self.target.read_text(),'external actor')
        self.assertEqual(list(self.root.glob('.aim-delta-*')),[])

if __name__=='__main__':main(verbosity=2)
