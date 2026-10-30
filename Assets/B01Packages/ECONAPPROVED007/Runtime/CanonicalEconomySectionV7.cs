// ECONAPPROVED007 / B01-22 / W25. Pure bounded binary SECTION, NOT a
// production file writer, SaveSlot, owner lock or whole #556 serializer.
// Receiver #749/#773 must embed these bytes in the SAME FULL world+wallet
// canonical envelope under ONE physical OS owner and one atomic commit.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace RoboRougelike.B01.Economy.ApprovedV7
{
    public static class CanonicalEconomySectionV7
    {
        public const int MaxSectionBytes=8*1024*1024;
        public const int MaxItems=10000;
        public const int MaxStringLength=512;
        public const int HeaderBytes=8+4+32;
        static readonly byte[] Magic=Encoding.ASCII.GetBytes("B01ECN7!");
        static readonly Encoding Utf8=new UTF8Encoding(false,true);
        static byte[] Hash(byte[] b)
        {
            using(var h=SHA256.Create())return h.ComputeHash(b);
        }
        static void WriteString(BinaryWriter w,string text)
        {
            if(text==null)text="";
            if(text.Length>MaxStringLength)throw new InvalidDataException("STRING_BOUND");
            w.Write(text);
        }
        static string ReadString(BinaryReader r)
        {
            string s=r.ReadString();
            if(s.Length>MaxStringLength)throw new InvalidDataException("STRING_BOUND");
            return s;
        }
        static void Count(BinaryWriter w,int n)
        {
            if(n<0||n>MaxItems)throw new InvalidDataException("ITEM_BOUND");
            w.Write(n);
        }
        static int ReadCount(BinaryReader r)
        {
            int n=r.ReadInt32();
            if(n<0||n>MaxItems)throw new InvalidDataException("ITEM_BOUND");
            return n;
        }
        public static bool TryEncode(ApprovedEconomySnapshotV7 state,
            out byte[] section,out string reason)
        {
            section=null;
            if(!ApprovedEconomyPolicyV7.Valid(state))
            {reason="INVALID_OWNER_ECONOMY";return false;}
            try
            {
                byte[] payload;
                using(var ms=new MemoryStream())
                {
                    using(var w=new BinaryWriter(ms,Utf8,true))
                    {
                        w.Write(7);
                        WriteString(w,state.OwnerId);
                        WriteString(w,state.RunId);
                        w.Write(state.Generation);
                        w.Write(state.Wins);
                        w.Write(state.Scrap);
                        w.Write(state.FreeUsedForWin);
                        Count(w,state.Parts.Count);
                        foreach(var p in state.Parts)
                        {
                            WriteString(w,p.Id);WriteString(w,p.Category);
                            WriteString(w,p.ReservationId);
                            w.Write(p.Hp);w.Write(p.PurchaseScrap);
                        }
                        Count(w,state.Pending.Count);
                        foreach(var t in state.Pending)
                        {
                            WriteString(w,t.ModuleId);
                            WriteString(w,t.ReservationId);
                            w.Write(t.AfterWins);w.Write(t.ChargedScrap);
                            w.Write(t.PurchaseScrap);w.Write(t.Free);
                        }
                        Count(w,state.ConsumedVictoryIds.Count);
                        foreach(var id in state.ConsumedVictoryIds)WriteString(w,id);
                        w.Write((int)state.Loaner.Phase);
                        w.Write(state.Loaner.EverEntered);
                        WriteString(w,state.Loaner.EncounterId);
                        WriteString(w,state.Loaner.PermitId);
                    }
                    payload=ms.ToArray();
                }
                if(payload.Length==0||payload.Length>MaxSectionBytes-HeaderBytes)
                    throw new InvalidDataException("SECTION_BOUND");
                using(var ms=new MemoryStream())
                {
                    using(var w=new BinaryWriter(ms,Utf8,true))
                    {
                        w.Write(Magic);w.Write(payload.Length);
                        w.Write(Hash(payload));w.Write(payload);
                    }
                    section=ms.ToArray();
                }
                reason="OK";return true;
            }
            catch(Exception ex) when(ex is IOException||ex is ArgumentException||
                ex is OutOfMemoryException||ex is OverflowException)
            {reason="ECON_SECTION_ENCODE_REJECTED";return false;}
        }
        public static bool TryDecode(byte[] bytes,
            out ApprovedEconomySnapshotV7 state,out string reason)
        {
            state=null;
            if(bytes==null||bytes.Length<HeaderBytes+1||bytes.Length>MaxSectionBytes)
            {reason="ECON_SECTION_SIZE";return false;}
            try
            {
                using(var ms=new MemoryStream(bytes,false))
                using(var r=new BinaryReader(ms,Utf8,true))
                {
                    if(!r.ReadBytes(8).SequenceEqual(Magic))
                    {reason="ECON_SECTION_MAGIC";return false;}
                    int len=r.ReadInt32();
                    if(len<=0||len!=bytes.Length-HeaderBytes)
                    {reason="ECON_SECTION_LENGTH";return false;}
                    var hash=r.ReadBytes(32);
                    var payload=r.ReadBytes(len);
                    if(hash.Length!=32||payload.Length!=len||
                       !hash.SequenceEqual(Hash(payload)))
                    {reason="ECON_SECTION_CHECKSUM";return false;}
                    using(var ps=new MemoryStream(payload,false))
                    using(var pr=new BinaryReader(ps,Utf8,true))
                    {
                        var version=pr.ReadInt32();
                        if(version!=7){reason="ECON_VERSION";return false;}
                        var candidate=new ApprovedEconomySnapshotV7 {
                            OwnerId=ReadString(pr),RunId=ReadString(pr),
                            Generation=pr.ReadInt64(),
                            Wins=pr.ReadInt32(),
                            Scrap=pr.ReadInt32(),
                            FreeUsedForWin=pr.ReadInt32()
                        };
                        int count=ReadCount(pr);
                        for(int i=0;i<count;i++)
                            candidate.Parts.Add(new OwnedModuleV7 {
                                Id=ReadString(pr),
                                Category=ReadString(pr),
                                ReservationId=ReadString(pr),
                                Hp=pr.ReadSingle(),
                                PurchaseScrap=pr.ReadInt32()
                            });
                        count=ReadCount(pr);
                        for(int i=0;i<count;i++)
                            candidate.Pending.Add(new RebuildTicketV7 {
                                ModuleId=ReadString(pr),
                                ReservationId=ReadString(pr),
                                AfterWins=pr.ReadInt32(),
                                ChargedScrap=pr.ReadInt32(),
                                PurchaseScrap=pr.ReadInt32(),
                                Free=pr.ReadBoolean()
                            });
                        count=ReadCount(pr);
                        for(int i=0;i<count;i++)
                            candidate.ConsumedVictoryIds.Add(ReadString(pr));
                        candidate.Loaner.Phase=(LoanerPhaseV7)pr.ReadInt32();
                        candidate.Loaner.EverEntered=pr.ReadBoolean();
                        candidate.Loaner.EncounterId=ReadString(pr);
                        candidate.Loaner.PermitId=ReadString(pr);
                        if(ps.Position!=ps.Length||!ApprovedEconomyPolicyV7.Valid(candidate))
                        {reason="INVALID_DECODED_ECONOMY";return false;}
                        state=candidate;reason="OK";return true;
                    }
                }
            }
            catch(Exception ex) when(ex is IOException||ex is ArgumentException||
                ex is OutOfMemoryException||ex is OverflowException)
            {reason="ECON_SECTION_CORRUPT";return false;}
        }
    }
}
