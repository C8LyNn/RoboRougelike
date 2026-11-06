using System;
using System.Collections.Generic;
using UnityEngine;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R1
{
    public sealed class EnemySelfDestructTimer
    {
        float disabledSeconds;
        public float Remaining=>Mathf.Max(0,5f-disabledSeconds);
        public bool Advance(bool disabled,bool nearby,float seconds)
        {
            if(!disabled){disabledSeconds=0;return false;}
            if(!float.IsNaN(seconds)&&!float.IsInfinity(seconds)&&seconds>0)disabledSeconds+=seconds;
            return disabledSeconds>=5f&&nearby;
        }
    }

    /// <summary>Uses the existing damage system; a disabled enemy can threaten a nearby vehicle.</summary>
    public sealed class EnemySelfDestruct : MonoBehaviour
    {
        readonly EnemySelfDestructTimer timer=new EnemySelfDestructTimer();
        P12PipelineSafeEnemyTurret enemy;Transform player;GameObject warning;Material warningMaterial;
        public void Configure(P12PipelineSafeEnemyTurret source,Transform target){enemy=source;player=target;}
        void Update()
        {
            if(enemy==null||enemy.IsDefeated||!enemy.CombatActive||player==null){if(warning!=null)warning.SetActive(false);return;}
            bool disabled=!enemy.WeaponOnline&&enemy.ActiveWheelCount==0;
            if(disabled&&warning==null)CreateWarning();
            if(warning!=null){warning.SetActive(disabled);if(disabled)warning.transform.localScale=Vector3.one*(.25f+.08f*Mathf.Sin(Time.time*8));}
            bool nearby=Vector3.Distance(enemy.transform.position,player.position)<=6f;
            if(!timer.Advance(disabled,nearby,Time.deltaTime))return;
            var center=enemy.Core.transform.position;
            var hit=new HashSet<P12PipelineSafeDamageable>();
            foreach(var part in player.GetComponentsInChildren<P12PipelineSafeDamageable>())
            {
                if(part==null||part.IsDead||!hit.Add(part))continue;
                float distance=Vector3.Distance(part.transform.position,center);
                if(distance>6f)continue;
                part.ApplyDamage(80f*Mathf.Clamp01(1f-distance/8f),part.transform.position,(part.transform.position-center).normalized);
            }
            enemy.Core.ApplyDamage(100000f,center,Vector3.up);
        }
        void CreateWarning()
        {
            warning=GameObject.CreatePrimitive(PrimitiveType.Sphere);warning.name="Ostrzeżenie autodestrukcji";
            warning.transform.SetParent(transform,false);warning.transform.localPosition=new Vector3(0,2.6f,0);
            var col=warning.GetComponent<Collider>();col.enabled=false;Destroy(col);
            var shader=Shader.Find("Universal Render Pipeline/Unlit");if(shader!=null){warningMaterial=new Material(shader);warningMaterial.SetColor("_BaseColor",Color.red);warning.GetComponent<Renderer>().sharedMaterial=warningMaterial;}
        }
        void OnDestroy(){if(warningMaterial!=null)Destroy(warningMaterial);}
    }
}