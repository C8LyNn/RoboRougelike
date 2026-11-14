using System.Collections.Generic;
using RoboRougelike.R1;
using RoboRougelike.R6;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.R3
{
    // A static, damageable surface remains terrain for the real wheel solver.
    public sealed class R3DeployableSurface : MonoBehaviour,IRRLTeamMember
    {
        public int TeamId {get;private set;}
        public void Configure(int team)=>TeamId=team;
    }
    public sealed class R3JumpPad : MonoBehaviour
    {
        readonly Dictionary<Rigidbody,HashSet<Collider>> contacts=new Dictionary<Rigidbody,HashSet<Collider>>();
        float launchSpeed;
        BoxCollider sensor;
        public int ActivationCount {get;private set;}
        public Rigidbody LastActivatedBody {get;private set;}
        public void Configure(float speed,BoxCollider trigger){launchSpeed=speed;sensor=trigger;}
        void OnTriggerEnter(Collider other)
        {
            var body=other!=null?other.attachedRigidbody:null;
            if(body==null||body.isKinematic||body.GetComponent<R1VehicleState>()==null&&body.GetComponent<P12PipelineSafeEnemyTurret>()==null)return;
            if(contacts.TryGetValue(body,out var previous))
            {
                previous.RemoveWhere(c=>c==null||!c.enabled||!c.gameObject.activeInHierarchy||c.attachedRigidbody!=body||sensor==null||!c.bounds.Intersects(sensor.bounds));
                if(previous.Count==0)contacts.Remove(body);
            }
            if(!contacts.TryGetValue(body,out var colliders))
            {
                contacts.Add(body,colliders=new HashSet<Collider>());
                ActivationCount++;LastActivatedBody=body;
                body.AddForce(Vector3.up*launchSpeed,ForceMode.VelocityChange);
            }
            colliders.Add(other);
        }
        void OnTriggerExit(Collider other)
        {
            var body=other!=null?other.attachedRigidbody:null;
            if(body==null||!contacts.TryGetValue(body,out var colliders))return;
            colliders.Remove(other);if(colliders.Count==0)contacts.Remove(body);
        }
    }
    public sealed class R3DeployableService : MonoBehaviour
    {
        public const string RampPool="deployable-ramp";
        public const string WallPool="deployable-wall",JumpPadPool="deployable-jump-pad",BlastTrapPool="deployable-blast-trap",EmpTrapPool="deployable-emp-trap",RepelTrapPool="deployable-repel-trap",DamageTrapPool="deployable-damage-trap",SlowTrapPool="deployable-slow-trap",ImmobilizeTrapPool="deployable-immobilize-trap",FlyingDronePool="deployable-flying-drone",GroundDronePool="deployable-ground-drone",OffensiveTurretPool="deployable-offensive-turret",HealingTurretPool="deployable-healing-turret",SupportTurretPool="deployable-support-turret",ShieldTurretPool="deployable-shield-turret";
        static readonly R3PartKind[] Kinds={R3PartKind.RampDeployer,R3PartKind.WallDeployer,R3PartKind.JumpPadDeployer,R3PartKind.BlastTrapDeployer,R3PartKind.EmpTrapDeployer,R3PartKind.RepelTrapDeployer,R3PartKind.DamageTrapDeployer,R3PartKind.SlowTrapDeployer,R3PartKind.ImmobilizeTrapDeployer,R3PartKind.FlyingDroneGenerator,R3PartKind.GroundDroneGenerator,R3PartKind.OffensiveTurretGenerator,R3PartKind.HealingTurretGenerator,R3PartKind.SupportTurretGenerator,R3PartKind.ShieldTurretGenerator};
        R3VehicleAssembly assembly;
        VehicleEnergyBank energy;
        Rigidbody body;
        bool poolsInitialized;
        readonly List<GameObject> spawned=new List<GameObject>();
        public void Configure(R3VehicleAssembly owner)
        {assembly=owner;energy=GetComponent<VehicleEnergyBank>();body=GetComponent<Rigidbody>();energy?.Refresh();}
        bool PlayerCanUse()
        {
            var run=R1RunController.Instance;
            return assembly!=null&&energy!=null&&body!=null&&run!=null&&run.Player!=null&&
                run.Player.gameObject==gameObject&&run.CanUseMovement;
        }
        bool Ready(R3MountedPart part,R3PartKind kind)=>part!=null&&part.Kind==kind&&part.Operational&&
            part.gameObject.activeInHierarchy&&part.transform.IsChildOf(transform)&&!RRLEmpPartState.IsBlocked(part.gameObject);
        static string Pool(R3PartKind kind)=>kind==R3PartKind.RampDeployer?RampPool:kind==R3PartKind.WallDeployer?WallPool:
            kind==R3PartKind.JumpPadDeployer?JumpPadPool:kind==R3PartKind.BlastTrapDeployer?BlastTrapPool:kind==R3PartKind.EmpTrapDeployer?EmpTrapPool:kind==R3PartKind.RepelTrapDeployer?RepelTrapPool:kind==R3PartKind.DamageTrapDeployer?DamageTrapPool:kind==R3PartKind.SlowTrapDeployer?SlowTrapPool:kind==R3PartKind.ImmobilizeTrapDeployer?ImmobilizeTrapPool:kind==R3PartKind.FlyingDroneGenerator?FlyingDronePool:kind==R3PartKind.GroundDroneGenerator?GroundDronePool:kind==R3PartKind.OffensiveTurretGenerator?OffensiveTurretPool:kind==R3PartKind.HealingTurretGenerator?HealingTurretPool:kind==R3PartKind.SupportTurretGenerator?SupportTurretPool:ShieldTurretPool;
        float ReadyAt(R3PartKind kind)=>kind==R3PartKind.RampDeployer?assembly.Model.RampDeployerReadyAt:
            kind==R3PartKind.WallDeployer?assembly.Model.WallDeployerReadyAt:kind==R3PartKind.JumpPadDeployer?assembly.Model.JumpPadDeployerReadyAt:
            kind==R3PartKind.BlastTrapDeployer?assembly.Model.BlastTrapDeployerReadyAt:kind==R3PartKind.EmpTrapDeployer?assembly.Model.EmpTrapDeployerReadyAt:kind==R3PartKind.RepelTrapDeployer?assembly.Model.RepelTrapDeployerReadyAt:kind==R3PartKind.DamageTrapDeployer?assembly.Model.DamageTrapDeployerReadyAt:kind==R3PartKind.SlowTrapDeployer?assembly.Model.SlowTrapDeployerReadyAt:kind==R3PartKind.ImmobilizeTrapDeployer?assembly.Model.ImmobilizeTrapDeployerReadyAt:kind==R3PartKind.FlyingDroneGenerator?assembly.Model.FlyingDroneReadyAt:kind==R3PartKind.GroundDroneGenerator?assembly.Model.GroundDroneReadyAt:kind==R3PartKind.OffensiveTurretGenerator?assembly.Model.OffensiveTurretReadyAt:kind==R3PartKind.HealingTurretGenerator?assembly.Model.HealingTurretReadyAt:kind==R3PartKind.SupportTurretGenerator?assembly.Model.SupportTurretReadyAt:assembly.Model.ShieldTurretReadyAt;
        void SetReadyAt(R3PartKind kind,float value)
        {if(kind==R3PartKind.RampDeployer)assembly.Model.RampDeployerReadyAt=value;else if(kind==R3PartKind.WallDeployer)assembly.Model.WallDeployerReadyAt=value;
         else if(kind==R3PartKind.JumpPadDeployer)assembly.Model.JumpPadDeployerReadyAt=value;else if(kind==R3PartKind.BlastTrapDeployer)assembly.Model.BlastTrapDeployerReadyAt=value;else if(kind==R3PartKind.EmpTrapDeployer)assembly.Model.EmpTrapDeployerReadyAt=value;else if(kind==R3PartKind.RepelTrapDeployer)assembly.Model.RepelTrapDeployerReadyAt=value;else if(kind==R3PartKind.DamageTrapDeployer)assembly.Model.DamageTrapDeployerReadyAt=value;else if(kind==R3PartKind.SlowTrapDeployer)assembly.Model.SlowTrapDeployerReadyAt=value;else if(kind==R3PartKind.ImmobilizeTrapDeployer)assembly.Model.ImmobilizeTrapDeployerReadyAt=value;else if(kind==R3PartKind.FlyingDroneGenerator)assembly.Model.FlyingDroneReadyAt=value;else if(kind==R3PartKind.GroundDroneGenerator)assembly.Model.GroundDroneReadyAt=value;else if(kind==R3PartKind.OffensiveTurretGenerator)assembly.Model.OffensiveTurretReadyAt=value;else if(kind==R3PartKind.HealingTurretGenerator)assembly.Model.HealingTurretReadyAt=value;else if(kind==R3PartKind.SupportTurretGenerator)assembly.Model.SupportTurretReadyAt=value;else assembly.Model.ShieldTurretReadyAt=value;}
        void EnsurePools()
        {
            if(poolsInitialized||assembly==null)return;
            foreach(var part in assembly.Parts)if(R3VehicleModel.IsDeployableSlotPart(part.Kind))
                assembly.Model.Charges.EnsurePool(Pool(part.Kind),System.Math.Max(0,assembly.Model.Economy.DeviceBaseCharges));
            poolsInitialized=true;
        }
        void Update(){if(PlayerCanUse())EnsurePools();}
        bool HasGenerator(R3PartKind kind){foreach(var part in assembly.Parts)if(Ready(part,kind))return true;return false;}
        public bool CanRecharge()
        {
            if(assembly==null)return false;
            foreach(var kind in Kinds)if(HasGenerator(kind)&&assembly.Model.Charges.Current(Pool(kind))<assembly.Model.Charges.Maximum(Pool(kind)))return true;
            return false;
        }
        public void Recharge(int amount)
        {if(assembly!=null)foreach(var kind in Kinds)if(HasGenerator(kind))assembly.Model.Charges.Recharge(Pool(kind),amount);}
        public string Status=>StatusFor(R3PartKind.RampDeployer);
        public string StatusFor(R3PartKind kind)
        {
                if(assembly==null||!R3VehicleModel.IsDeployableSlotPart(kind)||!HasGenerator(kind))return "niedostępny";
                if(PlayerCanUse())EnsurePools();
                float remaining=ReadyAt(kind)-Time.time;
                if(remaining>0)return $"odnowienie {Mathf.CeilToInt(remaining)} s";
                string pool=Pool(kind);
                int current=assembly.Model.Charges.Current(pool),maximum=assembly.Model.Charges.Maximum(pool);
                if(current==0)return "brak ładunków";
                if(kind==R3PartKind.FlyingDroneGenerator||kind==R3PartKind.GroundDroneGenerator)
                {
                    long units=0;foreach(var part in assembly.Parts)if(Ready(part,kind))units+=R3VehicleModel.TierEffectUnits(part.Tier);
                    int count=0;foreach(var item in spawned)if(item!=null&&item.TryGetComponent<R3FlyingDrone>(out var d)&&!d.IsTurret&&d.IsGround==(kind==R3PartKind.GroundDroneGenerator))count++;
                    if(count>=units*System.Math.Max(0,assembly.Model.Economy.DroneActivePerUnit))return "limit aktywnych dronów";
                }
                float cost=0;foreach(var part in assembly.Parts)if(Ready(part,kind))cost+=EnergyPerCopy(kind)*R3VehicleModel.TierActivationCost(part.Tier);
                if(energy==null||!energy.CanSpend(cost))return "brak energii";
                return kind!=R3PartKind.FlyingDroneGenerator&&kind!=R3PartKind.GroundDroneGenerator&&(body==null||body.linearVelocity.sqrMagnitude<.25f)?$"{current}/{maximum} • wymagany ruch":$"gotowy • {current}/{maximum}";
        }
        float EnergyPerCopy(R3PartKind kind)=>kind==R3PartKind.RampDeployer?assembly.Model.Economy.RampEnergyPerCopy:
            kind==R3PartKind.WallDeployer?assembly.Model.Economy.WallEnergyPerCopy:kind==R3PartKind.JumpPadDeployer?assembly.Model.Economy.JumpPadEnergyPerCopy:(kind==R3PartKind.FlyingDroneGenerator||kind==R3PartKind.GroundDroneGenerator)?assembly.Model.Economy.DroneEnergyPerCopy:(kind==R3PartKind.OffensiveTurretGenerator||kind==R3PartKind.HealingTurretGenerator||kind==R3PartKind.SupportTurretGenerator||kind==R3PartKind.ShieldTurretGenerator)?assembly.Model.Economy.TurretEnergyPerCopy:assembly.Model.Economy.TrapEnergyPerCopy;
        float Cooldown(R3PartKind kind)=>kind==R3PartKind.RampDeployer?assembly.Model.Economy.RampCooldown:
            kind==R3PartKind.WallDeployer?assembly.Model.Economy.WallCooldown:kind==R3PartKind.JumpPadDeployer?assembly.Model.Economy.JumpPadCooldown:assembly.Model.Economy.TrapCooldown;
        float Lifetime(R3PartKind kind)=>kind==R3PartKind.RampDeployer?assembly.Model.Economy.RampLifetime:
            kind==R3PartKind.WallDeployer?assembly.Model.Economy.WallLifetime:kind==R3PartKind.JumpPadDeployer?assembly.Model.Economy.JumpPadLifetime:assembly.Model.Economy.TrapLifetime;
        public bool TryDeployRamp()=>TryDeploy(R3PartKind.RampDeployer);
        public bool TryDeploy(R3PartKind kind)
        {
            if(!R3VehicleModel.IsDeployableSlotPart(kind)||!PlayerCanUse()||Time.time<ReadyAt(kind))return false;
            if(kind==R3PartKind.FlyingDroneGenerator||kind==R3PartKind.GroundDroneGenerator)return TryLaunchDrone(kind);
            if(kind==R3PartKind.OffensiveTurretGenerator||kind==R3PartKind.HealingTurretGenerator||kind==R3PartKind.SupportTurretGenerator||kind==R3PartKind.ShieldTurretGenerator)return TryPlaceTurret(kind);
            if(body.linearVelocity.sqrMagnitude<.25f)return false;
            EnsurePools();string pool=Pool(kind);if(assembly.Model.Charges.Current(pool)<1)return false;
            int units=0;float cost=0;R3MountedPart source=null;
            foreach(var part in assembly.Parts)if(Ready(part,kind))
            {
                source=part;units+=R3VehicleModel.TierEffectUnits(part.Tier);
                cost+=EnergyPerCopy(kind)*R3VehicleModel.TierActivationCost(part.Tier);
            }
            if(units<=0||source==null||float.IsNaN(cost)||float.IsInfinity(cost)||cost<0||!energy.CanSpend(cost))return false;
            Vector3 forward=Vector3.ProjectOnPlane(body.rotation*Vector3.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.5f)return false;
            Vector3 behind=-forward;float rear=0;
            foreach(var collider in GetComponentsInChildren<Collider>())
            {
                if(!collider.enabled||collider.isTrigger||collider.attachedRigidbody!=body)continue;
                var bounds=collider.bounds;var ext=bounds.extents;
                float projected=Vector3.Dot(bounds.center-body.position,behind)+Mathf.Abs(behind.x)*ext.x+Mathf.Abs(behind.z)*ext.z;
                rear=Mathf.Max(rear,projected);
            }
            bool trap=kind==R3PartKind.BlastTrapDeployer||kind==R3PartKind.EmpTrapDeployer||kind==R3PartKind.RepelTrapDeployer||kind==R3PartKind.DamageTrapDeployer||kind==R3PartKind.SlowTrapDeployer||kind==R3PartKind.ImmobilizeTrapDeployer;
            float width=(trap?1f:3f)*Mathf.Sqrt(units),length=trap?1f:kind==R3PartKind.WallDeployer?.35f:kind==R3PartKind.JumpPadDeployer?3f:4f;
            float thickness=kind==R3PartKind.WallDeployer?2f:.3f,angle=kind==R3PartKind.RampDeployer?18f:0f;
            Vector3 at=body.position+behind*(rear+length*.5f+.6f);
            var groundHits=Physics.RaycastAll(at+Vector3.up*20,Vector3.down,60,~0,QueryTriggerInteraction.Ignore);
            System.Array.Sort(groundHits,(a,b)=>a.distance.CompareTo(b.distance));
            Collider ground=null;Vector3 groundPoint=default;
            foreach(var hit in groundHits)if(R6WheelContacts.Terrain(hit.collider)&&hit.normal.y>.8f)
            {ground=hit.collider;groundPoint=hit.point;break;}
            // Do not put a ramp on a roof above the vehicle or across a deep drop.
            if(ground==null||groundPoint.y>body.position.y+.25f||groundPoint.y<body.position.y-5f)return false;
            Quaternion rotation=Quaternion.LookRotation(forward,Vector3.up)*Quaternion.Euler(angle,0,0);
            float lift=(length*Mathf.Sin(angle*Mathf.Deg2Rad)+thickness*Mathf.Cos(angle*Mathf.Deg2Rad))*.5f;
            Vector3 center=groundPoint+Vector3.up*(lift+.02f);
            foreach(var hit in Physics.OverlapBox(center,new Vector3(width,thickness,length)*.49f,rotation,~0,QueryTriggerInteraction.Ignore))
                if(hit!=null&&hit!=ground)return false;
            float lifetime=Lifetime(kind),cooldown=Cooldown(kind),launch=assembly.Model.Economy.JumpPadLaunchSpeed;
            if(float.IsNaN(lifetime)||float.IsInfinity(lifetime)||lifetime<=0||float.IsNaN(cooldown)||float.IsInfinity(cooldown)||cooldown<0)return false;
            if(kind==R3PartKind.JumpPadDeployer&&(float.IsNaN(launch)||float.IsInfinity(launch)||launch<=0))return false;
            float radius=assembly.Model.Economy.TrapTriggerRadius,damage=assembly.Model.Economy.TrapBlastDamage,duration=assembly.Model.Economy.TrapEmpDuration;
            if(trap&&(float.IsNaN(radius)||float.IsInfinity(radius)||radius<=0))return false;
            if(kind==R3PartKind.BlastTrapDeployer&&(float.IsNaN(damage)||float.IsInfinity(damage)||damage<=0||
                (double)damage*units>float.MaxValue))return false;
            if(kind==R3PartKind.EmpTrapDeployer&&(float.IsNaN(duration)||float.IsInfinity(duration)||duration<=0||
                radius>float.MaxValue/2||(double)duration*units>float.MaxValue))return false;
            float repel=assembly.Model.Economy.TrapRepelSpeed;
            if(kind==R3PartKind.RepelTrapDeployer&&(float.IsNaN(repel)||float.IsInfinity(repel)||repel<=0||(double)repel*units>float.MaxValue))return false;
            float dot=assembly.Model.Economy.TrapDamagePerSecond,fieldDuration=assembly.Model.Economy.TrapDamageDuration;
            if(kind==R3PartKind.DamageTrapDeployer&&(float.IsNaN(dot)||float.IsInfinity(dot)||dot<=0||(double)dot*units>float.MaxValue||
                float.IsNaN(fieldDuration)||float.IsInfinity(fieldDuration)||fieldDuration<=0))return false;
            float slow=assembly.Model.Economy.TrapSlowMultiplier,slowDuration=assembly.Model.Economy.TrapSlowDuration;
            if(kind==R3PartKind.SlowTrapDeployer&&(float.IsNaN(slow)||float.IsInfinity(slow)||slow<0||slow>=1||
                float.IsNaN(slowDuration)||float.IsInfinity(slowDuration)||slowDuration<=0||(double)slowDuration*units>float.MaxValue))return false;
            float immobilize=assembly.Model.Economy.TrapImmobilizeDuration;
            if(kind==R3PartKind.ImmobilizeTrapDeployer&&(float.IsNaN(immobilize)||float.IsInfinity(immobilize)||immobilize<=0||(double)immobilize*units>float.MaxValue))return false;
            if(!energy.TrySpend(cost))return false;
            assembly.Model.Charges.TrySpend(pool,1);SetReadyAt(kind,Time.time+cooldown);
            var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name=kind==R3PartKind.RampDeployer?"Rozstawiona rampa":kind==R3PartKind.WallDeployer?"Rozstawiony murek":kind==R3PartKind.JumpPadDeployer?"Rozstawiony jump pad":kind==R3PartKind.BlastTrapDeployer?"Pułapka wybuchowa":kind==R3PartKind.EmpTrapDeployer?"Pułapka EMP":kind==R3PartKind.RepelTrapDeployer?"Pułapka odrzucająca":kind==R3PartKind.DamageTrapDeployer?"Pułapka obrażeń":kind==R3PartKind.SlowTrapDeployer?"Pułapka spowalniająca":"Pułapka unieruchamiająca";
            ramp.transform.SetPositionAndRotation(center,rotation);ramp.transform.localScale=new Vector3(width,thickness,length);
            var renderer=source.GetComponent<Renderer>();if(renderer!=null)ramp.GetComponent<Renderer>().sharedMaterial=renderer.sharedMaterial;
            ramp.AddComponent<R3DeployableSurface>().Configure(RRLHitRouter.Team(transform));
            ramp.AddComponent<P12PipelineSafeDamageable>().Configure(150,false);
            if(kind==R3PartKind.JumpPadDeployer)
            {
                var trigger=ramp.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=new Vector3(0,2,0);trigger.size=new Vector3(1,4,1);
                ramp.AddComponent<R3JumpPad>().Configure(launch,trigger);
            }
            if(trap)
            {
                var sensor=new GameObject("Czujnik pułapki");sensor.transform.SetParent(ramp.transform,false);
                sensor.transform.localScale=new Vector3(1f/width,1f/thickness,1f/length);
                var trigger=sensor.AddComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=radius;
                sensor.AddComponent<R3DeployableTrap>().Configure(ramp.transform,transform,kind,radius,damage*units,duration*units,assembly.Model.Economy.TrapRepelSpeed*units,dot*units,fieldDuration,lifetime,kind==R3PartKind.ImmobilizeTrapDeployer?0:slow,(kind==R3PartKind.ImmobilizeTrapDeployer?immobilize:slowDuration)*units);
            }
            spawned.Add(ramp);if(!trap)Destroy(ramp,lifetime);return true;
        }
        bool TryPlaceTurret(R3PartKind kind)
        {
            if(body.linearVelocity.sqrMagnitude<.25f)return false;
            EnsurePools();if(assembly.Model.Charges.Current(Pool(kind))<1)return false;
            int units=0;float cost=0;R3MountedPart source=null;
            foreach(var part in assembly.Parts)if(Ready(part,kind))
            {source=part;units+=R3VehicleModel.TierEffectUnits(part.Tier);cost+=EnergyPerCopy(part.Kind)*R3VehicleModel.TierActivationCost(part.Tier);}
            var e=assembly.Model.Economy;
            if(source==null||units<=0||float.IsNaN(cost)||float.IsInfinity(cost)||cost<0||!energy.CanSpend(cost)||
                float.IsNaN(e.TurretCooldown)||float.IsInfinity(e.TurretCooldown)||e.TurretCooldown<0||
                float.IsNaN(e.TurretLifetime)||float.IsInfinity(e.TurretLifetime)||e.TurretLifetime<=0)return false;
            if(kind==R3PartKind.OffensiveTurretGenerator&&(float.IsNaN(e.TurretFireInterval)||float.IsInfinity(e.TurretFireInterval)||e.TurretFireInterval<=0||
                float.IsNaN(e.TurretDamage)||float.IsInfinity(e.TurretDamage)||e.TurretDamage<=0||(double)e.TurretDamage*units>float.MaxValue))return false;
            if(kind==R3PartKind.HealingTurretGenerator&&(float.IsNaN(e.HealingTurretRadius)||float.IsInfinity(e.HealingTurretRadius)||e.HealingTurretRadius<=0||
                float.IsNaN(e.HealingTurretRate)||float.IsInfinity(e.HealingTurretRate)||e.HealingTurretRate<=0||(double)e.HealingTurretRate*units>float.MaxValue))return false;
            if(kind==R3PartKind.ShieldTurretGenerator&&(float.IsNaN(e.ShieldTurretRadius)||float.IsInfinity(e.ShieldTurretRadius)||e.ShieldTurretRadius<=0||float.IsNaN(e.ShieldTurretHp)||float.IsInfinity(e.ShieldTurretHp)||e.ShieldTurretHp<=0||(double)e.ShieldTurretHp*units>float.MaxValue))return false; if(kind==R3PartKind.SupportTurretGenerator&&(float.IsNaN(e.SupportTurretRadius)||float.IsInfinity(e.SupportTurretRadius)||e.SupportTurretRadius<=0||
                float.IsNaN(e.SupportTurretEnergyPerSecond)||float.IsInfinity(e.SupportTurretEnergyPerSecond)||e.SupportTurretEnergyPerSecond<=0||(double)e.SupportTurretEnergyPerSecond*units>float.MaxValue))return false;
            Vector3 rear=-Vector3.ProjectOnPlane(body.rotation*Vector3.forward,Vector3.up).normalized;
            if(rear.sqrMagnitude<.5f)return false;float distance=0;
            foreach(var c in GetComponentsInChildren<Collider>())
            {
                if(!c.enabled||c.isTrigger||c.attachedRigidbody!=body)continue;
                var ext=c.bounds.extents;distance=Mathf.Max(distance,Vector3.Dot(c.bounds.center-body.position,rear)+Mathf.Abs(rear.x)*ext.x+Mathf.Abs(rear.z)*ext.z);
            }
            Vector3 point=body.position+rear*(distance+1.1f);var hits=Physics.RaycastAll(point+Vector3.up*20,Vector3.down,60,~0,QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));bool found=false;
            foreach(var hit in hits)if(R6WheelContacts.Terrain(hit.collider)&&hit.normal.y>.8f&&hit.point.y<body.position.y+.25f&&hit.point.y>body.position.y-5f)
            {point=hit.point+Vector3.up*.52f;found=true;break;}
            if(!found)return false;
            foreach(var hit in Physics.OverlapBox(point,Vector3.one*.48f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))if(hit!=null)return false;
            if(!energy.TrySpend(cost))return false;
            assembly.Model.Charges.TrySpend(Pool(kind),1);SetReadyAt(kind,Time.time+e.TurretCooldown);
            var turret=GameObject.CreatePrimitive(PrimitiveType.Cube);turret.name=kind==R3PartKind.HealingTurretGenerator?"Wieżyczka lecznicza":kind==R3PartKind.SupportTurretGenerator?"Wieżyczka wspierająca":kind==R3PartKind.ShieldTurretGenerator?"Kopuła osłonowa":"Wieżyczka ofensywna";turret.transform.position=point;
            var renderer=source.GetComponent<Renderer>();if(renderer!=null)turret.GetComponent<Renderer>().sharedMaterial=renderer.sharedMaterial;
            var rb=turret.AddComponent<Rigidbody>();rb.isKinematic=true;
            var hp=turret.AddComponent<P12PipelineSafeDamageable>();hp.Configure(kind==R3PartKind.ShieldTurretGenerator?e.ShieldTurretHp*units:100,false);
            if(kind==R3PartKind.OffensiveTurretGenerator)turret.AddComponent<R3FlyingDrone>().ConfigureTurret(transform,e.TurretFireInterval,e.TurretDamage*units);
            else if(kind==R3PartKind.HealingTurretGenerator)turret.AddComponent<R3HealingTurret>().Configure(assembly,e.HealingTurretRadius,e.HealingTurretRate*units);
            else if(kind==R3PartKind.SupportTurretGenerator)turret.AddComponent<R3SupportTurret>().Configure(assembly,e.SupportTurretRadius,e.SupportTurretEnergyPerSecond*units); else {var boundary=turret.AddComponent<SphereCollider>();boundary.isTrigger=true;turret.AddComponent<R3ShieldDome>().Configure(RRLHitRouter.Team(transform),e.ShieldTurretRadius);}
            hp.Died+=()=>{if(turret!=null)Destroy(turret);};spawned.Add(turret);Destroy(turret,e.TurretLifetime);return true;
        }
        bool TryLaunchDrone(R3PartKind kind)
        {
            EnsurePools();if(assembly.Model.Charges.Current(Pool(kind))<1)return false;
            int units=0;float cost=0;R3MountedPart source=null;
            foreach(var part in assembly.Parts)if(Ready(part,kind))
            {source=part;units+=R3VehicleModel.TierEffectUnits(part.Tier);cost+=EnergyPerCopy(part.Kind)*R3VehicleModel.TierActivationCost(part.Tier);}
            var e=assembly.Model.Economy;
            if(source==null||units<=0||e.DroneActivePerUnit<=0||float.IsNaN(cost)||float.IsInfinity(cost)||cost<0||
                !energy.CanSpend(cost)||float.IsNaN(e.DroneSpeed)||float.IsInfinity(e.DroneSpeed)||e.DroneSpeed<=0||
                float.IsNaN(e.DroneCooldown)||float.IsInfinity(e.DroneCooldown)||e.DroneCooldown<0||
                float.IsNaN(e.DroneLifetime)||float.IsInfinity(e.DroneLifetime)||e.DroneLifetime<=0||
                float.IsNaN(e.DroneFireInterval)||float.IsInfinity(e.DroneFireInterval)||e.DroneFireInterval<=0||
                float.IsNaN(e.DroneDamage)||float.IsInfinity(e.DroneDamage)||e.DroneDamage<=0)return false;
            spawned.RemoveAll(item=>item==null);
            bool ground=kind==R3PartKind.GroundDroneGenerator;
            int active=0;foreach(var item in spawned)if(item!=null&&item.TryGetComponent<R3FlyingDrone>(out var existing)&&!existing.IsTurret&&existing.IsGround==ground)active++;
            if(active>=(long)units*e.DroneActivePerUnit)return false;
            Vector3 rear=-Vector3.ProjectOnPlane(body.rotation*Vector3.forward,Vector3.up).normalized;
            if(rear.sqrMagnitude<.5f)return false;
            Vector3 position=default;bool available=false;
            for(int i=0;i<8&&!available;i++)
            {
                position=body.position+Quaternion.AngleAxis(i*45,Vector3.up)*rear*(ground?4f:3f);available=true;
                if(ground)
                {
                    var hits=Physics.RaycastAll(position+Vector3.up*20,Vector3.down,60,~0,QueryTriggerInteraction.Ignore);
                    System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));bool found=false;
                    foreach(var hit in hits)if(R6WheelContacts.Terrain(hit.collider)&&hit.normal.y>.8f&&hit.point.y<body.position.y+.25f&&hit.point.y>body.position.y-5f)
                    {position=hit.point+Vector3.up*.42f;found=true;break;}
                    if(!found){available=false;continue;}
                }
                else position+=Vector3.up*3f;
                foreach(var hit in Physics.OverlapBox(position,ground?new Vector3(.9f,.3f,.9f):Vector3.one*.4f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                    if(hit!=null&&!hit.transform.IsChildOf(transform)){available=false;break;}
            }
            if(!available)return false;
            if(!energy.TrySpend(cost))return false;
            assembly.Model.Charges.TrySpend(Pool(kind),1);SetReadyAt(kind,Time.time+e.DroneCooldown);
            var drone=GameObject.CreatePrimitive(ground?PrimitiveType.Cube:PrimitiveType.Sphere);drone.name=ground?"Dron naziemny":"Dron latający";drone.transform.position=position;
            if(ground)drone.GetComponent<BoxCollider>().size=new Vector3(.9f,.3f,1.2f);else drone.transform.localScale=Vector3.one*.8f;
            var renderer=source.GetComponent<Renderer>();if(renderer!=null)drone.GetComponent<Renderer>().sharedMaterial=renderer.sharedMaterial;
            if(ground)
            {
                // Keep the visible chassis aligned with its low physical collider.
                var visual=new GameObject("Kadłub drona");visual.transform.SetParent(drone.transform,false);
                visual.transform.localScale=new Vector3(.9f,.3f,1.2f);
                visual.AddComponent<MeshFilter>().sharedMesh=drone.GetComponent<MeshFilter>().sharedMesh;
                visual.AddComponent<MeshRenderer>().sharedMaterial=drone.GetComponent<Renderer>().sharedMaterial;
                drone.GetComponent<Renderer>().enabled=false;
            }
            var droneCollider=drone.GetComponent<Collider>();foreach(var own in GetComponentsInChildren<Collider>())if(own!=null)Physics.IgnoreCollision(droneCollider,own,true);
            var rb=drone.AddComponent<Rigidbody>();rb.mass=35;rb.useGravity=ground;rb.interpolation=RigidbodyInterpolation.Interpolate;if(!ground)rb.constraints=RigidbodyConstraints.FreezeRotation;
            var hp=drone.AddComponent<P12PipelineSafeDamageable>();hp.Configure(80,false);
            if(ground)
            {
                foreach(float x in new[]{-.6f,.6f})foreach(float z in new[]{-.45f,.45f})
                {
                    var wheel=GameObject.CreatePrimitive(PrimitiveType.Sphere);wheel.name="Koło drona";wheel.transform.SetParent(drone.transform,false);
                    wheel.transform.localPosition=new Vector3(x,-.15f,z);wheel.transform.localScale=Vector3.one*.5f;
                    if(renderer!=null)wheel.GetComponent<Renderer>().sharedMaterial=renderer.sharedMaterial;
                    var wheelCollider=wheel.GetComponent<Collider>();foreach(var own in GetComponentsInChildren<Collider>())if(own!=null)Physics.IgnoreCollision(wheelCollider,own,true);
                    wheel.AddComponent<P12PipelineSafeDamageable>().Configure(40,false);
                }
                drone.AddComponent<R6VehicleDrive>().RefreshParts();
            }
            var behaviour=drone.AddComponent<R3FlyingDrone>();behaviour.Configure(transform,e.DroneSpeed,e.DroneFireInterval,e.DroneDamage);
            hp.Died+=()=>{if(drone!=null)Destroy(drone);};spawned.Add(drone);Destroy(drone,e.DroneLifetime);return true;
        }
        void OnDestroy(){foreach(var item in spawned)if(item!=null)Destroy(item);spawned.Clear();}
    }
}
