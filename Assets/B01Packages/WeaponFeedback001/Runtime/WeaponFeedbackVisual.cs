using System.Collections.Generic;
using UnityEngine;
namespace B01.WeaponFeedback001
{
    // Deliberately visual-only. The authoritative gun calls NotifyShot/NotifyImpact
    // after its own fire/damage decisions; this component cannot shoot or spend ammo.
    [DisallowMultipleComponent]
    public sealed class WeaponFeedbackVisual : MonoBehaviour
    {
        [SerializeField] Transform recoilVisualPivot;
        [SerializeField] ParticleSystem muzzleFlash;
        [SerializeField] Light muzzleLight;
        [SerializeField] ParticleSystem impactPrefab;
        [SerializeField] ParticleSystem damagedWeaponSmoke;
        [SerializeField, Min(0f)] float recoilDistance = 0.13f;
        [SerializeField, Min(0.01f)] float recoilRecoverySpeed = 12f;
        [SerializeField, Min(0.01f)] float lightDurationSeconds = 0.07f;
        [SerializeField, Min(0.05f)] float impactLifetimeSeconds = 2f;
        [SerializeField, Range(1,32)] int maximumImpactInstances = 12;

        readonly Queue<ParticleSystem> impacts = new Queue<ParticleSystem>();
        Vector3 restLocalPosition;
        float lightLeft;
        bool pivotSet;
        public int ShotFeedbackCount { get; private set; }
        public int ImpactFeedbackCount { get; private set; }
        public float DamageVisualFraction { get; private set; }

        void Awake() { CachePivot(); if(muzzleLight)muzzleLight.enabled=false; }
        void CachePivot()
        {
            if(!recoilVisualPivot)return;
            restLocalPosition=recoilVisualPivot.localPosition;
            pivotSet=true;
        }
        public void BindVisuals(Transform pivot, ParticleSystem muzzle, Light flash,
                                ParticleSystem hitFx, ParticleSystem smoke)
        {
            recoilVisualPivot=pivot; muzzleFlash=muzzle; muzzleLight=flash;
            impactPrefab=hitFx;damagedWeaponSmoke=smoke;
            CachePivot();
            if(muzzleLight)muzzleLight.enabled=false;
        }
        // Not authoritative. Call exactly once after a confirmed shot.
        public void NotifyShot()
        {
            ShotFeedbackCount++;
            if(muzzleFlash)muzzleFlash.Play(true);
            if(muzzleLight){muzzleLight.enabled=true;lightLeft=lightDurationSeconds;}
            if(recoilVisualPivot && pivotSet)
                recoilVisualPivot.localPosition=restLocalPosition-Vector3.forward*recoilDistance;
        }
        // Not authoritative. Collision/damage code supplies the verified impact event.
        public void NotifyImpact(Vector3 position,Vector3 outwardNormal)
        {
            ImpactFeedbackCount++;
            if(!impactPrefab)return;
            Vector3 normal=outwardNormal.sqrMagnitude>.0001f?outwardNormal.normalized:Vector3.up;
            if(impacts.Count>=maximumImpactInstances)
            {
                var old=impacts.Dequeue();
                if(old)Destroy(old.gameObject);
            }
            ParticleSystem copy=Instantiate(impactPrefab,position,
                Quaternion.FromToRotation(Vector3.up,normal),null);
            copy.gameObject.SetActive(true);
            copy.Play(true);
            impacts.Enqueue(copy);
            Destroy(copy.gameObject,impactLifetimeSeconds);
        }
        // 0..1 comes from game health; this class never writes HP.
        public void SetWeaponDamageVisual(float fraction)
        {
            DamageVisualFraction=Mathf.Clamp01(fraction);
            if(!damagedWeaponSmoke)return;
            var emission=damagedWeaponSmoke.emission;
            emission.enabled=DamageVisualFraction>.01f;
            emission.rateOverTime=DamageVisualFraction*18f;
            if(DamageVisualFraction>.01f&&!damagedWeaponSmoke.isPlaying)
                damagedWeaponSmoke.Play(true);
            else if(DamageVisualFraction<=.01f&&damagedWeaponSmoke.isPlaying)
                damagedWeaponSmoke.Stop(true,ParticleSystemStopBehavior.StopEmitting);
        }
        void Update()
        {
            if(muzzleLight&&muzzleLight.enabled)
            {
                lightLeft-=Time.deltaTime;
                if(lightLeft<=0f)muzzleLight.enabled=false;
            }
            if(recoilVisualPivot&&pivotSet)
                recoilVisualPivot.localPosition=Vector3.Lerp(recoilVisualPivot.localPosition,
                    restLocalPosition,1f-Mathf.Exp(-recoilRecoverySpeed*Time.deltaTime));
        }
        void OnDisable()
        {
            if(muzzleLight)muzzleLight.enabled=false;
            if(recoilVisualPivot&&pivotSet)recoilVisualPivot.localPosition=restLocalPosition;
            while(impacts.Count>0){var old=impacts.Dequeue();if(old)Destroy(old.gameObject);}
        }
    }
}
