using UnityEngine;
using RoboRougelike.R1;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.Mechanics.StructuralScan;

namespace RoboRougelike.R6
{
    // Opt-in scanner for the live Gauntlet. No permanent scene/prefab, damage,
    // movement, salvage, inventory or AI changes. The only action is TryScan().
    [DisallowMultipleComponent]
    public sealed class COM001ProductionScanBridge : MonoBehaviour
    {
        private StructuralScanController scanner;
        private StructuralScanRegistry registry;
        private R1VehicleState currentPlayer;

        public StructuralScanController Scanner => scanner;

        private void Awake()
        {
            registry = GetComponent<StructuralScanRegistry>();
            if (registry == null) registry = gameObject.AddComponent<StructuralScanRegistry>();
            scanner = GetComponent<StructuralScanController>();
            if (scanner == null) scanner = gameObject.AddComponent<StructuralScanController>();
        }

        private static StructuralPartType Classify(P12PipelineSafeEnemyTurret enemy,
            P12PipelineSafeDamageable part)
        {
            if (part == enemy.Core) return StructuralPartType.Core;
            if (part == enemy.WeaponBlock) return StructuralPartType.Weapon;
            var functional = part.GetComponent<P12PipelineSafeFunctionalModule>();
            if (functional != null)
            {
                if (functional.ModuleType == P12FunctionalModuleType.Weapon)
                    return StructuralPartType.Weapon;
                if (functional.ModuleType == P12FunctionalModuleType.Wheel)
                    return StructuralPartType.Wheel;
            }
            if (part.name.StartsWith("Koło")) return StructuralPartType.Wheel;
            return StructuralPartType.Armor;
        }

        // Scans only already-existing, non-defeated enemy vehicles. Does not
        // enumerate all scene objects each Update() and never fabricates hidden parts.
        public bool TriggerScan()
        {
            var run = R1RunController.Instance;
            if (run == null || run.Progress == null ||
                run.Progress.State != R1RunState.Combat || run.Paused || run.Transitioning ||
                run.Player == null || run.Encounter == null || run.Player.gameObject != gameObject)
                return false;

            if (currentPlayer != run.Player || scanner == null)
            {
                currentPlayer = run.Player;
                if (registry == null) registry = GetComponent<StructuralScanRegistry>();
                if (scanner == null) scanner = GetComponent<StructuralScanController>();
            }
            // Do not erase the active snapshot when a player retries during cooldown.
            if (!scanner.IsReady)
            {
                Debug.Log("[COM001] Scanner cooldown: " + scanner.CooldownLeft.ToString("0.0") + " s", this);
                return false;
            }
            var sensor = currentPlayer.CameraPivot != null ? currentPlayer.CameraPivot : transform;
            scanner.Bind(sensor, transform, gameObject.GetInstanceID(), 1, registry);
            scanner.Configure(new StructuralScanSettings
            {
                rangeMeters = 60f,
                fullConeDegrees = 85f,
                cooldownSeconds = 8f,
                displaySeconds = 5f,
                activationEnergy = 0f,
                maxRevealedParts = 16,
                maxRegisteredParts = 512,
                obstructionMask = ~0
            });

            // Re-registration on explicit scan action prevents detached/dead debris
            // becoming permanent targets, with no per-frame FindObjects calls.
            registry.Clear();
            foreach (var enemy in run.Encounter.Enemies)
            {
                if (enemy == null || enemy.IsDefeated || !enemy.gameObject.activeInHierarchy)
                    continue;
                int owner = enemy.GetInstanceID();
                if (owner == 0) continue;
                foreach (var part in enemy.GetComponentsInChildren<P12PipelineSafeDamageable>(true))
                {
                    if (part == null || part.IsDead || !part.gameObject.activeInHierarchy)
                        continue;
                    var adapter = part.GetComponent<COM001LiveScanPartAdapter>();
                    if (adapter == null) adapter = part.gameObject.AddComponent<COM001LiveScanPartAdapter>();
                    adapter.Bind(enemy.transform, part, owner, 2, Classify(enemy, part));
                    if (adapter.IsAttached) registry.Register(adapter);
                }
            }

            bool success = scanner.TryScan();
            if (!success)
            {
                Debug.Log("[COM001] Scan unavailable: target not visible, occluded, beyond range, over capacity or cooldown.", this);
                return false;
            }
            Debug.Log("[COM001] Visible contacts=" + scanner.Contacts.Count +
                "; selected enemy owner=" + scanner.SelectedTargetOwner +
                ". Read-only snapshot; hidden modules excluded.", this);
            foreach (var contact in scanner.Contacts)
                Debug.Log("[COM001] part=" + contact.PartKey + " type=" + contact.Type +
                    " hp=" + contact.HealthFraction.ToString("0.00") +
                    " priority=" + contact.Priority.ToString("0.0"), this);
            return true;
        }

        private void OnDestroy()
        {
            if (registry != null) registry.Clear();
        }
    }
}
