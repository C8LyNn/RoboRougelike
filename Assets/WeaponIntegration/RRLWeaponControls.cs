using UnityEngine;
using RoboRougelike.R1;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoboRougelike.WeaponIntegration
{
    public sealed class RRLWeaponControls : MonoBehaviour
    {
        RRLShotgunBinding[] shotguns;
        RRLRailgunBinding[] rails;
        RRLRocketBinding[] rockets;
        RRLPointDefenseBinding[] defenses;
        void Start(){shotguns=GetComponentsInChildren<RRLShotgunBinding>();rails=GetComponentsInChildren<RRLRailgunBinding>();rockets=GetComponentsInChildren<RRLRocketBinding>();defenses=GetComponentsInChildren<RRLPointDefenseBinding>();}
        void Update()
        {
            var run=R1RunController.Instance;
            if(run?.Player==null||run.Player.gameObject!=gameObject||run.Paused||run.Transitioning||
                (run.Progress.State!=R1RunState.Combat&&run.Progress.State!=R1RunState.TestDrive))return;
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current?.tKey.wasPressedThisFrame==true)
            {
                foreach(var gun in shotguns)if(gun!=null)gun.BeginReload();
                foreach(var gun in rails)if(gun!=null)gun.BeginReload();
                foreach(var gun in rockets)if(gun!=null)gun.BeginReload();
                foreach(var defense in defenses)if(defense!=null)defense.BeginReload();
            }
#endif
        }
    }
}
