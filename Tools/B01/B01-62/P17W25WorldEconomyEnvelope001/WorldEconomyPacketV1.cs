// B01-62 W25 versioned ONE checkpoint wire: frozen ORIGINAL #556 bytes + real ORIGINAL B01-22 V6.
 // Serializer/validator ONLY. No disk APIs, no FileStream lease or second writer.
#nullable enable
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Globalization;
using B01.DetachedSaveContract001;
using RoboRougelike.B01.Economy.ApprovedV6;

namespace B01.WorldEconomyEnvelope001
{
    public enum PacketStatus { Accepted, Invalid, Corrupt, ForeignOwner, Stale }
    public sealed class GamePacket
    {
        public readonly long Run,Generation;
        public readonly Guid Slot;
        public readonly byte[] OriginalWorld556;
        public readonly ApprovedEconomySnapshotV6 Economy;
        public GamePacket(long run,long generation,Guid slot,byte[] world,ApprovedEconomySnapshotV6 economy)
        {Run=run;Generation=generation;Slot=slot;OriginalWorld556=world;Economy=economy;}
    }
    public static class WorldEconomyPacketV1
    {
        private static readonly byte[] Magic=Encoding.ASCII.GetBytes("B01W25E1");
        private const int Version=1;
        private const int MaxWorld=8*1024*1024,MaxEconomy=2*1024*1024;
        private static readonly JsonSerializerOptions Opt=new JsonSerializerOptions{
            IncludeFields=true,PropertyNameCaseInsensitive=false,WriteIndented=false
        };
        public static PacketStatus TryEncode(long run,Guid slot,long generation,
            byte[] original556,ApprovedEconomySnapshotV6 econ,out byte[] packet)
        {
            packet=Array.Empty<byte>();
            if(run<=0||slot==Guid.Empty||generation<=0||original556==null||
               econ==null||original556.Length>MaxWorld||!ApprovedEconomyPolicyV6.Valid(econ))
                return PacketStatus.Invalid;
            if(!ColdRestore.TryStage(original556,out ColdRestore? staged)||
                staged==null||staged.Snapshot.RunId!=run||
                staged.Snapshot.Generation!=generation||
                econ.Generation!=generation||econ.RunId!=run.ToString(CultureInfo.InvariantCulture)||
                econ.OwnerId!=slot.ToString("N"))
                return PacketStatus.ForeignOwner;
            byte[] economy;
            try{economy=JsonSerializer.SerializeToUtf8Bytes(econ,Opt);}
            catch{return PacketStatus.Invalid;}
            if(economy.Length<=0||economy.Length>MaxEconomy)return PacketStatus.Invalid;
            using var stream=new MemoryStream();
            using(var w=new BinaryWriter(stream,Encoding.UTF8,true))
            {
                w.Write(Magic);w.Write(Version);w.Write(run);w.Write(slot.ToByteArray());
                w.Write(generation);w.Write(original556.Length);w.Write(economy.Length);
                w.Write(original556);w.Write(economy);w.Flush();
            }
            byte[] prefix=stream.ToArray();
            byte[] digest=SHA256.HashData(prefix);
            stream.Write(digest,0,digest.Length);
            packet=stream.ToArray();
            return PacketStatus.Accepted;
        }
        public static PacketStatus TryDecode(byte[]? packet,long run,Guid slot,
            long minimumGeneration,out GamePacket? staged)
        {
            staged=null;
            if(run<=0||slot==Guid.Empty||minimumGeneration<0)return PacketStatus.Invalid;
            if(packet==null||packet.Length<8+4+8+16+8+4+4+32||
               packet.Length>MaxWorld+MaxEconomy+128)return PacketStatus.Corrupt;
            try
            {
                byte[] sha=SHA256.HashData(packet.AsSpan(0,packet.Length-32));
                if(!CryptographicOperations.FixedTimeEquals(
                     sha,packet.AsSpan(packet.Length-32,32)))
                    return PacketStatus.Corrupt;
                using var s=new MemoryStream(packet,false);
                using var rd=new BinaryReader(s,Encoding.UTF8,true);
                if(!rd.ReadBytes(8).AsSpan().SequenceEqual(Magic)||rd.ReadInt32()!=Version)
                    return PacketStatus.Corrupt;
                long observedRun=rd.ReadInt64();
                Guid observedSlot=new Guid(rd.ReadBytes(16));
                long generation=rd.ReadInt64();
                if(observedRun!=run||observedSlot!=slot)return PacketStatus.ForeignOwner;
                if(generation<minimumGeneration)return PacketStatus.Stale;
                if(generation<=0)return PacketStatus.Corrupt;
                int nWorld=rd.ReadInt32(),nEconomy=rd.ReadInt32();
                if(nWorld<=0||nWorld>MaxWorld||nEconomy<=0||nEconomy>MaxEconomy||
                   s.Position+(long)nWorld+nEconomy+32!=packet.Length)
                    return PacketStatus.Corrupt;
                byte[] world=rd.ReadBytes(nWorld),econBytes=rd.ReadBytes(nEconomy);
                if(world.Length!=nWorld||econBytes.Length!=nEconomy||
                   !ColdRestore.TryStage(world,out ColdRestore? original)||original==null||
                   original.Snapshot.RunId!=run||original.Snapshot.Generation!=generation)
                    return PacketStatus.Corrupt;
                ApprovedEconomySnapshotV6? economy=
                    JsonSerializer.Deserialize<ApprovedEconomySnapshotV6>(econBytes,Opt);
                if(economy==null||!ApprovedEconomyPolicyV6.Valid(economy)||
                   economy.Generation!=generation||
                   economy.RunId!=run.ToString(CultureInfo.InvariantCulture)||
                   economy.OwnerId!=slot.ToString("N"))
                    return PacketStatus.Corrupt;
                if(!JsonSerializer.SerializeToUtf8Bytes(economy,Opt).AsSpan()
                     .SequenceEqual(econBytes))return PacketStatus.Corrupt;
                staged=new GamePacket(run,generation,slot,
                     (byte[])world.Clone(),economy.Copy());
                return PacketStatus.Accepted;
            }
            catch{return PacketStatus.Corrupt;}
        }
    }
}
