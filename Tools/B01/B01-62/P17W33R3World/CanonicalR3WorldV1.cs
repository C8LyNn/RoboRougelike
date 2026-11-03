// B01-62 W33 / P0 #913 — versioned payload for ORIGINAL one Save V3 writer.
// DATA CODEC ONLY. Not a new Save writer, owner lease or Unity physical stage.
// 556 B01SV13! is FROZEN and untouched. Dynamic R3 data has its OWN MAGIC.
#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace B01.CanonicalR3World001
{
    public enum R3PartLocationV1 : byte
    {
        Mounted=0, Inventory=1, Detached=2
    }

    public sealed class R3SavedPartV1
    {
        // R3 stable GUID and separately mapped actual physical/ledger numeric ID.
        // A genuine host MUST prove mapping to real R3 / Rig[] on Unity main thread.
        public string Id="";
        public long PhysicalId, OwnerPhysicalId;
        public R3PartLocationV1 Location;
        public bool ConsumedDetach;
        public int Kind,Weapon,CellX,CellY,CellZ,TurnX,TurnY,TurnZ,Contacts;
        public float Hp=1;
        public int Ammo=-1,SteeringMode,UpgradeEffect,Group;
        public float ReloadReadyAt,DeviceReadyAt,DisruptionRemaining,ThermalHeat;
        public float DefenseCharge=-1,DefenseRechargeAge;
        public bool ThermalLocked;
        public string Source="",UpgradeId="",UpgradeTitle="",UpgradeDescription="";
        public float UpgradeAmount;
        // Physical rigidbody state must be captured from actual Rigidbody and
        // private dock ledger; this codec cannot authenticate that claim.
        public float Px,Py,Pz,Qx,Qy,Qz,Qw=1,Vx,Vy,Vz,Wx,Wy,Wz,Mass=1;
    }

    public sealed class CanonicalR3SnapshotV1
    {
        public long RunId,Generation;
        public int VehicleKind,DriveKind,SuspensionMode;
        public bool SuspensionEnabled=true,GyroEnabled=true;
        public float SuspensionHeightOffset,BoosterThrustNewtons=3000,
            BoosterDurationSeconds=1,HoverHeightMeters=1,GrappleReadyAt;
        public List<R3SavedPartV1> Parts=new List<R3SavedPartV1>();
    }

    public static class CanonicalR3WorldCodecV1
    {
        private const string Magic="B01R3W01";
        private const int Version=1;
        public const int MaxParts=1024;
        public const int MaxBytes=3*1024*1024;
        static readonly byte[] Prefix=Encoding.ASCII.GetBytes(Magic);

        public static bool IsR3World(byte[]? data)
        {
            if(data==null||data.Length<Prefix.Length)return false;
            for(int i=0;i<Prefix.Length;i++)
                if(data[i]!=Prefix[i])return false;
            return true;
        }
        private static bool Finite(float value,float bound)
            =>!float.IsNaN(value)&&!float.IsInfinity(value)&&
              value>=-bound&&value<=bound;
        private static bool Text(string? value,int max)
            =>value!=null&&value.Length<=max;
        private static bool PartValid(R3SavedPartV1 p)
        {
            if(p==null||!Guid.TryParseExact(p.Id,"N",out Guid id)||
               id==Guid.Empty||p.Id!=id.ToString("N")||
               p.PhysicalId<=0||p.OwnerPhysicalId<=0||
               p.Location<R3PartLocationV1.Mounted||
               p.Location>R3PartLocationV1.Detached||
               p.ConsumedDetach!=(p.Location==R3PartLocationV1.Detached)||
               p.Kind<0||p.Kind>18||
               // ORIGINAL R4WeaponKind enum at selected #877 has precisely
               // Basic..EmpEmitter (0..8). Only Gun may carry non-Basic.
               p.Weapon<0||p.Weapon>8||
               (p.Kind!=2&&p.Weapon!=0)||
               p.SteeringMode<0||p.SteeringMode>63||
               p.UpgradeEffect<0||p.UpgradeEffect>255||
               p.Group<0||p.Group>2||
               p.CellX< -512||p.CellX>512||
               p.CellY< -512||p.CellY>512||
               p.CellZ< -512||p.CellZ>512||
               p.TurnX< -3||p.TurnX>3||
               p.TurnY< -3||p.TurnY>3||
               p.TurnZ< -3||p.TurnZ>3||
               p.Contacts<0||p.Contacts>63||
               !Finite(p.Hp,1)||p.Hp<0||p.Ammo< -1||
               !Finite(p.ReloadReadyAt,1e8f)||
               !Finite(p.DeviceReadyAt,1e8f)||
               !Finite(p.DisruptionRemaining,1e8f)||
               !Finite(p.ThermalHeat,1e8f)||
               !Finite(p.DefenseCharge,1e8f)||
               !(float.IsPositiveInfinity(p.DefenseRechargeAge)||
                 Finite(p.DefenseRechargeAge,1e8f))||
               !Finite(p.UpgradeAmount,1e8f)||
               !Text(p.Source,256)||!Text(p.UpgradeId,256)||
               !Text(p.UpgradeTitle,256)||!Text(p.UpgradeDescription,1024)||
               !Finite(p.Px,100000)||!Finite(p.Py,100000)||
               !Finite(p.Pz,100000)||!Finite(p.Qx,1)||
               !Finite(p.Qy,1)||!Finite(p.Qz,1)||!Finite(p.Qw,1)||
               !Finite(p.Vx,10000)||!Finite(p.Vy,10000)||
               !Finite(p.Vz,10000)||!Finite(p.Wx,10000)||
               !Finite(p.Wy,10000)||!Finite(p.Wz,10000)||
               !Finite(p.Mass,10000)||p.Mass<=0)
                return false;
            double norm=(double)p.Qx*p.Qx+(double)p.Qy*p.Qy+
                (double)p.Qz*p.Qz+(double)p.Qw*p.Qw;
            return Math.Abs(norm-1d)<0.0002d;
        }
        public static bool Valid(CanonicalR3SnapshotV1? world)
        {
            if(world==null||world.RunId<=0||world.Generation<=0||
               world.VehicleKind<0||world.VehicleKind>2||
               world.DriveKind<0||world.DriveKind>3||
               world.SuspensionMode<0||world.SuspensionMode>16||
               !Finite(world.SuspensionHeightOffset,1000)||
               !Finite(world.BoosterThrustNewtons,1e8f)||
               !Finite(world.BoosterDurationSeconds,1e8f)||
               !Finite(world.HoverHeightMeters,1000)||
               !Finite(world.GrappleReadyAt,1e8f)||
               world.Parts==null||world.Parts.Count<1||
               world.Parts.Count>MaxParts)return false;
            var ids=new HashSet<string>(StringComparer.Ordinal);
            var physical=new HashSet<long>();
            int cores=0,mounted=0;
            long mountedOwner=0;
            foreach(var p in world.Parts)
            {
                if(!PartValid(p)||!ids.Add(p.Id)||!physical.Add(p.PhysicalId))
                    return false;
                if(p.Kind==3)
                {
                    if(p.Location!=R3PartLocationV1.Mounted)return false;
                    cores++;
                }
                if(p.Location==R3PartLocationV1.Mounted)
                {
                    // One physical mounted rig has ONE ledger owner. Otherwise
                    // a foreign vehicle part could be staged into this rig
                    // while all individual part IDs still appear unique.
                    if(mountedOwner==0)mountedOwner=p.OwnerPhysicalId;
                    if(p.OwnerPhysicalId!=mountedOwner)return false;
                    mounted++;
                }
            }
            return cores==1&&mounted>0;
        }

        public static byte[] Encode(CanonicalR3SnapshotV1 world)
        {
            if(!Valid(world))throw new InvalidDataException("Invalid live R3 canonical world");
            using var stream=new MemoryStream();
            using(var w=new BinaryWriter(stream,Encoding.UTF8,true))
            {
                w.Write(Prefix);w.Write(Version);
                w.Write(world.RunId);w.Write(world.Generation);
                w.Write(world.VehicleKind);w.Write(world.DriveKind);
                w.Write(world.SuspensionMode);
                w.Write(world.SuspensionEnabled);w.Write(world.GyroEnabled);
                w.Write(world.SuspensionHeightOffset);
                w.Write(world.BoosterThrustNewtons);
                w.Write(world.BoosterDurationSeconds);
                w.Write(world.HoverHeightMeters);w.Write(world.GrappleReadyAt);
                w.Write(world.Parts.Count);
                foreach(var p in world.Parts)
                {
                    w.Write(Guid.ParseExact(p.Id,"N").ToByteArray());
                    w.Write(p.PhysicalId);w.Write(p.OwnerPhysicalId);
                    w.Write((byte)p.Location);w.Write(p.ConsumedDetach);
                    w.Write(p.Kind);w.Write(p.Weapon);
                    w.Write(p.CellX);w.Write(p.CellY);w.Write(p.CellZ);
                    w.Write(p.TurnX);w.Write(p.TurnY);w.Write(p.TurnZ);
                    w.Write(p.Contacts);w.Write(p.Hp);w.Write(p.Ammo);
                    w.Write(p.SteeringMode);w.Write(p.UpgradeEffect);w.Write(p.Group);
                    w.Write(p.ReloadReadyAt);w.Write(p.DeviceReadyAt);
                    w.Write(p.DisruptionRemaining);w.Write(p.ThermalHeat);
                    w.Write(p.DefenseCharge);w.Write(p.DefenseRechargeAge);
                    w.Write(p.ThermalLocked);
                    w.Write(p.Source);w.Write(p.UpgradeId);
                    w.Write(p.UpgradeTitle);w.Write(p.UpgradeDescription);
                    w.Write(p.UpgradeAmount);
                    w.Write(p.Px);w.Write(p.Py);w.Write(p.Pz);
                    w.Write(p.Qx);w.Write(p.Qy);w.Write(p.Qz);w.Write(p.Qw);
                    w.Write(p.Vx);w.Write(p.Vy);w.Write(p.Vz);
                    w.Write(p.Wx);w.Write(p.Wy);w.Write(p.Wz);w.Write(p.Mass);
                }
                w.Flush();
            }
            byte[] data=stream.ToArray();
            if(data.Length>MaxBytes)
                throw new InvalidDataException("R3 world too large");
            return data;
        }
        public static bool TryDecode(byte[]? bytes,out CanonicalR3SnapshotV1? world)
        {
            world=null;
            if(!IsR3World(bytes)||bytes==null||
               bytes.Length>MaxBytes)return false;
            try
            {
                using var stream=new MemoryStream(bytes,false);
                using var r=new BinaryReader(stream,Encoding.UTF8,true);
                if(Encoding.ASCII.GetString(r.ReadBytes(8))!=Magic||
                   r.ReadInt32()!=Version)return false;
                var next=new CanonicalR3SnapshotV1{
                    RunId=r.ReadInt64(),Generation=r.ReadInt64(),
                    VehicleKind=r.ReadInt32(),DriveKind=r.ReadInt32(),
                    SuspensionMode=r.ReadInt32(),
                    SuspensionEnabled=r.ReadBoolean(),GyroEnabled=r.ReadBoolean(),
                    SuspensionHeightOffset=r.ReadSingle(),
                    BoosterThrustNewtons=r.ReadSingle(),
                    BoosterDurationSeconds=r.ReadSingle(),
                    HoverHeightMeters=r.ReadSingle(),GrappleReadyAt=r.ReadSingle()
                };
                int count=r.ReadInt32();
                if(count<1||count>MaxParts)return false;
                for(int i=0;i<count;i++)
                {
                    byte[] id=r.ReadBytes(16);
                    if(id.Length!=16)return false;
                    var p=new R3SavedPartV1{
                        Id=new Guid(id).ToString("N"),
                        PhysicalId=r.ReadInt64(),OwnerPhysicalId=r.ReadInt64(),
                        Location=(R3PartLocationV1)r.ReadByte(),
                        ConsumedDetach=r.ReadBoolean(),
                        Kind=r.ReadInt32(),Weapon=r.ReadInt32(),
                        CellX=r.ReadInt32(),CellY=r.ReadInt32(),CellZ=r.ReadInt32(),
                        TurnX=r.ReadInt32(),TurnY=r.ReadInt32(),TurnZ=r.ReadInt32(),
                        Contacts=r.ReadInt32(),Hp=r.ReadSingle(),Ammo=r.ReadInt32(),
                        SteeringMode=r.ReadInt32(),UpgradeEffect=r.ReadInt32(),
                        Group=r.ReadInt32(),
                        ReloadReadyAt=r.ReadSingle(),DeviceReadyAt=r.ReadSingle(),
                        DisruptionRemaining=r.ReadSingle(),ThermalHeat=r.ReadSingle(),
                        DefenseCharge=r.ReadSingle(),DefenseRechargeAge=r.ReadSingle(),
                        ThermalLocked=r.ReadBoolean(),
                        Source=r.ReadString(),UpgradeId=r.ReadString(),
                        UpgradeTitle=r.ReadString(),UpgradeDescription=r.ReadString(),
                        UpgradeAmount=r.ReadSingle(),
                        Px=r.ReadSingle(),Py=r.ReadSingle(),Pz=r.ReadSingle(),
                        Qx=r.ReadSingle(),Qy=r.ReadSingle(),Qz=r.ReadSingle(),
                        Qw=r.ReadSingle(),Vx=r.ReadSingle(),Vy=r.ReadSingle(),
                        Vz=r.ReadSingle(),Wx=r.ReadSingle(),Wy=r.ReadSingle(),
                        Wz=r.ReadSingle(),Mass=r.ReadSingle()
                    };
                    if(!PartValid(p))return false;
                    next.Parts.Add(p);
                }
                if(stream.Position!=stream.Length||!Valid(next))return false;
                // Canonical parsing: no alternate bool/string encoding, junk
                // trailing bytes, duplicated IDs or rewritten permutations.
                if(!Encode(next).SequenceEqual(bytes))return false;
                world=next;return true;
            }
            catch{return false;}
        }
    }
}
