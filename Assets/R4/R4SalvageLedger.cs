using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R3;

namespace RoboRougelike.R4
{
    public sealed class R4SalvageItem
    {
        public readonly string Id, Source;
        public readonly R3PartKind Kind;
        public readonly R4WeaponKind Weapon;
        public readonly float Health;
        private readonly R3PartState snapshot;
        public readonly int Ammo;
        public readonly float ThermalHeat;public readonly bool ThermalLocked;
        public readonly float DefenseCharge,DefenseRechargeAge;
        public readonly Vector3Int Rotation;
        public readonly int Contacts;
        public bool Recoverable => Health>0;
        // Wrecks are selectable too; armor and cores are not reward choices.
        public bool Selectable => Kind!=R3PartKind.Armor&&Kind!=R3PartKind.Core;
        public R4SalvageItem(string id,string source,R3PartKind kind,R4WeaponKind weapon,float health,int ammo=-1,
            float defenseCharge=-1,float defenseAge=float.PositiveInfinity,Vector3Int rotation=default,int contacts=R3MountingGrid.AllFaces,float thermalHeat=0,bool thermalLocked=false)
        { Id=id;Source=source;Kind=kind;Weapon=weapon;Ammo=ammo;DefenseCharge=defenseCharge;DefenseRechargeAge=defenseAge;
            Rotation=rotation;Contacts=contacts;ThermalHeat=thermalHeat;ThermalLocked=thermalLocked;Health=float.IsNaN(health)||float.IsInfinity(health)?0:Math.Max(0,Math.Min(1,health)); }
        public R4SalvageItem(R3PartState part,float health):this(part.Id,part.Source,part.Kind,part.Weapon,health,
            part.Ammo,part.DefenseCharge,part.DefenseRechargeAge,part.RotationQuarterTurns,part.ContactMask,part.ThermalHeat,part.ThermalLocked)
        {snapshot=part.Copy();}
        public R3PartState ToPart()
        {
            if(snapshot!=null){var part=snapshot.Copy();part.Health=Health;return part;}
            return LegacyPart();
        }
        R3PartState LegacyPart() => new R3PartState(default,Kind,Health){Id=Id,Source=Source,Weapon=Weapon,Ammo=Ammo,
            ThermalHeat=ThermalHeat,ThermalLocked=ThermalLocked,DefenseCharge=DefenseCharge,DefenseRechargeAge=DefenseRechargeAge,RotationQuarterTurns=Rotation,ContactMask=Contacts};
    }
    // One ledger per encounter. Contains no scene objects and survives wreck cleanup.
    public sealed class R4SalvageLedger
    {
        private readonly List<R4SalvageItem> items=new List<R4SalvageItem>();
        private readonly HashSet<string> enemies=new HashSet<string>();
        private readonly HashSet<string> ids=new HashSet<string>();
        public IReadOnlyList<R4SalvageItem> Items => items.AsReadOnly();
        public int Limit { get; }
        public bool Closed { get; private set; }
        public int DismantledScrap { get; private set; }
        public R4SalvageLedger(int limit=2){Limit=Math.Max(0,Math.Min(8,limit));}
        public bool Record(string enemyId,IEnumerable<R4SalvageItem> equipment)
        {
            if(Closed||string.IsNullOrWhiteSpace(enemyId)||enemies.Contains(enemyId)||equipment==null)return false;
            var batch=equipment.ToArray();
            if(batch.Any(p=>p==null||string.IsNullOrWhiteSpace(p.Id)||ids.Contains(p.Id)||p.Kind==R3PartKind.Core)||
                batch.Select(p=>p.Id).Distinct().Count()!=batch.Length)return false;
            enemies.Add(enemyId);foreach(var item in batch){ids.Add(item.Id);items.Add(item);}return true;
        }
        public int PreviewScrap(R3VehicleModel model,IEnumerable<string> selected)
        {
            var keep=new HashSet<string>(selected??Array.Empty<string>());
            return items.Where(p=>!keep.Contains(p.Id)).Sum(p=>p.Recoverable?model.DismantleValue(p.ToPart()):1);
        }
        public bool Claim(R3VehicleModel model,IEnumerable<string> selected) => ClaimPrepared(model,selected,null);
        public bool Claim(R3VehicleModel model,IEnumerable<string> selected,R3PartState bonus)
        {
            if(bonus==null||bonus.Kind!=R3PartKind.BonusModule)return false;
            return ClaimPrepared(model,selected,bonus);
        }
        private bool ClaimPrepared(R3VehicleModel model,IEnumerable<string> selected,R3PartState bonus)
        {
            if(Closed||model==null||selected==null)return false;
            var chosen=selected.ToArray();var keep=new HashSet<string>(chosen);
            if(chosen.Length!=keep.Count||keep.Count>Limit||keep.Any(id=>!items.Any(p=>p.Id==id&&p.Selectable)||model.Owns(id)))return false;
            var recovered=items.Where(p=>keep.Contains(p.Id)).Select(p=>p.ToPart()).ToArray();
            int scrap=PreviewScrap(model,keep);
            // Validate and store all incoming items together before closing or paying the ledger.
            var incoming=bonus==null?recovered:recovered.Concat(new[]{bonus}).ToArray();
            if(!model.ReceiveSalvage(incoming))return false;
            Closed=true;DismantledScrap=scrap;model.Award(scrap);return true;
        }
    }
}
