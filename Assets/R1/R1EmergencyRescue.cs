using System;
using System.Linq;
using RoboRougelike.R6;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        // Provisional values; only the explicit manual action uses these.
        public float EmergencyRescueCooldownSeconds=5f;
        public float EmergencyRescueDurationSeconds=2f;
        float emergencyRescueReadyAt,emergencyRescueAttackReadyAt,emergencyRescueMessageUntil,garageRescueUntil;
        public float EmergencyRescueCooldownLeft=>Mathf.Max(0,emergencyRescueReadyAt-Time.time);
        public float EmergencyRescueRemaining=>Mathf.Max(0,emergencyRescueAttackReadyAt-Time.time);
        public bool EmergencyRescueActive
        {
            get
            {
                if(Progress?.State==R1RunState.Garage)return Time.unscaledTime<garageRescueUntil;
                if(Progress?.State!=R1RunState.Combat&&Progress?.State!=R1RunState.TestDrive&&Progress?.State!=R1RunState.SafeZone)return false;
                var automatic=Player!=null?Player.GetComponent<R6CoreRecovery>():null;
                return EmergencyRescueRemaining>0||(automatic!=null&&automatic.isActiveAndEnabled&&automatic.IsRecovering);
            }
        }
        public bool EmergencyRescueAttackBlocked=>EmergencyRescueActive||Progress?.State==R1RunState.SafeZone;
        public string EmergencyRescueMessage {get;private set;}
        public string EmergencyRescueNotice=>Time.unscaledTime<emergencyRescueMessageUntil?EmergencyRescueMessage:null;
        void SetRescueNotice(string text){EmergencyRescueMessage=text;emergencyRescueMessageUntil=Time.unscaledTime+3f;}
        void ResetEmergencyRescue(){emergencyRescueReadyAt=0;emergencyRescueAttackReadyAt=0;emergencyRescueMessageUntil=0;garageRescueUntil=0;EmergencyRescueMessage=null;}

        public bool TryEmergencyRescue()
        {
            if(Transitioning||Paused||QuitRequested||Player==null||Progress==null)return false;
            if(Progress.State==R1RunState.Garage)
            {
                Garage.Open();garageRescueUntil=Time.unscaledTime+Mathf.Max(0,EmergencyRescueDurationSeconds);
                Garage.Message="Pojazd ustawiony w garażu.";SetRescueNotice(Garage.Message);return true;
            }
            if(EmergencyRescueCooldownLeft>0)return false;
            if(Progress.State!=R1RunState.Combat&&Progress.State!=R1RunState.TestDrive&&Progress.State!=R1RunState.SafeZone)return false;
            var body=Player.GetComponent<Rigidbody>();var core=Player.Core;
            if(body==null||body.isKinematic||core==null||core.IsDead)return false;
            if(!TryFindRescuePose(body,out var position,out var rotation))
            {SetRescueNotice("Brak bezpiecznego miejsca do ratunku.");return false;}
            bool combat=Progress.State==R1RunState.Combat;
            Player.MoveTo(position,rotation);
            body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.WakeUp();
            Player.Controller.SetGameplayInputEnabled(true);
            emergencyRescueAttackReadyAt=Time.time+Mathf.Max(0,EmergencyRescueDurationSeconds);
            emergencyRescueReadyAt=Time.time+Mathf.Max(0,EmergencyRescueCooldownSeconds);
            SetRescueNotice(combat?"Ratunek: 50% otrzymywanych obrażeń; atak wstrzymany.":"Ratunek: niewrażliwość; atak wstrzymany.");
            return true;
        }

        bool TryFindRescuePose(Rigidbody body,out Vector3 target,out Quaternion orientation)
        {
            target=default;orientation=Quaternion.Euler(0,body.rotation.eulerAngles.y,0);
            var colliders=body.GetComponentsInChildren<Collider>().Where(c=>c!=null&&c.enabled&&!c.isTrigger&&c.attachedRigidbody==body).ToArray();
            if(colliders.Length==0)return false;
            var delta=orientation*Quaternion.Inverse(body.rotation);
            var offsets=new System.Collections.Generic.List<Vector3>{Vector3.zero};
            foreach(float distance in new[]{4f,8f,12f,20f})
                foreach(var direction in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back,
                    new Vector3(1,0,1),new Vector3(-1,0,1),new Vector3(1,0,-1),new Vector3(-1,0,-1)})offsets.Add(direction*distance);
            var anchor=Progress.State==R1RunState.SafeZone?SafeZoneSpawn:Progress.State==R1RunState.TestDrive?
                (MapEditorTesting&&EditorDraft?.Spawn!=null?RoboRougelike.MapEditor.MapWorld.Position(EditorDraft.Spawn)+Vector3.up*.7f:new Vector3(0,.7f,-26)):
                playerSpawn;
            foreach(var offset in offsets)
            {
                var origin=anchor+offset+Vector3.up*15;
                foreach(var hit in Physics.RaycastAll(origin,Vector3.down,40,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                {
                    if(!R6WheelContacts.Terrain(hit.collider)||hit.normal.y<.7f||hit.point.y>anchor.y+1)continue;
                    float lowest=float.PositiveInfinity;
                    foreach(var c in colliders)
                    {
                        var e=c.bounds.extents;var y=Quaternion.Inverse(delta)*Vector3.up;
                        float extent=Mathf.Abs(y.x)*e.x+Mathf.Abs(y.y)*e.y+Mathf.Abs(y.z)*e.z;
                        lowest=Mathf.Min(lowest,(delta*(c.bounds.center-body.position)).y-extent);
                    }
                    var candidate=new Vector3(origin.x,hit.point.y-lowest+.08f,origin.z);
                    if(!RescuePoseClear(body,colliders,delta,candidate))continue;
                    target=candidate;return true;
                }
            }
            return false;
        }
        static bool RescuePoseClear(Rigidbody body,Collider[] colliders,Quaternion delta,Vector3 target)
        {
            foreach(var c in colliders)
            {
                if(c is MeshCollider mesh&&!mesh.convex)return false;
                var center=target+delta*(c.bounds.center-body.position);
                var position=target+delta*(c.transform.position-body.position);var rotation=delta*c.transform.rotation;
                foreach(var other in Physics.OverlapSphere(center,c.bounds.extents.magnitude+.1f,~0,QueryTriggerInteraction.Ignore))
                {
                    if(other==null||other.attachedRigidbody==body||other.transform.IsChildOf(body.transform))continue;
                    if(Physics.ComputePenetration(c,position,rotation,other,other.transform.position,other.transform.rotation,out _,out float depth)&&depth>.01f)return false;
                }
            }
            return true;
        }
    }
}
