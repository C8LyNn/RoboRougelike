using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R3;

namespace RoboRougelike.R4
{
    public sealed class R4SalvageBasket
    {
        public R3EnemyArchetype Archetype { get; }
        public int Percent { get; }
        public int AvailableParts { get; }
        public IReadOnlyList<R4SalvageItem> Items { get; }
        internal R4SalvageBasket(R3EnemyArchetype archetype,int available,R4SalvageItem[] parts)
        { Archetype=archetype;AvailableParts=available;Percent=parts.Length*100/available;Items=Array.AsReadOnly(parts); }
    }
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
        // Wrecks are selectable too; selected enemy parts arrive in working condition.
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
        private readonly List<(R3EnemyArchetype Type,R4SalvageItem[] Parts)> basketSources=new List<(R3EnemyArchetype,R4SalvageItem[])>();
        public IReadOnlyList<R4SalvageItem> Items => items.AsReadOnly();
        public int Limit { get; }
        public bool Closed { get; private set; }
        public int DismantledScrap { get; private set; }
        public R4SalvageLedger(int limit=2){Limit=Math.Max(0,Math.Min(8,limit));}
        public bool Record(string enemyId,IEnumerable<R4SalvageItem> equipment) => RecordCore(enemyId,equipment,null);
        public bool Record(string enemyId,IEnumerable<R4SalvageItem> equipment,R3EnemyArchetype archetype)
            => RecordCore(enemyId,equipment,archetype);
        private bool RecordCore(string enemyId,IEnumerable<R4SalvageItem> equipment,R3EnemyArchetype? archetype)
        {
            if(Closed||string.IsNullOrWhiteSpace(enemyId)||enemies.Contains(enemyId)||equipment==null)return false;
            var batch=equipment.ToArray();
            if(batch.Any(p=>p==null||string.IsNullOrWhiteSpace(p.Id)||ids.Contains(p.Id)||p.Kind==R3PartKind.Core||
                !R3VehicleModel.ValidEconomicPart(p.ToPart()))||
                batch.Select(p=>p.Id).Distinct().Count()!=batch.Length)return false;
            enemies.Add(enemyId);foreach(var item in batch){ids.Add(item.Id);items.Add(item);}
            if(archetype.HasValue)basketSources.Add((archetype.Value,batch));
            return true;
        }
        public IReadOnlyList<R4SalvageBasket> CreateBasketOffers(int seed,int stage)
        {
            // Select among the most common archetypes that actually appeared.
            // Every offer draws from one real defeated robot; armor stays excluded.
            var groups=basketSources.Where(s=>s.Parts.Any(p=>p.Selectable)).GroupBy(s=>s.Type)
                .OrderByDescending(g=>g.Count()).ThenBy(g=>g.Key).Take(3).ToArray();
            if(groups.Length==0||Limit==0)return Array.Empty<R4SalvageBasket>();
            var random=new System.Random(seed);
            var result=new List<R4SalvageBasket>();
            for(int i=0;i<3;i++)
            {
                var group=groups[i%groups.Length].ToArray();
                var source=group[random.Next(group.Length)];
                var pool=source.Parts.Where(p=>p.Selectable).OrderBy(p=>p.Id,StringComparer.Ordinal).ToArray();
                int tier=Math.Max(0,Math.Min(5,stage));
                int percent=random.Next(30+tier*5,61+tier*5);
                int count=Math.Min(Limit,Math.Max(1,(int)Math.Ceiling(pool.Length*percent/100d)));
                // Partial Fisher-Yates: no duplicated physical instance inside a basket.
                for(int j=0;j<count;j++){int k=random.Next(j,pool.Length);var swap=pool[j];pool[j]=pool[k];pool[k]=swap;}
                result.Add(new R4SalvageBasket(source.Type,pool.Length,pool.Take(count).ToArray()));
            }
            return result.AsReadOnly();
        }
        public int PreviewScrap(R3VehicleModel model,IEnumerable<string> selected)
        {
            if(model==null||items.Any(p=>p==null||!R3VehicleModel.ValidEconomicPart(p.ToPart())))return 0;
            var keep=new HashSet<string>(selected??Array.Empty<string>());
            long baseScrap=items.Where(p=>!keep.Contains(p.Id)).Sum(p=>(long)(p.Recoverable?model.DismantleValue(p.ToPart()):1));
            float bonus=model.Economy.SalvageOptimizerBonus;
            if(float.IsNaN(bonus)||float.IsInfinity(bonus))bonus=0;
            long units=model.Parts.Where(p=>p.Kind==R3PartKind.SalvageOptimizer&&p.Health>0)
                .Sum(p=>(long)R3VehicleModel.TierEffectUnits(p.Tier));
            return (int)Math.Min(int.MaxValue,Math.Floor(baseScrap*(1d+Math.Max(0,bonus)*units)));
        }
        public bool Claim(R3VehicleModel model,IEnumerable<string> selected) => ClaimPrepared(model,selected,null);
        public bool Claim(R3VehicleModel model,IEnumerable<string> selected,R3PartState bonus)
        {
            if(bonus==null||bonus.Kind!=R3PartKind.BonusModule)return false;
            return ClaimPrepared(model,selected,bonus);
        }
        private bool ClaimPrepared(R3VehicleModel model,IEnumerable<string> selected,R3PartState bonus)
        {
            if(Closed||model==null||selected==null||items.Any(p=>p==null||!R3VehicleModel.ValidEconomicPart(p.ToPart())))return false;
            var chosen=selected.ToArray();var keep=new HashSet<string>(chosen);
            if(chosen.Length!=keep.Count||keep.Count>Limit||keep.Any(id=>!items.Any(p=>p.Id==id&&p.Selectable)||model.Owns(id)))return false;
            // Repair only the copies selected as rewards. The immutable wreck
            // snapshots remain intact for condition-based scrap calculation.
            var recovered=items.Where(p=>keep.Contains(p.Id)).Select(p=>{var copy=p.ToPart();copy.Health=1;return copy;}).ToArray();
            int scrap=PreviewScrap(model,keep);
            // Validate and store all incoming items together before closing or paying the ledger.
            var expanded=new List<R3PartState>();
            foreach(var part in recovered)
            {
                int count=model.GrantQuantity(part.Kind,part.ArmorShape);
                if((long)expanded.Count+count+(bonus==null?0:1)+model.Inventory.Count>model.AvailableInventoryCapacity)return false;
                expanded.Add(part);
                for(int i=1;i<count;i++){var copy=part.Copy();copy.Id=Guid.NewGuid().ToString("N");expanded.Add(copy);}
            }
            var incoming=bonus==null?expanded.ToArray():expanded.Concat(new[]{bonus}).ToArray();
            if(!model.ReceiveSalvage(incoming))return false;
            int before=model.Scrap;Closed=true;model.Award(scrap);DismantledScrap=model.Scrap-before;return true;
        }
    }
}
