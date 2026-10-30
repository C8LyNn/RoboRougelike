using System;
using System.Collections.Generic;
using System.IO;

namespace B01.UnitySaveBridge001
{
    // R3 singleton authority for ONE demo checkpoint slot, in-process AND across
    // distinct managed processes. Does not replace the selected R2 disk CAS.
    // Lease file is intentionally NEVER removed: deleting a lock pathname between
    // two holders can let another process open a second lock inode on Unix.
    public sealed class HostSlotLease : IDisposable
    {
        private static readonly object Sync = new object();
        private static readonly HashSet<string> Live =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly string key;
        private FileStream? stream;
        private HostSlotLease(string file, FileStream gate) { key=file;stream=gate; }
        public bool Owned { get { lock(Sync) return stream!=null && Live.Contains(key); } }

        public static bool TryAcquire(string checkpointPath, out HostSlotLease? lease)
        {
            lease=null;
            if(string.IsNullOrWhiteSpace(checkpointPath) ||
               !Path.IsPathRooted(checkpointPath))return false;
            string path;
            try { path=Path.GetFullPath(checkpointPath)+".ownerlock"; }
            catch(ArgumentException){return false;}
            catch(NotSupportedException){return false;}
            lock(Sync)
            {
                if(Live.Contains(path))return false;
                FileStream? stream=null;
                try
                {
                    string dir=Path.GetDirectoryName(path) ??
                        throw new IOException("Missing private lock directory");
                    Directory.CreateDirectory(dir);
                    // Never follow an existing user/symlink redirect as an owner lock.
                    if(File.Exists(path) &&
                       (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)
                        return false;
                    stream=new FileStream(path,FileMode.OpenOrCreate,
                        FileAccess.ReadWrite,FileShare.None);
                    // Keep handle OPEN until the host GameObject is truly destroyed.
                    // Disabling an active old host must NOT release its authority.
                    Live.Add(path);
                    lease=new HostSlotLease(path,stream);
                    return true;
                }
                catch(IOException){stream?.Dispose();return false;}
                catch(UnauthorizedAccessException){stream?.Dispose();return false;}
            }
        }

        public void Dispose()
        {
            lock(Sync)
            {
                if(stream==null)return;
                // Close before unregistering, so a same-process race cannot sneak
                // between managed guard release and OS file handle close.
                try{stream.Dispose();}
                finally{stream=null;Live.Remove(key);}
            }
        }
    }
}
