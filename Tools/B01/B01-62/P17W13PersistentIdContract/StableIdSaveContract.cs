// B01-62 W13 — standalone proposal only; NOT a Unity save adapter.
// Compiled together with genuine immutable W06 DebrisImpulseCore and #510 AtomicDockLedger.
// Never mutate a live Unity Rigidbody or frozen #510 code.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

namespace B01.DataIntegrity.W13
{
    public sealed class Coordinate
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
    public sealed class PartSnapshot
    {
        public int Index { get; set; }
        public long PartId { get; set; }
        // Host-owned 128-bit persistent module instance identity, not Unity MonoScript .meta GUID.
        public string InstanceGuid { get; set; } = "";
        public bool Detached { get; set; }
        public bool Consumed { get; set; }
        public bool IsKinematic { get; set; }
        // "root" attached; "world" detached. Full actual Transform parent binding is host responsibility.
        public string Parent { get; set; } = "";
        public Coordinate WorldPosition { get; set; } = new Coordinate();
        public Coordinate LinearVelocity { get; set; } = new Coordinate();
        public Coordinate AngularVelocity { get; set; } = new Coordinate();
    }
    public sealed class SavePayload
    {
        public int SchemaVersion { get; set; } = 1;
        public string SourceSha { get; set; } = Contract.SourceSha;
        public string SaveSlotGuid { get; set; } = "";
        public long RunEpoch { get; set; }
        public PartSnapshot[] Parts { get; set; } = Array.Empty<PartSnapshot>();
    }
    public sealed class SaveEnvelope
    {
        public string Format { get; set; } = "B01-62-STABLE-ID-V1";
        public string PayloadSha256 { get; set; } = "";
        public SavePayload Payload { get; set; } = new SavePayload();
    }
    public sealed class RestoredPart
    {
        public PartSnapshot Snapshot { get; }
        public AtomicDockLedger OriginalR2Ledger { get; }
        public RestoredPart(PartSnapshot snapshot, AtomicDockLedger ledger)
        {
            Snapshot=snapshot;OriginalR2Ledger=ledger;
        }
    }
    public sealed class RestorePlan
    {
        public string SaveSlotGuid { get; }
        public long RunEpoch { get; }
        public RestoredPart[] Parts { get; }
        internal RestorePlan(string slot,long epoch,RestoredPart[] parts)
        {SaveSlotGuid=slot;RunEpoch=epoch;Parts=parts;}
    }
    public static class Contract
    {
        public const string SourceSha="d94400d95d13db7f27e1533df05c9292d5380431";
        public const int MaxBytes=16384;
        private static readonly JsonSerializerOptions Options=new JsonSerializerOptions
        {
            PropertyNamingPolicy=JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive=false,
            UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,
            MaxDepth=16,
            WriteIndented=false
        };
        private static void Require(bool ok,string reason)
        {if(!ok)throw new InvalidDataException("SAVELOAD_HOLD_"+reason);}
        private static bool IsGuid(string s)=>
            s!=null && s.Length==32 && s.All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f'));
        private static void ValidateCoordinate(Coordinate p)
        {
            Require(p!=null,"MISSING_VECTOR");
            foreach(double v in new[]{p.X,p.Y,p.Z})
                Require(!double.IsNaN(v)&&!double.IsInfinity(v)&&Math.Abs(v)<=1000000,"INVALID_VECTOR");
        }
        public static void Validate(SavePayload p)
        {
            Require(p!=null && p.SchemaVersion==1,"SCHEMA_VERSION");
            Require(p.SourceSha==SourceSha,"SOURCE_SHA");
            Require(IsGuid(p.SaveSlotGuid),"SLOT_GUID");
            Require(p.RunEpoch>0 && p.RunEpoch<=long.MaxValue/2,"RUN_EPOCH");
            Require(p.Parts!=null && p.Parts.Length==2,"RIG_COUNT");
            var ids=new HashSet<long>();
            var guids=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<2;i++)
            {
                PartSnapshot s=p.Parts[i];
                Require(s!=null && s.Index==i,"RIG_INDEX_ORDER");
                long expected=i==0?2310001:2310002; // immutable #510 original SecureRigCatalog
                Require(s.PartId==expected && ids.Add(s.PartId),"PART_ID_MISMATCH_DUPLICATE");
                Require(IsGuid(s.InstanceGuid)&&guids.Add(s.InstanceGuid),"INSTANCE_GUID_MISSING_DUPLICATE");
                Require(s.InstanceGuid!=p.SaveSlotGuid,"INSTANCE_GUID_EQUALS_SLOT");
                Require(s.Detached==s.Consumed,"LEDGER_DETACH_ID_INVARIANT");
                Require(s.IsKinematic==!s.Detached,"RIG_KINEMATIC_INVARIANT");
                Require(s.Parent==(s.Detached?"world":"root"),"RIG_PARENT_INVARIANT");
                ValidateCoordinate(s.WorldPosition);ValidateCoordinate(s.LinearVelocity);
                ValidateCoordinate(s.AngularVelocity);
                if(!s.Detached)
                {
                    Require(s.LinearVelocity.X==0&&s.LinearVelocity.Y==0&&s.LinearVelocity.Z==0,
                        "ATTACHED_VELOCITY");
                    Require(s.AngularVelocity.X==0&&s.AngularVelocity.Y==0&&s.AngularVelocity.Z==0,
                        "ATTACHED_ANGULAR");
                }
            }
        }
        // Catch duplicate JSON keys recursively; System.Text.Json would otherwise silently last-win.
        private static void NoDuplicateJsonProperties(JsonElement root)
        {
            if(root.ValueKind==JsonValueKind.Object)
            {
                var names=new HashSet<string>(StringComparer.Ordinal);
                foreach(JsonProperty p in root.EnumerateObject())
                {
                    Require(names.Add(p.Name),"DUPLICATE_JSON_KEY");
                    NoDuplicateJsonProperties(p.Value);
                }
            }
            else if(root.ValueKind==JsonValueKind.Array)
                foreach(var item in root.EnumerateArray())NoDuplicateJsonProperties(item);
        }
        private static string Digest(SavePayload payload)
        {
            byte[] raw=JsonSerializer.SerializeToUtf8Bytes(payload,Options);
            return Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant();
        }
        public static byte[] Encode(SavePayload p)
        {
            Validate(p);
            SaveEnvelope envelope=new SaveEnvelope {Payload=p,PayloadSha256=Digest(p)};
            byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(envelope,Options);
            Require(bytes.Length<=MaxBytes,"TOO_LARGE");
            return bytes;
        }
        public static SavePayload Decode(byte[] bytes)
        {
            Require(bytes!=null&&bytes.Length>0&&bytes.Length<=MaxBytes,"INVALID_SIZE");
            // strict UTF8 rejects damaged or noncanonical text.
            string json;
            try {json=new UTF8Encoding(false,true).GetString(bytes);}
            catch(DecoderFallbackException e){throw new InvalidDataException("SAVELOAD_HOLD_INVALID_UTF8",e);}
            try
            {
                using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=16,AllowTrailingCommas=false,CommentHandling=JsonCommentHandling.Disallow});
                NoDuplicateJsonProperties(doc.RootElement);
                var envelope=JsonSerializer.Deserialize<SaveEnvelope>(json,Options);
                Require(envelope!=null&&envelope.Format=="B01-62-STABLE-ID-V1","FORMAT");
                Validate(envelope.Payload);
                Require(envelope.PayloadSha256==Digest(envelope.Payload),"PAYLOAD_HASH");
                // Reject noncanonical Unicode escapes, alternate number notation, property drift and untrusted serialization aliases.
                Require(bytes.AsSpan().SequenceEqual(Encode(envelope.Payload)),"NONCANONICAL_BYTES");
                return envelope.Payload;
            }
            catch(JsonException e){throw new InvalidDataException("SAVELOAD_HOLD_INVALID_JSON",e);}
        }
        public static void WriteNewSlot(string path,SavePayload p,Func<bool> hostIdle)
        {
            Require(hostIdle!=null&&hostIdle(),"BUSY_BEFORE_SAVE");
            byte[] bytes=Encode(p);
            // No overwrite. Only caller-owned new path. This is a pure C# sidecar, not Unity host serialization.
            string absolute=Path.GetFullPath(path);
            Require(!File.Exists(absolute)&&!Directory.Exists(absolute),"SLOT_ALREADY_EXISTS");
            Require(Directory.Exists(Path.GetDirectoryName(absolute)),"NO_PARENT_DIRECTORY");
            string temporary=absolute+"."+Guid.NewGuid().ToString("N")+".new";
            try
            {
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    stream.Write(bytes,0,bytes.Length);
                    stream.Flush(true);
                }
                Require(hostIdle(),"BUSY_DURING_SAVE");
                File.Move(temporary,absolute); // File.Move never replaces existing slot without overwrite
            }
            finally {if(File.Exists(temporary))File.Delete(temporary);}
        }
        public static RestorePlan ReadAndStage(string path,Func<bool> hostIdle)
        {
            Require(hostIdle!=null&&hostIdle(),"BUSY_BEFORE_LOAD");
            var info=new FileInfo(path);
            Require(info.Exists&&info.Length>0&&info.Length<=MaxBytes,"SAVE_MISSING_OVERSIZE");
            SavePayload payload=Decode(File.ReadAllBytes(path));
            Require(hostIdle(),"BUSY_DURING_LOAD");
            // Only construct FRESH detached-ID ledger candidates. No interaction with active Unity scene.
            var planned=new RestoredPart[2];
            for(int i=0;i<2;i++)
            {
                var s=payload.Parts[i];
                var l=new AtomicDockLedger(); // exact genuine frozen #510 original C#
                if(s.Detached)
                {
                    bool ok=l.TryDetach(s.PartId,new Vector3d(0,0,0),
                        new Vector3d(0,0,0),new Vector3d(0,0,0),1,0,0,out var impulse);
                    Require(ok&&impulse.MagnitudeSquared==0&&l.Contains(s.PartId)&&l.Count==1,
                        "LEDGER_REHYDRATION");
                }
                else Require(l.Count==0&&!l.Contains(s.PartId),"ATTACHED_LEDGER_NOT_EMPTY");
                planned[i]=new RestoredPart(s,l);
            }
            Require(hostIdle(),"BUSY_BEFORE_RETURN_PLAN");
            return new RestorePlan(payload.SaveSlotGuid,payload.RunEpoch,planned);
        }
    }
}
