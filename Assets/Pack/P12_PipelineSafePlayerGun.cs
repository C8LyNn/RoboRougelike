using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RogueMachine.P12PipelineSafe
{
    [DefaultExecutionOrder(200)]
    public class P12PipelineSafePlayerGun : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Material tracerMaterial;

        [Header("Weapon")]
        [SerializeField] private float damage = 40f;
        [SerializeField] private float fireInterval = 0.32f;
        [SerializeField] private float maxRange = 150f;
        [SerializeField] private float tracerStartOffset = 0.12f;

        private float nextFireTime;
        private bool fireHeld;

        private Material runtimeTracerMaterial;

        public bool HasRequiredReferences =>
            playerRoot != null &&
            muzzle != null;

        public void Configure(
            Camera cameraToUse,
            Transform muzzleTransform,
            Transform root,
            Material tracer)
        {
            aimCamera = cameraToUse;
            muzzle = muzzleTransform;
            playerRoot = root;
            tracerMaterial = tracer;
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

        private void OnDestroy()
        {
            if (runtimeTracerMaterial != null)
                Destroy(
                    runtimeTracerMaterial
                );
        }

        public void RepairReferences()
        {
            if (playerRoot == null)
                playerRoot = transform;

            if (aimCamera == null)
                aimCamera = Camera.main;

            if (muzzle == null)
            {
                muzzle =
                    FindDeepChild(
                        playerRoot,
                        "Muzzle"
                    );
            }

            EnsureTracerMaterial();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            fireHeld =
                Mouse.current != null &&
                Mouse.current.leftButton.isPressed;
#else
            fireHeld =
                Input.GetMouseButton(0);
#endif
        }

        private void LateUpdate()
        {
            if (!HasRequiredReferences)
            {
                RepairReferences();

                if (!HasRequiredReferences)
                    return;
            }

            if (!fireHeld ||
                Time.time <
                nextFireTime)
            {
                return;
            }

            nextFireTime =
                Time.time +
                fireInterval;

            ShootFromCurrentBarrelPose();
        }

        private void ShootFromCurrentBarrelPose()
        {
            Vector3 direction =
                muzzle.forward.normalized;

            Vector3 origin =
                muzzle.position +
                direction *
                tracerStartOffset;

            Vector3 end =
                origin +
                direction *
                maxRange;

            if (TryHitIgnoringPlayer(
                origin,
                direction,
                maxRange,
                out RaycastHit hit))
            {
                end = hit.point;

                P12PipelineSafeDamageable damageable =
                    hit.collider.GetComponent<
                        P12PipelineSafeDamageable
                    >();

                if (damageable == null)
                {
                    damageable =
                        hit.collider
                        .GetComponentInParent<
                            P12PipelineSafeDamageable
                        >();
                }

                if (damageable != null)
                {
                    damageable.ApplyDamage(
                        damage,
                        hit.point,
                        direction
                    );
                }
            }

            P12PipelineSafeTracer.Create(
                origin,
                end,
                tracerMaterial
            );
        }

        private bool TryHitIgnoringPlayer(
            Vector3 origin,
            Vector3 direction,
            float range,
            out RaycastHit validHit)
        {
            RaycastHit[] hits =
                Physics.RaycastAll(
                    origin,
                    direction,
                    range,
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

                if (hitTransform == playerRoot ||
                    hitTransform.IsChildOf(
                        playerRoot
                    ))
                {
                    continue;
                }

                validHit = hit;
                return true;
            }

            validHit = default;
            return false;
        }

        private void EnsureTracerMaterial()
        {
            if (tracerMaterial != null)
                return;

            Renderer[] renderers =
                GetComponentsInChildren<
                    Renderer
                >(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    renderer.sharedMaterial == null ||
                    renderer.sharedMaterial.shader == null ||
                    !renderer.sharedMaterial.shader.isSupported)
                {
                    continue;
                }

                runtimeTracerMaterial =
                    new Material(
                        renderer.sharedMaterial
                    );

                runtimeTracerMaterial.name =
                    "P15_RuntimeTracer";

                Color tracerColor =
                    new Color(
                        1f,
                        0.72f,
                        0.12f,
                        1f
                    );

                if (runtimeTracerMaterial.HasProperty(
                    "_BaseColor"
                ))
                {
                    runtimeTracerMaterial.SetColor(
                        "_BaseColor",
                        tracerColor
                    );
                }

                if (runtimeTracerMaterial.HasProperty(
                    "_Color"
                ))
                {
                    runtimeTracerMaterial.SetColor(
                        "_Color",
                        tracerColor
                    );
                }

                tracerMaterial =
                    runtimeTracerMaterial;

                break;
            }
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
}
