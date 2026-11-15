using System;
using System.Collections.Generic;
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
        [SerializeField] private float turretYawSpeed = 140f;
        [SerializeField] private float cannonPitchSpeed = 105f;
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
        private bool fireHeld, secondaryFireHeld, thirdFireHeld, fourthFireHeld;
        private bool waitForPrimaryRelease, waitForSecondaryRelease, waitForThirdRelease, waitForFourthRelease;

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
            forwardSpeed = float.IsNaN(speed)||float.IsInfinity(speed)?0f:Mathf.Max(0,speed); reverseSpeed = forwardSpeed * 0.6f;
            turnSpeed = float.IsNaN(turning)||float.IsInfinity(turning)?35f:Mathf.Max(0,turning);
            GetComponent<Rigidbody>().mass = Mathf.Max(50, mass);
        }

        private Material projectileMaterial;
        private bool gameplayInputEnabled = true;
        private float damageModifier = 1f;
        private float basicDamageModifier = 1f;
        private float basicBlastDamageModifier = 1f;
        private float basicBlastFraction;
        private float intervalModifier = 1f;
        private readonly float[] scopedWeaponIntervals=new float[System.Enum.GetValues(typeof(RoboRougelike.R4.R4WeaponKind)).Length];
        private bool hasScopedWeaponIntervals;
        private float mobilityModifier = 1f;

        public float EffectiveDamage => (float)System.Math.Min(float.MaxValue,(double)shotDamage*((1d-basicBlastFraction)*basicDamageModifier+basicBlastFraction*basicBlastDamageModifier));
        public float EffectiveFireInterval => RoboRougelike.R3.R3MountedBonusEffects.SafeIntervalProduct(fireInterval,intervalModifier);
        public float EffectiveForwardSpeed => (float)System.Math.Min(float.MaxValue,(double)forwardSpeed * mobilityModifier);
        public float EffectiveReverseSpeed => (float)System.Math.Min(float.MaxValue,(double)reverseSpeed * mobilityModifier);
        public float EffectiveTurnSpeed => (float)System.Math.Min(float.MaxValue,(double)turnSpeed*mobilityModifier);
        public bool GameplayInputEnabled => gameplayInputEnabled;

        public void SetRunModifiers(float damage, float interval, float mobility)
        {
            damageModifier = float.IsNaN(damage)||float.IsInfinity(damage)?1f:Mathf.Max(0,damage);
            basicDamageModifier=damageModifier;
            basicBlastDamageModifier=damageModifier;basicBlastFraction=0;
            intervalModifier = float.IsNaN(interval)||float.IsInfinity(interval)?1f:Mathf.Max(0,interval);
            hasScopedWeaponIntervals=false;
            mobilityModifier = float.IsNaN(mobility)||float.IsInfinity(mobility)?1f:Mathf.Max(0,mobility);
        }
        public void SetBasicDamageModifier(float damage)
        {SetBasicDamageProfile(damage,damage,0);}
        public void SetBasicDamageProfile(float kinetic,float blast,float blastFraction)
        {
            basicDamageModifier=float.IsNaN(kinetic)||float.IsInfinity(kinetic)?damageModifier:Mathf.Max(0,kinetic);
            basicBlastDamageModifier=float.IsNaN(blast)||float.IsInfinity(blast)?damageModifier:Mathf.Max(0,blast);
            basicBlastFraction=float.IsNaN(blastFraction)||float.IsInfinity(blastFraction)?0:Mathf.Clamp01(blastFraction);
        }

        public void SetGameplayInputEnabled(bool active)
        {
            gameplayInputEnabled = active;
            if (active) return;
            AimFeedback=default;
            weaponReticles.Clear();
            moveInput = turnInput = currentSpeed = 0;
            fireHeld = secondaryFireHeld = thirdFireHeld = fourthFireHeld = false;
            waitForPrimaryRelease = waitForSecondaryRelease = waitForThirdRelease = waitForFourthRelease = true;
            if(modularAssembly!=null)modularAssembly.SetSlotTriggers(false,false,false,false);
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
            fireHeld = secondaryFireHeld = thirdFireHeld = fourthFireHeld = false;
            if (!gameplayInputEnabled) return;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            var controls=RoboRougelike.R1.R1ControlProfile.Current;

            {
                if (controls.Pressed("MoveForward") ||
                    (keyboard!=null&&keyboard.upArrowKey.isPressed))
                    moveInput += 1f;

                if (controls.Pressed("MoveBackward") ||
                    (keyboard!=null&&keyboard.downArrowKey.isPressed))
                    moveInput -= 1f;

                if (controls.Pressed("TurnRight") ||
                    (keyboard!=null&&keyboard.rightArrowKey.isPressed))
                    turnInput += 1f;

                if (controls.Pressed("TurnLeft") ||
                    (keyboard!=null&&keyboard.leftArrowKey.isPressed))
                    turnInput -= 1f;
            }

            bool primaryPressed=controls.Pressed("Weapon1");
            bool secondaryPressed=controls.Pressed("Weapon2");
            bool thirdPressed=controls.Pressed("Weapon3");
            bool fourthPressed=controls.Pressed("Weapon4");
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

            bool primaryPressed=Input.GetMouseButton(0);
            bool secondaryPressed=Input.GetMouseButton(1);
            bool thirdPressed=Input.GetKey(KeyCode.Q);
            bool fourthPressed=Input.GetKey(KeyCode.E);
#endif
            if(!primaryPressed)waitForPrimaryRelease=false;
            if(!secondaryPressed)waitForSecondaryRelease=false;
            if(!thirdPressed)waitForThirdRelease=false;
            if(!fourthPressed)waitForFourthRelease=false;
            fireHeld=primaryPressed&&!waitForPrimaryRelease;
            secondaryFireHeld=secondaryPressed&&!waitForSecondaryRelease;
            thirdFireHeld=thirdPressed&&!waitForThirdRelease;
            fourthFireHeld=fourthPressed&&!waitForFourthRelease;
        }

        private RoboRougelike.R6.R6VehicleDrive drive;
        private void FixedUpdate()
        {
            if (rb.isKinematic) return;
            if(drive==null)drive=GetComponent<RoboRougelike.R6.R6VehicleDrive>();
            RefreshCollisionEnvelope(false);
            drive.Step(gameplayInputEnabled?moveInput:0,gameplayInputEnabled?turnInput:0,EffectiveForwardSpeed,EffectiveReverseSpeed,EffectiveTurnSpeed);
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
            { AimFeedback=default;weaponReticles.Clear();if(modularAssembly!=null)modularAssembly.SetGroupTriggers(false,false);return; }

            EnsureFunctionalModuleSystem();

            if (moduleSystem != null &&
                !moduleSystem.WeaponOnline)
            {
                AimFeedback=default;
                weaponReticles.Clear();
                if(modularAssembly!=null)modularAssembly.SetGroupTriggers(false,false);
                return;
            }

            if (aimCamera == null)
                aimCamera = Camera.main;

            if (aimCamera == null)
            { AimFeedback=default;weaponReticles.Clear();if(modularAssembly!=null)modularAssembly.SetGroupTriggers(false,false);return; }

            Vector3 aimPoint =
                FindAimPoint(true);

            RotateTurretToward(
                aimPoint
            );

            PitchCannonToward(
                aimPoint
            );
            if (modularAssembly != null) modularAssembly.AimGuns(aimPoint);
            AimFeedback = RoboRougelike.WeaponIntegration.RRLAimFeedback.Evaluate(
                modularAssembly,this,aimPoint,LastConsumedAimCollider);
            RoboRougelike.WeaponIntegration.RRLWeaponReticles.Collect(aimCamera,modularAssembly,this,aimPoint,weaponReticles);

            var run=RoboRougelike.R1.R1RunController.Instance;
            if(run!=null&&run.Player!=null&&run.Player.Controller==this&&run.EmergencyRescueAttackBlocked)
            {if(modularAssembly!=null)modularAssembly.SetSlotTriggers(false,false,false,false);return;}

            if(modularAssembly!=null)
            {
                modularAssembly.SetSlotTriggers(fireHeld,secondaryFireHeld,thirdFireHeld,fourthFireHeld);
                if(fireHeld)modularAssembly.FireGroup(false);
                if(secondaryFireHeld)modularAssembly.FireGroup(true);
                if(thirdFireHeld)modularAssembly.FireSlot(2);
                if(fourthFireHeld)modularAssembly.FireSlot(3);
            }
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
        public Collider ModuleAimCollider()
        {
            if(aimCamera==null)aimCamera=Camera.main;
            if(aimCamera==null)return null;
            FindAimPoint(true);return LastConsumedAimCollider;
        }
        public Vector3 LastConsumedAimPoint {get;private set;}
        public Collider LastConsumedAimCollider {get;private set;}
        public int LastConsumedAimFrame {get;private set;}=-1;
        public RoboRougelike.WeaponIntegration.RRLAimFeedback.Snapshot AimFeedback {get;private set;}
        readonly List<RoboRougelike.WeaponIntegration.RRLWeaponReticles.Marker> weaponReticles = new List<RoboRougelike.WeaponIntegration.RRLWeaponReticles.Marker>(4);
        public IReadOnlyList<RoboRougelike.WeaponIntegration.RRLWeaponReticles.Marker> WeaponReticles=>weaponReticles;
        public float PhysicalShellLifetime => projectileLifetime;
        private RaycastHit[] aimHits = new RaycastHit[32];
        private static readonly IComparer<RaycastHit> AimHitDistanceComparer =
            Comparer<RaycastHit>.Create((a,b)=>a.distance.CompareTo(b.distance));
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

            RaycastHit[] hits = aimHits;
            int count;
            while(true)
            {
                count=Physics.RaycastNonAlloc(ray.origin,ray.direction,hits,aimRange,~0,QueryTriggerInteraction.Ignore);
                if(count<hits.Length)break;
                if(hits.Length>=1024)
                {
                    // A full buffer may have omitted the closest target. Keep correctness
                    // even in unusually dense scenes instead of imposing a hit limit.
                    hits=Physics.RaycastAll(ray.origin,ray.direction,aimRange,~0,QueryTriggerInteraction.Ignore);
                    count=hits.Length;
                    break;
                }
                hits=aimHits=new RaycastHit[hits.Length*2];
            }
            Array.Sort(hits,0,count,AimHitDistanceComparer);

            for(int i=0;i<count;i++)
            {
                RaycastHit hit=hits[i];
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
        {
            float kinetic=SafeShellDamage(shotDamage,1-basicBlastFraction,basicDamageModifier);
            float blast=SafeShellDamage(shotDamage,basicBlastFraction,basicBlastDamageModifier);
            SpawnPhysicalShell(barrel,kinetic,projectileSpeed,projectileDiameter,projectileMass,
                RoboRougelike.R4.R4WeaponKind.Basic,null,blast);
        }

        static float SafeShellDamage(float damage,float fraction,float modifier)=>
            (float)System.Math.Min(float.MaxValue,(double)damage*fraction*modifier);

        public void FireFrom(Transform barrel,RoboRougelike.R4.R4WeaponKind kind,string partId)
        { FireFrom(barrel,kind,partId,0); }
        public void FireFrom(Transform barrel,RoboRougelike.R4.R4WeaponKind kind,string partId,int extraShots)
        { FireFromInternal(barrel,kind,partId,extraShots,null); }
        public Collider FirePrimaryFrom(Transform barrel,RoboRougelike.R4.R4WeaponKind kind,string partId)
        { return FireFromInternal(barrel,kind,partId,0,null); }
        public void FireAdditionalFrom(Transform barrel,RoboRougelike.R4.R4WeaponKind kind,string partId,Collider primary,int count)
        {
            if(primary==null||count<=0)return;
            var shell=primary.GetComponent<P17PhysicalShell>();if(shell==null||!shell.IsActiveProjectile)return;
            var queue=GetComponent<RoboRougelike.R3.R3ConventionalProjectileQueue>();
            if(queue==null)queue=gameObject.AddComponent<RoboRougelike.R3.R3ConventionalProjectileQueue>();
            queue.Enqueue(shell,count);
        }
        private Collider FireFromInternal(Transform barrel,RoboRougelike.R4.R4WeaponKind kind,string partId,int extraShots,Collider existingSibling)
        {
            var profile=RoboRougelike.R4.R4WeaponDefinition.Get(kind);
            var mounted=barrel!=null?barrel.GetComponentInParent<RoboRougelike.R3.R3MountedPart>():null;
            float tier=mounted!=null&&mounted.Kind==RoboRougelike.R3.R3PartKind.Gun&&mounted.Id==partId?
                RoboRougelike.R3.R3VehicleModel.TierEffectUnits(mounted.Tier):1f;
            float weaponDamage=kind==RoboRougelike.R4.R4WeaponKind.Basic?basicDamageModifier:
                kind==RoboRougelike.R4.R4WeaponKind.Automatic?(GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.AutomaticDamageMultiplier??damageModifier):
                kind==RoboRougelike.R4.R4WeaponKind.Heavy?(GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.HeavyDamageMultiplier??damageModifier):PackageKineticDamageScale;
            float fraction=kind==RoboRougelike.R4.R4WeaponKind.Basic?basicBlastFraction:0;
            float kinetic=SafeShellDamage(profile.Damage*tier,1-fraction,weaponDamage);
            float blast=SafeShellDamage(profile.Damage*tier,fraction,basicBlastDamageModifier);
            var first=SpawnPhysicalShell(barrel,kinetic,profile.Speed,profile.Diameter,profile.ProjectileMass,kind,partId,blast);
            if(first==null)return null;
            if(existingSibling!=null)P17PhysicalShell.IgnoreProjectilePair(first,existingSibling);
            if(extraShots>0)FireAdditionalFrom(barrel,kind,partId,first,extraShots);
            return first;
        }
        public float WeaponInterval(RoboRougelike.R4.R4WeaponKind kind) => RoboRougelike.R3.R3MountedBonusEffects.SafeIntervalProduct(RoboRougelike.R4.R4WeaponDefinition.Get(kind).Interval,WeaponIntervalScale(kind));
        internal void SetScopedWeaponIntervals(float[] scales)
        {
            if(scales==null||scales.Length!=scopedWeaponIntervals.Length){hasScopedWeaponIntervals=false;return;}
            System.Array.Copy(scales,scopedWeaponIntervals,scales.Length);hasScopedWeaponIntervals=true;
        }
        public float WeaponIntervalScale(RoboRougelike.R4.R4WeaponKind kind)
        {
            int index=(int)kind;
            return hasScopedWeaponIntervals&&index>=0&&index<scopedWeaponIntervals.Length?scopedWeaponIntervals[index]:intervalModifier;
        }
        public float PackageDamageScale => GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.WeaponDamageMultiplier??damageModifier;
        public float PackageBlastDamageScale => GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.BlastDamageMultiplier??damageModifier;
        public float PackagePiercingDamageScale => GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.PiercingDamageMultiplier??damageModifier;
        public float PackageKineticDamageScale => GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.KineticDamageMultiplier??damageModifier;
        public float PackageShotgunDamageScale => GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.ShotgunDamageMultiplier??damageModifier;
        public float PackageRailgunDamageScale => GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.RailgunDamageMultiplier??damageModifier;
        public float PackageRocketDamageScale => GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.RocketDamageMultiplier??damageModifier;
        public float PackageBeamDamageScale => GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.BeamDamageMultiplier??damageModifier;
        public float PackageSawDamageScale => GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.SawDamageMultiplier??damageModifier;
        public float PackageAuraDamageScale => GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.AuraDamageMultiplier??damageModifier;
        public float PackageEnergyDamageScale => GetComponent<RoboRougelike.R3.R3MountedBonusEffects>()?.EnergyDamageMultiplier??damageModifier;
        public float PackageIntervalScale => intervalModifier;
        private Collider SpawnPhysicalShell(Transform barrel,float hitDamage,float projectileSpeed,float projectileDiameter,
            float projectileMass,RoboRougelike.R4.R4WeaponKind kind,string partId,float blastDamage=0)
        {
            if (barrel == null)
                return null;

            Vector3 direction =
                barrel.forward.normalized;

            Vector3 origin =
                barrel.position +
                direction *
                Mathf.Max(
                    0.16f,
                    projectileDiameter * 0.75f
                );

            P17PhysicalShell projectile=P17PhysicalShell.RentConventional();
            GameObject shell=projectile.gameObject;

            RoboRougelike.R6.R6CombatFeedback.Muzzle(barrel,kind);

            shell.name =
                "P17_PlayerPhysicalShell";
            var originTag=shell.GetComponent<RoboRougelike.R4.R4ProjectileOrigin>();originTag.Weapon=kind;originTag.PartId=partId;

            shell.transform.position =
                origin;
            shell.transform.rotation=Quaternion.identity;

            shell.transform.localScale =
                Vector3.one *
                projectileDiameter;

            Renderer shellRenderer =
                shell.GetComponent<Renderer>();

            if(shellRenderer!=null)shellRenderer.sharedMaterial=projectileMaterial!=null?projectileMaterial:projectile.DefaultMaterial;

            SphereCollider sphere =
                shell.GetComponent<SphereCollider>();

            if (sphere != null)
            {
                sphere.enabled = true;
                sphere.isTrigger = false;
            }

            Rigidbody projectileBody = shell.GetComponent<Rigidbody>();

            projectileBody.mass =
                projectileMass;

            projectileBody.useGravity =
                false;

            projectileBody.isKinematic = false;

            projectileBody.interpolation =
                RigidbodyInterpolation.Interpolate;

            projectileBody.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;

            shell.SetActive(true);
            projectileBody.angularVelocity=Vector3.zero;

#if UNITY_6000_0_OR_NEWER
            projectileBody.linearVelocity =
                direction *
                projectileSpeed;
#else
            projectileBody.velocity =
                direction *
                projectileSpeed;
#endif

            if(blastDamage>0)projectile.ConfigureHybrid(transform,hitDamage,blastDamage,projectileLifetime,direction,null);
            else projectile.Configure(transform,hitDamage,projectileLifetime,direction,null);

            return sphere;
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
        private struct CachedConventionalShell
        {
            public P17PhysicalShell Shell;
            public int ReleasedFrame;
        }
        // This is a retention limit for inactive objects, not a projectile
        // limit: excess inactive objects are destroyed and future shots can
        // always allocate a new one.
        private const int MaxCachedConventionalShells=256;
        private static readonly System.Collections.Generic.Queue<CachedConventionalShell> conventionalCache =
            new System.Collections.Generic.Queue<CachedConventionalShell>();
        private bool cacheableConventional;
        private double expiresAt;
        private static ulong nextLaunchSequence;
        public ulong LaunchSequence {get;private set;}
        private System.Collections.Generic.List<Collider> ignoredForFlight;
        internal Material DefaultMaterial {get;private set;}
        // A few reserved layers avoid quadratic pair filters for unusually
        // large single-gun volleys. Exhaustion falls back to ordinary pairs;
        // these leases never limit how many shots can be created.
        private sealed class VolleyLayerLease
        {
            internal readonly int Slot,Layer;
            internal int Members;
            internal VolleyLayerLease(int slot,int layer){Slot=slot;Layer=layer;}
        }
        private static readonly VolleyLayerLease[] volleyLayers=new VolleyLayerLease[8];
        private VolleyLayerLease volleyLayer;
        internal bool HasVolleyLayer=>volleyLayer!=null;
        private int originalVolleyLayer,originalVolleyExclusions;
        internal static bool TryGroupVolley(System.Collections.Generic.IReadOnlyList<GameObject> shots)
            =>TryGroupVolleyInternal(shots,false);
        private static bool TryGroupVolleyInternal(System.Collections.Generic.IReadOnlyList<GameObject> shots,bool allowUnpooled)
        {
            if(shots==null||shots.Count<2)return false;
            var unique=new System.Collections.Generic.HashSet<P17PhysicalShell>();
            foreach(var shot in shots)
            {
                var shell=shot!=null?shot.GetComponent<P17PhysicalShell>():null;
                var col=shot!=null?shot.GetComponent<Collider>():null;
                if(shell==null||!unique.Add(shell)||(!allowUnpooled&&!shell.cacheableConventional)||shell.volleyLayer!=null||!shell.isActiveAndEnabled||shot.layer!=0||col==null||!col.enabled)return false;
            }
            Collider[] sceneColliders=null;
            for(int slot=0;slot<volleyLayers.Length;slot++)
            {
                int layer=24+slot;
                if(volleyLayers[slot]!=null||LayerMask.NameToLayer("RRSalvo"+slot)!=layer)continue;
                // Preserve every global collision-matrix decision the normal
                // projectile layer would have made against other objects.
                bool compatible=true;
                for(int other=0;other<32;other++)
                    if(Physics.GetIgnoreLayerCollision(layer,other)!=Physics.GetIgnoreLayerCollision(0,other)){compatible=false;break;}
                if(compatible)
                {
                    if(sceneColliders==null)sceneColliders=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                    foreach(var existing in sceneColliders)
                    {
                        if(existing==null)continue;
                        // Do not claim a layer in use, or change how an
                        // existing collider's custom filters see the shot.
                        if(existing.gameObject.layer==layer||DistinguishesLayer(existing.includeLayers.value,layer)||DistinguishesLayer(existing.excludeLayers.value,layer))
                        {compatible=false;break;}
                        var body=existing.attachedRigidbody;
                        if(body!=null&&(DistinguishesLayer(body.includeLayers.value,layer)||DistinguishesLayer(body.excludeLayers.value,layer)))
                        {compatible=false;break;}
                    }
                }
                if(!compatible)continue;
                var lease=new VolleyLayerLease(slot,layer);volleyLayers[slot]=lease;
                foreach(var shot in shots)shot.GetComponent<P17PhysicalShell>().JoinVolleyLayer(lease);
                return true;
            }
            return false;
        }
        internal static bool TryGroupVolleyColliders(System.Collections.Generic.IReadOnlyList<Collider> colliders)
        {
            if(colliders==null)return false;
            var shots=new GameObject[colliders.Count];
            for(int i=0;i<colliders.Count;i++)
            {if(colliders[i]==null)return false;shots[i]=colliders[i].gameObject;}
            return TryGroupVolley(shots);
        }
        internal static bool TryGroupDeviceVolleyColliders(System.Collections.Generic.IReadOnlyList<Collider> colliders)
        {
            if(colliders==null)return false;
            var shots=new GameObject[colliders.Count];
            for(int i=0;i<colliders.Count;i++)
            {if(colliders[i]==null)return false;shots[i]=colliders[i].gameObject;}
            return TryGroupVolleyInternal(shots,true);
        }
        private static bool DistinguishesLayer(int mask,int layer)=>((mask&1)!=0)!=((mask&(1<<layer))!=0);
        internal static bool TryJoinVolley(Collider sibling,Collider added)
            =>TryJoinVolleyInternal(sibling,added,false);
        internal static bool TryJoinDeviceVolley(Collider sibling,Collider added)
            =>TryJoinVolleyInternal(sibling,added,true);
        private static bool TryJoinVolleyInternal(Collider sibling,Collider added,bool allowUnpooled)
        {
            if(sibling==null||added==null)return false;
            var first=sibling.GetComponent<P17PhysicalShell>();var next=added.GetComponent<P17PhysicalShell>();
            if(first==null||first.volleyLayer==null||next==null||(!allowUnpooled&&!next.cacheableConventional)||next.volleyLayer!=null||!next.isActiveAndEnabled||next.gameObject.layer!=0)return false;
            next.JoinVolleyLayer(first.volleyLayer);return true;
        }
        private void JoinVolleyLayer(VolleyLayerLease lease)
        {
            var col=GetComponent<Collider>();originalVolleyLayer=gameObject.layer;originalVolleyExclusions=col.excludeLayers.value;
            volleyLayer=lease;lease.Members++;col.excludeLayers=originalVolleyExclusions|(1<<lease.Layer);gameObject.layer=lease.Layer;
        }
        private void ReleaseVolleyLayer()
        {
            var lease=volleyLayer;if(lease==null)return;volleyLayer=null;
            var col=GetComponent<Collider>();if(col!=null)col.excludeLayers=originalVolleyExclusions;
            gameObject.layer=originalVolleyLayer;
            lease.Members--;if(lease.Members==0&&ReferenceEquals(volleyLayers[lease.Slot],lease))volleyLayers[lease.Slot]=null;
        }
        internal static void IgnoreProjectilePair(Collider first,Collider second)
        {
            if(first==null||second==null)return;
            Physics.IgnoreCollision(first,second,true);
            var a=first.GetComponent<P17PhysicalShell>();var b=second.GetComponent<P17PhysicalShell>();
            if(a!=null&&a.cacheableConventional)a.ignoredForFlight.Add(second);
            if(b!=null&&b.cacheableConventional)b.ignoredForFlight.Add(first);
        }
        // A pair may be excluded by collider/body layer overrides without an
        // explicit IgnoreCollision pair. Mutual exclusion is unambiguous even
        // when either collider uses a different override priority.
        private static bool MutuallyExcludedLayers(Collider first,Collider second)
        {
            if(first==null||second==null)return false;
            int firstMask=first.excludeLayers.value,secondMask=second.excludeLayers.value;
            var firstBody=first.attachedRigidbody;var secondBody=second.attachedRigidbody;
            if(firstBody!=null)firstMask|=firstBody.excludeLayers.value;
            if(secondBody!=null)secondMask|=secondBody.excludeLayers.value;
            return (firstMask&(1<<second.gameObject.layer))!=0&&(secondMask&(1<<first.gameObject.layer))!=0;
        }
        internal static P17PhysicalShell RentConventional()
        {
            while(conventionalCache.Count>0)
            {
                var cached=conventionalCache.Peek();
                if(cached.Shell==null){conventionalCache.Dequeue();continue;}
                // A collision callback can still hold the old reference this
                // frame. Only rent it again after that frame has finished.
                if(cached.ReleasedFrame>=Time.frameCount)break;
                conventionalCache.Dequeue();
                var shell=cached.Shell;shell.spent=false;shell.ownerRoot=null;shell.ownerWasAssigned=false;
                shell.targetRoot=null;shell.damage=0;shell.hybridDamage=false;shell.kineticComponent=0;shell.blastComponent=0;
                return shell;
            }
            var item=GameObject.CreatePrimitive(PrimitiveType.Sphere);item.SetActive(false);
            var defaultMaterial=item.GetComponent<Renderer>().sharedMaterial;
            item.AddComponent<Rigidbody>();item.AddComponent<RoboRougelike.R4.R4ProjectileOrigin>();
            var result=item.AddComponent<P17PhysicalShell>();result.cacheableConventional=true;result.DefaultMaterial=defaultMaterial;
            result.ignoredForFlight=new System.Collections.Generic.List<Collider>();
            return result;
        }
        internal static void ClearConventionalCache()
        {
            while(conventionalCache.Count>0)
            {
                var item=conventionalCache.Dequeue().Shell;
                if(item!=null)Destroy(item.gameObject);
            }
        }
        internal void Retire()
        {
            // Mark spent before resetting physics pairs; trigger callbacks
            // caused by a filter change must not process this shot again.
            spent=true;
            if(railgun!=null){railgun.enabled=false;railgun=null;}
            bool reusable=cacheableConventional;
            if(reusable)
            {
                var own=GetComponent<Collider>();
                if(own==null||!own.enabled||!own.gameObject.activeInHierarchy)reusable=false;
                else foreach(var other in ignoredForFlight)
                {
                    if(other==null||!Physics.GetIgnoreCollision(own,other))continue;
                    // Unity can retain ignore pairs across deactivation. If a
                    // counterpart is inactive we cannot safely reset it.
                    if(!other.enabled||!other.gameObject.activeInHierarchy){reusable=false;continue;}
                    Physics.IgnoreCollision(own,other,false);
                }
                ignoredForFlight.Clear();
            }
            gameObject.SetActive(false);
            ReleaseVolleyLayer();
            if(reusable&&conventionalCache.Count<MaxCachedConventionalShells)
                conventionalCache.Enqueue(new CachedConventionalShell{Shell=this,ReleasedFrame=Time.frameCount});
            else Destroy(gameObject);
        }
        // Unity physics and component traversal run on the main thread. Keep
        // one reusable scratch list, but isolate a nested setup if a physics
        // callback ever causes another projectile to be configured here.
        private static readonly System.Collections.Generic.List<Collider> projectileColliderBuffer =
            new System.Collections.Generic.List<Collider>();
        private static bool projectileColliderBufferInUse;
        public static int ActiveCount => activeShells.Count;
        private void OnEnable()
        {
            activeShells.Add(this);
            if(GetComponent<RoboRougelike.WeaponIntegration.RRLPointDefenseThreat>()==null)gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLPointDefenseThreat>();
        }
        private void OnDisable(){activeShells.Remove(this);}
        private void OnDestroy(){ReleaseVolleyLayer();}
        private Transform ownerRoot;
        private bool ownerWasAssigned;
        public Transform OwnerRoot=>ownerRoot;
        public float DamageAmount=>damage;
        public Vector3 LaunchPosition=>launchPosition;
        public bool IsActiveProjectile=>isActiveAndEnabled&&!spent&&ownerRoot!=null&&ownerRoot.gameObject.activeInHierarchy;
        public Vector3 FlightVelocity=>railgun!=null?railgun.Velocity:GetComponent<Rigidbody>().linearVelocity;
        internal struct ConventionalSnapshot
        {
            public Transform Owner,Target;
            public Vector3 Position,Direction,Velocity,Scale;
            public Quaternion Rotation;
            public float Mass,Lifetime,Range,Damage,Kinetic,Blast;
            public bool Hybrid;
            public Material Material;
            public RoboRougelike.R4.R4WeaponKind Weapon;
            public string PartId;
        }
        internal ConventionalSnapshot CaptureConventional()
        {
            if(!IsActiveProjectile||railgun!=null)throw new System.InvalidOperationException("A live conventional projectile is required.");
            var body=GetComponent<Rigidbody>();var renderer=GetComponent<Renderer>();
            var origin=GetComponent<RoboRougelike.R4.R4ProjectileOrigin>();
            if(body==null||origin==null)throw new System.InvalidOperationException("A conventional projectile is missing its body or identity.");
            double remaining=cacheableConventional?System.Math.Max(.5d,expiresAt-Time.timeAsDouble):.5d;
            return new ConventionalSnapshot{Owner=ownerRoot,Target=targetRoot,Position=launchPosition,Direction=launchDirection,Rotation=transform.rotation,
                Velocity=body.linearVelocity,Scale=transform.localScale,Mass=body.mass,Lifetime=(float)System.Math.Min(float.MaxValue,remaining),
                Range=maxTravelDistance,Damage=damage,Kinetic=kineticComponent,Blast=blastComponent,Hybrid=hybridDamage,
                Material=renderer!=null?renderer.sharedMaterial:null,Weapon=origin.Weapon,PartId=origin.PartId};
        }
        public bool Intercept(float amount)
        {if(!IsActiveProjectile||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return false;Retire();return true;}
        private static Mesh shellVisualMesh;
        private Transform shellVisual;
        private Rigidbody shellBody;
        private void CreateShellVisual()
        {
            var original=GetComponent<Renderer>();
            if(original==null)return;
            if(original.sharedMaterial==null)
            {if(shellVisual!=null)shellVisual.gameObject.SetActive(false);original.enabled=true;return;}
            if(shellVisual!=null)
            {
                shellVisual.gameObject.SetActive(true);shellVisual.GetComponent<Renderer>().sharedMaterial=original.sharedMaterial;
                shellBody=GetComponent<Rigidbody>();original.enabled=false;OrientShellVisual(launchDirection);return;
            }
            if(shellVisualMesh==null)
            {
                var template=GameObject.CreatePrimitive(PrimitiveType.Capsule);template.SetActive(false);
                shellVisualMesh=template.GetComponent<MeshFilter>().sharedMesh;Destroy(template);
            }
            if(shellVisualMesh==null)return;
            var visual=new GameObject("P17 shell visual");visual.transform.SetParent(transform,false);
            visual.transform.localScale=new Vector3(.72f,1.6f,.72f);
            visual.AddComponent<MeshFilter>().sharedMesh=shellVisualMesh;
            var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterial=original.sharedMaterial;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            shellVisual=visual.transform;shellBody=GetComponent<Rigidbody>();original.enabled=false;
            OrientShellVisual(launchDirection);
        }
        private void OrientShellVisual(Vector3 direction)
        {
            if(shellVisual==null||direction.sqrMagnitude<.000001f)return;
            shellVisual.rotation=Quaternion.LookRotation(direction)*Quaternion.Euler(90,0,0);
        }
        private void LateUpdate()
        {
            if(shellVisual==null)return;
            var direction=railgun!=null?railgun.Velocity:shellBody!=null?shellBody.linearVelocity:launchDirection;
            OrientShellVisual(direction);
        }
        private Transform targetRoot;
        private float damage;
        private Vector3 launchDirection;
        private bool spent;
        private bool hybridDamage;
        private float kineticComponent,blastComponent;
        private Vector3 launchPosition;
        private float maxTravelDistance=float.PositiveInfinity;
        // Shield path checks run for every approaching projectile. Reuse the
        // common buffers while retaining a complete fallback for dense scenes.
        private const int DomeQueryBufferLimit=256;
        private static Collider[] domeOverlapBuffer=new Collider[16];
        private static RaycastHit[] domeSweepBuffer=new RaycastHit[16];
        private static bool domeQueryInUse;
        private RoboRougelike.WeaponIntegration.RRLRailPenetration railgun;
        public void ConfigureRailgun(RoboRougelike.WPN002Railgun.RailgunShot shot,float damageScale)
        {
            railgun=GetComponent<RoboRougelike.WeaponIntegration.RRLRailPenetration>();
            if(railgun==null)railgun=gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLRailPenetration>();
            railgun.enabled=true;railgun.Bind(shot,damageScale);
        }
        private void FixedUpdate()
        {
            if(RetireLostOwner())return;
            if(railgun!=null){if(isActiveAndEnabled)railgun.Step(Time.fixedDeltaTime);return;}
            if(!IsActiveProjectile||damage<=0||float.IsNaN(damage)||float.IsInfinity(damage))return;
            var body=GetComponent<Rigidbody>();var own=GetComponent<Collider>();
            if(body==null||own==null)return;
            var step=body.linearVelocity*Time.fixedDeltaTime;float distance=step.magnitude;
            if(distance<=.00001f||float.IsNaN(distance)||float.IsInfinity(distance))return;
            if(!float.IsInfinity(maxTravelDistance))distance=Mathf.Min(distance,Mathf.Max(0,maxTravelDistance-(body.position-launchPosition).magnitude));
            if(distance<=.00001f)return;
            Vector3 direction=step.normalized,start=body.position,end=start+direction*distance;
            if(!RoboRougelike.R3.R3ShieldDome.TryFirstHit(start,end,ownerRoot,out var dome,out float shieldDistance))return;
            bool Blocks(Collider collider)
            {
                if(collider==null||collider==own||collider.isTrigger||collider.transform.IsChildOf(transform)||
                   collider.transform==ownerRoot||collider.transform.IsChildOf(ownerRoot)||Physics.GetIgnoreCollision(own,collider)||
                   MutuallyExcludedLayers(own,collider))return false;
                var hp=collider.GetComponentInParent<P12PipelineSafeDamageable>();
                return collider.GetComponentInParent<RoboRougelike.R3.R3Debris>()==null&&(hp==null||!hp.IsDead)&&
                    RoboRougelike.WeaponIntegration.RRLHitRouter.CanHit(collider,ownerRoot);
            }
            var sphere=own as SphereCollider;
            float radius=sphere!=null?sphere.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y),Mathf.Abs(transform.lossyScale.z)):.01f;
            radius=Mathf.Max(.001f,radius);
            bool shared=!domeQueryInUse;
            var overlaps=shared?domeOverlapBuffer:new Collider[16];
            var hits=shared?domeSweepBuffer:new RaycastHit[16];
            if(shared)domeQueryInUse=true;
            try
            {
                int count;
                while(true)
                {
                    count=Physics.OverlapSphereNonAlloc(start,radius,overlaps,~0,QueryTriggerInteraction.Ignore);
                    if(count<overlaps.Length)break;
                    if(overlaps.Length>=DomeQueryBufferLimit)
                    {overlaps=Physics.OverlapSphere(start,radius,~0,QueryTriggerInteraction.Ignore);count=overlaps.Length;break;}
                    System.Array.Resize(ref overlaps,Mathf.Min(DomeQueryBufferLimit,overlaps.Length*2));
                    if(shared)domeOverlapBuffer=overlaps;
                }
                for(int i=0;i<count;i++)if(Blocks(overlaps[i]))return;
                while(true)
                {
                    count=Physics.SphereCastNonAlloc(start,radius,direction,hits,shieldDistance,~0,QueryTriggerInteraction.Ignore);
                    if(count<hits.Length)break;
                    if(hits.Length>=DomeQueryBufferLimit)
                    {hits=Physics.SphereCastAll(start,radius,direction,shieldDistance,~0,QueryTriggerInteraction.Ignore);count=hits.Length;break;}
                    System.Array.Resize(ref hits,Mathf.Min(DomeQueryBufferLimit,hits.Length*2));
                    if(shared)domeSweepBuffer=hits;
                }
                for(int i=0;i<count;i++)if(Blocks(hits[i].collider))return;
            }
            finally
            {
                if(shared)
                {
                    System.Array.Clear(domeOverlapBuffer,0,domeOverlapBuffer.Length);
                    System.Array.Clear(domeSweepBuffer,0,domeSweepBuffer.Length);
                    domeQueryInUse=false;
                }
            }
            float amount=damage;Vector3 point=start+direction*shieldDistance;
            if(Intercept(1f))dome.Absorb(amount,point,direction);
        }


        public void Configure(
            Transform owner,
            float damageAmount,
            float lifetime,
            Vector3 initialDirection,
            Transform targetToIgnoreNonDamageable,
            float maxDistance=float.PositiveInfinity)
        {
            if(!isActiveAndEnabled)return;
            unchecked{nextLaunchSequence++;if(nextLaunchSequence==0)nextLaunchSequence++;}
            LaunchSequence=nextLaunchSequence;
            hybridDamage=false;kineticComponent=blastComponent=0;
            launchPosition=transform.position;
            maxTravelDistance=float.IsNaN(maxDistance)?0:Mathf.Max(0,maxDistance);
            ownerRoot = owner;
            ownerWasAssigned = owner != null;
            targetRoot =
                targetToIgnoreNonDamageable;

            damage =
                damageAmount;

            launchDirection =
                initialDirection.normalized;
            CreateShellVisual();

            if(cacheableConventional)expiresAt=Time.timeAsDouble+Mathf.Max(.5f,lifetime);
            else Destroy(gameObject,Mathf.Max(.5f,lifetime));

            IgnoreOwnerCollisions();

            if (targetRoot != null)
                IgnoreTargetHelpers();
        }

        public void ConfigureHybrid(Transform owner,float kinetic,float blast,float lifetime,Vector3 direction,Transform targetToIgnoreNonDamageable,float maxDistance=float.PositiveInfinity)
        {
            if(float.IsNaN(kinetic)||float.IsInfinity(kinetic)||kinetic<0||float.IsNaN(blast)||float.IsInfinity(blast)||blast<0)return;
            double total=(double)kinetic+blast,scale=total>float.MaxValue?float.MaxValue/total:1d;
            Configure(owner,(float)(total*scale),lifetime,direction,targetToIgnoreNonDamageable,maxDistance);
            if(!isActiveAndEnabled)return;
            kineticComponent=(float)(kinetic*scale);blastComponent=(float)(blast*scale);hybridDamage=true;
        }

        private void IgnoreOwnerCollisions()
        {
            Collider ownCollider =
                GetComponent<Collider>();

            if (ownCollider == null ||
                ownerRoot == null)
                return;

            bool shared=!projectileColliderBufferInUse;
            var ownerColliders=shared?projectileColliderBuffer:new System.Collections.Generic.List<Collider>();
            if(shared)projectileColliderBufferInUse=true;
            try
            {
                ownerRoot.GetComponentsInChildren(true,ownerColliders);
                foreach (Collider ownerCollider in ownerColliders)
                {
                    if (ownerCollider == null || ownerCollider == ownCollider)continue;
                    Physics.IgnoreCollision(ownCollider,ownerCollider,true);
                    if(cacheableConventional)ignoredForFlight.Add(ownerCollider);
                }
            }
            finally { ownerColliders.Clear(); if(shared)projectileColliderBufferInUse=false; }
        }

        private void Update()
        {
            if(RetireLostOwner())return;
            if(cacheableConventional&&!spent&&Time.timeAsDouble>=expiresAt){Retire();return;}
            if(!spent&&(transform.position-launchPosition).sqrMagnitude>maxTravelDistance*maxTravelDistance)
            {Retire();}
        }

        private bool RetireLostOwner()
        {
            if(!ownerWasAssigned||(ownerRoot!=null&&ownerRoot.gameObject.activeInHierarchy))return false;
            Retire();return true;
        }

        private void IgnoreTargetHelpers()
        {
            Collider ownCollider =
                GetComponent<Collider>();

            if (ownCollider == null ||
                targetRoot == null)
                return;

            bool shared=!projectileColliderBufferInUse;
            var targetColliders=shared?projectileColliderBuffer:new System.Collections.Generic.List<Collider>();
            if(shared)projectileColliderBufferInUse=true;
            try
            {
                targetRoot.GetComponentsInChildren(true,targetColliders);
                foreach (Collider targetCollider in targetColliders)
                {
                    if(targetCollider==null)continue;
                    // Match GetComponentInParent's active-ancestor behavior,
                    // while avoiding Editor allocations for absent components.
                    bool damageable=targetCollider.TryGetComponent<P12PipelineSafeDamageable>(out _);
                    for(var parent=targetCollider.transform.parent;!damageable&&parent!=null;parent=parent.parent)
                        if(parent.gameObject.activeInHierarchy&&parent.TryGetComponent<P12PipelineSafeDamageable>(out _))damageable=true;
                    if(!damageable)
                    {
                        Physics.IgnoreCollision(ownCollider,targetCollider,true);
                        if(cacheableConventional)ignoredForFlight.Add(targetCollider);
                    }
                }
            }
            finally { targetColliders.Clear(); if(shared)projectileColliderBufferInUse=false; }
        }

        private void OnCollisionEnter(
            Collision collision)
        {
            if(RetireLostOwner())return;
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
                if(own!=null)
                {
                    Physics.IgnoreCollision(own,collision.collider,true);
                    if(cacheableConventional)ignoredForFlight.Add(collision.collider);
                }
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
            {
                if(hybridDamage)RoboRougelike.WeaponIntegration.RRLHitRouter.ApplyHybrid(collision.collider,kineticComponent,blastComponent,point,launchDirection,ownerRoot);
                else RoboRougelike.WeaponIntegration.RRLHitRouter.Apply(collision.collider,damage,point,launchDirection,ownerRoot);
            }

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

            if(cacheableConventional)Retire();else Destroy(gameObject);
        }
    }

}
