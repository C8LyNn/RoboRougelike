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

        readonly List<ParticleSystem> impacts = new List<ParticleSystem>();
        readonly List<float> impactExpiry = new List<float>();
        int nextImpactIndex;
        int EffectiveImpactCapacity => Mathf.Clamp(maximumImpactInstances, 1, 32);
        Vector3 restLocalPosition;
        float lightLeft;
        bool pivotSet;
        public int ShotFeedbackCount { get; private set; }
        public int ImpactFeedbackCount { get; private set; }
        public float DamageVisualFraction { get; private set; }
        public int ImpactPoolSize => impacts.Count;

        void Awake()
        {
            CachePivot();
            if (muzzleLight) muzzleLight.enabled = false;
            if (muzzleFlash) muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void OnEnable()
        {
            // Restores the last visual damage state only when the component is active.
            SetWeaponDamageVisual(DamageVisualFraction);
        }
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
            if(muzzleFlash)muzzleFlash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            SetWeaponDamageVisual(DamageVisualFraction);
        }
        // Not authoritative. Call exactly once after a confirmed shot.
        public void NotifyShot()
        {
            if (!isActiveAndEnabled) return;
            ShotFeedbackCount++;
            if(muzzleFlash)muzzleFlash.Play(true);
            if(muzzleLight){muzzleLight.enabled=true;lightLeft=lightDurationSeconds;}
            if(recoilVisualPivot && pivotSet)
                recoilVisualPivot.localPosition=restLocalPosition-Vector3.forward*recoilDistance;
        }
        // Not authoritative. Collision/damage code supplies the verified impact event.
        public void NotifyImpact(Vector3 position,Vector3 outwardNormal)
        {
            if (!isActiveAndEnabled) return;
            ImpactFeedbackCount++;
            if(!impactPrefab)return;
            Vector3 normal=outwardNormal.sqrMagnitude>.0001f?outwardNormal.normalized:Vector3.up;
            // Reuse a bounded number of particle objects. Bursts must not allocate
            // one new GameObject per hit (or exceed the configured budget).
            ParticleSystem copy;
            int slot;
            if(impacts.Count < EffectiveImpactCapacity)
            {
                copy = Instantiate(impactPrefab, position,
                    Quaternion.FromToRotation(Vector3.up, normal), null);
                slot = impacts.Count;
                impacts.Add(copy);
                impactExpiry.Add(0f);
            }
            else
            {
                slot = nextImpactIndex % impacts.Count;
                copy = impacts[slot];
                if (!copy)
                {
                    copy = Instantiate(impactPrefab, position,
                        Quaternion.FromToRotation(Vector3.up, normal), null);
                    impacts[slot] = copy;
                }
                copy.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                copy.transform.SetPositionAndRotation(position,
                    Quaternion.FromToRotation(Vector3.up, normal));
            }
            nextImpactIndex = (slot + 1) % EffectiveImpactCapacity;
            copy.gameObject.SetActive(true);
            copy.Play(true);
            impactExpiry[slot] = Time.unscaledTime + impactLifetimeSeconds;
        }
        // 0..1 comes from game health; this class never writes HP.
        public void SetWeaponDamageVisual(float fraction)
        {
            DamageVisualFraction=Mathf.Clamp01(fraction);
            if(!damagedWeaponSmoke)return;
            var emission=damagedWeaponSmoke.emission;
            bool activeSmoke = isActiveAndEnabled && DamageVisualFraction > .01f;
            emission.enabled = activeSmoke;
            emission.rateOverTime = DamageVisualFraction * 18f;
            if (activeSmoke && !damagedWeaponSmoke.isPlaying)
                damagedWeaponSmoke.Play(true);
            else if (!activeSmoke && damagedWeaponSmoke.isPlaying)
                damagedWeaponSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void Update()
        {
            // Expire effects without destroying their pooled GameObjects.
            for (int i = 0; i < impacts.Count; i++)
                if (impacts[i] && impactExpiry[i] > 0f && Time.unscaledTime >= impactExpiry[i])
                {
                    impacts[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    impacts[i].gameObject.SetActive(false);
                    impactExpiry[i] = 0f;
                }
            if(muzzleLight&&muzzleLight.enabled)
            {
                lightLeft-=Time.unscaledDeltaTime;
                if(lightLeft<=0f)muzzleLight.enabled=false;
            }
            if(recoilVisualPivot&&pivotSet)
                recoilVisualPivot.localPosition=Vector3.Lerp(recoilVisualPivot.localPosition,
                    restLocalPosition,1f-Mathf.Exp(-recoilRecoverySpeed*Time.unscaledDeltaTime));
        }
        void OnDisable()
        {
            if (muzzleLight) muzzleLight.enabled = false;
            lightLeft = 0f;
            if (muzzleFlash) muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (damagedWeaponSmoke)
            {
                var emission = damagedWeaponSmoke.emission;
                emission.enabled = false;
                damagedWeaponSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (recoilVisualPivot && pivotSet) recoilVisualPivot.localPosition = restLocalPosition;
            // Disabling a gun must stop detached impact effects immediately, but
            // preserve pooled allocations for the same gun when it is re-enabled.
            for (int i = 0; i < impacts.Count; i++)
            {
                var impact = impacts[i];
                if (impact)
                {
                    impact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    impact.gameObject.SetActive(false);
                }
                impactExpiry[i] = 0f;
            }
        }
        void OnDestroy()
        {
            // Pool objects have no parent: reclaim them when the owning weapon dies.
            foreach (var impact in impacts)
            {
                if (!impact) continue;
                if (Application.isPlaying) Destroy(impact.gameObject);
                else DestroyImmediate(impact.gameObject);
            }
            impacts.Clear();
            impactExpiry.Clear();
            nextImpactIndex = 0;
        }
    }
}
