// B01-62/W16: standalone fail-closed persistent generation journal for selected #587
// Source-only; no Unity editor/native physics, no claim of power-loss-directory fsync.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace B01.W16Selected587Repair
{
    public sealed class FileGenerationAuthority : IDurableGenerationAuthority
    {
        private readonly string directory;
        private const string Header="B01-62-W16-556-EPOCH-V1";
        private const string Extension=".commit";
        public FileGenerationAuthority(string existingOwnedDirectory)
        {
            if(String.IsNullOrWhiteSpace(existingOwnedDirectory))
                throw new ArgumentException("Authoritative owned root required",nameof(existingOwnedDirectory));
            directory=Path.GetFullPath(existingOwnedDirectory);
            if(!Directory.Exists(directory))
                throw new InvalidDataException("Missing dedicated authority directory");
        }
        private sealed class Record
        {
            public long Run;
            public long Epoch;
            public string Hash="";
        }
        private static string Hash(byte[] b)
        {
            if(b==null||b.Length==0||b.Length>8192)
                throw new InvalidDataException("Invalid selected #556 payload");
            return Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
        }
        private static string Body(long run,long epoch,string hash)=>
            Header+"\n"+run.ToString(System.Globalization.CultureInfo.InvariantCulture)+
            "\n"+epoch.ToString(System.Globalization.CultureInfo.InvariantCulture)+
            "\n"+hash+"\n";
        private List<Record> Inspect()
        {
            var root=new DirectoryInfo(directory);
            if(!root.Exists||(root.Attributes&FileAttributes.ReparsePoint)!=0)
                throw new InvalidDataException("Authority root is missing/link");
            var records=new List<Record>();
            foreach(var entry in root.EnumerateFileSystemInfos())
            {
                if((entry.Attributes&FileAttributes.ReparsePoint)!=0 ||
                   (entry.Attributes&FileAttributes.Directory)!=0 ||
                   !entry.Name.StartsWith("epoch-",StringComparison.Ordinal) ||
                   !entry.Name.EndsWith(Extension,StringComparison.Ordinal))
                    throw new InvalidDataException("Foreign/temporary entry in authority journal");
                string middle=entry.Name.Substring(6,entry.Name.Length-6-Extension.Length);
                if(middle.Length!=20||!middle.All(char.IsDigit))
                    throw new InvalidDataException("Noncanonical journal filename");
                if(!long.TryParse(middle,out long epoch)||epoch<=0)
                    throw new InvalidDataException("Invalid epoch filename");
                if(entry.Length>256||entry.Length<60)
                    throw new InvalidDataException("Invalid record size");
                var raw=File.ReadAllBytes(entry.FullName);
                string val=new UTF8Encoding(false,true).GetString(raw);
                var lines=val.Split('\n');
                if(lines.Length!=5||lines[0]!=Header ||lines[4]!="")
                    throw new InvalidDataException("Malformed persisted authority");
                if(!long.TryParse(lines[1],out long run)||run<=0 ||
                   !long.TryParse(lines[2],out long stored)||stored!=epoch)
                    throw new InvalidDataException("Invalid committed run/generation");
                string h=lines[3];
                if(h.Length!=64 ||!h.All(ch=>(ch>='0'&&ch<='9')||(ch>='a'&&ch<='f')))
                    throw new InvalidDataException("Invalid commit digest");
                if(val!=Body(run,epoch,h))
                    throw new InvalidDataException("Noncanonical persisted authority");
                records.Add(new Record{Run=run,Epoch=epoch,Hash=h});
            }
            records.Sort((a,b)=>a.Epoch.CompareTo(b.Epoch));
            for(int i=1;i<records.Count;i++)
            {
                if(records[i].Run!=records[0].Run ||records[i].Epoch!=records[i-1].Epoch+1)
                    throw new InvalidDataException("Fork or missing committed generation");
            }
            return records;
        }
        public AuthorityDecision Evaluate(long run,long generation,byte[] exact556Bytes)
        {
            if(run<=0||generation<=0)return AuthorityDecision.Reject;
            string hash=Hash(exact556Bytes);
            var ledger=Inspect();
            if(ledger.Count==0)return AuthorityDecision.Advance;
            var top=ledger[ledger.Count-1];
            if(top.Run!=run)return AuthorityDecision.Reject;
            if(generation==top.Epoch)
                return hash==top.Hash?AuthorityDecision.Replay:AuthorityDecision.Reject;
            // After first checkpoint every next commit is sequential to prevent
            // simultaneous out-of-order generations and orphaned gaps.
            return generation>top.Epoch && generation==top.Epoch+1
                ?AuthorityDecision.Advance:AuthorityDecision.Reject;
        }
        public bool TryCommit(long run,long generation,byte[] exact556Bytes)
        {
            if(Evaluate(run,generation,exact556Bytes)!=AuthorityDecision.Advance)
                return false;
            string file="epoch-"+generation.ToString("D20",System.Globalization.CultureInfo.InvariantCulture)+Extension;
            string dest=Path.Combine(directory,file), tmp=dest+"."+Guid.NewGuid().ToString("N")+".pending";
            try
            {
                var data=Encoding.UTF8.GetBytes(Body(run,generation,Hash(exact556Bytes)));
                using(var s=new FileStream(tmp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    s.Write(data,0,data.Length);
                    s.Flush(true);
                }
                // File.Move MUST fail on a same-generation foreign journal.
                File.Move(tmp,dest);
                return Evaluate(run,generation,exact556Bytes)==AuthorityDecision.Replay;
            }
            finally
            {
                if(File.Exists(tmp))File.Delete(tmp);
            }
        }
    }
}
