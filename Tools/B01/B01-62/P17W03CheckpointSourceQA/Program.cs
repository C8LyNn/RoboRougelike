using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using RoboRougelike.R1;
using RoboRougelike.R3;

internal static class Program
{
    sealed class Temp : IDisposable
    {
        public readonly string DirectoryPath =
            Path.Combine(Path.GetTempPath(), "b0162-checkpoint-" + Guid.NewGuid().ToString("N"));
        public string PathName => Path.Combine(DirectoryPath, "checkpoint.json");
        public Temp() { Directory.CreateDirectory(DirectoryPath); }
        public void Dispose() => Directory.Delete(DirectoryPath, recursive:true);
    }

    static R1RunCheckpoint Sample(int progress=3) =>
        new R1RunCheckpoint {
            seed=31, vehicle=2, runId="17171717171717171717171717171717",
            defeated=7, earned=125, recovered=4, lastReward=13,
            progress=new R1ProgressCheckpoint{stage=progress,garage=true},
            model=new R3ModelCheckpoint{tag="real-source-test",
                                        ownedIds=new[]{"core-A","gun-1","wheel-B"}}
        };

    static void Check(bool ok, string name)
    {
        if(!ok) throw new Exception("ASSERT " + name);
    }

    static void Throws(Action action, string why)
    {
        try { action(); }
        catch(Exception) { return; }
        throw new Exception("SHOULD_REJECT " + why);
    }

    static string Hash(string payload) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

    static JsonObject Envelope(string path) =>
        JsonNode.Parse(File.ReadAllText(path))!.AsObject();

    static void SaveEnvelope(string path, JsonObject envelope) =>
        File.WriteAllText(path, envelope.ToJsonString());

    static int Main(string[] args)
    {
        if(args.Contains("--historical-negative"))
        {
            var gun = new R3PartState{ReloadReadyAt=1,DeviceReadyAt=1};
            var snapshot = R3VehicleModel.CallActualSavePart(gun,2);
            if(snapshot.ReloadReadyAt == 0)
            {
                Console.WriteLine("HISTORICAL_CODEX_RELOAD_EXPIRY_RED=OBSERVED");
                return 86;  // This failure MUST be produced by the old owner method.
            }
            Console.Error.WriteLine("HISTORICAL_NEGATIVE_CONTROL_NOT_REPRODUCED");
            return 99;
        }

        var tests = new List<(string, Action)>{
            ("C01 ORIGINAL storage actual disk create/read",()=>{
                using var t=new Temp();
                R1RunCheckpointStorage.Write(t.PathName,Sample());
                var d=R1RunCheckpointStorage.Read(t.PathName);
                Check(d.version==1&&d.seed==31&&d.defeated==7&&d.earned==125
                    &&d.model.ownedIds.SequenceEqual(new[]{"core-A","gun-1","wheel-B"})
                    &&d.progress.stage==3,"original byte data roundtrip");
            }),
            ("C02 ORIGINAL storage replace existing, no .tmp",()=>{
                using var t=new Temp();
                R1RunCheckpointStorage.Write(t.PathName,Sample(1));
                var old=File.ReadAllBytes(t.PathName);
                R1RunCheckpointStorage.Write(t.PathName,Sample(9));
                var newer=File.ReadAllBytes(t.PathName);
                Check(!old.SequenceEqual(newer),"actual replacement");
                Check(R1RunCheckpointStorage.Read(t.PathName).progress.stage==9,
                      "latest content");
                Check(!Directory.GetFiles(t.DirectoryPath,"*.tmp").Any(),"temp cleanup");
            }),
            ("C03 SHA256 sealed envelope matches original payload",()=>{
                using var t=new Temp();
                R1RunCheckpointStorage.Write(t.PathName,Sample());
                var env=Envelope(t.PathName);
                Check(env["sha256"]!.GetValue<string>() ==
                      Hash(env["payload"]!.GetValue<string>()),"actual hash");
            }),
            ("N04 payload tamper rejects",()=>{
                using var t=new Temp();R1RunCheckpointStorage.Write(t.PathName,Sample());
                var env=Envelope(t.PathName);
                env["payload"]=env["payload"]!.GetValue<string>().Replace("core-A","core-X");
                SaveEnvelope(t.PathName,env);
                Throws(()=>R1RunCheckpointStorage.Read(t.PathName),"tampered part ID");
            }),
            ("N05 hash tamper rejects",()=>{
                using var t=new Temp();R1RunCheckpointStorage.Write(t.PathName,Sample());
                var env=Envelope(t.PathName);env["sha256"]=new string('0',64);
                SaveEnvelope(t.PathName,env);
                Throws(()=>R1RunCheckpointStorage.Read(t.PathName),"tampered checksum");
            }),
            ("N06 correctly signed unsupported version rejects",()=>{
                using var t=new Temp();R1RunCheckpointStorage.Write(t.PathName,Sample());
                var env=Envelope(t.PathName);
                var data=JsonNode.Parse(env["payload"]!.GetValue<string>())!.AsObject();
                data["version"]=99;
                var payload=data.ToJsonString();
                env["payload"]=payload;env["sha256"]=Hash(payload);
                SaveEnvelope(t.PathName,env);
                Throws(()=>R1RunCheckpointStorage.Read(t.PathName),"version99 signed");
            }),
            ("N07 truncated envelope rejects",()=>{
                using var t=new Temp();File.WriteAllText(t.PathName,"{");
                Throws(()=>R1RunCheckpointStorage.Read(t.PathName),"invalid JSON");
            }),
            ("N08 empty content rejects",()=>{
                using var t=new Temp();File.WriteAllText(t.PathName,"");
                Throws(()=>R1RunCheckpointStorage.Read(t.PathName),"empty slot");
            }),
            ("N09 absent file rejects",()=>{
                using var t=new Temp();
                Throws(()=>R1RunCheckpointStorage.Read(t.PathName),"missing slot");
            }),
            ("N10 over 4MiB content rejects",()=>{
                using var t=new Temp();
                File.WriteAllBytes(t.PathName,new byte[4*1024*1024+1]);
                Throws(()=>R1RunCheckpointStorage.Read(t.PathName),"oversized slot");
            }),
            ("C11 original model expired inventory marker preserved",()=>{
                var gun=new R3PartState{ReloadReadyAt=1,Ammo=0,DeviceReadyAt=1};
                var c=R3VehicleModel.CallActualSavePart(gun,2);
                Check(c.ReloadReadyAt>0&&c.ReloadReadyAt<=.00011f,
                      "expired reload retained nonzero pending marker");
                Check(c.DeviceReadyAt==0,"device expiry remains zero");
            }),
            ("C12 original model future ready interval preserved",()=>{
                var gun=new R3PartState{ReloadReadyAt=9,DeviceReadyAt=6};
                var c=R3VehicleModel.CallActualSavePart(gun,2);
                Check(Math.Abs(c.ReloadReadyAt-7)<.00001f&&c.DeviceReadyAt==4,
                      "remaining time");
            }),
            ("C13 original model zero marker remains zero",()=>{
                var c=R3VehicleModel.CallActualSavePart(
                    new R3PartState{ReloadReadyAt=0},10);
                Check(c.ReloadReadyAt==0,"idle reload not invented");
            }),
            ("C14 original model source object not mutated",()=>{
                var gun=new R3PartState{ReloadReadyAt=1,DeviceReadyAt=8,Ammo=0};
                var c=R3VehicleModel.CallActualSavePart(gun,2);
                Check(!ReferenceEquals(gun,c)&&gun.ReloadReadyAt==1
                      &&gun.DeviceReadyAt==8,"original immutable");
            }),
            ("C15 original model infinity defence sentinel converted",()=>{
                var c=R3VehicleModel.CallActualSavePart(
                    new R3PartState{DefenseRechargeAge=float.PositiveInfinity},2);
                Check(c.DefenseRechargeAge==float.MaxValue,"defence sentinel");
            }),
        };

        int succeeded=0;
        foreach(var (name,run) in tests)
        {
            try { run(); Console.WriteLine("PASS " + name); succeeded++; }
            catch(Exception e) {Console.Error.WriteLine("FAIL "+name+": "+e);return 1;}
        }
        Console.WriteLine($"ORIGINAL_CODEX_CHECKPOINT_SOURCE_GREEN={succeeded}/{tests.Count} NOT_UNITY_NATIVE");
        return succeeded==15?0:2;
    }
}
