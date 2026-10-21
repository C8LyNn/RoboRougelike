using UnityEngine;
using UnityEngine.InputSystem;
using RoboRougelike.R1;

namespace RoboRougelike.MOVSeries
{
    public sealed class MOVSeriesInput : MonoBehaviour
    {
        MOVSeriesVehicleAdapter owner;
        public void Bind(MOVSeriesVehicleAdapter adapter)=>owner=adapter;
        void Update()
        {
            if(owner==null)return;owner.IndependentYaw=0;
            var run=R1RunController.Instance;
            if(run==null||run.Player==null||run.Player.gameObject!=gameObject||!run.CanUseMovement)return;
            var keys=Keyboard.current;if(keys==null)return;
            owner.IndependentYaw=(keys.cKey.isPressed?1:0)-(keys.zKey.isPressed?1:0);
            if(keys.leftShiftKey.wasPressedThisFrame||keys.rightShiftKey.wasPressedThisFrame)run.UseBooster();
            if(keys.spaceKey.wasPressedThisFrame)run.UseJump();if(keys.qKey.wasPressedThisFrame)run.UseBlink();
            if(keys.eKey.wasPressedThisFrame)run.UseGrapple();if(keys.gKey.wasPressedThisFrame)run.ToggleGyro();
        }
    }
}
