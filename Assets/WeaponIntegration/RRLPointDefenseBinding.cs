using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.PointDefense;

namespace RoboRougelike.WeaponIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLPointDefenseBinding : MonoBehaviour
    {
        public RRLDeviceBinding Device {get;private set;}
        public PointDefenseController Controller {get;private set;}
        public R3PartState State=>Device.State;
        public void Bind(R3PartState state,Transform owner)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();Device.Bind(state,owner);
            if(state.Ammo<0)state.Ammo=40;
            var pivot=new GameObject("DEF002 turret").transform;pivot.SetParent(transform,false);pivot.localPosition=new Vector3(.275f,.48f,.275f);
            var barrel=GameObject.CreatePrimitive(PrimitiveType.Cube);barrel.name="DEF002 visual barrel";barrel.transform.SetParent(pivot,false);
            barrel.transform.localPosition=Vector3.forward*.32f;barrel.transform.localScale=new Vector3(.12f,.12f,.6f);
            var col=barrel.GetComponent<Collider>();col.enabled=false;Destroy(col);barrel.GetComponent<Renderer>().sharedMaterial=GetComponent<Renderer>().sharedMaterial;
            var muzzle=new GameObject("DEF002 muzzle").transform;muzzle.SetParent(pivot,false);muzzle.localPosition=Vector3.forward*.7f;
            var template=GameObject.CreatePrimitive(PrimitiveType.Sphere);template.name="DEF002 inactive interceptor template";template.SetActive(false);template.transform.SetParent(transform,false);template.transform.localScale=Vector3.one*.10f;
            var collider=template.GetComponent<Collider>();collider.enabled=false;Destroy(collider);template.GetComponent<Renderer>().sharedMaterial=GetComponent<Renderer>().sharedMaterial;
            var prefab=template.AddComponent<PointDefenseInterceptor>();
            Controller=gameObject.AddComponent<PointDefenseController>();Controller.SetAutomaticSimulation(false);
            // Explicit live balance: two hits against the original 12-HP rocket, limited magazine.
            Controller.Configure(new PointDefenseConfiguration{useAmmunition=true,startingAmmunition=state.Ammo,interceptorDamage=6},prefab);
            Controller.Bind(Device.Body,owner,pivot,muzzle,RRLHitRouter.Team(owner));
            Controller.RestoreState(state.Ammo,Mathf.Max(0,state.DeviceReadyAt-Time.time));
            Device.Damageable.Died+=Stop;Refresh();
        }
        void Refresh()
        {
            if(State.ReloadReadyAt>0&&Time.time>=State.ReloadReadyAt){Controller.RestoreState(40,Controller.CooldownRemaining);State.Ammo=40;State.ReloadReadyAt=0;}
            Controller.SetOperational(Device.Available&&State.ReloadReadyAt<=Time.time);
        }
        void FixedUpdate(){Refresh();Controller.Advance(Time.fixedDeltaTime);CaptureState();}
        public void CaptureState(){if(Controller==null||Device?.State==null)return;State.Ammo=Controller.Ammunition;State.DeviceReadyAt=Time.time+Controller.CooldownRemaining;}
        public bool BeginReload(){Refresh();if(!Device.Available||Controller.Ammunition>=40||State.ReloadReadyAt>0)return false;State.ReloadReadyAt=Time.time+3;Controller.SetOperational(false);return true;}
        void Update(){Refresh();if(Device.Owner.GetComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>()!=null&&Device.Available&&Controller.Ammunition==0)BeginReload();}
        void Stop(){if(Controller!=null)Controller.SetOperational(false);CaptureState();}
        void OnDisable()=>Stop();
        void OnDestroy(){if(Device?.Damageable!=null)Device.Damageable.Died-=Stop;}
    }
}
