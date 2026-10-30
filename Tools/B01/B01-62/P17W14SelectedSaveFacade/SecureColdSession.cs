// B01-62 W14: receiver-only SEALED facade for EXACT original B01-23 #549 data schema.
// Keeps original RunSnapshot.Parts and ColdRestore.GetLedger OUT OF public receiver API.
// This is SOURCE-ONLY; never installs into frozen #510 private Unity Rig/Ledger.
using System;
using B01.DetachedSaveContract001;

namespace B01.B0162.SelectedSaveFacade
{
    public readonly struct PartView
    {
        public readonly long Id, OwnerId, ParentId;
        public readonly string AssetGuid, SocketGuid;
        public readonly bool Detached, Consumed, Kinematic;
        public readonly double PositionX, PositionY, PositionZ;
        public readonly double VelocityX, VelocityY, VelocityZ;
        public readonly double AngularX, AngularY, AngularZ;
        public readonly double Qx, Qy, Qz, Qw, MassKg;
        internal PartView(PersistedPart p)
        {
            Id=p.Id; OwnerId=p.OwnerId; ParentId=p.ParentId;
            AssetGuid=p.AssetGuid; SocketGuid=p.SocketGuid;
            Detached=p.State==PartDockState.Detached;
            Consumed=p.Consumed; Kinematic=p.Kinematic;
            PositionX=p.Position.X;PositionY=p.Position.Y;PositionZ=p.Position.Z;
            VelocityX=p.LinearVelocity.X;VelocityY=p.LinearVelocity.Y;VelocityZ=p.LinearVelocity.Z;
            AngularX=p.AngularVelocity.X;AngularY=p.AngularVelocity.Y;AngularZ=p.AngularVelocity.Z;
            Qx=p.Qx;Qy=p.Qy;Qz=p.Qz;Qw=p.Qw;MassKg=p.MassKg;
        }
    }

    public sealed class SecureColdSession
    {
        // Only this facade owns source-RED ColdSessionHandle; NO PUBLIC reference to it.
        private readonly ColdSessionHandle original=new ColdSessionHandle();

        public RestoreVerdict TryColdLoad(byte[]? source,long run,long generation,Func<bool> isHostIdle)
        {
            if(source==null)return RestoreVerdict.InvalidSnapshot;
            // Copy BEFORE invoking arbitrary caller hostIdle callback: protects raw save bytes
            // from mutation/reentrancy during validation or staging.
            var snapshot=(byte[])source.Clone();
            return original.TrySwap(snapshot,run,generation,isHostIdle);
        }

        public bool TryGetHeader(out long runId,out long generation)
        {
            var state=original.Current;
            runId=0;generation=0;
            if(state==null||!SnapshotRules.Valid(state.Snapshot))return false;
            runId=state.Snapshot.RunId;generation=state.Snapshot.Generation;
            return true;
        }

        public bool TryGetPart(int slot,out PartView part)
        {
            part=default;
            var state=original.Current;
            if(slot<0||slot>=FrozenPartCatalog.Slots||state==null||
                !SnapshotRules.Valid(state.Snapshot))return false;
            PersistedPart record=state.Snapshot.Parts[slot];
            // Check private actual original W06 ledger membership before exposing value.
            if(state.GetLedger(slot).Contains(record.Id)!=record.Consumed)
                return false;
            part=new PartView(record); // copies ALL scalar fields; zero mutable references
            return true;
        }

        public bool TryExport(out byte[]? data)
        {
            data=null;
            var state=original.Current;
            if(state==null||!SnapshotRules.Valid(state.Snapshot))return false;
            for(int slot=0;slot<FrozenPartCatalog.Slots;slot++)
            {
                PersistedPart p=state.Snapshot.Parts[slot];
                if(state.GetLedger(slot).Contains(p.Id)!=p.Consumed)return false;
            }
            // Writer returns fresh encoded bytes, never a reference to the validated array.
            data=SnapshotCodec.Encode(state.Snapshot);
            return true;
        }
    }
}
