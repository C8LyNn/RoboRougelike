using System;
using UnityEngine;

namespace RoboRougelike.R6
{
    [Serializable] public sealed class R6Tuning
    {
        public float WheelForce=400, BrakeForce=550, CoastFraction=.15f, LateralGrip=1.4f;
        public float BareHullDrag=2;
        public float TurnResponse=6, TurnLeverage=.55f, DamageTractionFloor=.35f;
        public float MaxDepenetrationSpeed=1.5f, ContactFriction=.35f;
        public float DebrisGraceSeconds=.65f, CameraSmoothSeconds=.09f;
        public int SandboxScrap=600, MaxEffects=48;
        static R6Tuning current;
        public static R6Tuning Current
        {
            get
            {
                if(current!=null)return current;
                var text=Resources.Load<TextAsset>("R6/Tuning");
                current=text!=null?JsonUtility.FromJson<R6Tuning>(text.text):new R6Tuning();
                current.WheelForce=Mathf.Clamp(current.WheelForce,100,1000);
                current.BrakeForce=Mathf.Clamp(current.BrakeForce,100,1500);
                current.MaxDepenetrationSpeed=Mathf.Clamp(current.MaxDepenetrationSpeed,.1f,3);
                return current;
            }
        }
    }
}
