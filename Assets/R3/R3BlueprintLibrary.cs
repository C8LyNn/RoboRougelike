using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoboRougelike.R1;
using RoboRougelike.R4;
using UnityEngine;

namespace RoboRougelike.R3
{
    // A blueprint describes a build, never owned identities or resources.
    [Serializable] public sealed class R3BlueprintPart
    {
        public Vector3Int cell,rotation;
        public R3PartKind kind;
        public R4WeaponKind weapon;
        public R3WheelStyle wheel;
        public R3ArmorShape shape;
        public int tier,footprint,contacts;
        public string upgradeId;
        public R1UpgradeEffect upgradeEffect;
        public R1ModifierOperation upgradeOperation;
        public R1BonusScope upgradeScope;
        public float upgradeAmount;
        public static R3BlueprintPart FromPart(R3PartState p,Vector3Int core)=>new R3BlueprintPart{
            cell=p.Cell-core,rotation=p.RotationQuarterTurns,kind=p.Kind,weapon=p.Weapon,wheel=p.WheelStyle,
            shape=p.ArmorShape,tier=R3VehicleModel.NormalizedTier(p.Tier),footprint=p.FootprintRevision,contacts=p.ContactMask,
            upgradeId=p.UpgradeId,upgradeEffect=p.UpgradeEffect,upgradeOperation=p.UpgradeOperation,upgradeScope=p.UpgradeScope,upgradeAmount=p.UpgradeAmount};
        public bool Matches(R3PartState p)=>p!=null&&p.Kind==kind&&R3VehicleModel.NormalizedTier(p.Tier)==tier&&
            (kind!=R3PartKind.Gun||(p.Weapon==weapon&&p.FootprintRevision==footprint))&&
            (kind!=R3PartKind.Wheel||(p.WheelStyle==wheel&&p.FootprintRevision==footprint))&&
            (kind!=R3PartKind.Armor||p.ArmorShape==shape)&&
            (kind!=R3PartKind.BonusModule||(p.UpgradeId==upgradeId&&p.UpgradeEffect==upgradeEffect&&p.UpgradeOperation==upgradeOperation&&p.UpgradeScope==upgradeScope&&p.UpgradeAmount==upgradeAmount));
    }
    [Serializable] public sealed class R3Blueprint
    {
        public int version=1;
        public string title;
        public R3BlueprintPart[] parts;
        public int[] weapons,active,deployables;
        public string[] globalBindings;
    }
    public sealed class R3BlueprintPreview
    {
        public int RequiredCount {get;internal set;}
        public int AvailableCount {get;internal set;}
        public int MissingCount=>RequiredCount-AvailableCount;
        public int BrokenCount {get;internal set;}
        public IReadOnlyList<string> Missing {get;internal set;}
    }
    public sealed class R3BlueprintPlacement
    {
        public string Id {get;internal set;}
        public Vector3Int Cell {get;internal set;}
        public Vector3Int Rotation {get;internal set;}
        public bool AlreadyPlaced {get;internal set;}
    }
    public sealed class R3BlueprintPlan
    {
        public R3BlueprintPreview Preview {get;internal set;}
        public IReadOnlyList<R3BlueprintPlacement> Placements {get;internal set;}
    }
    public sealed class R3BlueprintLibrary
    {
        public const int DefaultSlots=4,MaximumSlots=20,MaximumParts=1000;
        readonly string root;
        public int SlotCount {get;}
        public string RootDirectory=>root;
        public static string DefaultDirectory=>Path.Combine(Application.persistentDataPath,"Blueprints");
        public R3BlueprintLibrary(string directory,int slots=DefaultSlots)
        {
            if(string.IsNullOrWhiteSpace(directory)||slots<1||slots>MaximumSlots)throw new ArgumentException("Niepoprawna biblioteka projektów.");
            root=directory;SlotCount=slots;
        }
        string SlotPath(int slot)
        {if(slot<0||slot>=SlotCount)throw new ArgumentOutOfRangeException(nameof(slot));return Path.Combine(root,"slot-"+(slot+1)+".json");}
        public bool Exists(int slot)=>File.Exists(SlotPath(slot));
        static int[] WeaponSlots(R4WeaponKind?[] slots)=>slots.Select(x=>x.HasValue?(int)x.Value:-1).ToArray();
        static int[] PartSlots(R3PartKind?[] slots)=>slots.Select(x=>x.HasValue?(int)x.Value:-1).ToArray();
        public static R3Blueprint Capture(R3VehicleModel model,string title)
        {
            if(model==null||string.IsNullOrWhiteSpace(title)||title.Length>80)throw new ArgumentException("Niepoprawna nazwa projektu.");
            var core=model.Parts.SingleOrDefault(p=>p.Kind==R3PartKind.Core);
            if(core==null||model.Parts.Count>MaximumParts+1||model.Parts.Any(p=>!R3VehicleModel.ValidEconomicPart(p)))throw new ArgumentException("Niepoprawna konstrukcja.");
            return new R3Blueprint{title=title.Trim(),parts=model.Parts.Where(p=>p.Kind!=R3PartKind.Core).Select(p=>R3BlueprintPart.FromPart(p,core.Cell)).ToArray(),
                weapons=WeaponSlots(model.GetWeaponSlots()),active=PartSlots(model.GetActiveSlots()),deployables=PartSlots(model.GetDeployableSlots()),
                globalBindings=R1ControlProfile.Current.Snapshot()};
        }
        static bool SlotsValid(int[] slots,Func<int,bool> valid)=>slots!=null&&slots.Length==4&&
            slots.All(i=>i==-1||valid(i))&&slots.Where(i=>i>=0).Distinct().Count()==slots.Count(i=>i>=0);
        public static bool Valid(R3Blueprint document)
        {
            if(document==null||document.version!=1||string.IsNullOrWhiteSpace(document.title)||document.title.Length>80||document.parts==null||document.parts.Length>MaximumParts||
               (document.globalBindings!=null&&!R1ControlProfile.ValidSnapshot(document.globalBindings))||
               !SlotsValid(document.weapons,i=>Enum.IsDefined(typeof(R4WeaponKind),i))||
               !SlotsValid(document.active,i=>Enum.IsDefined(typeof(R3PartKind),i)&&R3VehicleModel.IsActiveSlotPart((R3PartKind)i))||
               !SlotsValid(document.deployables,i=>Enum.IsDefined(typeof(R3PartKind),i)&&R3VehicleModel.IsDeployableSlotPart((R3PartKind)i)))return false;
            var cells=new HashSet<Vector3Int>();
            foreach(var item in document.parts)
            {
                if(item==null||item.kind==R3PartKind.Core||!cells.Add(item.cell)||item.cell==Vector3Int.zero||
                   Math.Abs((long)item.cell.x)>100||Math.Abs((long)item.cell.y)>100||Math.Abs((long)item.cell.z)>100||
                   item.rotation.x<0||item.rotation.x>3||item.rotation.y<0||item.rotation.y>3||item.rotation.z<0||item.rotation.z>3)return false;
                var p=new R3PartState(item.cell,item.kind){Weapon=item.weapon,WheelStyle=item.wheel,ArmorShape=item.shape,Tier=item.tier,
                    FootprintRevision=item.footprint,ContactMask=item.contacts,UpgradeId=item.upgradeId,UpgradeEffect=item.upgradeEffect,UpgradeOperation=item.upgradeOperation,UpgradeScope=item.upgradeScope,UpgradeAmount=item.upgradeAmount};
                if(!R3VehicleModel.ValidEconomicPart(p)||item.contacts<0||item.contacts>63||item.tier<1||item.tier>3||
                   (item.kind==R3PartKind.BonusModule&&(string.IsNullOrWhiteSpace(item.upgradeId)||
                    !Enum.IsDefined(typeof(R1UpgradeEffect),item.upgradeEffect)||!R3VehicleModel.ValidBonusOperation(item.upgradeEffect,item.upgradeOperation)||!R3VehicleModel.ValidBonusScope(item.upgradeEffect,item.upgradeScope)||!R3VehicleModel.ValidBonusAmount(item.upgradeAmount))))return false;
            }
            return true;
        }
        public void Save(int slot,R3Blueprint document)
        {
            if(!Valid(document))throw new InvalidDataException("Niepoprawny projekt pojazdu.");
            R1RunCheckpointStorage.WritePayload(SlotPath(slot),JsonUtility.ToJson(document));
        }
        public R3Blueprint Load(int slot)
        {
            var document=JsonUtility.FromJson<R3Blueprint>(R1RunCheckpointStorage.ReadPayload(SlotPath(slot)));
            if(!Valid(document))throw new InvalidDataException("Niepoprawny projekt pojazdu.");return document;
        }
        public static R3BlueprintPreview Preview(R3Blueprint document,R3VehicleModel model)=>Plan(document,model).Preview;
        public static R3BlueprintPlan Plan(R3Blueprint document,R3VehicleModel model)
        {
            if(!Valid(document)||model==null)throw new ArgumentException("Niepoprawny projekt lub pojazd.");
            var core=model.Parts.SingleOrDefault(p=>p.Kind==R3PartKind.Core);
            if(core==null||core.Health<=0)throw new ArgumentException("Projekt wymaga sprawnego rdzenia.");
            var available=model.Parts.Concat(model.Inventory).Where(p=>p.Kind!=R3PartKind.Core).ToList();
            var mounted=new HashSet<string>(model.Parts.Select(p=>p.Id),StringComparer.Ordinal);
            var missing=new List<string>();var placements=new R3BlueprintPlacement[document.parts.Length];int found=0,broken=0;
            // Reserve every already-correct physical part before choosing any
            // interchangeable item for another cell. A one-pass greedy choice
            // can otherwise move a correct battery and swap identities needlessly.
            for(int n=0;n<document.parts.Length;n++)
            {
                var wanted=document.parts[n];
                var cell=core.Cell+wanted.cell;
                int index=available.FindIndex(p=>wanted.Matches(p)&&p.Health>0&&mounted.Contains(p.Id)&&p.Cell==cell&&p.RotationQuarterTurns==wanted.rotation&&p.ContactMask==wanted.contacts);
                if(index<0)continue;
                placements[n]=new R3BlueprintPlacement{Id=available[index].Id,Cell=cell,Rotation=wanted.rotation,AlreadyPlaced=true};
                available.RemoveAt(index);found++;
            }
            for(int n=0;n<document.parts.Length;n++)
            {
                if(placements[n]!=null)continue;
                var wanted=document.parts[n];var cell=core.Cell+wanted.cell;
                int index=available.FindIndex(p=>wanted.Matches(p)&&p.Health>0&&!mounted.Contains(p.Id));
                if(index<0)index=available.FindIndex(p=>wanted.Matches(p)&&p.Health>0);
                if(index>=0)
                {
                    var chosen=available[index];
                    placements[n]=new R3BlueprintPlacement{Id=chosen.Id,Cell=cell,Rotation=wanted.rotation,AlreadyPlaced=false};
                    available.RemoveAt(index);found++;continue;
                }
                index=available.FindIndex(p=>wanted.Matches(p)&&p.Health<=0);
                if(index>=0){available.RemoveAt(index);broken++;}
                var description=new R3PartState(default,wanted.kind){Weapon=wanted.weapon,WheelStyle=wanted.wheel,ArmorShape=wanted.shape,Tier=wanted.tier};
                string name=wanted.kind==R3PartKind.BonusModule?"Moduł bonusu "+wanted.upgradeId:R4WeaponDefinition.PartName(description);
                missing.Add(name+(index>=0?" — wymaga naprawy":" — brak"));
            }
            return new R3BlueprintPlan{Placements=placements.Where(p=>p!=null).ToArray(),Preview=new R3BlueprintPreview{RequiredCount=document.parts.Length,AvailableCount=found,BrokenCount=broken,Missing=missing}};
        }
    }
}
