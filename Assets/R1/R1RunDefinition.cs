using System;
using UnityEngine;

namespace RoboRougelike.R1
{
    [Serializable]
    public sealed class R1EncounterDefinition
    {
        public string Title = "Starcie";
        public string Id;
        public RoboRougelike.R5.R5ArenaKind Arena;
        public RoboRougelike.R3.R3EnemyArchetype[] Roster;
        public RoboRougelike.R5.R5EnemyVariant[] Variants;
        public RoboRougelike.R3.R3EnemyArchetype[] AlternativeRoster;
        public float ThreatBudget = 30;
        public string Risk = "Niskie";
        public string Loot = "Koła i uzbrojenie";
        public string Rule = "Zniszcz wszystkie rdzenie";
        public int RewardJitter;
        [Range(1, 8)] public int EnemyCount = 1;
        // Serialized rosters remain the authored base; all consumers resolve the same escort.
        public bool IsBoss => Variants!=null&&System.Array.IndexOf(Variants,RoboRougelike.R5.R5EnemyVariant.SiegeBoss)>=0;
        public int SpawnCount => IsBoss?1:Mathf.Clamp(EnemyCount+1,1,4);
        public RoboRougelike.R3.R3EnemyArchetype SpawnArchetype(int index) => index>=EnemyCount?
            RoboRougelike.R3.R3EnemyArchetype.Scout: Roster!=null&&index<Roster.Length?Roster[index]:RoboRougelike.R3.R3EnemyProfiles.InEncounter(EnemyCount,index);
        public RoboRougelike.R5.R5EnemyVariant SpawnVariant(int index) => index<EnemyCount&&Variants!=null&&index<Variants.Length?
            Variants[index]:RoboRougelike.R5.R5EnemyVariant.Normal;
        [Range(0.5f, 3f)] public float HealthMultiplier = 1;
        [Range(0.1f, 3f)] public float DamageMultiplier = 0.55f;
        [Range(0.3f, 3f)] public float FireIntervalMultiplier = 1.4f;
        [Range(0.5f, 2f)] public float ProjectileSpeedMultiplier = 1;
        [Range(0, 6f)] public float MoveSpeed = 2;
        [Range(6f, 20f)] public float PreferredDistance = 12;
        [Range(0, 1f)] public float StrafeStrength = 0.35f;
        [Min(0)] public int ScrapReward = 80;
    }

    [CreateAssetMenu(menuName = "RoboRougelike/Wyprawa")]
    public sealed class R1RunDefinition : ScriptableObject
    {
        public R1EncounterDefinition[] Encounters;
        public RoboRougelike.R5.R5StageDefinition[] RouteStages;
        public R1UpgradeDefinition[] Upgrades;
        [Range(0,8)] public int SalvageLimit = 2;
        public RoboRougelike.R3.R3GarageEconomy GaragePrices = new RoboRougelike.R3.R3GarageEconomy();
    }
}
