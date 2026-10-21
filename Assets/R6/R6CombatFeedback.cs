using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;

namespace RoboRougelike.R6
{
    public sealed class R6CombatFeedback : MonoBehaviour
    {
        sealed class Effect {public GameObject Obj;public float End,Start,Size;}
        readonly Queue<Effect> effects=new Queue<Effect>();
        readonly Queue<(string text,float until)> notices=new Queue<(string,float)>();
        readonly HashSet<int> losses=new HashSet<int>();
        static R6CombatFeedback instance;
        Material material;
        int playerId;
        public static R6CombatFeedback Ensure()
        {
            if(instance==null)instance=new GameObject("Combat feedback R6").AddComponent<R6CombatFeedback>();
            return instance;
        }
        public static Color PartColor(P12PipelineSafeDamageable part)
        {
            var mounted=part.GetComponent<R3MountedPart>();var module=part.GetComponent<P12PipelineSafeFunctionalModule>();
            if(mounted?.Kind==R3PartKind.Core||part.GetComponent<P12_PipelineSafePlayerBlock>()?.IsCore==true)return new Color(.2f,.7f,1);
            if(mounted?.Kind==R3PartKind.Gun||module?.ModuleType==P12FunctionalModuleType.Weapon)return new Color(1,.45f,.08f);
            if(mounted?.Kind==R3PartKind.Wheel||module?.ModuleType==P12FunctionalModuleType.Wheel||part.name.StartsWith("Koło"))return new Color(.8f,.85f,.9f);
            return new Color(1,.8f,.3f);
        }
        public static void Hit(P12PipelineSafeDamageable part,Vector3 point,bool destroyed)
        {
            Ensure().Emit(point,PartColor(part),destroyed?.42f:.15f,destroyed?.32f:.16f,part.GetComponent<Renderer>()?.sharedMaterial);
            if(destroyed)ModuleLost(part);
        }
        public static void ModuleLost(P12PipelineSafeDamageable part)
        {
            var player=R1RunController.Instance?.Player;
            if(player==null||!part.transform.IsChildOf(player.transform))return;
            var mounted=part.GetComponent<R3MountedPart>();var module=part.GetComponent<P12PipelineSafeFunctionalModule>();
            bool gun=mounted?.Kind==R3PartKind.Gun||module?.ModuleType==P12FunctionalModuleType.Weapon;
            bool wheel=mounted?.Kind==R3PartKind.Wheel||module?.ModuleType==P12FunctionalModuleType.Wheel;
            if(!gun&&!wheel)return;
            var feedback=Ensure();feedback.CheckPlayer();
            if(!feedback.losses.Add(part.GetInstanceID()))return;
            feedback.notices.Enqueue((gun?"UTRACONO BROŃ — sprawdź pozostałe uzbrojenie":"UTRACONO KOŁO — napęd i skręcanie osłabły",Time.unscaledTime+4));
            while(feedback.notices.Count>3)feedback.notices.Dequeue();
        }
        public static void Muzzle(Transform barrel,R4WeaponKind kind)
        {
            var renderer=barrel.GetComponentInParent<P12PipelineSafeDamageable>()?.GetComponent<Renderer>();
            Ensure().Emit(barrel.position,kind==R4WeaponKind.Automatic?new Color(.5f,.9f,1):new Color(1,.7f,.15f),
                kind==R4WeaponKind.Heavy?.3f:kind==R4WeaponKind.Automatic?.11f:.18f,kind==R4WeaponKind.Heavy?.13f:.07f,renderer?.sharedMaterial);
        }
        void Emit(Vector3 point,Color color,float size,float duration,Material template)
        {
            if(material==null)
            {
                if(template==null)return;
                material=new Material(template);material.name="Efekty walki R6";
            }
            while(effects.Count>=Mathf.Clamp(R6Tuning.Current.MaxEffects,8,48))RemoveFirst();
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name="Błysk trafienia";
            var collider=obj.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            obj.transform.SetParent(transform,false);obj.transform.position=point;obj.transform.localScale=Vector3.one*size;
            var renderer=obj.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var props=new MaterialPropertyBlock();props.SetColor("_BaseColor",color);renderer.SetPropertyBlock(props);
            effects.Enqueue(new Effect{Obj=obj,Start=Time.time,End=Time.time+duration,Size=size});
        }
        void CheckPlayer()
        {
            var id=R1RunController.Instance?.Player?.GetInstanceID()??0;
            if(id==playerId)return;playerId=id;losses.Clear();notices.Clear();
        }
        void Update()
        {
            CheckPlayer();while(effects.Count>0&&effects.Peek().End<=Time.time)RemoveFirst();
            foreach(var item in effects)if(item.Obj!=null)item.Obj.transform.localScale=Vector3.one*item.Size*Mathf.Clamp01((item.End-Time.time)/(item.End-item.Start));
            while(notices.Count>0&&notices.Peek().until<Time.unscaledTime)notices.Dequeue();
        }
        void RemoveFirst(){var item=effects.Dequeue();if(item.Obj!=null){item.Obj.SetActive(false);Destroy(item.Obj);}}
        public string[] Notices(){CheckPlayer();var result=new List<string>();foreach(var notice in notices)result.Add(notice.text);return result.ToArray();}
        void OnDestroy(){if(material!=null)Destroy(material);if(instance==this)instance=null;}
    }
}
