using System;
using UnityEngine;

namespace RogueMachine.P12PipelineSafe
{
    [DefaultExecutionOrder(100)]
    public class P12PipelineSafeTurret : MonoBehaviour
    {
        [SerializeField] private Transform vehicleRoot;
        [SerializeField] private Transform turretYaw;
        [SerializeField] private Transform cannonPitch;
        [SerializeField] private Camera aimCamera;

        [Header("Tracking")]
        [SerializeField] private float yawSpeed = 165f;
        [SerializeField] private float pitchSpeed = 120f;
        [SerializeField] private float minPitch = -12f;
        [SerializeField] private float maxPitch = 26f;
        [SerializeField] private float aimRange = 150f;

        private float currentYaw;
        private float currentPitch;
        private bool anglesInitialized;

        public bool HasRequiredReferences =>
            vehicleRoot != null &&
            turretYaw != null &&
            cannonPitch != null &&
            aimCamera != null;

        public void Configure(
            Transform root,
            Transform yawPart,
            Transform pitchPart,
            Camera cameraToUse)
        {
            vehicleRoot = root;
            turretYaw = yawPart;
            cannonPitch = pitchPart;
            aimCamera = cameraToUse;

            InitializeAngles();
        }

        private void Awake()
        {
            RepairReferences();
        }

        private void OnEnable()
        {
            RepairReferences();
        }

        private void Start()
        {
            RepairReferences();
        }

        public void RepairReferences()
        {
            if (vehicleRoot == null)
                vehicleRoot = transform;

            if (turretYaw == null)
                turretYaw =
                    FindDeepChild(
                        vehicleRoot,
                        "Turret_Yaw"
                    );

            if (cannonPitch == null)
                cannonPitch =
                    FindDeepChild(
                        vehicleRoot,
                        "Cannon_Pitch"
                    );

            if (aimCamera == null)
                aimCamera = Camera.main;

            if (HasRequiredReferences)
                InitializeAngles();
        }

        private void InitializeAngles()
        {
            if (anglesInitialized ||
                turretYaw == null ||
                cannonPitch == null)
            {
                return;
            }

            currentYaw =
                NormalizeAngle(
                    turretYaw.localEulerAngles.y
                );

            currentPitch =
                NormalizeAngle(
                    cannonPitch.localEulerAngles.x
                );

            anglesInitialized = true;
        }

        private void LateUpdate()
        {
            if (!HasRequiredReferences)
            {
                RepairReferences();

                if (!HasRequiredReferences)
                    return;
            }

            Vector3 aimPoint =
                FindCameraAimPoint();

            UpdateYaw(
                aimPoint
            );

            UpdatePitch(
                aimPoint
            );
        }

        private Vector3 FindCameraAimPoint()
        {
            Ray cameraRay =
                aimCamera.ViewportPointToRay(
                    new Vector3(
                        0.5f,
                        0.5f,
                        0f
                    )
                );

            RaycastHit[] hits =
                Physics.RaycastAll(
                    cameraRay.origin,
                    cameraRay.direction,
                    aimRange,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );

            Array.Sort(
                hits,
                (a, b) =>
                    a.distance.CompareTo(
                        b.distance
                    )
            );

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                Transform hitTransform =
                    hit.collider.transform;

                if (hitTransform == vehicleRoot ||
                    hitTransform.IsChildOf(
                        vehicleRoot
                    ))
                {
                    continue;
                }

                return hit.point;
            }

            return
                cameraRay.origin +
                cameraRay.direction *
                aimRange;
        }

        private void UpdateYaw(
            Vector3 aimPoint)
        {
            Vector3 worldDirection =
                aimPoint -
                turretYaw.position;

            Vector3 localDirection =
                vehicleRoot.InverseTransformDirection(
                    worldDirection
                );

            localDirection.y = 0f;

            if (localDirection.sqrMagnitude <
                0.0001f)
            {
                return;
            }

            float targetYaw =
                Mathf.Atan2(
                    localDirection.x,
                    localDirection.z
                ) *
                Mathf.Rad2Deg;

            currentYaw =
                Mathf.MoveTowardsAngle(
                    currentYaw,
                    targetYaw,
                    yawSpeed *
                    Time.deltaTime
                );

            turretYaw.localRotation =
                Quaternion.Euler(
                    0f,
                    currentYaw,
                    0f
                );
        }

        private void UpdatePitch(
            Vector3 aimPoint)
        {
            // Yaw has already been applied this frame.
            Vector3 localDirection =
                turretYaw.InverseTransformDirection(
                    aimPoint -
                    cannonPitch.position
                );

            float horizontalDistance =
                Mathf.Sqrt(
                    localDirection.x *
                    localDirection.x +
                    localDirection.z *
                    localDirection.z
                );

            if (horizontalDistance <
                0.0001f)
            {
                return;
            }

            // Negative X aims upward, positive X aims downward.
            float targetPitch =
                -Mathf.Atan2(
                    localDirection.y,
                    horizontalDistance
                ) *
                Mathf.Rad2Deg;

            targetPitch =
                Mathf.Clamp(
                    targetPitch,
                    minPitch,
                    maxPitch
                );

            currentPitch =
                Mathf.MoveTowardsAngle(
                    currentPitch,
                    targetPitch,
                    pitchSpeed *
                    Time.deltaTime
                );

            currentPitch =
                Mathf.Clamp(
                    currentPitch,
                    minPitch,
                    maxPitch
                );

            cannonPitch.localRotation =
                Quaternion.Euler(
                    currentPitch,
                    0f,
                    0f
                );
        }

        private static float NormalizeAngle(
            float angle)
        {
            return
                Mathf.DeltaAngle(
                    0f,
                    angle
                );
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
                    FindDeepChild(
                        child,
                        targetName
                    );

                if (nested != null)
                    return nested;
            }

            return null;
        }
    }

#if UNITY_EDITOR
    public static class P15FireControlRepairMenu
    {
        [UnityEditor.MenuItem(
            "Tools/Rogue Machine/[15] Weapon Runtime Bind/REPAIR WEAPON REFERENCES"
        )]
        public static void RepairWeaponReferences()
        {
            P12PipelineSafeTurret turret =
                UnityEngine.Object.FindFirstObjectByType<
                    P12PipelineSafeTurret
                >();

            P12PipelineSafePlayerGun gun =
                UnityEngine.Object.FindFirstObjectByType<
                    P12PipelineSafePlayerGun
                >();

            if (turret != null)
            {
                turret.RepairReferences();
                UnityEditor.EditorUtility.SetDirty(
                    turret
                );
            }

            if (gun != null)
            {
                gun.RepairReferences();
                UnityEditor.EditorUtility.SetDirty(
                    gun
                );
            }

            UnityEditor.SceneManagement
                .EditorSceneManager
                .MarkSceneDirty(
                    UnityEngine.SceneManagement
                    .SceneManager
                    .GetActiveScene()
                );

            Debug.Log(
                "[Pack 15] Weapon references repaired. " +
                "Turret: " +
                (turret != null) +
                " | Gun: " +
                (gun != null)
            );
        }
    }
#endif
}
