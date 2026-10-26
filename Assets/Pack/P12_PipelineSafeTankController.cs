using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RogueMachine.P12PipelineSafe
{
    [DefaultExecutionOrder(500)]
    [RequireComponent(typeof(Rigidbody))]
    public class P12PipelineSafeTankController : MonoBehaviour
    {
        [Header("Arcade tracked movement")]
        [SerializeField] private float forwardSpeed = 7.5f;
        [SerializeField] private float reverseSpeed = 4.5f;
        [SerializeField] private float turnSpeed = 82f;

        [Header("Flat arena lock")]
        [SerializeField] private float lockedRootY = 0.70f;

        [Header("Collision guard")]
        [SerializeField] private float collisionSkin = 0.045f;
        [SerializeField] private float envelopePadding = 0.025f;
        [SerializeField] private int depenetrationIterations = 4;

        [Header("Wheel layout")]
        [SerializeField] private float wheelSpacing = 0.98f;
        [SerializeField] private float wheelSideOffset = 2.36f;
        [SerializeField] private float wheelLocalY = -0.27f;

        [Header("Integrated turret")]
        [SerializeField] private float turretYawSpeed = 170f;
        [SerializeField] private float cannonPitchSpeed = 125f;
        [SerializeField] private float minCannonPitch = -12f;
        [SerializeField] private float maxCannonPitch = 26f;
        [SerializeField] private float aimRange = 160f;

        [Header("Integrated physical weapon")]
        [SerializeField] private float shotDamage = 40f;
        [SerializeField] private float fireInterval = 0.38f;
        [SerializeField] private float projectileSpeed = 34f;
        [SerializeField] private float projectileDiameter = 0.20f;
        [SerializeField] private float projectileMass = 1.8f;
        [SerializeField] private float projectileLifetime = 6f;

        private Rigidbody rb;
        private BoxCollider movementEnvelope;

        private float moveInput;
        private float turnInput;
        private bool fireHeld;

        private float currentSpeed;

        private int lastKnownBlockCount = -1;

        private Transform turretYaw;
        private Transform cannonPitch;
        private Transform muzzle;
        private Camera aimCamera;

        private float currentTurretYaw;
        private float currentCannonPitch;
        private float nextFireTime;

        private bool weaponBound;
        private RoboRougelike.R3.R3VehicleAssembly modularAssembly;

        public void ConfigureChassis(float speed, float turning, float mass)
        {
            forwardSpeed = Mathf.Clamp(speed, 2, 14); reverseSpeed = forwardSpeed * 0.6f;
            turnSpeed = Mathf.Clamp(turning, 35, 130);
            GetComponent<Rigidbody>().mass = Mathf.Max(50, mass);
        }

        private Material projectileMaterial;
        private bool gameplayInputEnabled = true;
        private float damageModifier = 1f;
        private float intervalModifier = 1f;
        private float mobilityModifier = 1f;

        public float EffectiveDamage => shotDamage * damageModifier;
        public float EffectiveFireInterval => fireInterval * intervalModifier;
        public float EffectiveForwardSpeed => forwardSpeed * mobilityModifier;
        public float EffectiveReverseSpeed => reverseSpeed * mobilityModifier;
        public float EffectiveTurnSpeed => turnSpeed * mobilityModifier;
        public bool GameplayInputEnabled => gameplayInputEnabled;

        public void SetRunModifiers(float damage, float interval, float mobility)
        {
            damageModifier = Mathf.Clamp(damage, 1f, 3f);
            intervalModifier = Mathf.Clamp(interval, 0.35f, 1f);
            mobilityModifier = Mathf.Clamp(mobility, 1f, 2f);
        }

        public void SetGameplayInputEnabled(bool active)
        {
            gameplayInputEnabled = active;
            if (active) return;
            moveInput = turnInput = currentSpeed = 0;
            fireHeld = false;
            GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>()?.CancelActions();
            if (rb != null) StopResidualPhysics();
        }

        
        private P12PipelineSafeVehicleModuleSystem moduleSystem;
public float CurrentForwardSpeed =>
            currentSpeed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            rb.useGravity = false;
            rb.interpolation =
                RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;

            rb.constraints =
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            movementEnvelope =
                GetComponent<BoxCollider>();

            if (movementEnvelope == null)
                movementEnvelope =
                    gameObject.AddComponent<BoxCollider>();

            Vector3 position = rb.position;
            position.y = lockedRootY;
            rb.position = position;

            movementEnvelope.isTrigger = true;
            RefreshCollisionEnvelope(true);
            FixWheelSpacing();
            EnsureFunctionalModuleSystem();
            BindWeaponRig();
            DisableLegacyWeaponControllers();
            modularAssembly = GetComponent<RoboRougelike.R3.R3VehicleAssembly>();
            drive=GetComponent<RoboRougelike.R6.R6VehicleDrive>();
            if(drive==null)drive=gameObject.AddComponent<RoboRougelike.R6.R6VehicleDrive>();
        }

        private void Start()
        {
            FixWheelSpacing();
            EnsureFunctionalModuleSystem();
            BindWeaponRig();
            DisableLegacyWeaponControllers();
        }

        private void OnDestroy()
        {
            if (projectileMaterial != null)
                Destroy(projectileMaterial);
        }

        private void Update()
        {
            moveInput = 0f;
            turnInput = 0f;
            fireHeld = false;
            if (!gameplayInputEnabled) return;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed ||
                    keyboard.upArrowKey.isPressed)
                    moveInput += 1f;

                if (keyboard.sKey.isPressed ||
                    keyboard.downArrowKey.isPressed)
                    moveInput -= 1f;

                if (keyboard.dKey.isPressed ||
                    keyboard.rightArrowKey.isPressed)
                    turnInput += 1f;

                if (keyboard.aKey.isPressed ||
                    keyboard.leftArrowKey.isPressed)
                    turnInput -= 1f;
            }

            fireHeld =
                Mouse.current != null &&
                Mouse.current.leftButton.isPressed;
#else
            if (Input.GetKey(KeyCode.W) ||
                Input.GetKey(KeyCode.UpArrow))
                moveInput += 1f;

            if (Input.GetKey(KeyCode.S) ||
                Input.GetKey(KeyCode.DownArrow))
                moveInput -= 1f;

            if (Input.GetKey(KeyCode.D) ||
                Input.GetKey(KeyCode.RightArrow))
                turnInput += 1f;

            if (Input.GetKey(KeyCode.A) ||
                Input.GetKey(KeyCode.LeftArrow))
                turnInput -= 1f;

            fireHeld =
                Input.GetMouseButton(0);
#endif
        }

        private RoboRougelike.R6.R6VehicleDrive drive;
        private void FixedUpdate()
        {
            if (rb.isKinematic) return;
            if(drive==null)drive=GetComponent<RoboRougelike.R6.R6VehicleDrive>();
            RefreshCollisionEnvelope(false);
            drive.Step(gameplayInputEnabled?moveInput:0,gameplayInputEnabled?turnInput:0,forwardSpeed*mobilityModifier,reverseSpeed*mobilityModifier,turnSpeed*mobilityModifier);
            currentSpeed=drive.ForwardSpeed;
        }

        private void LateUpdate()
        {
            if (!gameplayInputEnabled) return;
            // TankController definitely exists and runs because movement works.
            // It now owns the weapon too, so separate broken Turret/Gun
            // components cannot prevent aiming or firing.
            if (!weaponBound)
                BindWeaponRig();

            if (!weaponBound)
                return;

            EnsureFunctionalModuleSystem();

            if (moduleSystem != null &&
                !moduleSystem.WeaponOnline)
            {
                return;
            }

            if (aimCamera == null)
                aimCamera = Camera.main;

            if (aimCamera == null)
                return;

            Vector3 aimPoint =
                FindAimPoint(true);

            RotateTurretToward(
                aimPoint
            );

            PitchCannonToward(
                aimPoint
            );
            if (modularAssembly != null) modularAssembly.AimGuns(aimPoint);

            if(modularAssembly!=null)modularAssembly.SetContinuousFireHeld(fireHeld);
            if (fireHeld && modularAssembly != null) modularAssembly.FireGuns();
            else if (fireHeld && Time.time >= nextFireTime)
            {
                nextFireTime =
                    Time.time +
                    EffectiveFireInterval;

                FireCurrentBarrelDirection();
            }
        }

        private void EnsureFunctionalModuleSystem()
        {
            if (moduleSystem != null)
                return;

            moduleSystem =
                GetComponent<
                    P12PipelineSafeVehicleModuleSystem
                >();

            if (moduleSystem == null)
            {
                moduleSystem =
                    gameObject.AddComponent<
                        P12PipelineSafeVehicleModuleSystem
                    >();
            }
        }

        private void BindWeaponRig()
        {
            turretYaw =
                FindDeepChild(
                    transform,
                    "Turret_Yaw"
                );

            cannonPitch =
                FindDeepChild(
                    transform,
                    "Cannon_Pitch"
                );

            muzzle =
                FindDeepChild(
                    transform,
                    "Muzzle"
                );

            aimCamera =
                Camera.main;

            weaponBound =
                turretYaw != null &&
                cannonPitch != null &&
                muzzle != null;

            if (!weaponBound)
            {
                Debug.LogWarning(
                    "[Pack 16] Weapon rig not found yet. " +
                    "Expected Turret_Yaw / Cannon_Pitch / Muzzle."
                );
                return;
            }

            currentTurretYaw =
                NormalizeAngle(
                    turretYaw.localEulerAngles.y
                );

            currentCannonPitch =
                NormalizeAngle(
                    cannonPitch.localEulerAngles.x
                );

            EnsureProjectileMaterial();

            Debug.Log(
                "[Pack 16] Integrated weapon bound successfully."
            );
        }

        private void DisableLegacyWeaponControllers()
        {
            MonoBehaviour[] behaviours =
                GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null ||
                    behaviour == this)
                    continue;

                string typeName =
                    behaviour.GetType().Name;

                if (typeName ==
                        "P12PipelineSafeTurret" ||
                    typeName ==
                        "P12PipelineSafePlayerGun")
                {
                    behaviour.enabled = false;
                }
            }
        }

        public Vector3 ModuleAimPoint()
        {
            if(aimCamera==null)aimCamera=Camera.main;
            return aimCamera!=null?FindAimPoint():transform.position+transform.forward*24;
        }
        public Vector3 LastConsumedAimPoint {get;private set;}
        public Collider LastConsumedAimCollider {get;private set;}
        public int LastConsumedAimFrame {get;private set;}=-1;
        private Vector3 FindAimPoint(bool consumed=false)
        {
            Ray ray =
                aimCamera.ViewportPointToRay(
                    new Vector3(
                        0.5f,
                        0.5f,
                        0f
                    )
                );

            RaycastHit[] hits =
                Physics.RaycastAll(
                    ray.origin,
                    ray.direction,
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

                Transform t =
                    hit.collider.transform;

                if (t == transform ||
                    t.IsChildOf(transform))
                    continue;

                var shell=hit.collider.GetComponentInParent<P17PhysicalShell>();
                if(shell!=null&&shell.OwnerRoot==transform)continue;
                var rocket=hit.collider.GetComponentInParent<RoboRougelike.Mechanics.Rockets.RocketProjectile>();
                if(rocket!=null&&rocket.OwnerRoot==transform)continue;

                if(consumed){LastConsumedAimPoint=hit.point;LastConsumedAimCollider=hit.collider;LastConsumedAimFrame=Time.frameCount;}
                return hit.point;
            }

            var fallback=ray.origin+ray.direction*aimRange;
            if(consumed){LastConsumedAimPoint=fallback;LastConsumedAimCollider=null;LastConsumedAimFrame=Time.frameCount;}
            return fallback;
        }

        private void RotateTurretToward(
            Vector3 aimPoint)
        {
            Vector3 worldDirection =
                aimPoint -
                turretYaw.position;

            Vector3 localDirection =
                transform.InverseTransformDirection(
                    worldDirection
                );

            localDirection.y = 0f;

            if (localDirection.sqrMagnitude <
                0.0001f)
                return;

            float targetYaw =
                Mathf.Atan2(
                    localDirection.x,
                    localDirection.z
                ) *
                Mathf.Rad2Deg;

            currentTurretYaw =
                Mathf.MoveTowardsAngle(
                    currentTurretYaw,
                    targetYaw,
                    turretYawSpeed *
                    Time.deltaTime
                );

            turretYaw.localRotation =
                Quaternion.Euler(
                    0f,
                    currentTurretYaw,
                    0f
                );
        }

        private void PitchCannonToward(
            Vector3 aimPoint)
        {
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
                return;

            float targetPitch =
                -Mathf.Atan2(
                    localDirection.y,
                    horizontalDistance
                ) *
                Mathf.Rad2Deg;

            targetPitch =
                Mathf.Clamp(
                    targetPitch,
                    minCannonPitch,
                    maxCannonPitch
                );

            currentCannonPitch =
                Mathf.MoveTowardsAngle(
                    currentCannonPitch,
                    targetPitch,
                    cannonPitchSpeed *
                    Time.deltaTime
                );

            currentCannonPitch =
                Mathf.Clamp(
                    currentCannonPitch,
                    minCannonPitch,
                    maxCannonPitch
                );

            cannonPitch.localRotation =
                Quaternion.Euler(
                    currentCannonPitch,
                    0f,
                    0f
                );
        }

        private void FireCurrentBarrelDirection()
        { FireFrom(muzzle); }

        public void FireFrom(Transform barrel)
        { SpawnPhysicalShell(barrel,EffectiveDamage,projectileSpeed,projectileDiameter,projectileMass,
            RoboRougelike.R4.R4WeaponKind.Basic,null); }

        public void FireFrom(Transform barrel,RoboRougelike.R4.R4WeaponKind kind,string partId)
        {
            var profile=RoboRougelike.R4.R4WeaponDefinition.Get(kind);
            SpawnPhysicalShell(barrel,profile.Damage*damageModifier,profile.Speed,profile.Diameter,profile.ProjectileMass,kind,partId);
        }
        public float WeaponInterval(RoboRougelike.R4.R4WeaponKind kind) => RoboRougelike.R4.R4WeaponDefinition.Get(kind).Interval*intervalModifier;
        public float PackageDamageScale => damageModifier;
        public float PackageIntervalScale => intervalModifier;
        private void SpawnPhysicalShell(Transform barrel,float hitDamage,float projectileSpeed,float projectileDiameter,
            float projectileMass,RoboRougelike.R4.R4WeaponKind kind,string partId)
        {
            if (barrel == null)
                return;

            Vector3 direction =
                barrel.forward.normalized;

            Vector3 origin =
                barrel.position +
                direction *
                Mathf.Max(
                    0.16f,
                    projectileDiameter * 0.75f
                );

            GameObject shell =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere
                );

            RoboRougelike.R6.R6CombatFeedback.Muzzle(barrel,kind);

            shell.name =
                "P17_PlayerPhysicalShell";
            var originTag=shell.AddComponent<RoboRougelike.R4.R4ProjectileOrigin>();originTag.Weapon=kind;originTag.PartId=partId;

            shell.transform.position =
                origin;

            shell.transform.localScale =
                Vector3.one *
                projectileDiameter;

            Renderer shellRenderer =
                shell.GetComponent<Renderer>();

            if (shellRenderer != null &&
                projectileMaterial != null)
            {
                shellRenderer.sharedMaterial =
                    projectileMaterial;
            }

            SphereCollider sphere =
                shell.GetComponent<SphereCollider>();

            if (sphere != null)
            {
                sphere.isTrigger = false;
            }

            Rigidbody projectileBody =
                shell.AddComponent<Rigidbody>();

            projectileBody.mass =
                projectileMass;

            projectileBody.useGravity =
                false;

            projectileBody.interpolation =
                RigidbodyInterpolation.Interpolate;

            projectileBody.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;

#if UNITY_6000_0_OR_NEWER
            projectileBody.linearVelocity =
                direction *
                projectileSpeed;
#else
            projectileBody.velocity =
                direction *
                projectileSpeed;
#endif

            P17PhysicalShell projectile =
                shell.AddComponent<
                    P17PhysicalShell
                >();

            projectile.Configure(
                transform,
                hitDamage,
                projectileLifetime,
                direction,
                null
            );

            IgnoreOwnerColliders(
                sphere
            );
        }

        private void IgnoreOwnerColliders(
            Collider projectileCollider)
        {
            if (projectileCollider == null)
                return;

            Collider[] ownerColliders =
                GetComponentsInChildren<
                    Collider
                >(true);

            foreach (Collider ownerCollider
                     in ownerColliders)
            {
                if (ownerCollider == null ||
                    ownerCollider ==
                    projectileCollider)
                    continue;

                Physics.IgnoreCollision(
                    projectileCollider,
                    ownerCollider,
                    true
                );
            }
        }

        private void EnsureProjectileMaterial()
        {
            if (projectileMaterial != null)
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
                    continue;

                projectileMaterial =
                    new Material(
                        renderer.sharedMaterial
                    );

                projectileMaterial.name =
                    "P17_PhysicalShellMaterial";

                Color color =
                    new Color(
                        1f,
                        0.72f,
                        0.12f,
                        1f
                    );

                if (projectileMaterial.HasProperty(
                    "_BaseColor"))
                {
                    projectileMaterial.SetColor(
                        "_BaseColor",
                        color
                    );
                }

                if (projectileMaterial.HasProperty(
                    "_Color"))
                {
                    projectileMaterial.SetColor(
                        "_Color",
                        color
                    );
                }

                return;
            }
        }

        private void BuildWheelMounts()
        {
            // Pack 19: replaced by P12PipelineSafeVehicleModuleSystem.
        }

        private Material FindVehicleMaterial()
        {
            P12_PipelineSafePlayerBlock[] blocks =
                GetComponentsInChildren<
                    P12_PipelineSafePlayerBlock
                >(true);

            foreach (
                P12_PipelineSafePlayerBlock block
                in blocks)
            {
                if (block == null)
                    continue;

                Renderer renderer =
                    block.GetComponent<Renderer>();

                if (renderer != null &&
                    renderer.sharedMaterial != null)
                {
                    return
                        renderer.sharedMaterial;
                }
            }

            return null;
        }

        private void FixWheelSpacing()
        {
            Transform wheels =
                FindDeepChild(
                    transform,
                    "Wheels"
                );

            if (wheels == null ||
                wheels.childCount < 2)
                return;

            Transform[] left =
                new Transform[6];

            Transform[] right =
                new Transform[6];

            int leftCount = 0;
            int rightCount = 0;

            foreach (Transform wheel in wheels)
            {
                if (wheel.localPosition.x < 0f)
                {
                    if (leftCount < left.Length)
                        left[leftCount++] =
                            wheel;
                }
                else
                {
                    if (rightCount < right.Length)
                        right[rightCount++] =
                            wheel;
                }
            }

            SortByLocalZ(
                left,
                leftCount
            );

            SortByLocalZ(
                right,
                rightCount
            );

            RepositionWheelRow(
                left,
                leftCount,
                -wheelSideOffset
            );

            RepositionWheelRow(
                right,
                rightCount,
                wheelSideOffset
            );

        }

        private void RepositionWheelRow(
            Transform[] row,
            int count,
            float x)
        {
            if (count <= 0)
                return;

            float center =
                (count - 1) *
                0.5f;

            for (int i = 0;
                 i < count;
                 i++)
            {
                Transform wheel =
                    row[i];

                if (wheel == null)
                    continue;

                float z =
                    (i - center) *
                    wheelSpacing;

                wheel.localPosition =
                    new Vector3(
                        x,
                        wheelLocalY,
                        z
                    );
            }
        }

        private static void SortByLocalZ(
            Transform[] array,
            int count)
        {
            for (int i = 0;
                 i < count - 1;
                 i++)
            {
                for (int j = i + 1;
                     j < count;
                     j++)
                {
                    if (array[i] == null ||
                        array[j] == null)
                        continue;

                    if (array[j].localPosition.z <
                        array[i].localPosition.z)
                    {
                        Transform temp =
                            array[i];

                        array[i] =
                            array[j];

                        array[j] =
                            temp;
                    }
                }
            }
        }

        private void RefreshCollisionEnvelope(
            bool force)
        {
            if (movementEnvelope == null)
                return;

            P12_PipelineSafePlayerBlock[] blocks =
                GetComponentsInChildren<
                    P12_PipelineSafePlayerBlock
                >();

            if (!force &&
                blocks.Length ==
                lastKnownBlockCount)
                return;

            lastKnownBlockCount =
                blocks.Length;

            bool hasPoint = false;
            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;

            foreach (
                P12_PipelineSafePlayerBlock block
                in blocks)
            {
                if (block == null)
                    continue;

                Collider collider =
                    block.GetComponent<Collider>();

                if (collider == null ||
                    !collider.enabled)
                    continue;

                // World AABBs expand on yaw. Transform the collider's own local box corners
                // instead, so turning/rebuilding cannot enlarge the physical hull.
                var box = collider as BoxCollider;
                Bounds b = box != null ? new Bounds(box.center, box.size) : new Bounds(Vector3.zero, Vector3.one);

                Vector3 bMin = b.min;
                Vector3 bMax = b.max;

                Vector3[] corners =
                {
                    new Vector3(bMin.x, bMin.y, bMin.z),
                    new Vector3(bMin.x, bMin.y, bMax.z),
                    new Vector3(bMin.x, bMax.y, bMin.z),
                    new Vector3(bMin.x, bMax.y, bMax.z),
                    new Vector3(bMax.x, bMin.y, bMin.z),
                    new Vector3(bMax.x, bMin.y, bMax.z),
                    new Vector3(bMax.x, bMax.y, bMin.z),
                    new Vector3(bMax.x, bMax.y, bMax.z)
                };

                foreach (Vector3 worldCorner in corners)
                {
                    Vector3 localPoint =
                        transform.InverseTransformPoint(
                            collider.transform.TransformPoint(worldCorner)
                        );

                    if (!hasPoint)
                    {
                        min = localPoint;
                        max = localPoint;
                        hasPoint = true;
                    }
                    else
                    {
                        min =
                            Vector3.Min(
                                min,
                                localPoint
                            );

                        max =
                            Vector3.Max(
                                max,
                                localPoint
                            );
                    }
                }
            }

            if (!hasPoint)
                return;

            Vector3 padding =
                Vector3.one *
                envelopePadding;

            min -= padding;
            max += padding;

            movementEnvelope.center =
                (min + max) *
                0.5f;

            movementEnvelope.size =
                max - min;
        }

        private Vector3 ClampTranslation(
            Vector3 rootPosition,
            Quaternion rootRotation,
            Vector3 desiredDelta)
        {
            float distance =
                desiredDelta.magnitude;

            if (distance <= 0.00001f)
                return Vector3.zero;

            Vector3 direction =
                desiredDelta /
                distance;

            Vector3 center =
                GetEnvelopeCenterWorld(
                    rootPosition,
                    rootRotation
                );

            Vector3 halfExtents =
                GetEnvelopeHalfExtents();

            RaycastHit[] hits =
                Physics.BoxCastAll(
                    center,
                    halfExtents,
                    direction,
                    rootRotation,
                    distance +
                    collisionSkin,
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
                if (!IsExternalCollider(
                    hit.collider))
                    continue;

                float allowedDistance =
                    Mathf.Max(
                        0f,
                        hit.distance -
                        collisionSkin
                    );

                allowedDistance =
                    Mathf.Min(
                        allowedDistance,
                        distance
                    );

                return
                    direction *
                    allowedDistance;
            }

            return desiredDelta;
        }

        private bool CanOccupy(
            Vector3 rootPosition,
            Quaternion rootRotation)
        {
            Vector3 center =
                GetEnvelopeCenterWorld(
                    rootPosition,
                    rootRotation
                );

            Collider[] overlaps =
                Physics.OverlapBox(
                    center,
                    GetEnvelopeHalfExtents(),
                    rootRotation,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );

            foreach (Collider collider in overlaps)
            {
                if (IsExternalCollider(
                    collider))
                    return false;
            }

            return true;
        }

        private void ResolveExistingPenetration()
        {
            if (movementEnvelope == null)
                return;

            float remainingCorrection = 4f * Time.fixedDeltaTime;

            for (int iteration = 0;
                 iteration <
                 depenetrationIterations;
                 iteration++)
            {
                Vector3 center =
                    GetEnvelopeCenterWorld(
                        rb.position,
                        rb.rotation
                    );

                Collider[] overlaps =
                    Physics.OverlapBox(
                        center,
                        GetEnvelopeHalfExtents(),
                        rb.rotation,
                        ~0,
                        QueryTriggerInteraction.Ignore
                    );

                bool corrected = false;

                foreach (Collider other in overlaps)
                {
                    if (!IsExternalCollider(
                        other))
                        continue;

                    if (Physics.ComputePenetration(
                        movementEnvelope,
                        transform.position,
                        transform.rotation,
                        other,
                        other.transform.position,
                        other.transform.rotation,
                        out Vector3 direction,
                        out float distance))
                    {
                        direction.y = 0f;

                        if (direction.sqrMagnitude <
                            0.0001f)
                            continue;

                        direction.Normalize();

                        float step = Mathf.Min(distance + collisionSkin, remainingCorrection);
                        Vector3 correction = direction * step;
                        remainingCorrection -= step;

                        rb.position +=
                            correction;

                        Vector3 locked =
                            rb.position;

                        locked.y =
                            lockedRootY;

                        rb.position =
                            locked;

                        corrected = true;
                        break;
                    }
                }

                if (!corrected || remainingCorrection <= 0.00001f)
                    break;
            }
        }

        private bool IsExternalCollider(
            Collider collider)
        {
            if (collider == null ||
                !collider.enabled)
                return false;

            Transform t =
                collider.transform;

            if (t == transform ||
                t.IsChildOf(transform))
                return false;

            return true;
        }

        private Vector3 GetEnvelopeCenterWorld(
            Vector3 rootPosition,
            Quaternion rootRotation)
        {
            Vector3 scaledCenter =
                Vector3.Scale(
                    movementEnvelope.center,
                    transform.lossyScale
                );

            return
                rootPosition +
                rootRotation *
                scaledCenter;
        }

        private Vector3 GetEnvelopeHalfExtents()
        {
            Vector3 scale =
                transform.lossyScale;

            scale =
                new Vector3(
                    Mathf.Abs(scale.x),
                    Mathf.Abs(scale.y),
                    Mathf.Abs(scale.z)
                );

            return Vector3.Scale(
                movementEnvelope.size *
                0.5f,
                scale
            );
        }

        private void StopResidualPhysics()
        {
            if(rb == null || rb.isKinematic)return;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity =
                Vector3.zero;
            rb.angularVelocity =
                Vector3.zero;
#else
            rb.velocity =
                Vector3.zero;
            rb.angularVelocity =
                Vector3.zero;
#endif
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
                if (child.name ==
                    targetName)
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


    public sealed class P17PhysicalShell : MonoBehaviour
    {
        private static readonly System.Collections.Generic.HashSet<P17PhysicalShell> activeShells =
            new System.Collections.Generic.HashSet<P17PhysicalShell>();
        public static int AvailableSlots => Mathf.Max(0,96-activeShells.Count);
        private void OnEnable()
        {
            if(activeShells.Count>=96){gameObject.SetActive(false);Destroy(gameObject);return;}
            activeShells.Add(this);
            if(GetComponent<RoboRougelike.WeaponIntegration.RRLPointDefenseThreat>()==null)gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLPointDefenseThreat>();
        }
        private void OnDisable(){activeShells.Remove(this);}
        private Transform ownerRoot;
        public Transform OwnerRoot=>ownerRoot;
        public bool IsActiveProjectile=>isActiveAndEnabled&&!spent&&ownerRoot!=null;
        public Vector3 FlightVelocity=>railgun!=null?railgun.Velocity:GetComponent<Rigidbody>().linearVelocity;
        public bool Intercept(float amount)
        {if(!IsActiveProjectile||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return false;spent=true;gameObject.SetActive(false);Destroy(gameObject);return true;}
        private Transform targetRoot;
        private float damage;
        private Vector3 launchDirection;
        private bool spent;
        private Vector3 launchPosition;
        private float maxTravelDistance=float.PositiveInfinity;
        private RoboRougelike.WeaponIntegration.RRLRailPenetration railgun;
        public void ConfigureRailgun(RoboRougelike.WPN002Railgun.RailgunShot shot,float damageScale)
        {railgun=gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLRailPenetration>();railgun.Bind(shot,damageScale);}
        private void FixedUpdate(){if(railgun!=null&&isActiveAndEnabled)railgun.Step(Time.fixedDeltaTime);}


        public void Configure(
            Transform owner,
            float damageAmount,
            float lifetime,
            Vector3 initialDirection,
            Transform targetToIgnoreNonDamageable,
            float maxDistance=float.PositiveInfinity)
        {
            if(!isActiveAndEnabled)return;
            launchPosition=transform.position;
            maxTravelDistance=float.IsNaN(maxDistance)?0:Mathf.Max(0,maxDistance);
            ownerRoot = owner;
            targetRoot =
                targetToIgnoreNonDamageable;

            damage =
                damageAmount;

            launchDirection =
                initialDirection.normalized;

            Destroy(
                gameObject,
                Mathf.Max(
                    0.5f,
                    lifetime
                )
            );

            IgnoreOwnerCollisions();

            if (targetRoot != null)
                IgnoreTargetHelpers();
        }

        private void IgnoreOwnerCollisions()
        {
            Collider ownCollider =
                GetComponent<Collider>();

            if (ownCollider == null ||
                ownerRoot == null)
                return;

            Collider[] ownerColliders =
                ownerRoot
                .GetComponentsInChildren<
                    Collider
                >(true);

            foreach (
                Collider ownerCollider
                in ownerColliders)
            {
                if (ownerCollider == null ||
                    ownerCollider ==
                    ownCollider)
                    continue;

                Physics.IgnoreCollision(
                    ownCollider,
                    ownerCollider,
                    true
                );
            }
        }

        private void Update()
        {
            if(!spent&&(transform.position-launchPosition).sqrMagnitude>maxTravelDistance*maxTravelDistance)
            {spent=true;gameObject.SetActive(false);Destroy(gameObject);}
        }

        private void IgnoreTargetHelpers()
        {
            Collider ownCollider =
                GetComponent<Collider>();

            if (ownCollider == null ||
                targetRoot == null)
                return;

            Collider[] targetColliders =
                targetRoot
                .GetComponentsInChildren<
                    Collider
                >(true);

            foreach (
                Collider targetCollider
                in targetColliders)
            {
                if (targetCollider == null)
                    continue;

                P12PipelineSafeDamageable damageable =
                    targetCollider.GetComponent<
                        P12PipelineSafeDamageable
                    >();

                if (damageable == null)
                {
                    damageable =
                        targetCollider
                        .GetComponentInParent<
                            P12PipelineSafeDamageable
                        >();
                }

                if (damageable == null)
                {
                    Physics.IgnoreCollision(
                        ownCollider,
                        targetCollider,
                        true
                    );
                }
            }
        }

        private void OnCollisionEnter(
            Collision collision)
        {
            if (railgun!=null || spent ||
                collision == null ||
                collision.collider == null)
                return;

            Transform hitTransform =
                collision.collider.transform;

            if (ownerRoot != null &&
                (hitTransform == ownerRoot ||
                 hitTransform.IsChildOf(
                     ownerRoot)))
            {
                return;
            }

            // Dead/detached wrecks retain world physics but must not consume follow-up fire.
            // Also handles a second collision callback already queued in the death step.
            var deadPart=collision.collider.GetComponentInParent<P12PipelineSafeDamageable>();
            if(collision.collider.GetComponentInParent<RoboRougelike.R3.R3Debris>()!=null||
                (deadPart!=null&&deadPart.IsDead)||
                !RoboRougelike.WeaponIntegration.RRLHitRouter.CanHit(collision.collider,ownerRoot))
            {
                var own=GetComponent<Collider>();
                if(own!=null)Physics.IgnoreCollision(own,collision.collider,true);
                var shellBody=GetComponent<Rigidbody>();
                if(shellBody!=null)shellBody.linearVelocity=launchDirection*Mathf.Max(
                    shellBody.linearVelocity.magnitude,collision.relativeVelocity.magnitude);
                return;
            }
            spent = true;

            ContactPoint contact =
                collision.contactCount > 0
                    ? collision.GetContact(0)
                    : default;

            Vector3 point =
                collision.contactCount > 0
                    ? contact.point
                    : transform.position;

            if((point-launchPosition).sqrMagnitude<=maxTravelDistance*maxTravelDistance)
                RoboRougelike.WeaponIntegration.RRLHitRouter.Apply(collision.collider,damage,point,launchDirection,ownerRoot);

            Rigidbody hitBody =
                collision.rigidbody;

            if (hitBody != null && !hitBody.isKinematic)
            {
                hitBody.AddForceAtPosition(
                    launchDirection * 2.2f,
                    point,
                    ForceMode.Impulse
                );
            }

            Destroy(
                gameObject
            );
        }
    }

}
