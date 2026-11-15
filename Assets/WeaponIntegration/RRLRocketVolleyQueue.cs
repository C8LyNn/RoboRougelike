using System;
using System.Collections.Generic;
using RoboRougelike.Mechanics.Rockets;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RoboRougelike.WeaponIntegration
{
    // The vehicle owns already-earned bonus rockets, so destroying a launcher
    // does not discard a volley that has already consumed its one shot cost.
    public sealed class RRLRocketVolleyQueue : MonoBehaviour,IR3ProjectileWork
    {
        sealed class Batch
        {
            public System.Numerics.BigInteger Remaining;
            public RocketProjectile Template;
            public RocketSpec Spec;
            public Rigidbody Body;
            public Transform Owner;
            public GameObject Attacker;
            public Vector3 Position,Direction,Inherited;
            public Quaternion Rotation;
            public Action<RocketProjectile> Retirement;
        }
        readonly Queue<Batch> batches=new Queue<Batch>();
        double workBudgetMilliseconds=4d;

        internal void Enqueue(int count,RocketProjectile template,RocketSpec spec,Rigidbody body,Transform owner,
            GameObject attacker,Vector3 position,Quaternion rotation,Vector3 direction,Action<RocketProjectile> retirement)
            =>EnqueueExact(count,template,spec,body,owner,attacker,position,rotation,direction,retirement);
        internal void EnqueueExact(System.Numerics.BigInteger count,RocketProjectile template,RocketSpec spec,Rigidbody body,Transform owner,
            GameObject attacker,Vector3 position,Quaternion rotation,Vector3 direction,Action<RocketProjectile> retirement)
        {
            if(count<=0)return;
            if(template==null||body==null||owner==null)throw new ArgumentException("Rocket volley needs a template, owner and body.");
            var copy=Object.Instantiate(template);copy.gameObject.name="Queued rocket template";
            copy.gameObject.SetActive(false);copy.transform.SetParent(transform,false);
            batches.Enqueue(new Batch{Remaining=count,Template=copy,Spec=spec,Body=body,Owner=owner,Attacker=attacker,
                Position=position,Rotation=rotation,Direction=direction,
                Inherited=body.GetPointVelocity(position)*spec.InheritedVelocityFraction,Retirement=retirement});
            Drain();
        }
        void FixedUpdate(){if(Time.timeScale>0)Drain();}
        void OnEnable(){if(batches.Count>0)R3ProjectileWorkScheduler.Register(this);}
        void OnDisable(){R3ProjectileWorkScheduler.Unregister(this);if(!gameObject.activeInHierarchy)Clear();}
        void OnDestroy()=>Clear();
        void Clear(){while(batches.Count>0){var batch=batches.Dequeue();if(batch.Template!=null)Object.Destroy(batch.Template.gameObject);}}
        bool IR3ProjectileWork.HasProjectileWork=>batches.Count>0;
        bool IR3ProjectileWork.CanProcessProjectileWork
        {
            get
            {
                var run=R1RunController.Instance;
                return !(run!=null&&run.Player!=null&&run.Player.transform==transform&&run.EmergencyRescueAttackBlocked);
            }
        }
        bool IR3ProjectileWork.ProcessProjectileWork()=>ProcessOne();
        void Drain()
        {
            if(batches.Count==0)return;
            if(workBudgetMilliseconds<=0)
            {
                if(((IR3ProjectileWork)this).CanProcessProjectileWork)ProcessOne();
                return;
            }
            R3ProjectileWorkScheduler.Request(this,workBudgetMilliseconds);
        }
        bool ProcessOne()
        {
            if(batches.Count==0)return false;
            var batch=batches.Peek();
            if(batch.Owner==null||!batch.Owner.gameObject.activeInHierarchy||batch.Body==null)
            {batches.Dequeue();if(batch.Template!=null)Object.Destroy(batch.Template.gameObject);return true;}
            RocketProjectile rocket=null;
            try
            {
                rocket=Object.Instantiate(batch.Template,batch.Position,batch.Rotation);
                rocket.SetRetirementHandler(batch.Retirement);
                var attacker=batch.Attacker!=null?batch.Attacker:batch.Owner.gameObject;
                if(!rocket.TryInitializeWithInheritedVelocity(batch.Spec,batch.Body,batch.Owner,attacker,null,batch.Direction,batch.Inherited))
                    throw new InvalidOperationException("A queued rocket could not be initialized.");
                if(!rocket.gameObject.activeSelf)rocket.gameObject.SetActive(true);
                batch.Remaining--;batches.Dequeue();
                if(batch.Remaining==0)Object.Destroy(batch.Template.gameObject);
                else batches.Enqueue(batch);
                return true;
            }
            catch(Exception e){if(rocket!=null)Object.Destroy(rocket.gameObject);UnityEngine.Debug.LogException(e,this);return false;}
        }
    }
}
