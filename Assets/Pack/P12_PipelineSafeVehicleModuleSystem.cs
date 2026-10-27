using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueMachine.P12PipelineSafe
{
    public enum P12FunctionalModuleType
    {
        Wheel,
        Weapon
    }

    [DisallowMultipleComponent]
    public class P12PipelineSafeFunctionalModule : MonoBehaviour
    {
        [SerializeField] private P12FunctionalModuleType moduleType;
        [SerializeField] private int side;
        [SerializeField] private P12_PipelineSafePlayerBlock anchorBlock;
        [SerializeField] private Transform payload;
        [SerializeField] private Transform ownerRoot;
        [SerializeField] private bool moduleLost;

        private P12PipelineSafeDamageable damageable;
        private bool payloadAttachedToDebris;

        public P12FunctionalModuleType ModuleType => moduleType;
        public int Side => side;
        public bool ModuleLost => moduleLost;
        public bool DamageableAlive =>
            damageable != null && !damageable.IsDead;
        public P12_PipelineSafePlayerBlock AnchorBlock => anchorBlock;

        public void Configure(
            P12FunctionalModuleType type,
            int moduleSide,
            P12_PipelineSafePlayerBlock structuralAnchor,
            Transform payloadTransform,
            Transform vehicleRoot)
        {
            moduleType = type;
            side = moduleSide;
            anchorBlock = structuralAnchor;
            payload = payloadTransform;
            ownerRoot = vehicleRoot;
            damageable = GetComponent<P12PipelineSafeDamageable>();
            moduleLost = false;
            payloadAttachedToDebris = false;
        }

        private void Awake()
        {
            damageable = GetComponent<P12PipelineSafeDamageable>();
        }

        public bool IsOperational(
            P12PipelineSafeVehicleStructure structure)
        {
            if (!moduleLost &&
                (damageable == null ||
                 damageable.IsDead ||
                 ownerRoot == null ||
                 !transform.IsChildOf(ownerRoot)))
            {
                MarkLost();
            }

            if (moduleLost)
                return false;

            if (anchorBlock == null)
                return false;

            return structure == null ||
                structure.IsBlockConnected(anchorBlock);
        }

        public void ForceStructuralLoss(Vector3 outward)
        {
            if (moduleLost)
                return;

            if (damageable != null && !damageable.IsDead)
            {
                Vector3 direction =
                    outward.sqrMagnitude > 0.001f
                        ? outward.normalized
                        : transform.right;

                damageable.ApplyDamage(
                    100000f,
                    transform.position,
                    direction
                );
            }

            MarkLost();
        }

        private void MarkLost()
        {
            if (moduleLost)
            {
                AttachPayloadToDebris();
                return;
            }

            moduleLost = true;
            AttachPayloadToDebris();
        }

        private void AttachPayloadToDebris()
        {
            if (payloadAttachedToDebris || payload == null)
                return;

            payload.SetParent(transform, true);
            payloadAttachedToDebris = true;

            P12PipelineSafeWheelVisual[] visuals =
                payload.GetComponentsInChildren<
                    P12PipelineSafeWheelVisual
                >(true);

            foreach (P12PipelineSafeWheelVisual visual in visuals)
            {
                if (visual != null)
                    visual.enabled = false;
            }
        }
    }

    [DefaultExecutionOrder(375)]
    [DisallowMultipleComponent]
    public class P12PipelineSafeVehicleModuleSystem : MonoBehaviour
    {
        [Header("Mount durability")]
        [SerializeField] private float wheelMountHp = 80f;
        [SerializeField] private float weaponMountHp = 140f;

        [Header("Wheel mount geometry")]
        [SerializeField] private float wheelMountX = 1.70f;
        [SerializeField] private float wheelMountY = -0.10f;
        [SerializeField] private float wheelMountSize = 0.48f;
        [SerializeField] private float axleThickness = 0.16f;

        [Header("Weapon mount geometry")]
        [SerializeField] private Vector3 weaponMountLocalPosition =
            new Vector3(0f, 0.82f, 0.10f);
        [SerializeField] private float weaponMountSize = 0.50f;

        [Header("Mobility")]
        [SerializeField] private float minimumDriveFactor = 0.24f;
        [SerializeField] private float minimumTurnFactor = 0.38f;
        [SerializeField] private float oneSideTurnFactor = 0.16f;

        [SerializeField] private List<P12PipelineSafeFunctionalModule>
            wheelModules = new List<P12PipelineSafeFunctionalModule>();

        [SerializeField] private P12PipelineSafeFunctionalModule
            weaponModule;

        private P12PipelineSafeVehicleStructure structure;
        private int initialWheelCount;
        private int initialLeftCount;
        private int initialRightCount;

        private int activeWheelCount;
        private int activeLeftCount;
        private int activeRightCount;
        private float driveFactor = 1f;
        private float turnFactor = 1f;
        private bool weaponOnline = true;
        private float nextEvaluation;
        [SerializeField] private bool externalLayout;
        private readonly List<P12PipelineSafeFunctionalModule> externalWeapons = new List<P12PipelineSafeFunctionalModule>();
        private IEnumerable<P12PipelineSafeFunctionalModule> WeaponModules => externalLayout ? externalWeapons :
            weaponModule != null ? new[] { weaponModule } : Array.Empty<P12PipelineSafeFunctionalModule>();
        public int ActiveWeaponCount { get; private set; }

        public void ConfigureExternalModules(IEnumerable<P12PipelineSafeFunctionalModule> wheels,
            IEnumerable<P12PipelineSafeFunctionalModule> weapons, int plannedLeft, int plannedRight)
        {
            externalLayout = true;
            wheelModules.Clear(); wheelModules.AddRange(wheels);
            externalWeapons.Clear(); externalWeapons.AddRange(weapons);
            initialLeftCount = plannedLeft; initialRightCount = plannedRight;
            initialWheelCount = plannedLeft + plannedRight;
            ResolveDependencies(); EvaluateModules();
        }
        public void RefreshModules() { EvaluateModules(); }

        public int ActiveWheelCount => activeWheelCount;
        public int InitialWheelCount => initialWheelCount;
        public int ActiveLeftCount => activeLeftCount;
        public int ActiveRightCount => activeRightCount;
        public float DriveFactor => driveFactor;
        public float TurnFactor => turnFactor;
        public bool WeaponOnline => weaponOnline;

        private void Awake()
        {
            ResolveDependencies();
            DisableLegacyWeaponAnchor();
        }

        private void Start()
        {
            ResolveDependencies();
            if (externalLayout) { EvaluateModules(); return; }

            if (!HasValidModules())
                RebuildModulesNow();
            else
                InitializeModuleCounts();

            EvaluateModules();
        }

        private void Update()
        {
            if (Time.time < nextEvaluation)
                return;

            nextEvaluation = Time.time + 0.05f;
            EvaluateModules();
        }

        public void RebuildModulesNow()
        {
            if (externalLayout) { EvaluateModules(); return; }
            ResolveDependencies();
            DisableLegacyWeaponAnchor();
            RemoveOldMounts();

            wheelModules.Clear();
            weaponModule = null;

            BuildWheelModules();
            BuildWeaponModule();

            InitializeModuleCounts();
            EvaluateModules();

            Debug.Log("[Pack 19] Functional Modules ready. Wheels=" +
                initialWheelCount + " | Weapon=" + (weaponModule != null));
        }

        private void InitializeModuleCounts()
        {
            initialWheelCount = wheelModules.Count;
            initialLeftCount = 0;
            initialRightCount = 0;

            foreach (P12PipelineSafeFunctionalModule module in wheelModules)
            {
                if (module == null)
                    continue;

                if (module.Side < 0)
                    initialLeftCount++;
                else if (module.Side > 0)
                    initialRightCount++;
            }

        }

        private void ResolveDependencies()
        {
            if (structure == null)
            {
                structure =
                    GetComponent<P12PipelineSafeVehicleStructure>();

                if (structure == null)
                {
                    structure =
                        gameObject.AddComponent<
                            P12PipelineSafeVehicleStructure
                        >();

                    structure.RebuildNow();
                }
            }
        }

        private bool HasValidModules()
        {
            if (wheelModules == null ||
                wheelModules.Count == 0 ||
                weaponModule == null)
            {
                return false;
            }

            foreach (P12PipelineSafeFunctionalModule module in wheelModules)
            {
                if (module == null)
                    return false;
            }

            return true;
        }

        private void BuildWheelModules()
        {
            Transform wheelsRoot =
                FindDeepChild(transform, "Wheels");

            if (wheelsRoot == null)
            {
                Debug.LogWarning("[Pack 19] Wheels root not found.");
                return;
            }

            List<Transform> wheels = new List<Transform>();

            foreach (Transform wheel in wheelsRoot)
            {
                if (wheel != null)
                    wheels.Add(wheel);
            }

            Material material = FindVehicleMaterial();

            foreach (Transform wheel in wheels)
            {
                int side = wheel.localPosition.x < 0f ? -1 : 1;
                float z = wheel.localPosition.z;

                GameObject mount =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);

                mount.name =
                    side < 0
                        ? "P19_WheelMount_L"
                        : "P19_WheelMount_R";

                mount.transform.SetParent(transform, false);
                mount.transform.localPosition =
                    new Vector3(
                        side * wheelMountX,
                        wheelMountY,
                        z
                    );

                mount.transform.localScale =
                    Vector3.one * wheelMountSize;

                Renderer mountRenderer = mount.GetComponent<Renderer>();

                if (mountRenderer != null && material != null)
                    mountRenderer.sharedMaterial = material;

                P12PipelineSafeDamageable damageable =
                    mount.AddComponent<P12PipelineSafeDamageable>();

                damageable.Configure(wheelMountHp, true);

                P12_PipelineSafePlayerBlock anchor =
                    structure.FindNearestStructuralBlock(
                        mount.transform.position
                    );

                P12PipelineSafeFunctionalModule module =
                    mount.AddComponent<P12PipelineSafeFunctionalModule>();

                module.Configure(
                    P12FunctionalModuleType.Wheel,
                    side,
                    anchor,
                    wheel,
                    transform
                );

                BuildAxle(mount.transform, wheel, side, material);
                wheelModules.Add(module);
            }
        }

        private void BuildAxle(
            Transform mount,
            Transform wheel,
            int side,
            Material material)
        {
            float mountOuter =
                Mathf.Abs(mount.localPosition.x) +
                wheelMountSize * 0.5f;

            float wheelInner =
                Mathf.Abs(wheel.localPosition.x) - 0.19f;

            float length =
                Mathf.Max(
                    0.12f,
                    wheelInner - mountOuter
                );

            GameObject axle =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            axle.name = "P19_WheelAxle";
            axle.transform.SetParent(transform, false);
            axle.transform.localPosition =
                new Vector3(
                    side * ((mountOuter + wheelInner) * 0.5f),
                    wheelMountY,
                    wheel.localPosition.z
                );

            axle.transform.localScale =
                new Vector3(
                    length,
                    axleThickness,
                    axleThickness
                );

            Renderer renderer = axle.GetComponent<Renderer>();

            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            Collider collider = axle.GetComponent<Collider>();

            if (collider != null)
                DestroyModuleObject(collider);
        }

        private void BuildWeaponModule()
        {
            Transform turret =
                FindDeepChild(transform, "Turret_Yaw");

            if (turret == null)
            {
                weaponOnline = false;
                Debug.LogWarning("[Pack 19] Turret_Yaw not found.");
                return;
            }

            GameObject mount =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            mount.name = "P19_WeaponMount";
            mount.transform.SetParent(transform, false);
            mount.transform.localPosition =
                weaponMountLocalPosition;
            mount.transform.localScale =
                Vector3.one * weaponMountSize;

            Renderer renderer = mount.GetComponent<Renderer>();
            Material material = FindTurretMaterial(turret);

            if (material == null)
                material = FindVehicleMaterial();

            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            P12PipelineSafeDamageable damageable =
                mount.AddComponent<P12PipelineSafeDamageable>();

            damageable.Configure(weaponMountHp, true);

            P12_PipelineSafePlayerBlock anchor =
                structure.FindNearestStructuralBlock(
                    mount.transform.position
                );

            weaponModule =
                mount.AddComponent<P12PipelineSafeFunctionalModule>();

            weaponModule.Configure(
                P12FunctionalModuleType.Weapon,
                0,
                anchor,
                turret,
                transform
            );
        }

        private void EvaluateModules()
        {
            ResolveDependencies();

            if (structure != null && !structure.CoreAlive)
                ForceAllModulesLost();

            activeWheelCount = 0;
            activeLeftCount = 0;
            activeRightCount = 0;

            foreach (P12PipelineSafeFunctionalModule module in wheelModules)
            {
                if (module == null)
                    continue;

                if (!module.ModuleLost &&
                    module.AnchorBlock != null &&
                    !structure.IsBlockConnected(module.AnchorBlock))
                {
                    module.ForceStructuralLoss(
                        module.transform.position - transform.position
                    );
                }

                if (!module.IsOperational(structure))
                    continue;

                activeWheelCount++;

                if (module.Side < 0)
                    activeLeftCount++;
                else if (module.Side > 0)
                    activeRightCount++;
            }

            ActiveWeaponCount = 0;
            foreach (var weapon in WeaponModules)
            {
                if (weapon == null) continue;
                if (!weapon.ModuleLost && weapon.AnchorBlock != null && !structure.IsBlockConnected(weapon.AnchorBlock))
                    weapon.ForceStructuralLoss(weapon.transform.position - transform.position);
                if (weapon.IsOperational(structure)) ActiveWeaponCount++;
            }
            weaponOnline = ActiveWeaponCount > 0;

            RecalculateMobility();
        }

        private void RecalculateMobility()
        {
            if (initialWheelCount <= 0)
            {
                driveFactor = 1f;
                turnFactor = 1f;
                return;
            }

            if (activeWheelCount == 0)
            {
                driveFactor = 0f;
                turnFactor = 0f;
                return;
            }

            float totalRatio =
                Mathf.Clamp01(
                    activeWheelCount /
                    (float)initialWheelCount
                );

            driveFactor =
                Mathf.Lerp(
                    minimumDriveFactor,
                    1f,
                    totalRatio
                );

            if (activeLeftCount == 0 || activeRightCount == 0)
            {
                turnFactor = oneSideTurnFactor;
                return;
            }

            float leftRatio =
                initialLeftCount > 0
                    ? activeLeftCount / (float)initialLeftCount
                    : 1f;

            float rightRatio =
                initialRightCount > 0
                    ? activeRightCount / (float)initialRightCount
                    : 1f;

            turnFactor =
                Mathf.Lerp(
                    minimumTurnFactor,
                    1f,
                    Mathf.Min(leftRatio, rightRatio)
                );
        }

        private void ForceAllModulesLost()
        {
            foreach (P12PipelineSafeFunctionalModule module in wheelModules)
            {
                if (module != null && !module.ModuleLost)
                {
                    module.ForceStructuralLoss(
                        module.transform.position - transform.position
                    );
                }
            }

            foreach (var weapon in WeaponModules)
            {
                if (weapon != null && !weapon.ModuleLost)
                    weapon.ForceStructuralLoss(weapon.transform.position - transform.position);
            }
        }

        private void DisableLegacyWeaponAnchor()
        {
            MonoBehaviour[] behaviours =
                GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour != null &&
                    behaviour.GetType().Name ==
                    "P12PipelineSafeWeaponAnchor")
                {
                    behaviour.enabled = false;
                }
            }
        }

        private void RemoveOldMounts()
        {
            List<GameObject> remove = new List<GameObject>();

            Transform[] all =
                GetComponentsInChildren<Transform>(true);

            foreach (Transform item in all)
            {
                if (item == null || item == transform)
                    continue;

                string n = item.name;

                if (n == "P17_WheelMounts" ||
                    n == "P17_WheelMountBlock" ||
                    n == "P17_WheelAxle" ||
                    n == "WheelMountBlock" ||
                    n == "WheelAxle" ||
                    n.StartsWith("P19_WheelMount_", StringComparison.Ordinal) ||
                    n == "P19_WheelAxle" ||
                    n == "P19_WeaponMount")
                {
                    remove.Add(item.gameObject);
                }
            }

            HashSet<GameObject> unique =
                new HashSet<GameObject>(remove);

            foreach (GameObject item in unique)
            {
                if (item != null)
                    DestroyModuleObject(item);
            }
        }

        private Material FindVehicleMaterial()
        {
            P12_PipelineSafePlayerBlock[] blocks =
                GetComponentsInChildren<
                    P12_PipelineSafePlayerBlock
                >(true);

            foreach (P12_PipelineSafePlayerBlock block in blocks)
            {
                if (block == null)
                    continue;

                Renderer renderer = block.GetComponent<Renderer>();

                if (renderer != null && renderer.sharedMaterial != null)
                    return renderer.sharedMaterial;
            }

            return null;
        }

        private static Material FindTurretMaterial(Transform turret)
        {
            Renderer renderer =
                turret != null
                    ? turret.GetComponentInChildren<Renderer>(true)
                    : null;

            return renderer != null
                ? renderer.sharedMaterial
                : null;
        }

        private static Transform FindDeepChild(
            Transform parent,
            string targetName)
        {
            if (parent == null)
                return null;

            foreach (Transform child in parent)
            {
                if (child.name == targetName)
                    return child;

                Transform nested =
                    FindDeepChild(child, targetName);

                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static void DestroyModuleObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(target);
                return;
            }
#endif

            UnityEngine.Object.Destroy(target);
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem(
            "Tools/Rogue Machine/[19] Functional Modules/REBUILD MODULES ON CURRENT TANK"
        )]
        private static void RebuildCurrentModules()
        {
            P12PipelineSafeTankController tank =
                UnityEngine.Object.FindFirstObjectByType<
                    P12PipelineSafeTankController
                >();

            if (tank == null)
            {
                Debug.LogError("[Pack 19] No player tank found.");
                return;
            }

            P12PipelineSafeVehicleModuleSystem system =
                tank.GetComponent<
                    P12PipelineSafeVehicleModuleSystem
                >();

            if (system == null)
            {
                system =
                    UnityEditor.Undo.AddComponent<
                        P12PipelineSafeVehicleModuleSystem
                    >(tank.gameObject);
            }

            system.RebuildModulesNow();

            UnityEditor.EditorUtility.SetDirty(system);

            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager
                        .GetActiveScene()
                );
        }

        [UnityEditor.MenuItem(
            "Tools/Rogue Machine/[19] Functional Modules/DUMP MODULE STATUS"
        )]
        private static void DumpStatus()
        {
            P12PipelineSafeVehicleModuleSystem system =
                UnityEngine.Object.FindFirstObjectByType<
                    P12PipelineSafeVehicleModuleSystem
                >();

            if (system == null)
            {
                Debug.LogWarning(
                    "[Pack 19] Functional module system not found."
                );
                return;
            }

            Debug.Log(
                "[Pack 19] Wheels " +
                system.ActiveWheelCount + "/" +
                system.InitialWheelCount +
                " | L=" + system.ActiveLeftCount +
                " R=" + system.ActiveRightCount +
                " | Drive=" +
                Mathf.RoundToInt(system.DriveFactor * 100f) +
                "% | Turn=" +
                Mathf.RoundToInt(system.TurnFactor * 100f) +
                "% | Weapon=" +
                (system.WeaponOnline ? "ONLINE" : "OFFLINE")
            );
        }
#endif
    }
}
