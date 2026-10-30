// B01-62 W16 -- one selected original producer #587, no competing Save schema.
// Source-executable external receiver gate for missing generation/commit authority.
// NOT a patch of frozen #510/#556 or B01-23's selected adapter source.
using System;
using System.Security.Cryptography;
using B01.DetachedSaveContract001;
using B01.UnitySaveBridge001;

namespace B01.B0162.Selected587CheckpointGate
{
    public enum GateVerdict
    {
        Accepted, InvalidSnapshot, WrongRun, StaleGeneration,
        SameGenerationFork, Busy, Rejected, Quarantine
    }

    public sealed class CheckpointRecord
    {
        public readonly long Run, Generation;
        public readonly string Fingerprint;
        public CheckpointRecord(long run,long generation,string digest)
        { Run=run;Generation=generation;Fingerprint=digest; }
    }
    // CRITICAL: a production provider must keep this *exclusive*, survive process
    // restart, persist the pending intent BEFORE activation and record commit durably.
    // If a pending intent remains after crash, require quarantine/recovery.
    public interface ICheckpointAuthority
    {
        CheckpointRecord? Committed { get; }
        bool Pending { get; }
        void Begin(CheckpointRecord next);
        void Commit();
        void Cancel();
    }

    public sealed class Selected587CheckpointGate
    {
        private readonly AtomicWorldSwap selected;
        private readonly ICheckpointAuthority authority;
        private bool busy, quarantined;
        public bool Quarantined => quarantined || selected.Quarantined;
        public Selected587CheckpointGate(AtomicWorldSwap selected,ICheckpointAuthority authority)
        {
            this.selected=selected??throw new ArgumentNullException(nameof(selected));
            this.authority=authority??throw new ArgumentNullException(nameof(authority));
        }

        private static CheckpointRecord? Fingerprint(byte[]? data)
        {
            // Digest *canonical original #556* payload bytes; do not introduce a
            // competing schema. Original codec rejects bad GUID/ID/run/physics.
            if(!SnapshotCodec.TryDecode(data,out RunSnapshot? snapshot)||snapshot==null)
                return null;
            byte[] canonical=SnapshotCodec.Encode(snapshot);
            string digest=Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();
            return new CheckpointRecord(snapshot.RunId,snapshot.Generation,digest);
        }

        public GateVerdict TryLoad(byte[]? candidate,long run,long minimumGeneration,Func<bool> idle)
        {
            if(Quarantined)return GateVerdict.Quarantine;
            if(busy)return GateVerdict.Busy;
            busy=true;
            bool started=false,committed=false;
            try
            {
                if(authority.Pending)return Quarantine();
                // Original input COPY BEFORE invoking external world/idle callback.
                byte[]? privateBytes=candidate==null?null:(byte[])candidate.Clone();
                CheckpointRecord? incoming=Fingerprint(privateBytes);
                if(incoming==null)return GateVerdict.InvalidSnapshot;
                if(incoming.Run!=run||run<=0)return GateVerdict.WrongRun;
                if(minimumGeneration<=0||incoming.Generation<minimumGeneration)
                    return GateVerdict.StaleGeneration;
                CheckpointRecord? last=authority.Committed;
                if(last!=null)
                {
                    if(last.Run!=incoming.Run)return GateVerdict.WrongRun;
                    if(incoming.Generation<last.Generation)return GateVerdict.StaleGeneration;
                    if(incoming.Generation==last.Generation&&
                       !String.Equals(incoming.Fingerprint,last.Fingerprint,StringComparison.Ordinal))
                        return GateVerdict.SameGenerationFork;
                }
                // This action must be a crash-persistent PREPARE intent; NO physics yet.
                authority.Begin(incoming);
                started=true;
                WorldSwapVerdict verdict=selected.TryLoad(privateBytes!,run,minimumGeneration,idle);
                if(selected.Quarantined)return Quarantine(); // #582-style masked cleanup
                if(verdict!=WorldSwapVerdict.Accepted)
                    return verdict==WorldSwapVerdict.HostBusy?GateVerdict.Busy:
                           verdict==WorldSwapVerdict.StaleGeneration?GateVerdict.StaleGeneration:
                           verdict==WorldSwapVerdict.WrongRun?GateVerdict.WrongRun:
                           verdict==WorldSwapVerdict.InvalidSnapshot?GateVerdict.InvalidSnapshot:
                           GateVerdict.Rejected;
                // #572: also demand verified actual postactivation readback AFTER
                // producer's own post-activation Capture, BEFORE checkpoint promotion.
                if(selected.CleanupPending || !selected.TrySave(run,idle,out byte[] observed))
                    return Quarantine();
                if(!SnapshotCodec.TryDecode(observed,out RunSnapshot? world)||world==null||
                   !SnapshotCodec.TryDecode(privateBytes,out RunSnapshot? expected)||expected==null||
                   !AtomicWorldSwap.Consistent(world,expected))
                    return Quarantine();
                // Do not blindly compare bytes: real Unity float roundtrips have
                // a 1e-4 physical tolerance already specified by selected producer.
                authority.Commit();
                committed=true;
                return GateVerdict.Accepted;
            }
            catch {return Quarantine();}
            finally
            {
                if(started&&!committed&&!Quarantined)
                {
                    try {authority.Cancel();}
                    catch {quarantined=true;}
                }
                busy=false;
            }
        }

        public GateVerdict TrySave(long run,Func<bool> idle,out byte[]? data)
        {
            data=null;
            if(Quarantined)return GateVerdict.Quarantine;
            if(busy)return GateVerdict.Busy;
            busy=true;
            try
            {
                if(authority.Pending)return Quarantine();
                if(!selected.TrySave(run,idle,out byte[] saved))
                    return GateVerdict.Rejected;
                CheckpointRecord? observed=Fingerprint(saved);
                CheckpointRecord? previous=authority.Committed;
                if(observed==null)return Quarantine();
                if(previous!=null)
                {
                    if(previous.Run!=observed.Run)return GateVerdict.WrongRun;
                    if(observed.Generation<previous.Generation)
                        return GateVerdict.StaleGeneration;
                    // Moving physics with unchanged generation means checkpoint
                    // revision has not advanced. Reject until owner bumps epoch.
                    if(observed.Generation==previous.Generation&&
                       !String.Equals(observed.Fingerprint,previous.Fingerprint,StringComparison.Ordinal))
                        return GateVerdict.SameGenerationFork;
                }
                data=(byte[])saved.Clone();
                return GateVerdict.Accepted;
            }
            catch {return Quarantine();}
            finally {busy=false;}
        }
        private GateVerdict Quarantine(){quarantined=true;return GateVerdict.Quarantine;}
    }
}
