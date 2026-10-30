using System;
using System.IO;
using B01.Canonical510Port001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;

static class Test
{
    static int good,bad;
    static void Check(string name,Func<bool> action)
    {
        try
        {
            if(action()){good++;Console.WriteLine("PASS "+name);}
            else {bad++;Console.WriteLine("FAIL "+name);}
        }
        catch(Exception ex){bad++;Console.WriteLine("FAIL "+name+": "+ex);}
    }
    static byte[] Make(long run,long gen,bool a,bool b)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool off=i==0?a:b;
            var s=SecureRigCatalog.At(i);
            parts[i]=new PersistedPart(s.PartId,FrozenPartCatalog.Owner(i),
                off?0:FrozenPartCatalog.Owner(i),FrozenPartCatalog.Asset(i),
                FrozenPartCatalog.Socket(i),off?PartDockState.Detached:PartDockState.Attached,
                off,!off,new Vector3d(s.RootX+s.PartOffsetX,off?2.2:1.2,0),
                0,0,0,1,off?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,gen,parts));
    }
    sealed class World : ICanonical510PreparedWorld
    {
        private readonly RunSnapshot state;
        private readonly bool wrongLedger;
        public bool IsOffstageInactive {get;private set;} = true;
        public bool Active,Retired;
        public World(RunSnapshot snapshot,bool badLedger){state=snapshot;wrongLedger=badLedger;}
        public bool HasPrivateR2LedgerForSlot(int slot,long id,bool consumed)
        {
            var p=state.Parts[slot];
            return !wrongLedger&&p.Id==id&&p.Consumed==consumed;
        }
        public RunSnapshot Capture()=>state;
        public void Activate(){IsOffstageInactive=false;Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Active=false;Retired=true;}
    }
    sealed class Owner : ICanonical510WorldOwner
    {
        public long RunId {get;set;}=51;
        public Guid PersistentSlotId {get;set;}=Guid.Parse("eb67f6b8-9292-4300-9eaa-fc271f1b72a8");
        public bool Idle=true,WrongLedger,StageActive;
        public World? Last;
        public bool MainThreadQuiescentAndOwnsLease()=>Idle;
        public ICanonical510PreparedWorld PrepareFreshPrivateRigAndLedger(RunSnapshot data)
        {
            Last=new World(data,WrongLedger);
            if(StageActive)Last.Activate();
            return Last;
        }
    }
    static bool WithDir(Func<string,bool> callback)
    {
        string root=Path.Combine(Path.GetTempPath(),"b0123canon_"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return callback(root);}
        finally{Directory.Delete(root,true);}
    }
    static int Main()
    {
        Check("original binary #556 decoded with frozen #510 part IDs",()=>
            SnapshotCodec.TryDecode(Make(51,1,true,false),out RunSnapshot? s)&&
            s!=null&&s.Parts[0].Id==2310001&&s.Parts[1].Id==2310002);
        Check("new owner port cold stage original W06 Light+Heavy consumed",()=>WithDir(dir=>{
            var owner=new Owner();var api=new Canonical510OwnerGateway(owner,dir);
            return api.TryRestoreExact556(Make(51,1,true,true))==WorldSwapVerdict.Accepted&&
                api.ContainsPrivateConsumedId(0,2310001)&&
                api.ContainsPrivateConsumedId(1,2310002)&&owner.Last!.Active;
        }));
        Check("owner quiescence must precede physical original world mutation",()=>WithDir(dir=>{
            var owner=new Owner{Idle=false};var api=new Canonical510OwnerGateway(owner,dir);
            return api.TryRestoreExact556(Make(51,1,true,false))==WorldSwapVerdict.HostBusy&&
                   owner.Last==null&&!api.HasWorld;
        }));
        Check("different run id is always rejected before staged world",()=>WithDir(dir=>{
            var owner=new Owner();var api=new Canonical510OwnerGateway(owner,dir);
            return api.TryRestoreExact556(Make(52,1,true,false))==WorldSwapVerdict.WrongRun&&
                   owner.Last==null;
        }));
        Check("wrong original private R2 consumed membership fails closed",()=>WithDir(dir=>{
            var owner=new Owner{WrongLedger=true};var api=new Canonical510OwnerGateway(owner,dir);
            return api.TryRestoreExact556(Make(51,1,true,false))==WorldSwapVerdict.HostException&&
                   !api.HasWorld&&owner.Last!.Retired;
        }));
        Check("live staged Rigidbody fails offstage admission",()=>WithDir(dir=>{
            var owner=new Owner{StageActive=true};var api=new Canonical510OwnerGateway(owner,dir);
            return api.TryRestoreExact556(Make(51,1,true,false))==WorldSwapVerdict.HostException&&
                owner.Last!.Retired&&!api.HasWorld;
        }));
        Check("identity and #556 Save from published original owner port",()=>WithDir(dir=>{
            var owner=new Owner();var api=new Canonical510OwnerGateway(owner,dir);
            if(api.TryRestoreExact556(Make(51,3,true,false))!=WorldSwapVerdict.Accepted||
               !api.TryCaptureExact556(out byte[] bytes)||
               !SnapshotCodec.TryDecode(bytes,out RunSnapshot? snap)||snap==null)return false;
            return snap.RunId==51&&snap.Generation==4&&snap.Parts[0].Consumed&&
                   !snap.Parts[1].Consumed;
        }));
        Check("cold re-entry same slot original bytes and generation",()=>WithDir(dir=>{
            var owner=new Owner();var api=new Canonical510OwnerGateway(owner,dir);
            byte[] orig=Make(51,5,false,true);
            if(api.TryRestoreExact556(orig)!=WorldSwapVerdict.Accepted)return false;
            var restarted=new Canonical510OwnerGateway(new Owner(),dir);
            return restarted.TryRestoreExact556(orig)==WorldSwapVerdict.Accepted&&
                   restarted.ContainsPrivateConsumedId(1,2310002);
        }));
        Check("same-generation conflicting consumed identity rejected",()=>WithDir(dir=>{
            var api=new Canonical510OwnerGateway(new Owner(),dir);
            if(api.TryRestoreExact556(Make(51,3,true,false))!=WorldSwapVerdict.Accepted)return false;
            var second=new Canonical510OwnerGateway(new Owner(),dir);
            return second.TryRestoreExact556(Make(51,3,false,true))==WorldSwapVerdict.StaleGeneration;
        }));
        Check("wrong authenticated slot does not share checkpoint file",()=>WithDir(dir=>{
            var a=new Canonical510OwnerGateway(new Owner(),dir);
            var b=new Canonical510OwnerGateway(new Owner{PersistentSlotId=Guid.NewGuid()},dir);
            return a.TryRestoreExact556(Make(51,5,true,false))==WorldSwapVerdict.Accepted&&
                   b.TryRestoreExact556(Make(51,1,false,true))==WorldSwapVerdict.Accepted;
        }));
        Check("corrupted binary must fail before owner Prepare",()=>WithDir(dir=>{
            var owner=new Owner();var api=new Canonical510OwnerGateway(owner,dir);
            var bad=Make(51,1,true,false);bad[80]^=1;
            return api.TryRestoreExact556(bad)==WorldSwapVerdict.InvalidSnapshot&&owner.Last==null;
        }));
        Console.WriteLine("TOTAL "+(good+bad)+" PASS "+good+" FAIL "+bad);
        Console.WriteLine("NATIVE_UNITY_NOT_RUN / ORIGINAL_PRIVATE_510_OWNER_IMPLEMENTATION_NOT_PRESENT / G2_HOLD");
        return bad==0?0:1;
    }
}
