// B01-62 / P17 W25 / NEW SOURCE V1.0.0: ONE physical authoritative Save
// containing immutable original #556 world payload AND original B01-22 #764 economy V6.
// No second writer, lease, world lock, journal, sidecar or automatic old Save migration.
// Cannot be installed alongside old #749/#773/#787 raw Store; separate owner selection.
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using RoboRougelike.B01.Economy.ApprovedV6;

namespace B01.WholeGameEconomy001
{
    public enum WholeGameResult
    {
        Accepted, Empty, Invalid, WrongOwner, Busy, Stale, Corrupt,
        Quarantined, Closed, IOError
    }

    public interface IOneFilePublisher
    {
        void Publish(string staging,string authoritative);
    }

    public sealed class OneFileOsPublisher : IOneFilePublisher
    {
        public void Publish(string staging,string authoritative)
            => File.Move(staging,authoritative,true);
    }

    // Separate uniquely versioned *replacement* writer, not a sidecar around
    // #773. The owner must choose precisely ONE Save writer for each slot.
    public sealed class WholeGameEconomySlotV1 : IDisposable
    {
        private const string Magic="B0162G01";
        private const string Marker="slot.canonical-game-v1";
        private const string Final="save.envelope";
        private const int Version=1;
        private const int MaxBytes=12*1024*1024;
        private const int MaxEconomyBytes=2*1024*1024;
        private readonly string directory;
        private readonly long runId;
        private readonly Guid slotId;
        private readonly CanonicalWorldSlotLease lease;
        private readonly IOneFilePublisher publisher;
        private bool closed,busy,quarantined;
        private static readonly object Gate=new object();
        private static readonly Dictionary<string,WholeGameEconomySlotV1> Active=
            new Dictionary<string,WholeGameEconomySlotV1>(StringComparer.Ordinal);
        private static readonly JsonSerializerOptions Json=new JsonSerializerOptions
        {
            IncludeFields=true,PropertyNameCaseInsensitive=false,
            WriteIndented=false
        };

        private WholeGameEconomySlotV1(string path,long run,Guid id,
            CanonicalWorldSlotLease owner,IOneFilePublisher pub)
        {directory=path;runId=run;slotId=id;lease=owner;publisher=pub;}

        public bool Quarantined=>quarantined;
        public bool Closed=>closed;

        // Only original selected #729 .owner supplies authority. Exact
        // physical casing matters on POSIX. No path alias / foreign siblings.
        private static bool Canonical(string dir,long run,Guid slot,
            CanonicalWorldSlotLease owner)
        {
            if(owner==null||run<=0||slot==Guid.Empty||
                owner.RunId!=run||owner.SlotId!=slot||!owner.Live||
                string.IsNullOrWhiteSpace(dir))return false;
            try
            {
                string parent=Path.GetDirectoryName(owner.Identity)!;
                string root=Path.GetDirectoryName(parent)!;
                string basePath=Path.Combine(root,"b0162_canonical_single_authority_v3");
                string expected=Path.Combine(basePath,"run_"+
                    run.ToString(CultureInfo.InvariantCulture)+"_slot_"+
                    slot.ToString("N"));
                if(!string.Equals(Path.GetFullPath(expected),
                    Path.GetFullPath(dir),StringComparison.Ordinal))return false;
                if(!Directory.Exists(root) || IsLink(root)||
                   IsLink(parent) || (Directory.Exists(basePath)&&IsLink(basePath)))
                    return false;
                return true;
            }
            catch{return false;}
        }

        private static bool IsLink(string path)
        {
            if(!File.Exists(path)&&!Directory.Exists(path))return false;
            try{return (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0;}
            catch{return true;}
        }

        private static string OwnerMarker(long run,Guid id,string identity)
            => "B0162_WHOLE_GAME_ECONOMY_V1|"+run+"|"+id.ToString("N")+
               "|"+identity+"\n";

        private static bool AlreadyHeld(CanonicalWorldSlotLease owner)
            => Active.TryGetValue(owner.Identity,out var prior)&&
                ReferenceEquals(prior.lease,owner);

        public static WholeGameResult Create(string dir,long run,Guid id,
            CanonicalWorldSlotLease owner,out WholeGameEconomySlotV1? result,
            IOneFilePublisher? hook=null)
        {
            result=null;
            if(!Canonical(dir,run,id,owner))return WholeGameResult.Invalid;
            lock(Gate)
            {
                if(AlreadyHeld(owner))return WholeGameResult.Busy;
                if(Directory.Exists(dir)||File.Exists(dir))
                    return WholeGameResult.WrongOwner; // foreign even EMPTY
                try
                {
                    Directory.CreateDirectory(dir);
                    if(IsLink(dir))return WholeGameResult.WrongOwner;
                    string path=Path.Combine(dir,Marker);
                    using(var marker=new FileStream(path,FileMode.CreateNew,
                        FileAccess.Write,FileShare.None))
                    {
                        byte[] bytes=Encoding.UTF8.GetBytes(
                            OwnerMarker(run,id,owner.Identity));
                        marker.Write(bytes,0,bytes.Length);
                        marker.Flush(true);
                    }
                    result=new WholeGameEconomySlotV1(dir,run,id,owner,
                        hook??new OneFileOsPublisher());
                    Active[owner.Identity]=result;
                    return WholeGameResult.Accepted;
                }
                catch(IOException){return WholeGameResult.IOError;}
                catch(UnauthorizedAccessException){return WholeGameResult.IOError;}
            }
        }

        public static WholeGameResult Open(string dir,long run,Guid id,
            CanonicalWorldSlotLease owner,out WholeGameEconomySlotV1? result,
            IOneFilePublisher? hook=null)
        {
            result=null;
            if(!Canonical(dir,run,id,owner))return WholeGameResult.Invalid;
            lock(Gate)
            {
                if(AlreadyHeld(owner))return WholeGameResult.Busy;
                if(!Directory.Exists(dir)||IsLink(dir))
                    return WholeGameResult.WrongOwner;
                try
                {
                    string path=Path.Combine(dir,Marker);
                    if(!File.Exists(path)||IsLink(path)||File.ReadAllText(path)!=
                        OwnerMarker(run,id,owner.Identity)||
                        File.Exists(Path.Combine(dir,"slot.canonical-v3"))||
                        File.Exists(Path.Combine(dir,"slot.owner"))||
                        IsLink(Path.Combine(dir,Final)))
                        return WholeGameResult.WrongOwner;
                    result=new WholeGameEconomySlotV1(dir,run,id,owner,
                        hook??new OneFileOsPublisher());
                    Active[owner.Identity]=result;
                    return WholeGameResult.Accepted;
                }
                catch(IOException){return WholeGameResult.IOError;}
                catch(UnauthorizedAccessException){return WholeGameResult.IOError;}
            }
        }

        // Stable physical numeric ID -> exact V6 GUID identity. Identifiers
        // never shift with Save generations or changing part inventory order.
        public static string EconomicPartId(long physicalId)
        {
            byte[] sha=SHA256.HashData(Encoding.UTF8.GetBytes(
                "B01/OWNED-PHYSICAL-PART/V1/"+physicalId.ToString(CultureInfo.InvariantCulture)));
            return new Guid(sha.AsSpan(0,16)).ToString("N");
        }

        private bool Semantic(byte[] world,ApprovedEconomySnapshotV6 econ,
            long generation)
        {
            if(world==null||world.Length<1||econ==null||generation<=0||
                !ColdRestore.TryStage(world,out ColdRestore? staged)||
                staged==null||staged.Snapshot.RunId!=runId||
                staged.Snapshot.Generation!=generation||
                !ApprovedEconomyPolicyV6.Valid(econ)||
                econ.OwnerId!=slotId.ToString("N")||
                econ.RunId!=runId.ToString(CultureInfo.InvariantCulture)||
                econ.Generation!=generation)return false;
            var worldIds=staged.Snapshot.Parts.Select(p=>EconomicPartId(p.Id)).ToArray();
            var economyIds=econ.Parts.Select(p=>p.Id).ToArray();
            return worldIds.Length==economyIds.Length &&
                new HashSet<string>(worldIds,StringComparer.Ordinal)
                    .SetEquals(economyIds);
        }

        private byte[] Encode(long epoch,byte[] world,ApprovedEconomySnapshotV6 econ)
        {
            byte[] e=JsonSerializer.SerializeToUtf8Bytes(econ,Json);
            using var stream=new MemoryStream();
            using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
            {
                writer.Write(Encoding.ASCII.GetBytes(Magic));
                writer.Write(Version);
                writer.Write(runId);
                writer.Write(slotId.ToByteArray());
                writer.Write(epoch);
                writer.Write(world.Length);
                writer.Write(e.Length);
                writer.Write(world);
                writer.Write(e);
                writer.Flush();
            }
            byte[] body=stream.ToArray();
            byte[] digest=SHA256.HashData(body);
            stream.Write(digest,0,digest.Length);
            return stream.ToArray();
        }

        private WholeGameResult Decode(byte[] data,out long epoch,out byte[] world,
            out ApprovedEconomySnapshotV6? economy)
        {
            epoch=0;world=Array.Empty<byte>();economy=null;
            try
            {
                if(data.Length<8+4+8+16+8+4+4+32||data.Length>MaxBytes)
                    return WholeGameResult.Corrupt;
                if(!CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(data.AsSpan(0,data.Length-32)),
                    data.AsSpan(data.Length-32,32)))return WholeGameResult.Corrupt;
                using var stream=new MemoryStream(data,false);
                using var reader=new BinaryReader(stream,Encoding.UTF8,true);
                if(Encoding.ASCII.GetString(reader.ReadBytes(8))!=Magic||
                    reader.ReadInt32()!=Version)return WholeGameResult.Corrupt;
                if(reader.ReadInt64()!=runId||
                    new Guid(reader.ReadBytes(16))!=slotId)
                    return WholeGameResult.WrongOwner;
                long gen=reader.ReadInt64();
                int wLength=reader.ReadInt32();
                int eLength=reader.ReadInt32();
                if(gen<=0||wLength<1||eLength<2||eLength>MaxEconomyBytes||
                    wLength>MaxBytes-MaxEconomyBytes||
                    stream.Position+(long)wLength+eLength+32!=data.Length)
                    return WholeGameResult.Corrupt;
                byte[] w=reader.ReadBytes(wLength);
                byte[] e=reader.ReadBytes(eLength);
                var parsed=JsonSerializer.Deserialize<ApprovedEconomySnapshotV6>(e,Json);
                if(w.Length!=wLength||e.Length!=eLength||
                    !Semantic(w,parsed!,gen))return WholeGameResult.Corrupt;
                epoch=gen;world=w;economy=parsed;
                return WholeGameResult.Accepted;
            }
            catch{return WholeGameResult.Corrupt;}
        }

        private WholeGameResult ReadCore(out long gen,out byte[] world,
            out ApprovedEconomySnapshotV6? econ)
        {
            gen=0;world=Array.Empty<byte>();econ=null;
            string path=Path.Combine(directory,Final);
            if(IsLink(path))return WholeGameResult.WrongOwner;
            if(!File.Exists(path))return WholeGameResult.Empty;
            try{return Decode(File.ReadAllBytes(path),out gen,out world,out econ);}
            catch(IOException){return WholeGameResult.IOError;}
            catch(UnauthorizedAccessException){return WholeGameResult.IOError;}
        }

        public WholeGameResult Read(out long generation,out byte[] world,
            out ApprovedEconomySnapshotV6? econ)
        {
            generation=0;world=Array.Empty<byte>();econ=null;
            if(closed)return WholeGameResult.Closed;
            if(!lease.Live){quarantined=true;return WholeGameResult.Quarantined;}
            if(quarantined)return WholeGameResult.Quarantined;
            if(busy)return WholeGameResult.Busy;
            busy=true;
            try
            {
                var result=ReadCore(out generation,out world,out econ);
                if(result!=WholeGameResult.Accepted&&result!=WholeGameResult.Empty)
                    quarantined=true;
                return result;
            }
            finally{busy=false;}
        }

        // MUST be invoked with an actual owner-verified world+combat receipt,
        // not arbitrary UI. One CAS applies to BOTH world and economy in ONE file.
        public WholeGameResult Commit(long expectedGeneration,byte[] world,
            ApprovedEconomySnapshotV6 econ)
        {
            if(closed)return WholeGameResult.Closed;
            if(!lease.Live){quarantined=true;return WholeGameResult.Quarantined;}
            if(quarantined)return WholeGameResult.Quarantined;
            if(busy)return WholeGameResult.Busy;
            if(expectedGeneration<0||expectedGeneration==long.MaxValue||
                !Semantic(world,econ,expectedGeneration+1))
                return WholeGameResult.Invalid;
            busy=true;
            bool publishAttempted=false;
            try
            {
                WholeGameResult prior=ReadCore(out long existing,out _,out _);
                if(prior!=WholeGameResult.Accepted&&prior!=WholeGameResult.Empty)
                {quarantined=true;return prior;}
                if(existing!=expectedGeneration)return WholeGameResult.Stale;
                byte[] payload=Encode(expectedGeneration+1,(byte[])world.Clone(),
                    econ.Copy());
                if(payload.Length>MaxBytes)return WholeGameResult.Invalid;
                string stage=Path.Combine(directory,"pending-"+
                    Guid.NewGuid().ToString("N")+".stage");
                string final=Path.Combine(directory,Final);
                using(var file=new FileStream(stage,FileMode.CreateNew,
                    FileAccess.Write,FileShare.None,65536,FileOptions.WriteThrough))
                {
                    file.Write(payload,0,payload.Length);
                    file.Flush(true);
                }
                if(Decode(File.ReadAllBytes(stage),out long stageGen,out _,out _)!=
                    WholeGameResult.Accepted||stageGen!=expectedGeneration+1)
                {quarantined=true;return WholeGameResult.Corrupt;}
                publishAttempted=true;
                publisher.Publish(stage,final);
                WholeGameResult outcome=ReadCore(out long actual,
                    out byte[] actualWorld,out ApprovedEconomySnapshotV6? actualEconomy);
                if(outcome!=WholeGameResult.Accepted||
                    actual!=expectedGeneration+1||
                    !actualWorld.SequenceEqual(world)||
                    actualEconomy==null||
                    !JsonSerializer.SerializeToUtf8Bytes(actualEconomy,Json)
                        .SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(econ,Json)))
                {quarantined=true;return WholeGameResult.Quarantined;}
                return WholeGameResult.Accepted;
            }
            catch
            {
                if(publishAttempted)
                {quarantined=true;return WholeGameResult.Quarantined;}
                return WholeGameResult.IOError;
            }
            finally{busy=false;}
        }

        public void Dispose()
        {
            closed=true;
            // NEVER releases .owner or Active registry. Only original #729
            // gateway after TRUE physical retirement can release the OS owner.
        }
    }
}
