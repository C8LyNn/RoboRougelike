// B01-23 P17 W13: pure C#9 Save/Load CONTRACT for frozen Secure V2 #510.
// Not a MonoBehaviour and NOT a retrofit into frozen #510's private Rig/Ledger.
// Do not claim native Unity snapshot/restore until an authorized future receiver
// adds a controlled main-thread capture + atomic fresh-world swap.
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using B01.DebrisRepulse001;

namespace B01.SecureV2SaveContract001
{
    public readonly struct V3
    {
        public readonly double X,Y,Z;
        public V3(double x,double y,double z) { X=x;Y=y;Z=z; }
        public static V3 Zero => new V3(0,0,0);
    }
    public readonly struct Q4
    {
        public readonly double X,Y,Z,W;
        public Q4(double x,double y,double z,double w) { X=x;Y=y;Z=z;W=w; }
        public static Q4 Identity => new Q4(0,0,0,1);
    }

    public sealed class PartRecord
    {
        public long Id {get;}
        public Guid ModuleGuid {get;}
        public bool Detached {get;}
        public bool Consumed {get;}
        public bool IsKinematic {get;}
        public V3 WorldPosition {get;}
        public Q4 WorldRotation {get;}
        public V3 LinearVelocity {get;}
        public V3 AngularVelocity {get;}
        public V3 LocalMount {get;}
        public PartRecord(long id,Guid guid,bool detached,bool consumed,bool kinematic,
            V3 position,Q4 rotation,V3 linear,V3 angular,V3 mount)
        {
            Id=id;ModuleGuid=guid;Detached=detached;Consumed=consumed;
            IsKinematic=kinematic;WorldPosition=position;WorldRotation=rotation;
            LinearVelocity=linear;AngularVelocity=angular;LocalMount=mount;
        }
    }

    public sealed class RunSnapshot
    {
        public string SourceSha {get;}
        public Guid RunId {get;}
        public long Generation {get;}
        public bool TransactionPending {get;}
        public PartRecord[] Parts {get;}
        public RunSnapshot(string sha,Guid runId,long generation,bool pending,PartRecord[] parts)
        {
            SourceSha=sha;RunId=runId;Generation=generation;TransactionPending=pending;
            Parts=parts == null ? Array.Empty<PartRecord>() : (PartRecord[])parts.Clone();
        }
    }

    public sealed class TrustedPart
    {
        public long Id {get;}
        public Guid ModuleGuid {get;}
        public V3 Mount {get;}
        public double RootX {get;}
        public TrustedPart(long id,Guid guid,V3 mount,double rootX)
        { Id=id;ModuleGuid=guid;Mount=mount;RootX=rootX; }
    }

    public sealed class TrustedManifest
    {
        // Frozen selected candidate only; alternate #511/#494 source SHAs are invalid.
        public const string SelectedSourceSha="d94400d95d13db7f27e1533df05c9292d5380431";
        public const int Schema=1;
        public readonly TrustedPart[] Parts;
        public TrustedManifest(TrustedPart[] parts)
        {
            if(parts==null || parts.Length!=2 || parts[0]==null || parts[1]==null ||
                parts[0].Id<=0 || parts[1].Id<=0 || parts[0].Id>=parts[1].Id ||
                parts[0].ModuleGuid==Guid.Empty || parts[1].ModuleGuid==Guid.Empty ||
                parts[0].ModuleGuid==parts[1].ModuleGuid)
                throw new InvalidDataException("TRUSTED_MANIFEST_BAD");
            Parts=(TrustedPart[])parts.Clone();
        }
        // DEMO ONLY: assigned stable module GUID registry from the receiver,
        // NOT Unity MonoScript .meta GUID, and not canonical game save GUID.
        public static TrustedManifest SelectedFixture()
        {
            var a=B01.SecureReattachV2.SecureRigCatalog.Light;
            var b=B01.SecureReattachV2.SecureRigCatalog.Heavy;
            return new TrustedManifest(new []{
                new TrustedPart(a.PartId,Guid.Parse("23100010-0000-4000-8000-000000000001"),
                    new V3(a.PartOffsetX,1.2,0),a.RootX),
                new TrustedPart(b.PartId,Guid.Parse("23100010-0000-4000-8000-000000000002"),
                    new V3(b.PartOffsetX,1.2,0),b.RootX)
            });
        }
    }

    public sealed class PreparedRestore
    {
        // Staging creates real ORIGINAL #510 AtomicDockLedger + W06 core for detached
        // IDs. It CANNOT replace #510 private Rig.Ledger or write Rigidbody state.
        public RunSnapshot Snapshot {get;}
        private readonly B01.SecureReattachV2.AtomicDockLedger[] staged;
        internal PreparedRestore(RunSnapshot s,B01.SecureReattachV2.AtomicDockLedger[] l)
        {Snapshot=s;staged=l;}
        public B01.SecureReattachV2.AtomicDockLedger StagedLedger(int index)
        {
            if(index<0||index>=staged.Length)throw new ArgumentOutOfRangeException(nameof(index));
            return staged[index];
        }
    }

    public static class StableIdSnapshotCodec
    {
        private static readonly byte[] Magic=Encoding.ASCII.GetBytes("B0123SL1");
        // exact 2-rig fixture, fixed binary length, strict trailing-data rejection.
        public const int TotalBytes=8+2+40+16+8+1+1+2*(8+16+3+16*8)+32;

        private static bool Finite(double v,double bound) =>
            !double.IsNaN(v) && !double.IsInfinity(v) && Math.Abs(v)<=bound;
        private static bool Valid(V3 p,double max) =>
            Finite(p.X,max)&&Finite(p.Y,max)&&Finite(p.Z,max);
        private static bool Equal(V3 a,V3 b) =>
            Math.Abs(a.X-b.X)<1e-7 && Math.Abs(a.Y-b.Y)<1e-7 && Math.Abs(a.Z-b.Z)<1e-7;
        private static bool Identity(Q4 q) =>
            Math.Abs(q.X)<1e-7 && Math.Abs(q.Y)<1e-7 &&
            Math.Abs(q.Z)<1e-7 && Math.Abs(q.W-1)<1e-7;

        public static void Validate(RunSnapshot s,TrustedManifest manifest,Guid expectedRun,long minGeneration)
        {
            if(s==null||manifest==null || s.SourceSha!=TrustedManifest.SelectedSourceSha ||
                s.RunId==Guid.Empty || s.RunId!=expectedRun || s.Generation<minGeneration ||
                s.Generation<0 || s.TransactionPending || s.Parts.Length!=2)
                throw new InvalidDataException("SNAPSHOT_HEADER_OR_TRANSACTION_REJECTED");
            for(int i=0;i<2;i++)
            {
                PartRecord p=s.Parts[i];
                TrustedPart t=manifest.Parts[i];
                if(p==null || p.Id!=t.Id || p.ModuleGuid!=t.ModuleGuid ||
                    p.ModuleGuid==Guid.Empty || !Equal(p.LocalMount,t.Mount) ||
                    p.Consumed!=p.Detached || p.IsKinematic==p.Detached)
                    throw new InvalidDataException("PART_ID_OR_LEDGER_STATE_REJECTED");
                if(!Valid(p.WorldPosition,100000) ||
                    !Valid(p.LinearVelocity,10000) || !Valid(p.AngularVelocity,10000) ||
                    !Valid(p.LocalMount,1000) || !Finite(p.WorldRotation.X,1) ||
                    !Finite(p.WorldRotation.Y,1) || !Finite(p.WorldRotation.Z,1) ||
                    !Finite(p.WorldRotation.W,1))
                    throw new InvalidDataException("NONFINITE_OR_OUT_OF_RANGE_POSE");
                double q=p.WorldRotation.X*p.WorldRotation.X+
                    p.WorldRotation.Y*p.WorldRotation.Y+
                    p.WorldRotation.Z*p.WorldRotation.Z+
                    p.WorldRotation.W*p.WorldRotation.W;
                if(Math.Abs(q-1)>1e-6)
                    throw new InvalidDataException("INVALID_ROTATION_QUATERNION");
                if(!p.Detached &&
                    (!Equal(p.WorldPosition,new V3(t.RootX+t.Mount.X,t.Mount.Y,t.Mount.Z)) ||
                     !Identity(p.WorldRotation) ||
                     !Equal(p.LinearVelocity,V3.Zero) ||
                     !Equal(p.AngularVelocity,V3.Zero)))
                    throw new InvalidDataException("ATTACHED_BODY_NOT_AT_AUTHORIZED_MOUNT");
            }
        }
        private static void Write3(BinaryWriter b,V3 v)
        {b.Write(v.X);b.Write(v.Y);b.Write(v.Z);}
        private static V3 Read3(BinaryReader b) =>new V3(b.ReadDouble(),b.ReadDouble(),b.ReadDouble());
        private static void Write4(BinaryWriter b,Q4 q)
        {b.Write(q.X);b.Write(q.Y);b.Write(q.Z);b.Write(q.W);}
        private static Q4 Read4(BinaryReader b) =>
            new Q4(b.ReadDouble(),b.ReadDouble(),b.ReadDouble(),b.ReadDouble());
        private static byte Flag(bool v)=>v?(byte)1:(byte)0;
        private static bool ReadFlag(BinaryReader b)
        {
            byte x=b.ReadByte();
            if(x!=0 && x!=1)throw new InvalidDataException("INVALID_BOOLEAN_ENCODING");
            return x==1;
        }
        private static byte[] Hash(byte[] bytes,int length)
        {
            using(var sha=SHA256.Create())return sha.ComputeHash(bytes,0,length);
        }
        public static byte[] Encode(RunSnapshot s,TrustedManifest m)
        {
            Validate(s,m,s.RunId,0);
            using(var stream=new MemoryStream())
            using(var b=new BinaryWriter(stream,Encoding.ASCII,true))
            {
                b.Write(Magic);
                b.Write((ushort)TrustedManifest.Schema);
                b.Write(Encoding.ASCII.GetBytes(s.SourceSha));
                b.Write(s.RunId.ToByteArray());
                b.Write(s.Generation);
                b.Write(Flag(s.TransactionPending));
                b.Write((byte)2);
                foreach(var p in s.Parts)
                {
                    b.Write(p.Id);b.Write(p.ModuleGuid.ToByteArray());
                    b.Write(Flag(p.Detached));b.Write(Flag(p.Consumed));b.Write(Flag(p.IsKinematic));
                    Write3(b,p.WorldPosition);Write4(b,p.WorldRotation);
                    Write3(b,p.LinearVelocity);Write3(b,p.AngularVelocity);Write3(b,p.LocalMount);
                }
                b.Flush();
                byte[] payload=stream.ToArray();
                if(payload.Length!=TotalBytes-32)
                    throw new InvalidDataException("INTERNAL_BINARY_SCHEMA_LENGTH");
                b.Write(Hash(payload,payload.Length));
                b.Flush();
                return stream.ToArray();
            }
        }
        public static RunSnapshot Decode(byte[] data,TrustedManifest manifest,Guid expectedRun,long minGeneration)
        {
            if(data==null || data.Length!=TotalBytes)
                throw new InvalidDataException("CORRUPT_OR_UNSUPPORTED_BINARY_LENGTH");
            byte[] digest=Hash(data,data.Length-32);
            int diff=0;
            for(int i=0;i<32;i++)diff|=digest[i]^data[data.Length-32+i];
            if(diff!=0)throw new InvalidDataException("CORRUPT_SNAPSHOT_SHA256");
            try
            {
                using(var ms=new MemoryStream(data,false))
                using(var b=new BinaryReader(ms,Encoding.ASCII,true))
                {
                    byte[] magic=b.ReadBytes(8);
                    if(!BytesEqual(magic,Magic))throw new InvalidDataException("UNSUPPORTED_SNAPSHOT_MAGIC");
                    int schema=b.ReadUInt16();
                    if(schema!=TrustedManifest.Schema)
                        throw new InvalidDataException("UNSUPPORTED_SNAPSHOT_SCHEMA_VERSION");
                    string source=Encoding.ASCII.GetString(b.ReadBytes(40));
                    Guid run=new Guid(b.ReadBytes(16));
                    long generation=b.ReadInt64();
                    bool pending=ReadFlag(b);
                    byte count=b.ReadByte();
                    if(count!=2)throw new InvalidDataException("UNSUPPORTED_PART_COUNT");
                    var parts=new PartRecord[2];
                    for(int i=0;i<2;i++)
                    {
                        long id=b.ReadInt64();
                        Guid guid=new Guid(b.ReadBytes(16));
                        bool detached=ReadFlag(b),consumed=ReadFlag(b),kinematic=ReadFlag(b);
                        V3 position=Read3(b);Q4 rotation=Read4(b);
                        V3 linear=Read3(b),angular=Read3(b),mount=Read3(b);
                        parts[i]=new PartRecord(id,guid,detached,consumed,kinematic,position,rotation,linear,angular,mount);
                    }
                    if(ms.Position!=data.Length-32)
                        throw new InvalidDataException("SNAPSHOT_TRAILING_DATA");
                    var snapshot=new RunSnapshot(source,run,generation,pending,parts);
                    Validate(snapshot,manifest,expectedRun,minGeneration);
                    return snapshot;
                }
            }
            catch(EndOfStreamException e)
            {
                throw new InvalidDataException("TRUNCATED_SNAPSHOT",e);
            }
        }
        private static bool BytesEqual(byte[] a,byte[] b)
        {
            if(a.Length!=b.Length)return false;
            int diff=0;for(int i=0;i<a.Length;i++)diff|=a[i]^b[i];
            return diff==0;
        }

        public static PreparedRestore Prepare(byte[] data,TrustedManifest authority,
            Guid expectedRun,long minGeneration,bool isDetachDockOrResetActive)
        {
            if(isDetachDockOrResetActive)
                throw new InvalidOperationException("SAVELOAD_BLOCKED_DURING_LIVE_TRANSACTION");
            // Entire validation completes before creating any new ledger; this method
            // NEVER touches existing Rigidbody, existing #510 ledgers or GameObjects.
            RunSnapshot snapshot=Decode(data,authority,expectedRun,minGeneration);
            var ledgers=new B01.SecureReattachV2.AtomicDockLedger[2];
            for(int i=0;i<2;i++)
            {
                PartRecord p=snapshot.Parts[i];
                var ledger=new B01.SecureReattachV2.AtomicDockLedger();
                if(p.Detached)
                {
                    Vector3d zero=new Vector3d(0,0,0);
                    if(!ledger.TryDetach(p.Id,zero,zero,zero,1,0,0,out Vector3d impulse) ||
                       ledger.Count!=1 || !ledger.Contains(p.Id) ||
                       impulse.MagnitudeSquared!=0)
                        throw new InvalidDataException("CANNOT_STAGE_ORIGINAL_W06_CONSUMED_ID");
                }
                else if(ledger.Count!=0 || ledger.Contains(p.Id))
                    throw new InvalidDataException("ATTACHED_ID_WAS_CONSUMED");
                ledgers[i]=ledger;
            }
            return new PreparedRestore(snapshot,ledgers);
        }
    }
}
