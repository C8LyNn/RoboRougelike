using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R1
{
    public sealed class R1EncounterManager : MonoBehaviour
    {
        private readonly List<P12PipelineSafeEnemyTurret> enemies = new List<P12PipelineSafeEnemyTurret>();
        public IReadOnlyList<P12PipelineSafeEnemyTurret> Enemies => enemies;
        public int AliveCount => enemies.Count(enemy => enemy != null && !enemy.IsDefeated);
        public bool Cleared => enemies.Count > 0 && AliveCount == 0;

        public void Spawn(P12PipelineSafeEnemyTurret template, Transform player,
            R1EncounterDefinition definition,RoboRougelike.R4.R4SalvageLedger salvage=null,string encounterId="")
        {
            Clear();
            // Add one regular escort per encounter without changing the boss finale.
            // The extra enemy uses an ordinary archetype, not an elite/boss variant.
            int count=definition.SpawnCount;
            for (int i = 0; i < count; i++)
            {
                // Staggered positions leave an open central firing lane around the existing walls.
                float x = count == 1 ? 0 : (i - (count - 1) * 0.5f) * 9f;
                var enemy = Instantiate(template, new Vector3(x, 0, 17 + (i % 2) * 5), Quaternion.identity);
                var kind=definition.SpawnArchetype(i);
                enemy.ConfigureArchetype(kind);
                enemy.name = RoboRougelike.R3.R3EnemyProfiles.Name(kind)+" "+(i+1);
                enemy.ConfigureVariant(definition.SpawnVariant(i));
                enemy.ConfigureSalvage(salvage,encounterId+"/enemy/"+i);
                enemy.gameObject.SetActive(true);
                enemy.ConfigureEncounter(player, definition.HealthMultiplier, definition.DamageMultiplier,
                    definition.FireIntervalMultiplier, definition.ProjectileSpeedMultiplier);
                enemy.ConfigureMovement(definition.MoveSpeed, definition.PreferredDistance, definition.StrafeStrength);
                // One real shotgun escort on larger encounters; its actual equipment enters salvage.
                if(count>=3&&i==count-1&&definition.SpawnVariant(i)==RoboRougelike.R5.R5EnemyVariant.Normal)
                    enemy.EquipShotgun();
                enemies.Add(enemy);
            }
        }

        public void SetCombatActive(bool active)
        {
            foreach (var enemy in enemies) if (enemy != null) enemy.SetCombatActive(active);
        }

        public void Clear()
        {
            foreach (var enemy in enemies)
                if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            enemies.Clear();
            ClearTransientObjects();
        }

        public static void ClearTransientObjects()
        {
            foreach (var shell in Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None))
            { shell.gameObject.SetActive(false); Object.Destroy(shell.gameObject); }
            var debris = new HashSet<GameObject>();
            foreach (var part in Object.FindObjectsByType<P12PipelineSafeDamageable>(FindObjectsSortMode.None))
            {
                var block = part.GetComponent<P12_PipelineSafePlayerBlock>();
                if (!part.IsDead && (block == null || !block.DetachedFromVehicle)) continue;
                var body = part.GetComponentInParent<Rigidbody>();
                if (body != null && body.GetComponent<P12PipelineSafeTankController>() == null)
                    debris.Add(body.gameObject);
            }
            foreach (var item in debris) { item.SetActive(false); Object.Destroy(item); }
            foreach(var item in Object.FindObjectsByType<RoboRougelike.R3.R3Debris>(FindObjectsSortMode.None))
            {item.gameObject.SetActive(false);Object.Destroy(item.gameObject);}
        }
    }
}
