using System;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Purely visual: R6WheelContacts still reads the original tire collider.
    [DisallowMultipleComponent]
    public sealed class R4WheelArt : MonoBehaviour
    {
        float radiusMeters=.39f;
        Transform rotor,visualRoot,steeringKnuckle;
        Rigidbody owner;
        public static bool ForkWheelAvailable(R3WheelStyle style)
        {var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");return catalog!=null&&
            (style==R3WheelStyle.SmallFork?catalog.SmallForkWheel!=null:style==R3WheelStyle.BalloonFork?catalog.BalloonForkWheel!=null:style==R3WheelStyle.SpikedFork?catalog.SpikedForkWheel!=null:style==R3WheelStyle.TwinRoad?catalog.TwinRoadWheel!=null:style==R3WheelStyle.SteerRoad?catalog.SteerRoadWheel!=null:style==R3WheelStyle.SteerOffroad&&catalog.SteerOffroadWheel!=null);}
        public static bool SizedWheelAvailable(R3WheelStyle style)
        {
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            return catalog!=null&&(style==R3WheelStyle.LargeOffroad?catalog.SideWheelLarge!=null:
                style==R3WheelStyle.Monster&&catalog.SideWheelMonster!=null);
        }

        // Select the model's side from the wheel's rotated mounting axis and
        // the connected neighbor; world X alone fails for rotated builds.
        public static bool MirrorForMount(R3VehicleModel model,R3PartState wheel)
        {
            if(model==null||wheel==null)return false;
            var axis=R3MountingGrid.Rotate(Vector3Int.right,wheel.RotationQuarterTurns);
            var plus=model.At(wheel.Cell+axis);var minus=model.At(wheel.Cell-axis);
            bool positive=plus!=null&&plus.Health>0&&R3MountingGrid.Compatible(wheel,plus,axis);
            bool negative=minus!=null&&minus.Health>0&&R3MountingGrid.Compatible(wheel,minus,-axis);
            if(positive!=negative)return positive;
            // Ambiguous or unattached ghosts retain the nearest sensible side.
            return wheel.Cell.x<0?axis.x>0:axis.x<0;
        }
        public static R4WheelArt TryAttach(Transform block,Transform physicalWheel,bool mirrorSide=false,R3WheelStyle style=R3WheelStyle.Offroad)
        {
            if(block==null||physicalWheel==null)return null;
            var scale=block.lossyScale;
            if(float.IsNaN(scale.x)||float.IsInfinity(scale.x)||float.IsNaN(scale.y)||float.IsInfinity(scale.y)||
                float.IsNaN(scale.z)||float.IsInfinity(scale.z)||scale.x<=.0001f||
                Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return null;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            bool large=style==R3WheelStyle.LargeOffroad;
            bool monster=style==R3WheelStyle.Monster;
            bool small=style==R3WheelStyle.SmallFork;
            bool balloon=style==R3WheelStyle.BalloonFork;bool spiked=style==R3WheelStyle.SpikedFork;bool twin=style==R3WheelStyle.TwinRoad;bool largeSteer=style==R3WheelStyle.SteerOffroad;bool steer=style==R3WheelStyle.SteerRoad||largeSteer;bool fork=small||balloon||spiked||twin||steer;
            if((large||monster)&&!SizedWheelAvailable(style))return null;
            if(fork&&!ForkWheelAvailable(style))return null;
            var slick=style==R3WheelStyle.Slick&&catalog!=null&&catalog.SideWheelSlick!=null;
            var side=!fork&&catalog!=null&&(monster||large||slick||catalog.SideWheel!=null);
            var prefab=fork?(small?catalog.SmallForkWheel:balloon?catalog.BalloonForkWheel:spiked?catalog.SpikedForkWheel:twin?catalog.TwinRoadWheel:largeSteer?catalog.SteerOffroadWheel:catalog.SteerRoadWheel):side?(monster?catalog.SideWheelMonster:large?catalog.SideWheelLarge:slick?catalog.SideWheelSlick:catalog.SideWheel):catalog!=null?catalog.ModelFor(R3PartKind.Wheel):null;
            if(prefab==null)return null;
            var visual=Instantiate(prefab,block,false);
            var spinning=Array.Find(visual.GetComponentsInChildren<Transform>(true),t=>t.name==(small?"R4M_WheelS_Fork_Road_Rotor":balloon?"R4M_WheelL_Fork_Balloon_Rotor":spiked?"R4M_WheelM_Fork_Spiked_Rotor":twin?"R4M_WheelM_Twin_Road_Rotor":largeSteer?"R4M_WheelL_Steer_Offroad_Rotor":steer?"R4M_WheelM_Steer_Road_Rotor":monster?"R4M_WheelXL_Side_Monster_Rotor":large?"R4M_WheelL_Side_Offroad_Rotor":slick?"R4M_WheelM_Side_Slick_Rotor":side?"R4M_WheelM_Side_Offroad_Rotor":"R4M_Wheel_Rotor"));
            var knuckle=steer?Array.Find(visual.GetComponentsInChildren<Transform>(true),t=>t.name==(largeSteer?"R4M_WheelL_Steer_Offroad_Knuckle":"R4M_WheelM_Steer_Road_Knuckle")):null;
            var socket=Array.Find(visual.GetComponentsInChildren<Transform>(true),t=>t.name=="Socket_Mount");
            var renderer=physicalWheel.GetComponent<Renderer>();
            if(spinning==null||socket==null||renderer==null||(steer&&(knuckle==null||!spinning.IsChildOf(knuckle)))){Destroy(visual);return null;}
            visual.transform.localScale/=scale.x;
            if(slick)visual.transform.localScale*=.39f/.37f;
            if(side)
            {
                // Source has its port on -X; mirror for the left side.
                if(mirrorSide){var v=visual.transform.localScale;v.x=-v.x;visual.transform.localScale=v;}
                // Match the authored axle to the collider; do not move physics.
                visual.transform.position+=physicalWheel.position-spinning.position;
            }
            else if(fork)visual.transform.position+=physicalWheel.position-spinning.position;
            var view=block.gameObject.AddComponent<R4WheelArt>();view.rotor=spinning;view.visualRoot=visual.transform;view.steeringKnuckle=knuckle;
            view.radiusMeters=R3VehicleModel.WheelRadius(style);
            view.owner=block.GetComponentInParent<Rigidbody>(true);renderer.enabled=false;R41PartGeometry.HideMountingShell(block,visual);
            return view;
        }

        public bool AttachToSteeringPivot(Transform pivot)
        {
            if(visualRoot==null||pivot==null)return false;
            (steeringKnuckle!=null?steeringKnuckle:visualRoot).SetParent(pivot,true);
            return true;
        }

        void LateUpdate()
        {
            if(rotor==null)return;
            if(owner==null||!transform.IsChildOf(owner.transform))owner=GetComponentInParent<Rigidbody>(true);
            if(owner==null)return;
            var direction=Vector3.Cross(rotor.right,transform.up);
            if(direction.sqrMagnitude<.0001f)return;
            float speed=Vector3.Dot(owner.GetPointVelocity(rotor.position),direction.normalized);
            if(float.IsNaN(speed)||float.IsInfinity(speed))return;
            rotor.Rotate(Vector3.right,speed/radiusMeters*Mathf.Rad2Deg*Time.deltaTime,Space.Self);
        }
    }
}
