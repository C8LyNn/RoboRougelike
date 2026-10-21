using System.Collections.Generic;
using UnityEngine;
using RoboRougelike.R3;
using System.Linq;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RoboRougelike.R5;

namespace RogueMachine.P12PipelineSafe
{
    public class P12PipelineSafeEnemyTurret : MonoBehaviour
    {
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform yawPivot;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private P12PipelineSafeDamageable core;
        [SerializeField] private Material tracerMaterial;

        [Header("Enemy physical weapon")]
        [SerializeField] private float damage = 34f;
        [SerializeField] private float fireInterval = 1.25f;
        [SerializeField] private float maxRange = 60f;
        [SerializeField] private float turnSpeed = 85f;
        [SerializeField] private float aimToleranceDegrees = 9f;
        [SerializeField] private float projectileSpeed = 27f;
        [SerializeField] private float projectileDiameter = 0.18f;
        [SerializeField] private float projectileMass = 1.5f;
        [SerializeField] private float projectileLifetime = 6f;

        private float nextFireTime;
        private Material runtimeProjectileMaterial;
        private bool defeated;
        private bool combatActive = true;
        private P12PipelineSafeDamageable weaponBlock;
        private Collider currentTarget;
        private float nextTargetTime;
        private P12PipelineSafeDamageable turretSupport;
        private Rigidbody movementBody;
        private float moveSpeed, preferredDistance = 12, strafeStrength;
        private int strafeDirection = 1;
        private Vector3 lastMoveNormal,obstacleTangent;
        private float navigationUntil;
        private readonly RaycastHit[] movementHits = new RaycastHit[256];
        private Vector3 movementHalfExtents = new Vector3(0.88f, 0.67f, 0.88f);
        private readonly List<P12PipelineSafeDamageable> driveWheels=new List<P12PipelineSafeDamageable>();
        private P12PipelineSafeDamageable turretAnchor;
        private float nextStrafeChange;
        private bool profiled;
        public R4WeaponKind WeaponKind { get; private set; }
        private R4SalvageLedger salvage;
        private string salvageId;
        private Material weaponMaterial;
        private bool snapshotTaken;
        public P12PipelineSafeDamageable WeaponBlock => variantGuns.Count>0?variantGuns[0].Block:weaponBlock;
        public void ConfigureSalvage(R4SalvageLedger ledger,string id){salvage=ledger;salvageId=id;}
        public R3EnemyArchetype Archetype { get; private set; }
        public int ActiveWheelCount => driveWheels.Count(p=>p!=null&&!p.IsDead&&p.transform.IsChildOf(transform));
        public int InitialWheelCount => driveWheels.Count;

        public void ConfigureArchetype(R3EnemyArchetype kind)
        {
            Archetype=kind;profiled=true;
            WeaponKind=kind==R3EnemyArchetype.Scout?R4WeaponKind.Automatic:
                kind==R3EnemyArchetype.Flanker?R4WeaponKind.Basic:R4WeaponKind.Heavy;
            var oldBase=transform.Find("Base");if(oldBase==null||core==null)return;
            var armor=oldBase.GetComponentInChildren<Renderer>().sharedMaterial;
            var coreMaterial=core.GetComponent<Renderer>().sharedMaterial;
            oldBase.gameObject.SetActive(false);oldBase.name="Stara podstawa";Destroy(oldBase.gameObject);
            var baseRoot=new GameObject("Base").transform;baseRoot.SetParent(transform,false);
            int width=kind==R3EnemyArchetype.Bruiser?2:1;
            int length=kind==R3EnemyArchetype.Scout?1:2;
            for(int y=0;y<2;y++)for(int x=-width;x<=width;x++)for(int z=-length;z<=length;z++)
            {
                var part=MakeProfileBlock(baseRoot,new Vector3Int(x,y,z),armor,false);
                if(x==0&&y==1&&z==0){core=part;part.GetComponent<Renderer>().sharedMaterial=coreMaterial;}
            }
            turretAnchor=core;
            if(kind==R3EnemyArchetype.Gunner)
                turretAnchor=MakeProfileBlock(baseRoot,new Vector3Int(0,2,0),armor,false);
            if(kind==R3EnemyArchetype.Flanker)
                for(int z=-1;z<=1;z++)MakeProfileBlock(baseRoot,new Vector3Int(2,1,z),armor,false);
            int wheelCount=kind==R3EnemyArchetype.Bruiser?3:2;
            for(int side=-1;side<=1;side+=2)for(int i=0;i<wheelCount;i++)
                driveWheels.Add(MakeProfileBlock(baseRoot,new Vector3Int(side*(width+1),0,
                    wheelCount==2?i*2-1:i-1),armor,true));
            yawPivot.localPosition=new Vector3(0,kind==R3EnemyArchetype.Gunner?1.75f:1.155f,0);
            movementHalfExtents=new Vector3((width+1)*0.55f+0.32f,0.67f,length*0.55f+0.32f);
            CacheWeapon();
            var profile=R4WeaponDefinition.Get(WeaponKind);
            damage=profile.Damage*0.85f;fireInterval=profile.Interval*(WeaponKind==R4WeaponKind.Automatic?3:2.5f);
            projectileSpeed=profile.Speed*0.8f;projectileDiameter=profile.Diameter;projectileMass=profile.ProjectileMass;
            if(weaponBlock!=null)
            {
                weaponBlock.Configure(profile.Hp,true);
                weaponBlock.transform.localScale=Vector3.Scale(weaponBlock.transform.localScale,
                    WeaponKind==R4WeaponKind.Heavy?new Vector3(1.5f,1.5f,1.3f):WeaponKind==R4WeaponKind.Automatic?new Vector3(0.65f,0.65f,1.3f):Vector3.one);
                var renderer=weaponBlock.GetComponent<Renderer>();weaponMaterial=new Material(renderer.sharedMaterial);
                weaponMaterial.SetColor("_BaseColor",profile.Color);renderer.sharedMaterial=weaponMaterial;
            }
        }
        private static P12PipelineSafeDamageable MakeProfileBlock(Transform parent,Vector3Int cell,Material material,bool wheel)
        {
            var part=GameObject.CreatePrimitive(wheel?PrimitiveType.Cylinder:PrimitiveType.Cube);
            part.name=wheel?"Koło przeciwnika":"P12_EnemyBlock";part.transform.SetParent(parent,false);
            part.transform.localPosition=new Vector3(cell.x*0.55f,0.275f+cell.y*0.55f,cell.z*0.55f);
            part.transform.localScale=wheel?new Vector3(0.53f,0.15f,0.53f):Vector3.one*0.53f;
            if(wheel)part.transform.localRotation=Quaternion.Euler(0,0,90);
            part.GetComponent<Renderer>().sharedMaterial=material;
            var hp=part.AddComponent<P12PipelineSafeDamageable>();hp.Configure(wheel?75:100,true);return hp;
        }

        public P12PipelineSafeDamageable Core => core;
        public bool IsDefeated => defeated || core == null || core.IsDead;
        public bool WeaponOnline => variantGuns.Count>0?variantGuns.Any(g=>g.Online(transform)):weaponBlock != null && !weaponBlock.IsDead &&
            turretSupport != null && !turretSupport.IsDead && yawPivot != null && muzzle != null &&
            (turretAnchor==null||!turretAnchor.IsDead&&turretAnchor.transform.IsChildOf(transform));

        public void ConfigureMovement(float speed, float distance, float strafe)
        {
            moveSpeed = Mathf.Clamp(speed, 0, 6);
            preferredDistance = Mathf.Clamp(distance, 6, 20);
            strafeStrength = Mathf.Clamp01(strafe);
            if(profiled)
            {
                moveSpeed=Archetype==R3EnemyArchetype.Bruiser?speed*0.7f:Archetype==R3EnemyArchetype.Gunner?speed*0.65f:speed*1.25f;
                preferredDistance=Archetype==R3EnemyArchetype.Bruiser?7:Archetype==R3EnemyArchetype.Gunner?18:12;
                strafeStrength=Archetype==R3EnemyArchetype.Bruiser?0.12f:Archetype==R3EnemyArchetype.Gunner?0.2f:0.85f;
            }
            movementBody = GetComponent<Rigidbody>();
            if (movementBody == null) movementBody = gameObject.AddComponent<Rigidbody>();
            movementBody.isKinematic = false;
            movementBody.useGravity = false;
            movementBody.interpolation = RigidbodyInterpolation.Interpolate;
            ConfigureVariantMovement();
            if(GetComponent<RoboRougelike.R6.R6VehicleDrive>()==null)gameObject.AddComponent<RoboRougelike.R6.R6VehicleDrive>();

            // The existing 18-block base uses the same connectivity/destruction graph as the player.
            var baseRoot = transform.Find("Base");
            if (baseRoot == null || core == null) return;
            foreach (var part in baseRoot.GetComponentsInChildren<P12PipelineSafeDamageable>())
                if (part.GetComponent<P12_PipelineSafePlayerBlock>() == null)
                    part.gameObject.AddComponent<P12_PipelineSafePlayerBlock>();
            var structure = GetComponent<P12PipelineSafeVehicleStructure>();
            if (structure == null) structure = gameObject.AddComponent<P12PipelineSafeVehicleStructure>();
            structure.SetDesignatedCore(core.GetComponent<P12_PipelineSafePlayerBlock>());
        }

        public void SetCombatActive(bool active) { combatActive = active; }

        public void ConfigureEncounter(Transform player, float healthMultiplier,
            float damageMultiplier, float intervalMultiplier, float speedMultiplier)
        {
            configuredProjectileSpeed=Mathf.Clamp(speedMultiplier,.5f,2);
            playerRoot = player;
            foreach (var part in GetComponentsInChildren<P12PipelineSafeDamageable>())
                part.SetDurabilityMultiplier(healthMultiplier);
            damage *= Mathf.Clamp(damageMultiplier, 0.1f, 3f);
            fireInterval = Mathf.Max(0.3f, fireInterval * intervalMultiplier);
            if(profiled)
            {
                float health=Archetype==R3EnemyArchetype.Bruiser?1.15f:Archetype==R3EnemyArchetype.Scout?0.85f:0.95f;
                foreach(var part in GetComponentsInChildren<P12PipelineSafeDamageable>())part.SetDurabilityMultiplier(healthMultiplier*health);
                if(Archetype==R3EnemyArchetype.Gunner)fireInterval*=0.8f;
            }
            projectileSpeed *= Mathf.Clamp(speedMultiplier, 0.5f, 2f);
            nextFireTime = Time.time + 1.5f;
        }

        public void Configure(
            Transform player,
            Transform yaw,
            Transform muzzleTransform,
            Transform root,
            P12PipelineSafeDamageable coreBlock,
            Material tracer)
        {
            playerRoot = player;
            yawPivot = yaw;
            muzzle = muzzleTransform;
            enemyRoot = root;
            core = coreBlock;
            tracerMaterial = tracer;
        }

        private void Start()
        {
            if(variantGuns.Count==0)RepairReferences();
            EnsureProjectileMaterial();
            if(variantGuns.Count==0)CacheWeapon();
            if(core!=null)core.Died+=HandleCoreDeath;
        }
        private void CacheWeapon()
        {
            if (yawPivot != null && muzzle != null)
            {
                float nearest = float.PositiveInfinity;
                foreach (var part in yawPivot.GetComponentsInChildren<P12PipelineSafeDamageable>())
                {
                    float distance = (part.transform.position - muzzle.position).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; weaponBlock = part; }
                    if (turretSupport == null || part.transform.localPosition.sqrMagnitude <
                        turretSupport.transform.localPosition.sqrMagnitude) turretSupport = part;
                }
            }
        }

        private void OnDestroy()
        {
            if(core!=null)core.Died-=HandleCoreDeath;
            foreach(var owned in variantMaterials)if(owned!=null)Destroy(owned);
            if(weaponMaterial!=null)Destroy(weaponMaterial);
            if (runtimeProjectileMaterial != null)
                Destroy(runtimeProjectileMaterial);
        }
        private static float Condition(P12PipelineSafeDamageable part) => part==null||part.IsDead?0:Mathf.Clamp01(part.CurrentHp/part.MaxHp);
        private void HandleCoreDeath(){CaptureSalvage();DropTurret();}
        private void CaptureSalvage()
        {
            if(snapshotTaken||salvage==null)return;snapshotTaken=true;
            var items=new List<R4SalvageItem>();
            if(variantGuns.Count>0)
                for(int i=0;i<variantGuns.Count;i++)
                {
                    var gun=variantGuns[i];
                    items.Add(new R4SalvageItem(salvageId+"/gun/"+i,name+" / bateria "+(i+1),R3PartKind.Gun,gun.Kind,
                        gun.Online(transform)?Condition(gun.Block):0));
                }
            else items.Add(new R4SalvageItem(salvageId+"/gun",name+" / uzbrojenie",R3PartKind.Gun,WeaponKind,
                Mathf.Min(Condition(weaponBlock),Condition(turretSupport))));
            for(int i=0;i<driveWheels.Count;i++)items.Add(new R4SalvageItem(salvageId+"/wheel/"+i,
                name+" / koło "+(i+1),R3PartKind.Wheel,R4WeaponKind.Basic,Condition(driveWheels[i])));
            salvage.Record(salvageId,items);
        }

        private void RepairReferences()
        {
            if (enemyRoot == null)
                enemyRoot =
                    transform;

            if (playerRoot == null)
            {
                P12PipelineSafeTankController player =
                    FindFirstObjectByType<
                        P12PipelineSafeTankController
                    >();

                if (player != null)
                    playerRoot =
                        player.transform;
            }

            if (yawPivot == null)
                yawPivot =
                    FindDeepChild(
                        enemyRoot,
                        "Enemy_Yaw"
                    );

            if (muzzle == null)
                muzzle =
                    FindDeepChild(
                        enemyRoot,
                        "Enemy_Muzzle"
                    );
        }

        private void Update()
        {
            // A missing destroyed core is still a dead enemy, even after its debris expires.
            if (IsDefeated) { DropTurret(); defeated = true; enabled=false; return; }
            if (!combatActive) return;
            if(variantGuns.Count>0){UpdateVariantWeapons();return;}
            if (!WeaponOnline)
            {
                DropTurret();
                return;
            }
            if (playerRoot == null ||
                yawPivot == null ||
                muzzle == null ||
                enemyRoot == null)
            {
                RepairReferences();

                if (playerRoot == null ||
                    yawPivot == null ||
                    muzzle == null ||
                    enemyRoot == null)
                    return;
            }

            if (core != null &&
                core.IsDead)
                return;

            if (currentTarget == null || !currentTarget.enabled || Time.time >= nextTargetTime)
            {
                currentTarget = SelectTargetCollider();
                nextTargetTime = Time.time + 0.8f;
            }
            Collider targetCollider = currentTarget;

            if (targetCollider == null)
                return;

            Vector3 aimPoint =
                targetCollider.bounds.center;

            RotateToward(
                aimPoint
            );

            float distance =
                Vector3.Distance(
                    muzzle.position,
                    aimPoint
                );

            if (distance >
                maxRange)
                return;

            Vector3 flatForward =
                Vector3.ProjectOnPlane(
                    yawPivot.forward,
                    Vector3.up
                );

            Vector3 flatDesired =
                Vector3.ProjectOnPlane(
                    aimPoint -
                    yawPivot.position,
                    Vector3.up
                );

            float angle =
                Vector3.Angle(
                    flatForward,
                    flatDesired
                );

            if (angle >
                aimToleranceDegrees)
                return;

            if (Time.time <
                nextFireTime)
                return;
            // Do not shoot through another vehicle from the same encounter.
            if(FriendlyInLine(aimPoint))return;

            nextFireTime =
                Time.time +
                fireInterval;

            FirePhysicalShell(
                aimPoint
            );
        }

        private void DropTurret()
        {
            if(yawPivot==null||!yawPivot.IsChildOf(transform))return;
            var detached=yawPivot;var owner=GetComponent<Rigidbody>();
            var velocity=owner!=null?owner.GetPointVelocity(detached.position):Vector3.zero;
            var spin=owner!=null?owner.angularVelocity:Vector3.zero;
            float mass=detached.GetComponentsInChildren<P12PipelineSafeDamageable>().Where(p=>!p.IsDead).Sum(RoboRougelike.R6.R6VehicleDrive.PartMass);
            detached.SetParent(null,true);
            var body=detached.GetComponent<Rigidbody>();if(body==null)body=detached.gameObject.AddComponent<Rigidbody>();
            RoboRougelike.R6.R6DebrisRelease.Release(body,owner,velocity,spin,Mathf.Max(8,mass));
            RoboRougelike.R6.R6DebrisRelease.Eject(body,owner,detached.forward);
            body.collisionDetectionMode=CollisionDetectionMode.Continuous;
            if(detached.GetComponent<R3Debris>()==null)detached.gameObject.AddComponent<R3Debris>();
            yawPivot=null;muzzle=null;
        }

        private void FixedUpdate()
        {
            var drive=GetComponent<RoboRougelike.R6.R6VehicleDrive>();
            if(drive!=null)drive.StepWorld(Vector3.zero,0);
            if (!combatActive || IsDefeated || movementBody == null || playerRoot == null || moveSpeed <= 0) return;
            Vector3 toward = Vector3.ProjectOnPlane(playerRoot.position - movementBody.position, Vector3.up);
            float distance = toward.magnitude;
            if (distance < 0.01f) return;
            toward /= distance;
            if(profiled&&Time.time>=nextStrafeChange&&Time.time>=navigationUntil)
            { strafeDirection=-strafeDirection;nextStrafeChange=Time.time+(Archetype==R3EnemyArchetype.Flanker?2:4.5f); }
            Vector3 sideways = Vector3.Cross(Vector3.up, toward) * strafeDirection;
            float approach = Mathf.Clamp((distance - preferredDistance) / 3, -1, 1);
            Vector3 desired = (toward * approach + sideways * strafeStrength).normalized;
            var encounter=RoboRougelike.R1.R1RunController.Instance?.Encounter;
            if(encounter!=null)
                foreach(var other in encounter.Enemies)
                {
                    if(other==null||other==this||other.IsDefeated)continue;
                    var away=Vector3.ProjectOnPlane(movementBody.position-other.transform.position,Vector3.up);
                    float d=away.magnitude;
                    if(d>0.01f&&d<6)desired+=away/d*((6-d)/6)*1.5f;
                }
            desired.Normalize();
            float wheelFactor=driveWheels.Count==0?1:ActiveWheelCount/(float)driveWheels.Count;
            if(wheelFactor<=0)return;
            float step = moveSpeed * Mathf.Lerp(0.3f,1,wheelFactor) * Time.fixedDeltaTime;
            float clearance = Mathf.Max(step + 0.08f, 1.4f);
            if (!CanMove(desired, clearance))
            {
                // Keep a wall-parallel escape direction across periodic combat strafing changes.
                var normal=lastMoveNormal;
                var tangent=Time.time<navigationUntil?obstacleTangent:Vector3.Cross(Vector3.up,normal).normalized;
                if(Time.time>=navigationUntil&&Vector3.Dot(tangent,desired)<0)tangent=-tangent;
                if(!CanMove(tangent,step+.08f))
                {
                    tangent=Vector3.Cross(Vector3.up,normal).normalized;
                    if(Vector3.Dot(tangent,desired)<0)tangent=-tangent;
                    if(!CanMove(tangent,step+.08f))tangent=-tangent;
                }
                desired=CanMove(tangent,step+.08f)?tangent:normal;
                obstacleTangent=desired;navigationUntil=Time.time+1.1f;
            }
            else if(Time.time<navigationUntil&&CanMove(obstacleTangent,step+.08f))desired=obstacleTangent;
            Vector3 next = movementBody.position + desired * step;
            if(Mathf.Abs(next.x)>38)desired.x=0;
            if(Mathf.Abs(next.z)>38)desired.z=0;
            if(desired.sqrMagnitude<0.001f)desired=Vector3.ProjectOnPlane(-movementBody.position,Vector3.up).normalized;
            desired.Normalize();
            if (!CanMove(desired, step + 0.08f)) return;
            next=movementBody.position+desired*step;
            next.x=Mathf.Clamp(next.x,-38,38);next.z=Mathf.Clamp(next.z,-38,38);
            if(drive!=null)drive.StepWorld(desired,moveSpeed * Mathf.Lerp(0.3f,1,wheelFactor));
        }

        private bool FriendlyInLine(Vector3 point)
        {
            var delta=point-muzzle.position;
            int count=Physics.RaycastNonAlloc(muzzle.position,delta.normalized,movementHits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var ally=movementHits[i].collider.GetComponentInParent<P12PipelineSafeEnemyTurret>();
                if(ally!=null&&ally!=this&&!ally.IsDefeated)return true;
            }
            return false;
        }

        private bool CanMove(Vector3 direction, float distance)
        {
            int count = Physics.BoxCastNonAlloc(movementBody.position + Vector3.up * 0.75f,
                movementHalfExtents, direction, movementHits, Quaternion.identity, distance,
                ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = movementHits[i].collider;
                if (hit == null || hit.transform.IsChildOf(transform) ||
                    hit.GetComponent<P17PhysicalShell>() != null) continue;
                // Ignore the flat ground, but avoid walls, live vehicles, and loose debris.
                if (hit.bounds.max.y < 0.12f) continue;
                lastMoveNormal=Vector3.ProjectOnPlane(movementHits[i].normal,Vector3.up).normalized;
                if(lastMoveNormal.sqrMagnitude<.01f)lastMoveNormal=-direction;
                return false;
            }
            return count < movementHits.Length;
        }

        private void RotateToward(
            Vector3 aimPoint)
        {
            Vector3 flatDirection =
                Vector3.ProjectOnPlane(
                    aimPoint -
                    yawPivot.position,
                    Vector3.up
                );

            if (flatDirection.sqrMagnitude <
                0.0001f)
                return;

            Quaternion desired =
                Quaternion.LookRotation(
                    flatDirection.normalized,
                    Vector3.up
                );

            yawPivot.rotation =
                Quaternion.RotateTowards(
                    yawPivot.rotation,
                    desired,
                    turnSpeed *
                    Time.deltaTime
                );
        }

        private Collider SelectTargetCollider()
        {
            if (playerRoot == null)
                return null;

            List<Collider> moduleTargets =
                new List<Collider>();

            P12PipelineSafeFunctionalModule[] modules =
                playerRoot.GetComponentsInChildren<
                    P12PipelineSafeFunctionalModule
                >(true);

            foreach (P12PipelineSafeFunctionalModule module in modules)
            {
                if (module == null ||
                    module.ModuleLost ||
                    !module.DamageableAlive)
                    continue;

                Collider collider =
                    module.GetComponent<Collider>();

                if (collider != null && collider.enabled)
                    moduleTargets.Add(collider);
            }

            if (moduleTargets.Count > 0 &&
                Random.value < 0.34f)
            {
                return moduleTargets[
                    Random.Range(0, moduleTargets.Count)
                ];
            }

            List<Collider> blockTargets =
                new List<Collider>();

            P12_PipelineSafePlayerBlock[] blocks =
                playerRoot.GetComponentsInChildren<
                    P12_PipelineSafePlayerBlock
                >(true);

            foreach (P12_PipelineSafePlayerBlock block in blocks)
            {
                if (block == null)
                    continue;

                P12PipelineSafeDamageable damageable =
                    block.GetComponent<
                        P12PipelineSafeDamageable
                    >();

                Collider collider =
                    block.GetComponent<Collider>();

                if (damageable != null &&
                    !damageable.IsDead &&
                    collider != null &&
                    collider.enabled)
                {
                    blockTargets.Add(collider);
                }
            }

            if (blockTargets.Count == 0)
                return null;

            return blockTargets[
                Random.Range(0, blockTargets.Count)
            ];
        }

        private void FirePhysicalShell(
            Vector3 aimPoint)
        {
            RoboRougelike.R6.R6CombatFeedback.Muzzle(muzzle,WeaponKind);
            Vector3 direction =
                (aimPoint -
                 muzzle.position)
                .normalized;

            Vector3 origin =
                muzzle.position +
                direction *
                Mathf.Max(
                    0.14f,
                    projectileDiameter *
                    0.75f
                );

            GameObject shell =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere
                );

            shell.name =
                "P17_EnemyPhysicalShell";

            shell.transform.position =
                origin;

            shell.transform.localScale =
                Vector3.one *
                projectileDiameter;

            Renderer renderer =
                shell.GetComponent<Renderer>();

            if (renderer != null &&
                runtimeProjectileMaterial != null)
            {
                renderer.sharedMaterial =
                    runtimeProjectileMaterial;
            }

            SphereCollider sphere =
                shell.GetComponent<
                    SphereCollider
                >();

            Rigidbody body =
                shell.AddComponent<Rigidbody>();

            body.mass =
                projectileMass;

            body.useGravity =
                false;

            body.interpolation =
                RigidbodyInterpolation.Interpolate;

            body.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;

#if UNITY_6000_0_OR_NEWER
            body.linearVelocity =
                direction *
                projectileSpeed;
#else
            body.velocity =
                direction *
                projectileSpeed;
#endif

            P17PhysicalShell projectile =
                shell.AddComponent<
                    P17PhysicalShell
                >();

            projectile.Configure(
                enemyRoot,
                damage,
                projectileLifetime,
                direction,
                playerRoot
            );

            IgnoreOwnerColliders(
                sphere
            );
        }

        private void IgnoreOwnerColliders(
            Collider projectileCollider)
        {
            if (projectileCollider == null ||
                enemyRoot == null)
                return;

            Collider[] colliders =
                enemyRoot
                .GetComponentsInChildren<
                    Collider
                >(true);

            foreach (Collider collider in colliders)
            {
                if (collider == null ||
                    collider ==
                    projectileCollider)
                    continue;

                Physics.IgnoreCollision(
                    projectileCollider,
                    collider,
                    true
                );
            }
        }

        private void EnsureProjectileMaterial()
        {
            if (runtimeProjectileMaterial != null)
                return;

            Material template =
                tracerMaterial;

            if (template == null)
            {
                Renderer renderer =
                    GetComponentInChildren<
                        Renderer
                    >(true);

                if (renderer != null)
                    template =
                        renderer.sharedMaterial;
            }

            if (template == null)
                return;

            runtimeProjectileMaterial =
                new Material(
                    template
                );

            runtimeProjectileMaterial.name =
                "P17_EnemyShellMaterial";

            Color red =
                new Color(
                    1f,
                    0.14f,
                    0.06f,
                    1f
                );

            if (runtimeProjectileMaterial.HasProperty(
                "_BaseColor"))
            {
                runtimeProjectileMaterial.SetColor(
                    "_BaseColor",
                    red
                );
            }

            if (runtimeProjectileMaterial.HasProperty(
                "_Color"))
            {
                runtimeProjectileMaterial.SetColor(
                    "_Color",
                    red
                );
            }
        }

        public sealed class VariantGun
        {
            public P12PipelineSafeDamageable Block,Support;
            public Transform Pivot,Muzzle;
            public R4WeaponKind Kind;
            public float NextShot;
            public bool Online(Transform owner)=>Block!=null&&!Block.IsDead&&Block.transform.IsChildOf(owner)&&
                Support!=null&&!Support.IsDead&&Support.transform.IsChildOf(owner)&&Pivot!=null&&Muzzle!=null;
        }
        private readonly List<VariantGun> variantGuns=new List<VariantGun>();
        private float configuredProjectileSpeed=1;
        private readonly List<Material> variantMaterials=new List<Material>();
        public IReadOnlyList<VariantGun> VariantWeapons => variantGuns.AsReadOnly();
        public R5EnemyVariant Variant { get; private set; }

        public void ConfigureVariant(R5EnemyVariant variant)
        {
            Variant=variant;if(variant==R5EnemyVariant.Normal)return;
            var baseRoot=transform.Find("Base");var armor=core.GetComponent<Renderer>().sharedMaterial;
            var accent=new Material(armor);accent.SetColor("_BaseColor",new Color(1,.65f,.1f));variantMaterials.Add(accent);
            if(yawPivot!=null){yawPivot.gameObject.SetActive(false);Destroy(yawPivot.gameObject);yawPivot=null;muzzle=null;}
            bool boss=variant==R5EnemyVariant.SiegeBoss;
            if(boss)
            {
                name="OBLĘŻNIK";
                foreach(var wheel in driveWheels){wheel.gameObject.SetActive(false);Destroy(wheel.gameObject);}driveWheels.Clear();
                for(int y=0;y<2;y++)for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++)
                    if(Mathf.Abs(x)>2||Mathf.Abs(z)>2)MakeProfileBlock(baseRoot,new Vector3Int(x,y,z),armor,false);
                for(int side=-1;side<=1;side+=2)foreach(int z in new[]{-2,-1,1,2})
                    driveWheels.Add(MakeProfileBlock(baseRoot,new Vector3Int(side*4,0,z),armor,true));
                for(int x=-1;x<=1;x++)MakeProfileBlock(baseRoot,new Vector3Int(x,2,0),armor,false);
                movementHalfExtents=new Vector3(3.1f,1.2f,2.1f);
            }
            else if(variant==R5EnemyVariant.EliteBruiser)
            {
                name="Bastion • elita";
                for(int x=-2;x<=2;x++)for(int y=0;y<2;y++)MakeProfileBlock(baseRoot,new Vector3Int(x,y,3),armor,false);
                for(int side=-1;side<=1;side+=2)driveWheels.Add(MakeProfileBlock(baseRoot,new Vector3Int(side*3,0,2),armor,true));
                movementHalfExtents=new Vector3(2.55f,.9f,2.1f);
            }
            else
            {
                name="Wartownik • elita";
                for(int side=-1;side<=1;side+=2)MakeProfileBlock(baseRoot,new Vector3Int(side,2,0),armor,false);
                for(int x=-1;x<=1;x++)MakeProfileBlock(baseRoot,new Vector3Int(x,2,-2),armor,false);
                movementHalfExtents=new Vector3(2.05f,1.4f,1.5f);
            }
            int bridge=boss?4:variant==R5EnemyVariant.EliteBruiser?3:2;
            int height=variant==R5EnemyVariant.EliteGunner?2:1;
            for(int side=-1;side<=1;side+=2)
            {
                var support=MakeProfileBlock(baseRoot,new Vector3Int(side*bridge,height,0),accent,false);support.name="Podpora baterii "+side;
                support.Configure(boss?90:75,true);
                var gun=MakeProfileBlock(baseRoot,new Vector3Int(side*(bridge+1),height,0),accent,false);gun.name="Bateria "+side;
                var kind=variant==R5EnemyVariant.EliteGunner&&side==1?R4WeaponKind.Automatic:R4WeaponKind.Heavy;
                var profile=R4WeaponDefinition.Get(kind);gun.Configure(profile.Hp,true);
                var pivot=R41PartGeometry.Payload(gun.transform,R3PartKind.Gun,kind,accent,true,out var barrel);
                var block=gun.gameObject.AddComponent<P12_PipelineSafePlayerBlock>();
                var module=gun.gameObject.AddComponent<P12PipelineSafeFunctionalModule>();module.Configure(P12FunctionalModuleType.Weapon,side,block,pivot,transform);
                variantGuns.Add(new VariantGun{Block=gun,Support=support,Kind=kind,Pivot=pivot,Muzzle=barrel,NextShot=Time.time+1.8f+variantGuns.Count*.7f});
                if(boss)foreach(int z in new[]{-1,1})MakeProfileBlock(baseRoot,new Vector3Int(side*(bridge+1),height,z),armor,false);
            }
        }
        private void ConfigureVariantMovement()
        {
            if(Variant==R5EnemyVariant.Normal)return;
            moveSpeed*=Variant==R5EnemyVariant.SiegeBoss?.55f:.8f;
            preferredDistance=Variant==R5EnemyVariant.EliteBruiser?10:18;strafeStrength=Variant==R5EnemyVariant.EliteGunner?.25f:.1f;
            movementBody.mass=GetComponentsInChildren<P12PipelineSafeDamageable>().Length*8;
        }
        private void UpdateVariantWeapons()
        {
            if(playerRoot==null)return;
            if(currentTarget==null||!currentTarget.enabled||Time.time>=nextTargetTime){currentTarget=SelectTargetCollider();nextTargetTime=Time.time+.8f;}
            if(currentTarget==null)return;
            var point=currentTarget.bounds.center;
            foreach(var gun in variantGuns)
            {
                if(!gun.Online(transform))continue;
                var delta=point-gun.Pivot.position;
                gun.Pivot.rotation=Quaternion.RotateTowards(gun.Pivot.rotation,Quaternion.LookRotation(delta),85*Time.deltaTime);
                if(delta.magnitude>maxRange||Vector3.Angle(gun.Pivot.forward,delta)>9||Time.time<gun.NextShot)continue;
                var ray=point-gun.Muzzle.position;
                int count=Physics.RaycastNonAlloc(gun.Muzzle.position,ray.normalized,movementHits,ray.magnitude,~0,QueryTriggerInteraction.Ignore);
                bool ally=false;for(int i=0;i<count;i++){var other=movementHits[i].collider.GetComponentInParent<P12PipelineSafeEnemyTurret>();if(other!=null&&other!=this&&!other.IsDefeated)ally=true;}
                if(ally)continue;
                var profile=R4WeaponDefinition.Get(gun.Kind);
                RoboRougelike.R6.R6CombatFeedback.Muzzle(gun.Muzzle,gun.Kind);
                gun.NextShot=Time.time+profile.Interval*(fireInterval/R4WeaponDefinition.Get(WeaponKind).Interval);
                var direction=(point-gun.Muzzle.position).normalized;
                var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="P17_EnemyPhysicalShell";
                shell.transform.position=gun.Muzzle.position+direction*.25f;shell.transform.localScale=Vector3.one*profile.Diameter;
                shell.GetComponent<Renderer>().sharedMaterial=runtimeProjectileMaterial;
                var body=shell.AddComponent<Rigidbody>();body.mass=profile.ProjectileMass;body.useGravity=false;
                body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearVelocity=direction*profile.Speed*.8f*configuredProjectileSpeed;
                var origin=shell.AddComponent<R4ProjectileOrigin>();origin.Weapon=gun.Kind;origin.PartId=name+"/"+gun.Block.name;
                shell.AddComponent<P17PhysicalShell>().Configure(transform,profile.Damage*(damage/R4WeaponDefinition.Get(WeaponKind).Damage),6,direction,playerRoot);
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
}
