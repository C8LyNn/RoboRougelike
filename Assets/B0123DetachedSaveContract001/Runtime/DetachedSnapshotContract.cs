using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using B01.DebrisRepulse001;

namespace B01.DetachedSaveContract001
{
    // Version 2: validated snapshot immutability and opaque W06 ledgers. This is NOT a patch to frozen #510.
    public enum PartDockState : byte { Attached=0, Detached=1 }
    public enum RestoreVerdict { Accepted, InvalidSnapshot, WrongRun, StaleGeneration, TransactionBusy, Quarantine }

    public sealed class PersistedPart
    {
        public readonly long Id, OwnerId, ParentId;
        public readonly string AssetGuid, SocketGuid;
        public readonly PartDockState State;
        public readonly bool Consumed, Kinematic;
        public readonly Vector3d Position, LinearVelocity, AngularVelocity;
        public readonly double Qx, Qy, Qz, Qw, MassKg;

        public PersistedPart(long id,long ownerId,long parentId,string assetGuid,string socketGuid,
            PartDockState state,bool consumed,bool kinematic,Vector3d position,
            double qx,double qy,double qz,double qw,Vector3d velocity,Vector3d angular,
            double massKg)
        {
            Id=id;OwnerId=ownerId;ParentId=parentId;AssetGuid=assetGuid;SocketGuid=socketGuid;
            State=state;Consumed=consumed;Kinematic=kinematic;Position=position;
            Qx=qx;Qy=qy;Qz=qz;Qw=qw;LinearVelocity=velocity;AngularVelocity=angular;MassKg=massKg;
        }
    }

    public sealed class RunSnapshot
    {
        public readonly long RunId, Generation;
        private readonly PersistedPart[] parts;
        // Defensive COPY: no caller receives the validated backing store.
        // PersistedPart contains readonly fields and immutable strings/values.
        public PersistedPart[] Parts => (PersistedPart[])parts.Clone();
        public RunSnapshot(long runId,long generation,IReadOnlyList<PersistedPart> parts)
        {
            RunId=runId;Generation=generation;
            if(parts==null)throw new ArgumentNullException(nameof(parts));
            this.parts=new PersistedPart[parts.Count];
            for(int i=0;i<parts.Count;i++)this.parts[i]=parts[i];
        }
    }

    // Fixed logical asset/socket IDs (NOT Unity .meta GUIDs), paired to frozen #510 rig IDs.
    public static class FrozenPartCatalog
    {
        public const string SelectedSourceSha="d94400d95d13db7f27e1533df05c9292d5380431";
        public const int Slots=2;
        public static long Id(int slot)=>slot==0?2310001:slot==1?2310002:throw new ArgumentOutOfRangeException(nameof(slot));
        public static long Owner(int slot)=>slot==0?41001:slot==1?42001:throw new ArgumentOutOfRangeException(nameof(slot));
        public static double Mass(int slot)=>slot==0?1.5:slot==1?12.0:throw new ArgumentOutOfRangeException(nameof(slot));
        public static string Asset(int slot)=>slot==0?"b0123001000000000000000000000001":slot==1?"b0123002000000000000000000000002":throw new ArgumentOutOfRangeException(nameof(slot));
        public static string Socket(int slot)=>slot==0?"b0123011000000000000000000000011":slot==1?"b0123012000000000000000000000012":throw new ArgumentOutOfRangeException(nameof(slot));
    }

    public static class SnapshotRules
    {
        private static bool Finite(double x,double max)=>!double.IsNaN(x)&&!double.IsInfinity(x)&&Math.Abs(x)<=max;
        private static bool Vector(Vector3d v,double max)=>Finite(v.X,max)&&Finite(v.Y,max)&&Finite(v.Z,max);
        public static bool Valid(RunSnapshot? s)
        {
            if(s==null||s.RunId<=0||s.Generation<=0||s.Parts.Length!=FrozenPartCatalog.Slots)return false;
            var ids=new HashSet<long>();
            var assets=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<s.Parts.Length;i++)
            {
                PersistedPart p=s.Parts[i];
                if(p==null||p.Id!=FrozenPartCatalog.Id(i)||p.OwnerId!=FrozenPartCatalog.Owner(i)||
                   p.AssetGuid!=FrozenPartCatalog.Asset(i)||p.SocketGuid!=FrozenPartCatalog.Socket(i)||
                   !ids.Add(p.Id)||!assets.Add(p.AssetGuid)||p.MassKg!=FrozenPartCatalog.Mass(i))
                    return false;
                bool detached=p.State==PartDockState.Detached;
                if(p.State!=PartDockState.Attached&&!detached)return false;
                if(p.Consumed!=detached||p.Kinematic==detached||
                   p.ParentId!=(detached?0:p.OwnerId))return false;
                if(!Vector(p.Position,100000)||
                   !Vector(p.LinearVelocity,10000)||!Vector(p.AngularVelocity,10000)||
                   !Finite(p.Qx,1)||!Finite(p.Qy,1)||!Finite(p.Qz,1)||!Finite(p.Qw,1))
                    return false;
                double n=p.Qx*p.Qx+p.Qy*p.Qy+p.Qz*p.Qz+p.Qw*p.Qw;
                if(Math.Abs(n-1.0)>0.0001)return false;
                if(!detached&&(p.LinearVelocity.MagnitudeSquared!=0||
                               p.AngularVelocity.MagnitudeSquared!=0))return false;
            }
            return true;
        }
    }

    // Deterministic little-endian, bounded binary, SHA256 corruption checksum (not an HMAC).
    // Signed/foreign / malicious content is NOT authenticated by the checksum.
    public static class SnapshotCodec
    {
        private static readonly byte[] Magic=Encoding.ASCII.GetBytes("B01SV14!");
        private const int Version=2;
        private const int DigestBytes=32;
        public const int MaxBytes=8192;
        public static byte[] Encode(RunSnapshot snap)
        {
            if(!SnapshotRules.Valid(snap))throw new InvalidDataException("Invalid detached-state ledger snapshot");
            using(var ms=new MemoryStream())
            {
                using(var w=new BinaryWriter(ms,Encoding.UTF8,true))
                {
                    w.Write(Magic);
                    w.Write(Version);
                    w.Write(Encoding.ASCII.GetBytes(FrozenPartCatalog.SelectedSourceSha));
                    w.Write(snap.RunId);w.Write(snap.Generation);w.Write(snap.Parts.Length);
                    for(int i=0;i<snap.Parts.Length;i++)
                    {
                        PersistedPart p=snap.Parts[i];
                        w.Write(p.Id);w.Write(p.OwnerId);w.Write(p.ParentId);
                        w.Write(Encoding.ASCII.GetBytes(p.AssetGuid));
                        w.Write(Encoding.ASCII.GetBytes(p.SocketGuid));
                        w.Write((byte)p.State);w.Write((byte)(p.Consumed?1:0));w.Write((byte)(p.Kinematic?1:0));
                        WriteVector(w,p.Position);
                        w.Write(p.Qx);w.Write(p.Qy);w.Write(p.Qz);w.Write(p.Qw);
                        WriteVector(w,p.LinearVelocity);WriteVector(w,p.AngularVelocity);w.Write(p.MassKg);
                    }
                    w.Flush();
                }
                byte[] body=ms.ToArray();
                using(var sha=SHA256.Create())
                {
                    byte[] digest=sha.ComputeHash(body);
                    byte[] result=new byte[body.Length+DigestBytes];
                    Buffer.BlockCopy(body,0,result,0,body.Length);
                    Buffer.BlockCopy(digest,0,result,body.Length,DigestBytes);
                    return result;
                }
            }
        }
        private static void WriteVector(BinaryWriter w,Vector3d v){w.Write(v.X);w.Write(v.Y);w.Write(v.Z);}
        private static Vector3d ReadVector(BinaryReader r)=>new Vector3d(r.ReadDouble(),r.ReadDouble(),r.ReadDouble());
        public static bool TryDecode(byte[]? bytes,out RunSnapshot? snapshot)
        {
            snapshot=null;
            if(bytes==null||bytes.Length<110||bytes.Length>MaxBytes)return false;
            int payloadSize=bytes.Length-DigestBytes;
            if(payloadSize<=0)return false;
            using(var sha=SHA256.Create())
            {
                byte[] expected=sha.ComputeHash(bytes,0,payloadSize);
                int diff=0;
                for(int i=0;i<DigestBytes;i++)diff|=expected[i]^bytes[payloadSize+i];
                if(diff!=0)return false;
            }
            try
            {
                using(var stream=new MemoryStream(bytes,0,payloadSize,false))
                using(var r=new BinaryReader(stream,Encoding.UTF8,false))
                {
                    byte[] magic=r.ReadBytes(Magic.Length);
                    if(magic.Length!=Magic.Length)return false;
                    for(int i=0;i<magic.Length;i++)if(magic[i]!=Magic[i])return false;
                    if(r.ReadInt32()!=Version)return false;
                    if(Encoding.ASCII.GetString(r.ReadBytes(40))!=FrozenPartCatalog.SelectedSourceSha)return false;
                    long runId=r.ReadInt64(),generation=r.ReadInt64();
                    int count=r.ReadInt32();
                    if(count!=FrozenPartCatalog.Slots)return false;
                    PersistedPart[] parts=new PersistedPart[count];
                    for(int i=0;i<count;i++)
                    {
                        long id=r.ReadInt64(),owner=r.ReadInt64(),parent=r.ReadInt64();
                        string asset=Encoding.ASCII.GetString(r.ReadBytes(32));
                        string socket=Encoding.ASCII.GetString(r.ReadBytes(32));
                        byte state=r.ReadByte(),consumed=r.ReadByte(),kinematic=r.ReadByte();
                        if(state>1||consumed>1||kinematic>1)return false;
                        Vector3d position=ReadVector(r);
                        double qx=r.ReadDouble(),qy=r.ReadDouble(),qz=r.ReadDouble(),qw=r.ReadDouble();
                        Vector3d linear=ReadVector(r),angular=ReadVector(r);
                        double mass=r.ReadDouble();
                        parts[i]=new PersistedPart(id,owner,parent,asset,socket,
                            (PartDockState)state,consumed==1,kinematic==1,position,
                            qx,qy,qz,qw,linear,angular,mass);
                    }
                    if(stream.Position!=stream.Length)return false; // no trailing/hidden members
                    var candidate=new RunSnapshot(runId,generation,parts);
                    if(!SnapshotRules.Valid(candidate))return false;
                    snapshot=candidate;
                    return true;
                }
            }
            catch(EndOfStreamException){return false;}
            catch(IOException){return false;}
            catch(ArgumentException){return false;}
            catch(OverflowException){return false;}
        }
    }

    // Cold, prevalidated detached-ID reconstruction from ACTUAL frozen W06 source.
    // This does NOT access or mutate #510's private AtomicDockLedger.
    public sealed class ColdRestore
    {
        public readonly RunSnapshot Snapshot;
        private readonly DebrisImpulseLedger[] rebuilt;
        private static readonly Vector3d Zero=new Vector3d(0,0,0);
        private ColdRestore(RunSnapshot snapshot,DebrisImpulseLedger[] ledgers)
        { Snapshot=snapshot;rebuilt=ledgers; }
        // Read-only ID checks ONLY. Do NOT expose the mutable W06 ledger,
        // which permits RearmAfterReattach without any physical dock transaction.
        public bool IsConsumed(int slot,long id)
        {
            if(slot<0||slot>=rebuilt.Length)throw new ArgumentOutOfRangeException(nameof(slot));
            return id==FrozenPartCatalog.Id(slot) && rebuilt[slot].Contains(id);
        }
        public int ConsumedCount(int slot)
        {
            if(slot<0||slot>=rebuilt.Length)throw new ArgumentOutOfRangeException(nameof(slot));
            return rebuilt[slot].Count;
        }
        public static bool TryStage(byte[] bytes,out ColdRestore? plan)
        {
            plan=null;
            if(!SnapshotCodec.TryDecode(bytes,out RunSnapshot? snap)||snap==null)return false;
            var built=new DebrisImpulseLedger[FrozenPartCatalog.Slots];
            for(int i=0;i<built.Length;i++)
            {
                built[i]=new DebrisImpulseLedger();
                PersistedPart p=snap.Parts[i];
                if(p.State==PartDockState.Detached)
                {
                    if(!built[i].TryDetach(p.Id,Zero,Zero,Zero,p.MassKg,0,0,out Vector3d impulse)||
                       impulse.MagnitudeSquared!=0||!built[i].Contains(p.Id)||built[i].Count!=1)
                        return false;
                }
                else if(built[i].Count!=0)return false;
            }
            plan=new ColdRestore(snap,built);
            return true;
        }
    }

    // Safe pointer-only cold logical session replacement. Unity host must provide
    // a separate verified transactional fresh-world adapter; none exists in #510.
    public sealed class ColdSessionHandle
    {
        public ColdRestore? Current { get;private set; }
        private bool swapping;
        public RestoreVerdict TrySwap(byte[] bytes,long expectedRunId,long minGeneration,
            Func<bool> hostIdle)
        {
            if(hostIdle==null)return RestoreVerdict.Quarantine;
            if(swapping)return RestoreVerdict.TransactionBusy;
            bool idle;
            try{idle=hostIdle();}
            catch{return RestoreVerdict.Quarantine;}
            if(!idle)return RestoreVerdict.TransactionBusy;
            if(!ColdRestore.TryStage(bytes,out ColdRestore? ready)||ready==null)
                return RestoreVerdict.InvalidSnapshot;
            if(expectedRunId<=0||ready.Snapshot.RunId!=expectedRunId)return RestoreVerdict.WrongRun;
            if(minGeneration<=0||ready.Snapshot.Generation<minGeneration||
               (Current!=null&&ready.Snapshot.Generation<Current.Snapshot.Generation))
                return RestoreVerdict.StaleGeneration;
            swapping=true;
            try
            {
                // Entire new logical session prepared before the only publish operation.
                Current=ready;
                return RestoreVerdict.Accepted;
            }
            finally{swapping=false;}
        }
    }
}
