using System;
using System.IO;
using B01.UnitySaveBridge001;

// Independent CI41 proof of source-level native test fixture state collision.
// Uses EXACT ORIGINAL #596 FileCheckpointStore, not copied persistence code.
// Only writes disposable own GUID-named OS directory; NEVER real Unity persistentDataPath.
// This C#9 process is NOT Unity Editor, Rigidbody or PhysX G1.
internal static class Program
{
    private static int assertions;
    private static void Check(bool value,string message)
    {
        assertions++;
        if(!value)throw new InvalidOperationException("FAIL "+message);
    }
    private static int Main()
    {
        string root=Path.Combine(Path.GetTempPath(),"CI41_P17W17_"+
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string path=Path.Combine(root,"b0123_one_save_host_v2.checkpoint");
        string oldHash=new string('a',64);
        string nextHash=new string('b',64);
        try
        {
            // Exact same physical checkpoint sidecar name as original Unity #596
            // IsolatedTwinRigSaveHost.Awake(). Actual original PlayMode test run IDs:
            // 31,31,41,9. Prove shared slot cannot support independent tests.
            var testRun31=new FileCheckpointStore(path);
            Check(testRun31.Read(31,out long initial,out string initialHash),
                "initial run31 read is allowed");
            Check(initial==0&&initialHash=="","fresh sidecar has no checkpoint");
            Check(File.Exists(path+".lock"),"Read itself creates persistent lockfile");
            Check(testRun31.Commit(31,0,"",1,oldHash),"first run31 commits");
            Check(File.Exists(path),"physical checkpoint created");
            Check(File.ReadAllText(path).StartsWith("B0123CHK2|31|1|",
                StringComparison.Ordinal),"original on-disk format and run persisted");

            var independentOtherNativeTestRun41=new FileCheckpointStore(path);
            Check(!independentOtherNativeTestRun41.Read(41,out _,out _),
                "new Unity native test run41 cannot read run31-owned shared sidecar");
            Check(!independentOtherNativeTestRun41.Commit(41,0,"",4,nextHash),
                "new Unity native test run41 cannot overwrite run31-owned sidecar");
            var independentNativeTestRun9=new FileCheckpointStore(path);
            Check(!independentNativeTestRun9.Read(9,out _,out _),
                "new Unity native test run9 cannot read run31-owned sidecar");

            // Even a second test reusing run 31 cannot treat a newly generated
            // distinct same-generation checkpoint as a clean independent case.
            var secondNativeTestRun31=new FileCheckpointStore(path);
            Check(secondNativeTestRun31.Read(31,out long gen,out string digest),
                "run31 can read its old persistent state");
            Check(gen==1&&digest==oldHash,"run31 sees stale previous test checkpoint");
            Check(!secondNativeTestRun31.Commit(31,0,"",1,nextHash),
                "same-run original test cannot begin from clean generation zero");
            Check(!secondNativeTestRun31.Commit(31,1,oldHash,1,nextHash),
                "new same-generation fingerprint explicitly rejected");

            Check(secondNativeTestRun31.Commit(31,1,oldHash,2,nextHash),
                "only true monotonically increasing generation may advance");
            Check(secondNativeTestRun31.Read(31,out long later,out string laterHash)
                  &&later==2&&laterHash==nextHash,
                "new checkpoint persists in same shared OS sidecar");
            Check(!independentOtherNativeTestRun41.Read(41,out _,out _),
                "run41 remains blocked after later run31 writes");
            Check(File.Exists(path+".lock"),"shared lock residue remains");
            Console.WriteLine("ORIGINAL_SELECTED_596_NATIVE_PLAYMODE_SHARED_CHECKPOINT_ISOLATION_RED_REPRODUCED asserts="+assertions);
            Console.WriteLine("NATIVE_UNITY_G1_NOT_RUN_SOURCE_PROCESS_ONLY");
            Console.WriteLine("SINGLE_FIXED_CHECKPOINT_FILENAME_ACROSS_4_ORIGINAL_PLAYMODE_CASES_RELEASE_HOLD");
            return 0; // expected SOURCE-RED witness reproduced, not release PASS
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("CI41_NATIVE_ISOLATION_TEST_UNEXPECTED_ERROR "+e);
            return 2;
        }
        finally
        {
            // We own this unique random directory exclusively; no shared or
            // occupied Unity path is ever touched.
            if(Directory.Exists(root))Directory.Delete(root,true);
        }
    }
}
