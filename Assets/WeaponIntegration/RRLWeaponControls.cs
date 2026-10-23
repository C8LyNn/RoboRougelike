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
        void Start()=>CacheBindings();
        void CacheBindings(){shotguns=GetComponentsInChildren<RRLShotgunBinding>();rails=GetComponentsInChildren<RRLRailgunBinding>();rockets=GetComponentsInChildren<RRLRocketBinding>();defenses=GetComponentsInChildren<RRLPointDefenseBinding>();}
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current?.tKey.wasPressedThisFrame==true)Reload();
#endif
        }
        void OnGUI()
        {
            if(HandleGuiInput(Event.current))Event.current.Use();
        }
        public bool HandleGuiInput(Event evt)
        {
            if(evt==null||evt.type!=EventType.KeyDown||evt.alt||evt.control||evt.command)return false;
            if(evt.keyCode!=KeyCode.T&&evt.character!='t'&&evt.character!='T')return false;
            return Reload();
        }
        public bool Reload()
        {
            var run=R1RunController.Instance;
            if(run?.Player==null||run.Player.gameObject!=gameObject||run.Paused||run.Transitioning||
                (run.Progress.State!=R1RunState.Combat&&run.Progress.State!=R1RunState.TestDrive))return false;
            if(shotguns==null)CacheBindings();
            bool accepted=false;
            foreach(var gun in shotguns)if(gun!=null)accepted|=gun.BeginReload();
            foreach(var gun in rails)if(gun!=null)accepted|=gun.BeginReload();
            foreach(var gun in rockets)if(gun!=null)accepted|=gun.BeginReload();
            foreach(var defense in defenses)if(defense!=null)accepted|=defense.BeginReload();
            return accepted;
        }
    }
}
