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
    public sealed class R3DeployableService : MonoBehaviour
    {
        public const string RampPool="deployable-ramp";
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
        bool Ready(R3MountedPart part)=>part!=null&&part.Kind==R3PartKind.RampDeployer&&part.Operational&&
            part.gameObject.activeInHierarchy&&part.transform.IsChildOf(transform)&&!RRLEmpPartState.IsBlocked(part.gameObject);
        void EnsurePools()
        {
            if(poolsInitialized||assembly==null)return;
            foreach(var part in assembly.Parts)if(part.Kind==R3PartKind.RampDeployer)
            {assembly.Model.Charges.EnsurePool(RampPool,System.Math.Max(0,assembly.Model.Economy.DeviceBaseCharges));break;}
            poolsInitialized=true;
        }
        void Update(){if(PlayerCanUse())EnsurePools();}
        bool HasGenerator(){foreach(var part in assembly.Parts)if(Ready(part))return true;return false;}
        public bool CanRecharge()=>assembly!=null&&HasGenerator()&&assembly.Model.Charges.Current(RampPool)<assembly.Model.Charges.Maximum(RampPool);
        public void Recharge(int amount){if(assembly!=null&&HasGenerator())assembly.Model.Charges.Recharge(RampPool,amount);}
        public string Status
        {
            get
            {
                if(assembly==null||!HasGenerator())return "niedostępny";
                if(PlayerCanUse())EnsurePools();
                float remaining=assembly.Model.RampDeployerReadyAt-Time.time;
                if(remaining>0)return $"odnowienie {Mathf.CeilToInt(remaining)} s";
                int current=assembly.Model.Charges.Current(RampPool),maximum=assembly.Model.Charges.Maximum(RampPool);
                if(current==0)return "brak ładunków";
                float cost=0;foreach(var part in assembly.Parts)if(Ready(part))cost+=assembly.Model.Economy.RampEnergyPerCopy*R3VehicleModel.TierActivationCost(part.Tier);
                if(energy==null||!energy.CanSpend(cost))return "brak energii";
                return body==null||body.linearVelocity.sqrMagnitude<.25f?$"{current}/{maximum} • wymagany ruch":$"gotowy • {current}/{maximum}";
            }
        }
        public bool TryDeployRamp()
        {
            if(!PlayerCanUse()||Time.time<assembly.Model.RampDeployerReadyAt||body.linearVelocity.sqrMagnitude<.25f)return false;
            EnsurePools();if(assembly.Model.Charges.Current(RampPool)<1)return false;
            int units=0;float cost=0;R3MountedPart source=null;
            foreach(var part in assembly.Parts)if(Ready(part))
            {source=part;units+=R3VehicleModel.TierEffectUnits(part.Tier);cost+=assembly.Model.Economy.RampEnergyPerCopy*R3VehicleModel.TierActivationCost(part.Tier);}
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
            float width=3*Mathf.Sqrt(units),length=4f,thickness=.3f,angle=18f;
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
            float lifetime=assembly.Model.Economy.RampLifetime,cooldown=assembly.Model.Economy.RampCooldown;
            if(float.IsNaN(lifetime)||float.IsInfinity(lifetime)||lifetime<=0||float.IsNaN(cooldown)||float.IsInfinity(cooldown)||cooldown<0)return false;
            if(!energy.TrySpend(cost))return false;
            assembly.Model.Charges.TrySpend(RampPool,1);assembly.Model.RampDeployerReadyAt=Time.time+cooldown;
            var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.name="Rozstawiona rampa";
            ramp.transform.SetPositionAndRotation(center,rotation);ramp.transform.localScale=new Vector3(width,thickness,length);
            var renderer=source.GetComponent<Renderer>();if(renderer!=null)ramp.GetComponent<Renderer>().sharedMaterial=renderer.sharedMaterial;
            ramp.AddComponent<R3DeployableSurface>().Configure(RRLHitRouter.Team(transform));
            ramp.AddComponent<P12PipelineSafeDamageable>().Configure(150,false);
            spawned.Add(ramp);Destroy(ramp,lifetime);return true;
        }
        void OnDestroy(){foreach(var item in spawned)if(item!=null)Destroy(item);spawned.Clear();}
    }
}
