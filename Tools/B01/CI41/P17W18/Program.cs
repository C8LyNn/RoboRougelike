using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using B01.UnitySaveBridge001;

// Real ORIGINAL #596 FileCheckpointStore, actual independent OS processes,
// actual simultaneous FileShare.None contention on disposable OS paths.
// No mock CAS, no UnityEngine; native Unity / PhysX G1 remains NOT_RUN.
internal static class Program
{
    static int assertions, races, childProcesses, deniedWhileLocked, damagedRefusals;
    static void Assert(bool ok,string why)
    {
        assertions++;
        if(!ok)throw new Exception("CI41_CAS_ASSERTION_FAILED:"+why);
    }
    static string Hash(char c)=>new string(c,64);

    // Worker runs in a NEW independent CLR process, not a Task/thread.
    static int Worker(string[] args)
    {
        string file=args[1], start=args[2], ready=args[3],result=args[4];
        long run=long.Parse(args[5]);
        string hash=args[6];
        File.WriteAllText(ready,"READY");
        var sw=Stopwatch.StartNew();
        while(!File.Exists(start))
        {
            if(sw.Elapsed>TimeSpan.FromSeconds(25))
            {
                File.WriteAllText(result,"TIMED_OUT");
                return 3;
            }
            Thread.Sleep(5);
        }
        bool committed=new FileCheckpointStore(file).Commit(run,0,"",1,hash);
        File.WriteAllText(result,committed?"COMMITTED":"DENIED");
        return 0;
    }

    static Process Launch(string file,string start,string ready,string result,long run,string hash)
    {
        string dll=typeof(Program).Assembly.Location;
        var info=new ProcessStartInfo("dotnet")
        {
            UseShellExecute=false,
            RedirectStandardOutput=false,
            RedirectStandardError=false,
            CreateNoWindow=true,
        };
        foreach(var arg in new[]{"worker",file,start,ready,result,run.ToString(),hash})
            info.ArgumentList.Add(arg);
        // Current executable is a portable .NET DLL, launch it with dotnet.
        info.ArgumentList.Insert(0,dll);
        Process? p=Process.Start(info);
        if(p==null)throw new Exception("CLR_CHILD_NOT_CREATED");
        childProcesses++;
        return p;
    }

    static void Ready(string readyA,string readyB)
    {
        var timer=Stopwatch.StartNew();
        while(!File.Exists(readyA)||!File.Exists(readyB))
        {
            if(timer.Elapsed>TimeSpan.FromSeconds(25))
                throw new Exception("CLR_CHILD_READY_TIMEOUT");
            Thread.Sleep(10);
        }
    }

    static void Race(string root,int index,bool sameRun,bool sameHash)
    {
        string folder=Path.Combine(root,"race-"+index.ToString("D3"));
        Directory.CreateDirectory(folder);
        string file=Path.Combine(folder,"selected596.checkpoint");
        string start=Path.Combine(folder,"START");
        string readyA=Path.Combine(folder,"ready-a");
        string readyB=Path.Combine(folder,"ready-b");
        string resultA=Path.Combine(folder,"result-a");
        string resultB=Path.Combine(folder,"result-b");
        long runA=1000+index,runB=sameRun?runA:2000+index;
        string ha=Hash('a'),hb=Hash(sameHash?'a':'b');
        using var a=Launch(file,start,readyA,resultA,runA,ha);
        using var b=Launch(file,start,readyB,resultB,runB,hb);
        Ready(readyA,readyB);
        File.WriteAllText(start,"GO"); // original independent real process attempts
        Assert(a.WaitForExit(25000)&&b.WaitForExit(25000),"both real processes exit");
        Assert(a.ExitCode==0&&b.ExitCode==0,"both workers observed valid attempt");
        string ra=File.ReadAllText(resultA),rb=File.ReadAllText(resultB);
        Assert((ra=="COMMITTED") != (rb=="COMMITTED"),
            "EXACT_ONE_COMMITTED_ORIGINAL_DISK_CAS_NOT_BOTH");
        long owner=ra=="COMMITTED"?runA:runB;
        string winner=ra=="COMMITTED"?ha:hb;
        var verifier=new FileCheckpointStore(file);
        Assert(verifier.Read(owner,out long generation,out string digest)
               &&generation==1&&digest==winner,"winning original sidecar durable bytes");
        Assert(!verifier.Commit(owner,0,"",1,Hash('c')),
            "loser stale read-before-write cannot recreate generation1");
        if(runA!=runB)
            Assert(!verifier.Read(owner==runA?runB:runA,out _,out _),
                "foreign run cannot read another real process checkpoint");
        Assert(verifier.Commit(owner,1,winner,2,Hash('d')),
            "legitimate next generation from independent authority accepted");
        Assert(verifier.Read(owner,out long next,out string nextHash)
               &&next==2&&nextHash==Hash('d'),"monotonic state after contest");
        Assert(Directory.GetFiles(folder,"*.tmp").Length==0,
            "original CAS temp files fully cleaned after losing race");
        races++;
    }

    static void LockAndCorruption(string root)
    {
        string folder=Path.Combine(root,"contention");
        Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,"selected596.checkpoint");
        var source=new FileCheckpointStore(path);
        Assert(source.Commit(999,0,"",1,Hash('a')),"initial real physical source record");
        using(var lockFile=new FileStream(path+".lock",FileMode.OpenOrCreate,
            FileAccess.ReadWrite,FileShare.None))
        {
            var second=new FileCheckpointStore(path);
            Assert(!second.Read(999,out _,out _),"held actual OS lock rejects concurrent Read");
            Assert(!second.Commit(999,1,Hash('a'),2,Hash('b')),
                "held actual OS lock rejects physical Commit");
            deniedWhileLocked+=2;
        }
        Assert(source.Read(999,out long generation,out string hash)
               &&generation==1&&hash==Hash('a'),
               "after lock release exact physical record unchanged");
        Assert(source.Commit(999,1,Hash('a'),2,Hash('b')),
               "original source recovers after released lock");
        File.WriteAllText(path,"B0123CHK2|999|2|NOT_A_VALID_HASH");
        Assert(!source.Read(999,out _,out _),"corrupt physical sidecar denied");
        Assert(!source.Commit(999,2,Hash('b'),3,Hash('c')),
               "corrupt sidecar cannot be silently overwritten");
        damagedRefusals+=2;
    }

    static int Main(string[] args)
    {
        if(args.Length>0&&args[0]=="worker")return Worker(args);
        string root=Path.Combine(Path.GetTempPath(),"CI41_W18_CAS_"+
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            // Three original CAS scenarios in 12 REAL simultaneous process pairs:
            // same run/different snapshot, same run/same snapshot, foreign run.
            for(int i=0;i<12;i++)Race(root,i,i%3!=2,i%3==1);
            LockAndCorruption(root);
            Assert(races==12&&childProcesses==24,"24 authentic CLR subprocesses");
            Assert(deniedWhileLocked==2&&damagedRefusals==2,
                "held gate and corrupt-sidecar negative controls");
            Console.WriteLine(
                "CI41_W18_REAL_ORIGINAL_596_CROSS_PROCESS_CAS_PASS races="+races+
                " child_processes="+childProcesses+" asserts="+assertions+
                " lock_denials="+deniedWhileLocked+" corrupt_denials="+damagedRefusals);
            Console.WriteLine("SELECTED_596_ORIGINAL_CAS_PROCESS_LOCK_POSITIVE_ONLY");
            Console.WriteLine("ORIGINAL_HOST_MULTI_INSTANCE_LEASE_AND_NATIVE_UNITY_G1_NOT_RUN_HOLD");
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("CI41_W18_REAL_CAS_UNEXPECTED_FAIL "+e);
            return 2;
        }
        finally
        {
            // This test created root with unpredictable UUID under own temp directory.
            if(Directory.Exists(root))Directory.Delete(root,true);
        }
    }
}
