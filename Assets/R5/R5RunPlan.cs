using System;
using System.Linq;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;

namespace RoboRougelike.R5
{
    public enum R5ArenaKind { SalvageYard, Wasteland, Industrial }
    public enum R5EnemyVariant { Normal, EliteBruiser, EliteGunner, SiegeBoss }
    [Serializable] public sealed class R5StageDefinition { public R1EncounterDefinition[] Options; }
    [Serializable] public sealed class R5Catalog { public R5StageDefinition[] Stages; }

    // One immutable generated itinerary per expedition. Looking at the map cannot reroll it.
    public sealed class R5RunPlan
    {
        private readonly R1EncounterDefinition[][] stages;
        public int Seed { get; }
        public int Count => stages.Length;
        public R5RunPlan(R5StageDefinition[] definitions,int seed)
        {
            Validate(definitions);
            Seed=seed;stages=new R1EncounterDefinition[6][];
            for(int stage=0;stage<6;stage++)
            {
                var rng=new System.Random(StageSeed(seed,stage,17));
                stages[stage]=definitions[stage].Options.Select(source=>
                {
                    var item=JsonUtility.FromJson<R1EncounterDefinition>(JsonUtility.ToJson(source));
                    if(item.AlternativeRoster!=null&&item.AlternativeRoster.Length>0&&rng.Next(2)==1)
                        item.Roster=item.AlternativeRoster.ToArray();
                    item.EnemyCount=item.Roster.Length;
                    item.ScrapReward=Math.Max(0,item.ScrapReward+(item.RewardJitter>0?rng.Next(-item.RewardJitter,item.RewardJitter+1):0));
                    if(item.EnemyCount<1||item.EnemyCount>3||item.Variants.Length!=item.EnemyCount||Threat(item)>item.ThreatBudget)
                        throw new ArgumentException("Niepoprawny budżet/skład starcia: "+item.Id);
                    item.Risk=RiskName(item);
                    return item;
                }).ToArray();
                if(stages[stage].Length<1)throw new ArgumentException("Pusty etap.");
            }
        }
        public R1EncounterDefinition[] Offers(int stage) => stages[stage].ToArray();
        public static void Validate(R5StageDefinition[] definitions)
        {
            if(definitions==null||definitions.Length!=6)throw new ArgumentException("Wyprawa wymaga sześciu etapów.");
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var stage in definitions)
            {
                if(stage?.Options==null||stage.Options.Length<1||stage.Options.Length>2)throw new ArgumentException("Etap wymaga 1–2 tras.");
                foreach(var item in stage.Options)
                {
                    if(item==null||string.IsNullOrWhiteSpace(item.Id)||!ids.Add(item.Id)||string.IsNullOrWhiteSpace(item.Title)||
                        !Enum.IsDefined(typeof(R5ArenaKind),item.Arena))throw new ArgumentException("Niepoprawny identyfikator, tytuł lub arena.");
                    if(!In(item.HealthMultiplier,.5f,3)||!In(item.DamageMultiplier,.1f,3)||!In(item.FireIntervalMultiplier,.3f,3)||
                        !In(item.ProjectileSpeedMultiplier,.5f,2)||!In(item.MoveSpeed,0,6)||!In(item.PreferredDistance,6,20)||
                        !In(item.StrafeStrength,0,1)||!In(item.ThreatBudget,.1f,100)||item.ScrapReward<0||item.ScrapReward>100000||
                        item.RewardJitter<0||item.RewardJitter>1000||item.RewardJitter>item.ScrapReward)
                        throw new ArgumentException("Parametry poza zakresem: "+item.Id);
                    ValidateRoster(item,item.Roster);
                    if(item.AlternativeRoster!=null&&item.AlternativeRoster.Length>0)ValidateRoster(item,item.AlternativeRoster);
                }
            }
        }
        static bool In(float value,float min,float max)=>!float.IsNaN(value)&&!float.IsInfinity(value)&&value>=min&&value<=max;
        static void ValidateRoster(R1EncounterDefinition item,R3EnemyArchetype[] roster)
        {
            if(roster==null||roster.Length<1||roster.Length>3||item.Variants==null||item.Variants.Length!=roster.Length||item.EnemyCount!=roster.Length)
                throw new ArgumentException("Niepoprawny skład: "+item.Id);
            for(int i=0;i<roster.Length;i++)
            {
                var variant=item.Variants[i];var kind=roster[i];
                if(!Enum.IsDefined(typeof(R3EnemyArchetype),kind)||!Enum.IsDefined(typeof(R5EnemyVariant),variant)||
                    (variant==R5EnemyVariant.EliteGunner&&kind!=R3EnemyArchetype.Gunner)||
                    ((variant==R5EnemyVariant.EliteBruiser||variant==R5EnemyVariant.SiegeBoss)&&kind!=R3EnemyArchetype.Bruiser))
                    throw new ArgumentException("Niezgodny archetyp/wariant: "+item.Id);
            }
            var copy=JsonUtility.FromJson<R1EncounterDefinition>(JsonUtility.ToJson(item));copy.Roster=roster;
            if(Threat(copy)>item.ThreatBudget)throw new ArgumentException("Budżet zagrożenia przekroczony: "+item.Id);
        }
        public static string RiskName(R1EncounterDefinition item)=>item.Variants.Contains(R5EnemyVariant.SiegeBoss)?"Boss":Threat(item)<5?"Niskie":Threat(item)<9?"Umiarkowane":Threat(item)<12?"Wysokie":"Bardzo wysokie";
        public System.Random UpgradeRandom(int stage) => new System.Random(StageSeed(Seed,stage,73));
        private static int StageSeed(int seed,int stage,int salt){unchecked{return seed*397 ^ stage*7919 ^ salt;}}
        public static string ArenaName(R5ArenaKind arena) => arena==R5ArenaKind.SalvageYard?"Złomowisko":arena==R5ArenaKind.Wasteland?"Pustkowia":"Kompleks przemysłowy";
        public static float Threat(R1EncounterDefinition item)
        {
            float score=0;
            for(int i=0;i<item.SpawnCount;i++)
            {
                var kind=item.SpawnArchetype(i);
                float hull=kind==R3EnemyArchetype.Bruiser?1.6f:kind==R3EnemyArchetype.Scout?0.8f:1.1f;
                float weapon=kind==R3EnemyArchetype.Bruiser||kind==R3EnemyArchetype.Gunner?1.6f:1;
                float mobility=kind==R3EnemyArchetype.Flanker?1.4f:1;
                float range=kind==R3EnemyArchetype.Gunner?1.3f:1;
                float variant=item.SpawnVariant(i)==R5EnemyVariant.SiegeBoss?3.8f:item.SpawnVariant(i)==R5EnemyVariant.Normal?1:1.65f;
                score+=(hull*item.HealthMultiplier+weapon*item.DamageMultiplier/Mathf.Max(.3f,item.FireIntervalMultiplier)+mobility*item.MoveSpeed*.22f+range)*variant;
            }
            float combination=Enumerable.Range(0,item.SpawnCount).Select(item.SpawnArchetype).Distinct().Count()>1?1.12f:1;
            float arena=item.Arena==R5ArenaKind.Industrial?1.15f:item.Arena==R5ArenaKind.Wasteland?1.05f:1;
            return score*combination*arena;
        }
    }
}
