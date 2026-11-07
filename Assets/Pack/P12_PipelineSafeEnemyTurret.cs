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
        public bool CombatActive => combatActive;
        private RoboRougelike.WeaponIntegration.RRLShotgunBinding packageShotgun;
        private RoboRougelike.WeaponIntegration.RRLRailgunBinding packageRailgun;
        private RoboRougelike.WeaponIntegration.RRLBeamBinding packageBeam;
        private RoboRougelike.WeaponIntegration.RRLSawBinding packageSaw;
        private RoboRougelike.WeaponIntegration.RRLEmpBinding packageEmp;
        private RoboRougelike.WeaponIntegration.RRLRocketBinding packageRocket;
        private RoboRougelike.WeaponIntegration.RRLPointDefenseBinding packagePointDefense;
        private RoboRougelike.WeaponIntegration.RRLShieldBinding packageShield;
        // Equipment data belongs to the enemy, independently of temporary debris views.
        private R3PartState packageWeaponState,packageShieldState,packagePointDefenseState,packageBatteryState;
        private P12PipelineSafeDamageable packageBattery;
        // All enemy guns, including package bindings, share physical part state with salvage.
        public bool SetPrimaryWeaponTier(int tier)
        {
            if(tier<1||tier>3||IsDefeated||variantGuns.Count>0||weaponBlock==null||weaponBlock.IsDead||
                !weaponBlock.transform.IsChildOf(transform))return false;
            packageWeaponState=SetGunTier(weaponBlock,WeaponKind,"gun",tier);
            GetComponent<RoboRougelike.R6.R6VehicleDrive>()?.RefreshMass();
            return packageWeaponState!=null;
        }
        R3PartState SetGunTier(P12PipelineSafeDamageable hp,R4WeaponKind kind,string slot,int tier)
            => SetEquipmentTier(hp,R3PartKind.Gun,kind,slot,tier);
        R3PartState SetEquipmentTier(P12PipelineSafeDamageable hp,R3PartKind partKind,R4WeaponKind kind,string slot,int tier)
        {
            if(hp==null||hp.IsDead||!hp.transform.IsChildOf(transform)||tier<1||tier>3)return null;
            var device=hp.GetComponent<RoboRougelike.WeaponIntegration.RRLDeviceBinding>();
            var state=device!=null?device.State:null;
            if(state==null)
            {
                state=new R3PartState(default,partKind){Id=EquipmentInstanceId(slot),Weapon=kind,Source=name+" / część"};
                if(device==null)device=hp.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLDeviceBinding>();
                device.Bind(state,transform);
            }
            if(state.Kind!=partKind||(partKind==R3PartKind.Gun&&state.Weapon!=kind))return null;
            int oldTier=R3VehicleModel.NormalizedTier(state.Tier);
            if(oldTier!=tier)
            {
                float condition=Condition(hp);
                float max=hp.MaxHp/R3VehicleModel.TierHealth(oldTier)*R3VehicleModel.TierHealth(tier);
                hp.Configure(max,true);hp.RestoreHealthRatio(condition);
                state.Tier=tier;
            }
            return state;
        }
        public void SetOtherWeaponTiers(int tier)
        {
            if(tier<1||tier>3)return;
            for(int i=0;i<variantGuns.Count;i++)SetGunTier(variantGuns[i].Block,variantGuns[i].Kind,"gun/"+i,tier);
            for(int i=0;i<additionalGuns.Count;i++)SetGunTier(additionalGuns[i].Block,additionalGuns[i].Kind,"extra-gun/"+i,tier);
            GetComponent<RoboRougelike.R6.R6VehicleDrive>()?.RefreshMass();
        }
        public void SetWheelTiers(int tier)
        {
            if(tier<1||tier>3)return;
            for(int i=0;i<driveWheels.Count;i++)
                SetEquipmentTier(driveWheels[i],R3PartKind.Wheel,R4WeaponKind.Basic,"wheel/"+i,tier);
            GetComponent<RoboRougelike.R6.R6VehicleDrive>()?.RefreshParts();
        }
        public bool EquipBattery() => EquipBattery(1);
        public bool EquipBattery(int tier)
        {
            if(tier<1||tier>3||packageBattery!=null||IsDefeated||weaponBlock==null)return false;
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.name="Bateria przeciwnika +100";
            item.transform.SetParent(transform,false);item.transform.localPosition=new Vector3(0,1.65f,-1.65f);
            item.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
            item.GetComponent<Renderer>().sharedMaterial=weaponBlock.GetComponent<Renderer>().sharedMaterial;
            packageBatteryState=new R3PartState(new Vector3Int(0,3,-3),R3PartKind.Battery)
                {Id=EquipmentInstanceId("battery"),Source=name+" / bateria",Tier=tier};
            RoboRougelike.WeaponIntegration.RRLEnemyDeviceMount.Attach(item,transform,packageBatteryState);
            packageBattery=item.AddComponent<P12PipelineSafeDamageable>();packageBattery.Configure(100*R3VehicleModel.TierHealth(tier),true);
            packageBattery.SetBaseTint(new Color(.2f,.9f,.35f));item.AddComponent<VehicleEnergyBattery>().Configure(packageBatteryState);
            GetComponent<RoboRougelike.R6.R6VehicleDrive>()?.RefreshParts();
            GetComponent<VehicleEnergyBank>()?.Refresh();return true;
        }
        public bool EquipDirectionalShield()
        {
            if(packageShield!=null||IsDefeated)return false;
            var state=new R3PartState(new Vector3Int(-1,3,3),R3PartKind.DirectionalShield)
                {Id=EquipmentInstanceId("shield"),Source=name+" / generator",RotationQuarterTurns=new Vector3Int(0,2,0)};
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.name="DEF001 mounted shield";
            item.transform.SetParent(transform,false);item.transform.localPosition=(Vector3)state.Cell*.55f;
            item.transform.localRotation=R3MountingGrid.Rotation(state.RotationQuarterTurns);
            item.GetComponent<Renderer>().sharedMaterial=weaponMaterial!=null?weaponMaterial:weaponBlock.GetComponent<Renderer>().sharedMaterial;
            item.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
            RoboRougelike.WeaponIntegration.RRLEnemyDeviceMount.Attach(item,transform,state);
            var hp=item.AddComponent<P12PipelineSafeDamageable>();hp.Configure(180,true);hp.SetBaseTint(new Color(.25f,.6f,1));
            packageShield=item.AddComponent<RoboRougelike.WeaponIntegration.RRLShieldBinding>();packageShield.Bind(state,transform);packageShieldState=state;
            GetComponent<RoboRougelike.R6.R6VehicleDrive>()?.RefreshMass();return true;
        }
        public bool EquipPointDefense()
        {
            if(packagePointDefense!=null||IsDefeated)return false;
            var state=new R3PartState(new Vector3Int(1,3,3),R3PartKind.PointDefense)
                {Id=EquipmentInstanceId("point-defense"),Source=name+" / obrona punktowa",RotationQuarterTurns=new Vector3Int(0,2,0)};
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.name="DEF002 mounted defense";
            item.transform.SetParent(transform,false);item.transform.localPosition=(Vector3)state.Cell*.55f;
            item.GetComponent<Renderer>().sharedMaterial=weaponMaterial!=null?weaponMaterial:weaponBlock.GetComponent<Renderer>().sharedMaterial;
            item.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
            RoboRougelike.WeaponIntegration.RRLEnemyDeviceMount.Attach(item,transform,state);
            var hp=item.AddComponent<P12PipelineSafeDamageable>();hp.Configure(160,true);
            packagePointDefense=item.AddComponent<RoboRougelike.WeaponIntegration.RRLPointDefenseBinding>();packagePointDefense.Bind(state,transform);packagePointDefenseState=state;return true;
        }
        public bool EquipShotgun()
        {
            if(variantGuns.Count>0||weaponBlock==null||weaponBlock.IsDead||muzzle==null)return false;
            var old=R4WeaponDefinition.Get(WeaponKind);float ds=damage/Mathf.Max(.001f,old.Damage);
            float cadence=fireInterval/Mathf.Max(.001f,old.Interval),durability=weaponBlock.MaxHp/old.Hp;
            float condition=Condition(weaponBlock);WeaponKind=R4WeaponKind.Shotgun;
            var profile=R4WeaponDefinition.Get(WeaponKind);damage=profile.Damage*ds;fireInterval=profile.Interval*cadence;
            weaponBlock.Configure(profile.Hp*durability,true);weaponBlock.RestoreHealthRatio(condition);
            var state=new RoboRougelike.R3.R3PartState(default,RoboRougelike.R3.R3PartKind.Gun)
                {Id=EquipmentInstanceId("gun"),Weapon=WeaponKind,Source=name+" / uzbrojenie"};
            packageShotgun=weaponBlock.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
            packageShotgun.Bind(state,transform,muzzle,ds,cadence);packageWeaponState=state;return true;
        }
        public bool EquipRailgun()
        {
            if(variantGuns.Count>0||weaponBlock==null||weaponBlock.IsDead||muzzle==null)return false;
            var old=R4WeaponDefinition.Get(WeaponKind);float ds=damage/Mathf.Max(.001f,old.Damage);
            float cadence=fireInterval/Mathf.Max(.001f,old.Interval),durability=weaponBlock.MaxHp/old.Hp;
            float condition=Condition(weaponBlock);WeaponKind=R4WeaponKind.Railgun;
            var profile=R4WeaponDefinition.Get(WeaponKind);damage=profile.Damage*ds;fireInterval=profile.Interval*cadence;
            weaponBlock.Configure(profile.Hp*durability,true);weaponBlock.RestoreHealthRatio(condition);
            var state=new RoboRougelike.R3.R3PartState(default,RoboRougelike.R3.R3PartKind.Gun)
                {Id=EquipmentInstanceId("gun"),Weapon=WeaponKind,Source=name+" / uzbrojenie"};
            packageRailgun=weaponBlock.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>();
            packageRailgun.Bind(state,transform,muzzle,ds,cadence);packageWeaponState=state;return true;
        }
        public bool EquipRocket()
        {
            if(variantGuns.Count>0||weaponBlock==null||weaponBlock.IsDead||muzzle==null)return false;
            var old=R4WeaponDefinition.Get(WeaponKind);float ds=damage/Mathf.Max(.001f,old.Damage);
            float cadence=fireInterval/Mathf.Max(.001f,old.Interval),durability=weaponBlock.MaxHp/old.Hp;
            float condition=Condition(weaponBlock);WeaponKind=R4WeaponKind.RocketLauncher;
            var profile=R4WeaponDefinition.Get(WeaponKind);damage=profile.Damage*ds;fireInterval=profile.Interval*cadence;
            weaponBlock.Configure(profile.Hp*durability,true);weaponBlock.RestoreHealthRatio(condition);
            var state=new RoboRougelike.R3.R3PartState(default,RoboRougelike.R3.R3PartKind.Gun)
                {Id=EquipmentInstanceId("gun"),Weapon=WeaponKind,Source=name+" / uzbrojenie"};
            packageRocket=weaponBlock.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>();
            packageRocket.Bind(state,transform,muzzle,ds,cadence);packageWeaponState=state;return true;
        }
        public bool EquipBeam()
        {
            if(variantGuns.Count>0||weaponBlock==null||weaponBlock.IsDead||muzzle==null)return false;
            var old=R4WeaponDefinition.Get(WeaponKind);float ds=damage/Mathf.Max(.001f,old.Damage);
            float cadence=fireInterval/Mathf.Max(.001f,old.Interval),durability=weaponBlock.MaxHp/old.Hp;
            float condition=Condition(weaponBlock);WeaponKind=R4WeaponKind.BeamCutter;
            var profile=R4WeaponDefinition.Get(WeaponKind);damage=profile.Damage*ds;fireInterval=profile.Interval*cadence;
            weaponBlock.Configure(profile.Hp*durability,true);weaponBlock.RestoreHealthRatio(condition);
            var state=new RoboRougelike.R3.R3PartState(default,RoboRougelike.R3.R3PartKind.Gun)
                {Id=EquipmentInstanceId("gun"),Weapon=WeaponKind,Source=name+" / uzbrojenie"};
            packageBeam=weaponBlock.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLBeamBinding>();
            packageBeam.Bind(state,transform,muzzle,ds);packageWeaponState=state;return true;
        }
        public bool EquipEmp()
        {
            if(variantGuns.Count>0||weaponBlock==null||weaponBlock.IsDead||muzzle==null)return false;
            var old=R4WeaponDefinition.Get(WeaponKind);float ds=damage/Mathf.Max(.001f,old.Damage);
            float cadence=fireInterval/Mathf.Max(.001f,old.Interval),durability=weaponBlock.MaxHp/old.Hp;
            float condition=Condition(weaponBlock);WeaponKind=R4WeaponKind.EmpEmitter;
            var profile=R4WeaponDefinition.Get(WeaponKind);damage=profile.Damage*ds;fireInterval=profile.Interval*cadence;
            weaponBlock.Configure(profile.Hp*durability,true);weaponBlock.RestoreHealthRatio(condition);
            var state=new RoboRougelike.R3.R3PartState(default,RoboRougelike.R3.R3PartKind.Gun)
                {Id=EquipmentInstanceId("gun"),Weapon=WeaponKind,Source=name+" / uzbrojenie"};
            packageEmp=weaponBlock.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLEmpBinding>();
            packageEmp.Bind(state,transform,cadence);packageWeaponState=state;preferredDistance=6;moveSpeed=Mathf.Max(moveSpeed,2.5f);return true;
        }
        public bool EquipSaw()
        {
            if(variantGuns.Count>0||weaponBlock==null||weaponBlock.IsDead||muzzle==null)return false;
            var old=R4WeaponDefinition.Get(WeaponKind);float ds=damage/Mathf.Max(.001f,old.Damage);
            float cadence=fireInterval/Mathf.Max(.001f,old.Interval),durability=weaponBlock.MaxHp/old.Hp;
            float condition=Condition(weaponBlock);WeaponKind=R4WeaponKind.RotarySaw;
            var profile=R4WeaponDefinition.Get(WeaponKind);damage=profile.Damage*ds;fireInterval=profile.Interval*cadence;
            weaponBlock.Configure(profile.Hp*durability,true);weaponBlock.RestoreHealthRatio(condition);
            var state=new RoboRougelike.R3.R3PartState(default,RoboRougelike.R3.R3PartKind.Gun)
                {Id=EquipmentInstanceId("gun"),Weapon=WeaponKind,Source=name+" / uzbrojenie"};
            packageSaw=weaponBlock.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLSawBinding>();
            packageSaw.Bind(state,transform,muzzle,ds);packageWeaponState=state;preferredDistance=1.6f;moveSpeed=Mathf.Max(moveSpeed,3.5f);strafeStrength=.1f;return true;
        }
        private R4SalvageLedger salvage;
        private string salvageId;
        private Material weaponMaterial;
        private bool snapshotTaken;
        public P12PipelineSafeDamageable WeaponBlock => variantGuns.Count>0?variantGuns[0].Block:weaponBlock;
        readonly Dictionary<string,string> equipmentIds=new Dictionary<string,string>();
        readonly string equipmentOwnerId=System.Guid.NewGuid().ToString("N");
        // A physical slot gets one canonical identity before firing/recovery.
        // Rebinding a ledger never renames already existing equipment.
        public string EquipmentInstanceId(string slot)
        {
            if(string.IsNullOrWhiteSpace(slot))throw new System.ArgumentException("Missing equipment slot");
            if(equipmentIds.TryGetValue(slot,out var existing))return existing;
            string owner=string.IsNullOrWhiteSpace(salvageId)?equipmentOwnerId:salvageId;
            using(var sha=System.Security.Cryptography.SHA256.Create())
            {
                var hash=sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes("RRL/Equipment/v1/"+owner+"/"+slot));
                var bytes=new byte[16];System.Array.Copy(hash,bytes,16);
                var value=new System.Guid(bytes).ToString("N");equipmentIds.Add(slot,value);return value;
            }
        }
        public void ConfigureSalvage(R4SalvageLedger ledger,string id)
        {
            salvage=ledger;salvageId=id;EquipmentInstanceId("gun");
            for(int i=0;i<variantGuns.Count;i++)EquipmentInstanceId("gun/"+i);
            for(int i=0;i<driveWheels.Count;i++)EquipmentInstanceId("wheel/"+i);
        }
        public R3EnemyArchetype Archetype { get; private set; }
        public int ActiveWheelCount => driveWheels.Count(p=>p!=null&&!p.IsDead&&p.transform.IsChildOf(transform)&&!RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(p.gameObject));
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
            var renderer=part.GetComponent<Renderer>();renderer.sharedMaterial=material;
            var hp=part.AddComponent<P12PipelineSafeDamageable>();hp.Configure(wheel?75:100,true);
            if(wheel)
            {
                var visual=new GameObject("Obrót koła");visual.transform.SetParent(part.transform,false);
                visual.AddComponent<MeshFilter>().sharedMesh=part.GetComponent<MeshFilter>().sharedMesh;
                visual.AddComponent<MeshRenderer>().sharedMaterial=material;
                visual.AddComponent<P12EnemyWheelVisual>().Configure(renderer,.265f);
                R4EnemyWheelArt.TryAttach(part.transform,renderer,visual.GetComponent<Renderer>(),.265f);
                renderer.enabled=false;
            }
            return hp;
        }

        public P12PipelineSafeDamageable Core => core;
        public bool IsDefeated => defeated || core == null || core.IsDead;
        bool PrimaryWeaponOnline => variantGuns.Count>0?variantGuns.Any(g=>g.Online(transform)):weaponBlock != null && !weaponBlock.IsDead &&
            turretSupport != null && !turretSupport.IsDead && yawPivot != null && muzzle != null &&
            (turretAnchor==null||!turretAnchor.IsDead&&turretAnchor.transform.IsChildOf(transform));
        public bool WeaponOnline => PrimaryWeaponOnline||additionalGuns.Any(g=>g.Online(transform));

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
            movementBody.interpolation = RigidbodyInterpolation.Interpolate;
            ConfigureVariantMovement();
            var drive=GetComponent<RoboRougelike.R6.R6VehicleDrive>();
            if(drive==null)drive=gameObject.AddComponent<RoboRougelike.R6.R6VehicleDrive>();
            drive.EnableSuspensionPhysics(.05f);drive.RefreshParts();

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
        public Transform EncounterTarget=>playerRoot;
        public void RetargetEncounter(Transform target)
        {
            if(target==null||playerRoot==target)return;
            playerRoot=target;currentTarget=null;nextTargetTime=0;
        }

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
        // Unity keeps a managed reference after a detached wreck expires.
        // A physically missing module still yields a repairable 0-HP salvage item.
        private static float SalvageDisruption(P12PipelineSafeDamageable part) =>
            part!=null?part.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>()?.RemainingSeconds??0:0;
        private void HandleCoreDeath(){CaptureSalvage();if(combatActive&&playerRoot!=null)RoboRougelike.R3.EnergyPickup.Spawn(core.transform.position);DropTurret();}
        private void CaptureSalvage()
        {
            if(snapshotTaken||salvage==null)return;snapshotTaken=true;
            foreach(var emp in GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLEmpPartState>())emp.CaptureState();
            if(packageEmp!=null)packageEmp.CaptureState();
            var items=new List<R4SalvageItem>();
            if(variantGuns.Count>0)
                for(int i=0;i<variantGuns.Count;i++)
                {
                    var gun=variantGuns[i];
                    var state=gun.Block?.GetComponent<RoboRougelike.WeaponIntegration.RRLDeviceBinding>()?.State??
                        new R3PartState(default,R3PartKind.Gun){Id=EquipmentInstanceId("gun/"+i),Source=name+" / bateria "+(i+1),Weapon=gun.Kind};
                    state.DisruptionRemaining=SalvageDisruption(gun.Block);
                    items.Add(new R4SalvageItem(state,gun.Online(transform)?Condition(gun.Block):0));
                }
            else
            {
                var state=packageWeaponState;
                if(packageSaw!=null)packageSaw.CaptureState();
                if(packageBeam!=null)packageBeam.CaptureState();
                float condition=Mathf.Min(Condition(weaponBlock),Condition(turretSupport));
                items.Add(state!=null?new R4SalvageItem(state,condition):new R4SalvageItem(new R3PartState(default,R3PartKind.Gun){Id=EquipmentInstanceId("gun"),Source=name+" / uzbrojenie",Weapon=WeaponKind,DisruptionRemaining=SalvageDisruption(weaponBlock)},condition));
            }
            for(int i=0;i<additionalGuns.Count;i++)
            {
                var gun=additionalGuns[i];
                var state=gun.Block?.GetComponent<RoboRougelike.WeaponIntegration.RRLDeviceBinding>()?.State??
                    new R3PartState(default,R3PartKind.Gun){Id=EquipmentInstanceId("extra-gun/"+i),Source=name+" / dodatkowe działo",Weapon=gun.Kind};
                state.DisruptionRemaining=SalvageDisruption(gun.Block);
                items.Add(new R4SalvageItem(state,gun.Online(transform)?Condition(gun.Block):0));
            }
            for(int i=0;i<driveWheels.Count;i++)
            {
                var wheel=driveWheels[i];
                var state=wheel?.GetComponent<RoboRougelike.WeaponIntegration.RRLDeviceBinding>()?.State??
                    new R3PartState(default,R3PartKind.Wheel){Id=EquipmentInstanceId("wheel/"+i),Source=name+" / koło "+(i+1)};
                state.DisruptionRemaining=SalvageDisruption(wheel);
                items.Add(new R4SalvageItem(state,Condition(wheel)));
            }
            if(packageBatteryState!=null)
                items.Add(new R4SalvageItem(packageBatteryState,packageBattery!=null&&packageBattery.transform.IsChildOf(transform)?Condition(packageBattery):0));
            if(packageShieldState!=null)
            {
                if(packageShield!=null)packageShield.CaptureState();
                float condition=packageShield!=null&&packageShield.transform.IsChildOf(transform)?Condition(packageShield.Device.Damageable):0;
                items.Add(new R4SalvageItem(packageShieldState,condition));
            }
            if(packagePointDefenseState!=null)
            {
                if(packagePointDefense!=null)packagePointDefense.CaptureState();
                float condition=packagePointDefense!=null&&packagePointDefense.transform.IsChildOf(transform)?Condition(packagePointDefense.Device.Damageable):0;
                items.Add(new R4SalvageItem(packagePointDefenseState,condition));
            }
            if(salvage.Record(salvageId,items,Archetype))
                RoboRougelike.R1.R1RunController.Instance?.DiscoverEnemyTechnology(items);
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
            if(packageSaw!=null)packageSaw.SetMotorRequested(false);
            if(packageBeam!=null)packageBeam.SetTriggerHeld(false);
            if (IsDefeated) { DropTurret(); defeated = true; enabled=false; return; }
            if (!combatActive) return;
            if(variantGuns.Count>0){UpdateVariantWeapons(variantGuns,"gun/");return;}
            if(additionalGuns.Count>0)UpdateVariantWeapons(additionalGuns,"extra-gun/");
            if (!PrimaryWeaponOnline)
            {
                DropTurret();
                return;
            }
            if(weaponBlock!=null&&RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(weaponBlock.gameObject))return;
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

            if (!ValidTarget(currentTarget) || Time.time >= nextTargetTime)
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

            if(packageSaw!=null){packageSaw.SetMotorRequested(!FriendlyInLine(aimPoint));return;}
            if(packageBeam!=null)
            {packageBeam.SetTriggerHeld(!FriendlyInLine(aimPoint));return;}
            if(packageEmp!=null&&distance>8.5f)return;
            if (Time.time <
                nextFireTime)
                return;
            // Do not shoot through another vehicle from the same encounter.
            if(FriendlyInLine(aimPoint))return;

            if(packageEmp==null&&packageRocket==null&&packageRailgun==null&&packageShotgun==null)
            {var energy=GetComponent<VehicleEnergyBank>();if(energy!=null&&!energy.TrySpend(VehicleEnergyCosts.ConventionalShot(WeaponKind)*R3VehicleModel.TierActivationCost(packageWeaponState?.Tier??1)))return;}

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

        private Bounds movementBounds=new Bounds(Vector3.zero,new Vector3(76,100,76));
        public Bounds MovementBounds=>movementBounds;
        public void ConfigureMovementBounds(Bounds bounds)
        {
            if(!(bounds.size.x>0&&bounds.size.z>0)||float.IsNaN(bounds.center.x)||float.IsNaN(bounds.center.z)||
                float.IsInfinity(bounds.center.x)||float.IsInfinity(bounds.center.z)||float.IsInfinity(bounds.size.x)||float.IsInfinity(bounds.size.z))
                throw new System.ArgumentException("Invalid encounter movement bounds");
            movementBounds=bounds;
        }
        public static bool ShouldRam(bool weaponOnline,R3EnemyArchetype archetype,float distance,float cycleTime)
        {
            return !weaponOnline||archetype==R3EnemyArchetype.Bruiser||distance<8f||Mathf.Repeat(cycleTime,12f)<5f;
        }        private void FixedUpdate()
        {
            var drive=GetComponent<RoboRougelike.R6.R6VehicleDrive>();
            if(drive!=null)drive.StepWorld(Vector3.zero,0);
            if (!combatActive || IsDefeated || movementBody == null || playerRoot == null || moveSpeed <= 0) return;
            Vector3 toward = Vector3.ProjectOnPlane(playerRoot.position - movementBody.position, Vector3.up);
            float distance = toward.magnitude;
            if (distance < 0.01f) return;
            toward /= distance;
            // A mobile enemy with no working gun can still physically ram the
            // player. Keep the original R6 wheel solver and ally/world avoidance.
            bool ramming=ShouldRam(WeaponOnline,Archetype,distance,Time.time+Mathf.Abs(GetInstanceID()%997)*.033f);
            if(profiled&&Time.time>=nextStrafeChange&&Time.time>=navigationUntil)
            { strafeDirection=-strafeDirection;nextStrafeChange=Time.time+(Archetype==R3EnemyArchetype.Flanker?2:4.5f); }
            Vector3 sideways = Vector3.Cross(Vector3.up, toward) * strafeDirection;
            float approach = ramming?1:Mathf.Clamp((distance - preferredDistance) / 3, -1, 1);
            Vector3 desired = (toward * approach + sideways * (ramming?0:strafeStrength)).normalized;
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
            if (!CanMove(desired, clearance,ramming))
            {
                // Keep a wall-parallel escape direction across periodic combat strafing changes.
                var normal=lastMoveNormal;
                var tangent=Time.time<navigationUntil?obstacleTangent:Vector3.Cross(Vector3.up,normal).normalized;
                if(Time.time>=navigationUntil&&Vector3.Dot(tangent,desired)<0)tangent=-tangent;
                if(!CanMove(tangent,step+.08f,ramming))
                {
                    tangent=Vector3.Cross(Vector3.up,normal).normalized;
                    if(Vector3.Dot(tangent,desired)<0)tangent=-tangent;
                    if(!CanMove(tangent,step+.08f,ramming))tangent=-tangent;
                }
                desired=CanMove(tangent,step+.08f,ramming)?tangent:normal;
                obstacleTangent=desired;navigationUntil=Time.time+1.1f;
            }
            else if(Time.time<navigationUntil&&CanMove(obstacleTangent,step+.08f,ramming))desired=obstacleTangent;
            Vector3 next = movementBody.position + desired * step;
            bool cutX=(next.x<movementBounds.min.x&&desired.x<0)||(next.x>movementBounds.max.x&&desired.x>0);
            bool cutZ=(next.z<movementBounds.min.z&&desired.z<0)||(next.z>movementBounds.max.z&&desired.z>0);
            if(cutX)desired.x=0;
            if(cutZ)desired.z=0;
            if(ramming&&(cutX||cutZ)&&desired.sqrMagnitude<0.001f)
            {
                // A target outside the arena can otherwise make an unarmed robot
                // alternate between driving outward and back toward the centre.
                // Retain an unobstructed boundary tangent using the existing R6 drive.
                if(TryBoundaryTangent(cutX,cutZ,step,ramming,out var tangent))
                {
                    desired=tangent;obstacleTangent=tangent;navigationUntil=Time.time+1.1f;
                }
                else
                {
                    // If both sides are obstructed, keep backing away long enough
                    // to clear the obstacle instead of immediately ramming outward.
                    var inward=Vector3.ProjectOnPlane(movementBounds.center-movementBody.position,Vector3.up).normalized;
                    if(inward.sqrMagnitude<.001f||!CanMove(inward,step+.08f,ramming))return;
                    desired=inward;obstacleTangent=inward;navigationUntil=Time.time+2.5f;
                }
            }
            if(desired.sqrMagnitude<0.001f)desired=Vector3.ProjectOnPlane(movementBounds.center-movementBody.position,Vector3.up).normalized;
            desired.Normalize();
            next=movementBody.position+desired*step;
            bool outwardX=(next.x<movementBounds.min.x&&desired.x<0)||(next.x>movementBounds.max.x&&desired.x>0);
            bool outwardZ=(next.z<movementBounds.min.z&&desired.z<0)||(next.z>movementBounds.max.z&&desired.z>0);
            if(ramming&&(outwardX||outwardZ))
            {
                if(!TryBoundaryTangent(outwardX,outwardZ,step,ramming,out var safeTangent))return;
                desired=safeTangent;obstacleTangent=safeTangent;navigationUntil=Time.time+1.1f;
            }
            if (!CanMove(desired, step + 0.08f,ramming)) return;
            next=movementBody.position+desired*step;
            next.x=Mathf.Clamp(next.x,movementBounds.min.x,movementBounds.max.x);next.z=Mathf.Clamp(next.z,movementBounds.min.z,movementBounds.max.z);
            if(drive!=null)drive.StepToward(desired,moveSpeed * Mathf.Lerp(0.3f,1,wheelFactor),turnSpeed);
        }

        private bool TryBoundaryTangent(bool cutX,bool cutZ,float step,bool ramming,out Vector3 tangent)
        {
            // Pick a stable side and try the opposite side when terrain blocks it.
            // At a corner also try the other axis; never direct the wheels outward.
            for(int axis=0;axis<2;axis++)
            {
                if((axis==0&&!cutX)||(axis==1&&!cutZ))continue;
                Vector3 direction=axis==0?Vector3.forward:Vector3.right;
                float offset=axis==0?movementBody.position.z-movementBounds.center.z:movementBody.position.x-movementBounds.center.x;
                float previous=Vector3.Dot(obstacleTangent,direction);
                if(previous<-.5f||(Mathf.Abs(previous)<=.5f&&
                    (offset>0.001f||(Mathf.Abs(offset)<=0.001f&&strafeDirection<0))))direction=-direction;
                for(int side=0;side<2;side++,direction=-direction)
                {
                    Vector3 candidate=movementBody.position+direction*step;
                    if((candidate.x<movementBounds.min.x&&direction.x<0)||(candidate.x>movementBounds.max.x&&direction.x>0)||
                       (candidate.z<movementBounds.min.z&&direction.z<0)||(candidate.z>movementBounds.max.z&&direction.z>0))continue;
                    if(!CanMove(direction,step+.08f,ramming))continue;
                    tangent=direction;return true;
                }
            }
            tangent=Vector3.zero;return false;
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

        private bool CanMove(Vector3 direction, float distance,bool allowPlayerContact=false)
        {
            int count = Physics.BoxCastNonAlloc(movementBody.position + Vector3.up * 0.75f,
                movementHalfExtents, direction, movementHits, Quaternion.identity, distance,
                ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = movementHits[i].collider;
                if (hit == null || hit.transform.IsChildOf(transform) ||
                    hit.GetComponent<P17PhysicalShell>() != null) continue;
                if(allowPlayerContact&&playerRoot!=null&&hit.transform.IsChildOf(playerRoot))continue;
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

        private bool ValidTarget(Collider candidate)
        {
            if(candidate==null||!candidate.enabled||!candidate.gameObject.activeInHierarchy||
                playerRoot==null||!candidate.transform.IsChildOf(playerRoot))return false;
            var health=candidate.GetComponent<P12PipelineSafeDamageable>();
            return health==null||!health.IsDead;
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

                if (ValidTarget(collider))
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
                    ValidTarget(collider))
                {
                    blockTargets.Add(collider);
                }
            }

            if(blockTargets.Count==0)
                foreach(var hp in playerRoot.GetComponentsInChildren<P12PipelineSafeDamageable>())
                {
                    var collider=hp.GetComponent<Collider>();
                    if(!hp.IsDead&&ValidTarget(collider))blockTargets.Add(collider);
                }
            if(blockTargets.Count==0)return null;

            return blockTargets[
                Random.Range(0, blockTargets.Count)
            ];
        }

        private void FirePhysicalShell(
            Vector3 aimPoint)
        {
            if(packageEmp!=null){packageEmp.Fire();return;}
            if(packageRocket!=null){packageRocket.Fire();return;}
            if(packageRailgun!=null){packageRailgun.Fire();return;}
            if(packageShotgun!=null){packageShotgun.Fire();return;}
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
            var equipmentOrigin=shell.AddComponent<R4ProjectileOrigin>();
            equipmentOrigin.Weapon=WeaponKind;equipmentOrigin.PartId=EquipmentInstanceId("gun");

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
                damage*R3VehicleModel.TierEffectUnits(packageWeaponState?.Tier??1),
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
        private readonly List<VariantGun> additionalGuns=new List<VariantGun>();
        public IReadOnlyList<VariantGun> AdditionalWeapons=>additionalGuns.AsReadOnly();
        private float configuredProjectileSpeed=1;
        private readonly List<Material> variantMaterials=new List<Material>();
        public IReadOnlyList<VariantGun> VariantWeapons => variantGuns.AsReadOnly();
        public R5EnemyVariant Variant { get; private set; }

        public bool EquipAdditionalGun()
        {
            if(Variant!=R5EnemyVariant.Normal||additionalGuns.Count>0||core==null)return false;
            var gun=GameObject.CreatePrimitive(PrimitiveType.Cube);gun.name="Dodatkowe działo przeciwnika";
            gun.transform.SetParent(transform,false);gun.transform.localPosition=new Vector3(-.8f,1f,-.6f);
            gun.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
            var material=core.GetComponent<Renderer>().sharedMaterial;gun.GetComponent<Renderer>().sharedMaterial=material;
            var state=new R3PartState(default,R3PartKind.Gun){Weapon=R4WeaponKind.Automatic};
            RoboRougelike.WeaponIntegration.RRLEnemyDeviceMount.Attach(gun,transform,state);
            var support=gun.transform.parent.GetComponent<P12PipelineSafeDamageable>();
            if(support==null){Destroy(gun);return false;}
            var hp=gun.AddComponent<P12PipelineSafeDamageable>();hp.Configure(R4WeaponDefinition.Get(state.Weapon).Hp,true);
            var pivot=R41PartGeometry.Payload(gun.transform,R3PartKind.Gun,state.Weapon,material,true,out var barrel);
            var block=gun.AddComponent<P12_PipelineSafePlayerBlock>();
            var module=gun.AddComponent<P12PipelineSafeFunctionalModule>();module.Configure(P12FunctionalModuleType.Weapon,0,block,pivot,transform);
            additionalGuns.Add(new VariantGun{Block=hp,Support=support,Kind=state.Weapon,Pivot=pivot,Muzzle=barrel,NextShot=Time.time+2.2f});
            return true;
        }
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
                var art=R4WeaponArt.TryAttach(gun.transform,pivot,barrel,kind);
                if(art!=null)barrel=art.Muzzle;
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
        private void UpdateVariantWeapons(List<VariantGun> guns,string idPrefix)
        {
            if(playerRoot==null)return;
            if(!ValidTarget(currentTarget)||Time.time>=nextTargetTime){currentTarget=SelectTargetCollider();nextTargetTime=Time.time+.8f;}
            if(currentTarget==null)return;
            var point=currentTarget.bounds.center;
            foreach(var gun in guns)
            {
                if(!gun.Online(transform)||RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(gun.Block.gameObject))continue;
                var delta=point-gun.Pivot.position;
                gun.Pivot.rotation=Quaternion.RotateTowards(gun.Pivot.rotation,Quaternion.LookRotation(delta),85*Time.deltaTime);
                gun.Block.GetComponent<R4WeaponArt>()?.Sync();
                if(delta.magnitude>maxRange||Vector3.Angle(gun.Pivot.forward,delta)>9||Time.time<gun.NextShot)continue;
                var ray=point-gun.Muzzle.position;
                int count=Physics.RaycastNonAlloc(gun.Muzzle.position,ray.normalized,movementHits,ray.magnitude,~0,QueryTriggerInteraction.Ignore);
                bool ally=false;for(int i=0;i<count;i++){var other=movementHits[i].collider.GetComponentInParent<P12PipelineSafeEnemyTurret>();if(other!=null&&other!=this&&!other.IsDefeated)ally=true;}
                if(ally)continue;
                var energy=GetComponent<VehicleEnergyBank>();
                int tier=gun.Block.GetComponent<RoboRougelike.WeaponIntegration.RRLDeviceBinding>()?.State?.Tier??1;
                if(energy!=null&&!energy.TrySpend(VehicleEnergyCosts.ConventionalShot(gun.Kind)*R3VehicleModel.TierActivationCost(tier)))continue;
                var profile=R4WeaponDefinition.Get(gun.Kind);
                RoboRougelike.R6.R6CombatFeedback.Muzzle(gun.Muzzle,gun.Kind);
                gun.NextShot=Time.time+profile.Interval*(fireInterval/R4WeaponDefinition.Get(WeaponKind).Interval);
                var direction=(point-gun.Muzzle.position).normalized;
                var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="P17_EnemyPhysicalShell";
                shell.transform.position=gun.Muzzle.position+direction*.25f;shell.transform.localScale=Vector3.one*profile.Diameter;
                shell.GetComponent<Renderer>().sharedMaterial=runtimeProjectileMaterial;
                var body=shell.AddComponent<Rigidbody>();body.mass=profile.ProjectileMass;body.useGravity=false;
                body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearVelocity=direction*profile.Speed*.8f*configuredProjectileSpeed;
                var origin=shell.AddComponent<R4ProjectileOrigin>();origin.Weapon=gun.Kind;origin.PartId=EquipmentInstanceId(idPrefix+guns.IndexOf(gun));
                shell.AddComponent<P17PhysicalShell>().Configure(transform,profile.Damage*(damage/R4WeaponDefinition.Get(WeaponKind).Damage)*R3VehicleModel.TierEffectUnits(tier),6,direction,playerRoot);
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
