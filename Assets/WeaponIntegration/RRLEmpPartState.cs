using System.Collections.Generic;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.EMP;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLEmpPartState : MonoBehaviour
    {
        static readonly HashSet<RRLEmpPartState> active=new HashSet<RRLEmpPartState>();
        public EmpReceiver Receiver {get;private set;}
        Transform owner;Rigidbody body;P12PipelineSafeDamageable hp;R3MountedPart mounted;R3PartState canonical;
        bool endingView;
        R3PartState State=>canonical??GetComponent<RRLDeviceBinding>()?.State;
        public static bool IsBlocked(GameObject part)=>part!=null&&part.GetComponent<EmpReceiver>()?.IsDisrupted==true;
        public void Bind(Transform root,R3PartState state)
        {
            owner=root;body=root.GetComponent<Rigidbody>();hp=GetComponent<P12PipelineSafeDamageable>();mounted=GetComponent<R3MountedPart>();canonical=state;
            Receiver=GetComponent<EmpReceiver>();if(Receiver==null)Receiver=gameObject.AddComponent<EmpReceiver>();
            Receiver.ConfigureOwnership(root,root.GetInstanceID(),RRLHitRouter.Team(root));Receiver.ConfigureSusceptibility(0);
            Receiver.DisruptionChanged+=Changed;Receiver.Disabling+=FreezeState;hp.Died+=RefreshStructural;
            RefreshStructural();if(State!=null&&State.DisruptionRemaining>0)Receiver.ApplyEmp(State.DisruptionRemaining);CaptureState();
        }
        bool Attached=>owner!=null&&owner.gameObject.activeInHierarchy&&hp!=null&&!hp.IsDead&&transform.IsChildOf(owner)&&GetComponentInParent<Rigidbody>()==body&&(mounted==null||mounted.Operational);
        void OnEnable()
        {
            active.Add(this);endingView=false;
            if(Receiver!=null){RefreshStructural();if(State!=null&&State.DisruptionRemaining>0)Receiver.ApplyEmp(State.DisruptionRemaining);}
        }
        void OnDisable(){active.Remove(this);endingView=true;}
        void OnTransformParentChanged(){if(Receiver!=null)RefreshStructural();}
        void OnDestroy(){if(Receiver!=null){Receiver.DisruptionChanged-=Changed;Receiver.Disabling-=FreezeState;}if(hp!=null)hp.Died-=RefreshStructural;}
        public void RefreshStructural(){if(Receiver!=null)Receiver.SetOperational(Attached);}
        void SaveCurrent(){var state=State;if(state!=null&&Receiver!=null)state.DisruptionRemaining=Receiver.RemainingSeconds;}
        public void CaptureState(){if(!endingView)SaveCurrent();}
        void FreezeState(EmpReceiver receiver){SaveCurrent();endingView=true;}
        void Update(){RefreshStructural();CaptureState();}
        void Changed(EmpReceiver receiver,bool disrupted)
        {
            if(endingView)return;CaptureState();if(owner==null||!owner.gameObject.activeInHierarchy)return;
            GetComponent<RRLDeviceBinding>()?.RefreshAvailability();
            owner.GetComponent<P12PipelineSafeVehicleModuleSystem>()?.RefreshModules();
            owner.GetComponent<RoboRougelike.R6.R6VehicleDrive>()?.RefreshMass();
        }
        public static void RefreshEligibility()
        {
            var snapshot=new RRLEmpPartState[active.Count];active.CopyTo(snapshot);
            foreach(var part in snapshot)if(part!=null)part.RefreshStructural();
        }
    }
}
