using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    public interface IRRLDeviceLifecycle {void RefreshAvailability();}
    // Shared capability gate; persistent identity/resources remain in R3PartState.
    // Reading this never changes HP, body velocity or structural ownership.
    [DisallowMultipleComponent]
    public sealed class RRLDeviceBinding : MonoBehaviour
    {
        public R3PartState State {get;private set;}
        public Transform Owner {get;private set;}
        public Rigidbody Body {get;private set;}
        public P12PipelineSafeDamageable Damageable {get;private set;}
        R3MountedPart part;
        R1VehicleState player;
        P12PipelineSafeEnemyTurret enemy;
        public void Bind(R3PartState state,Transform owner)
        {
            State=state;Owner=owner;Body=owner.GetComponent<Rigidbody>();
            Damageable=GetComponent<P12PipelineSafeDamageable>();part=GetComponent<R3MountedPart>();
            player=owner.GetComponent<R1VehicleState>();enemy=owner.GetComponent<P12PipelineSafeEnemyTurret>();
            owner.GetComponent<RoboRougelike.R6.R6VehicleDrive>()?.RefreshParts();
        }
        public void RefreshAvailability()
        {foreach(var component in GetComponents<MonoBehaviour>())if(component!=null&&component is IRRLDeviceLifecycle device)device.RefreshAvailability();}
        public bool HardwareAvailable
        {
            get
            {
                if(!isActiveAndEnabled||State==null||Owner==null||Body==null||Damageable==null||Damageable.IsDead||
                    !Owner.gameObject.activeInHierarchy||!transform.IsChildOf(Owner)||GetComponentInParent<Rigidbody>()!=Body)return false;
                if(RRLEmpPartState.IsBlocked(gameObject))return false;
                if(part!=null&&!part.Operational)return false;
                if(enemy!=null&&(!enemy.isActiveAndEnabled||enemy.IsDefeated))return false;
                if(player!=null&&(player.Structure==null||!player.Structure.CoreAlive))return false;
                return true;
            }
        }
        public bool Available
        {
            get
            {
                if(!HardwareAvailable||(enemy!=null&&!enemy.CombatActive))return false;
                var run=R1RunController.Instance;
                return run?.Player?.transform!=Owner||(!run.Paused&&!run.Transitioning&&
                    (run.Progress.State==R1RunState.Combat||run.Progress.State==R1RunState.TestDrive));
            }
        }
    }
}
