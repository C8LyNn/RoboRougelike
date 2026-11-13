using System.Collections;
using UnityEngine;

namespace RogueMachine.P12PipelineSafe
{
    [DisallowMultipleComponent]
    public class P12PipelineSafeDamageable : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float currentHp = 100f;
        [SerializeField] private bool detachOnDeath = true;
        [SerializeField] private float debrisLifetime = 5f;

        private Renderer cachedRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Color baseColor = Color.white;
        private bool explicitTint;
        public void SetBaseTint(Color color){baseColor=color;explicitTint=true;SetColor(DamagedColor());}
        private Color ReadBaseColor()
        {
            if(!explicitTint&&cachedRenderer!=null&&cachedRenderer.sharedMaterial!=null)
            {
                var material=cachedRenderer.sharedMaterial;
                if(material.HasProperty("_BaseColor"))baseColor=material.GetColor("_BaseColor");
                else if(material.HasProperty("_Color"))baseColor=material.GetColor("_Color");
            }
            return baseColor;
        }
        private Coroutine flashRoutine;
        public bool VisualFlashing { get; private set; }
        public event System.Action VisualDamageChanged;
        private bool dead;
        private float baseMaxHp;
        public event System.Action Died;

        public bool IsDead => dead;
        public float MaxHp => maxHp;
        public float CurrentHp => currentHp;
        public void RestoreHealthRatio(float ratio)
        {
            if (!dead && !float.IsNaN(ratio) && !float.IsInfinity(ratio))
                currentHp = maxHp * Mathf.Clamp(ratio, 0.001f, 1);
            if(!dead)SetColor(DamagedColor());
        }

        public void SetDurabilityMultiplier(float multiplier)
        {
            if (dead) return;
            if (baseMaxHp <= 0f) baseMaxHp = Mathf.Max(1f, maxHp);
            float ratio = Mathf.Clamp01(currentHp / Mathf.Max(1f, maxHp));
            if(float.IsNaN(multiplier)||float.IsInfinity(multiplier))return;
            maxHp = (float)System.Math.Min(float.MaxValue,(double)baseMaxHp*Mathf.Max(.0001f,multiplier));
            currentHp = maxHp * ratio;
            SetColor(DamagedColor());
        }

        public void Configure(float hp, bool shouldDetach)
        {
            maxHp = Mathf.Max(1f, hp);
            baseMaxHp = maxHp;
            currentHp = maxHp;
            detachOnDeath = shouldDetach;
            SetColor(DamagedColor());
        }

        private void Awake()
        {
            baseMaxHp = Mathf.Max(1f, maxHp);
            if(GetComponent<RoboRougelike.WeaponIntegration.RRLRocketDamageReceiver>()==null)
                gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLRocketDamageReceiver>();
            cachedRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();

            if (cachedRenderer != null &&
                cachedRenderer.sharedMaterial != null)
            {
                Material material = cachedRenderer.sharedMaterial;

                if (material.HasProperty("_BaseColor"))
                    baseColor = material.GetColor("_BaseColor");
                else if (material.HasProperty("_Color"))
                    baseColor = material.GetColor("_Color");
            }
        }

        public void ApplyDamage(float amount,Vector3 hitPoint,Vector3 hitDirection)
        { ApplyDamageResolved(amount,hitPoint,hitDirection); }

        public float ApplyDamageResolved(
            float amount,
            Vector3 hitPoint,
            Vector3 hitDirection)
        { return ApplyDamageResolvedTyped(amount,hitPoint,hitDirection,RoboRougelike.R3.R3DamagePropagation.DamageKind.Kinetic); }

        public float ApplyDamageResolvedTyped(float amount,Vector3 hitPoint,Vector3 hitDirection,
            RoboRougelike.R3.R3DamagePropagation.DamageKind damageKind)
        {
            if (dead || amount<=0 || float.IsNaN(amount) || float.IsInfinity(amount))
                return 0;

            var filter=GetComponentInParent<RoboRougelike.WeaponIntegration.IRRLPreHpFilter>();
            if(filter!=null)
            {
                float filtered=filter.FilterDamage(this,amount,hitPoint,hitDirection);
                if(filtered<=0||float.IsNaN(filtered)||float.IsInfinity(filtered))return 0;
                amount=Mathf.Min(amount,filtered);
            }

            var structure=GetComponentInParent<P12PipelineSafeVehicleStructure>();
            if(structure!=null&&structure.TryRouteDamage(this,amount,hitPoint,hitDirection,damageKind))
                return amount;

            StageRoutedDamage(amount);
            FinishRoutedDamage(hitPoint,hitDirection);
            return amount;
        }

        // An impact stages all connected HP changes before any death callback
        // can detach pieces or alter the graph used by this same impact.
        internal void StageRoutedDamage(float amount)
        { if(!dead)currentHp=Mathf.Max(0,currentHp-Mathf.Max(0,amount)); }

        internal void FinishRoutedDamage(Vector3 hitPoint,Vector3 hitDirection)
        {
            if(dead)return;
            RoboRougelike.R6.R6CombatFeedback.Hit(this,hitPoint,currentHp<=0);

            if (currentHp <= 0f)
            {
                Die(hitPoint, hitDirection);
                return;
            }

            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(DamageFlash());
        }

        private IEnumerator DamageFlash()
        {
            VisualFlashing=true;
            SetColor(
                Color.Lerp(
                    ReadBaseColor(),
                    Color.red,
                    0.72f
                )
            );

            yield return new WaitForSeconds(0.09f);

            VisualFlashing=false;
            SetColor(DamagedColor());
            flashRoutine = null;
        }

        private Color DamagedColor()=>Color.Lerp(ReadBaseColor(),new Color(.16f,.09f,.06f),Mathf.Clamp01(1-currentHp/Mathf.Max(1,maxHp))*.8f);
        private void SetColor(Color color)
        {
            if (cachedRenderer == null)
                return;

            cachedRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);

            cachedRenderer.SetPropertyBlock(propertyBlock);
            VisualDamageChanged?.Invoke();
        }

        private void Die(
            Vector3 hitPoint,
            Vector3 hitDirection)
        {
            var owner=GetComponentInParent<Rigidbody>();
            Vector3 inherited=owner!=null?owner.GetPointVelocity(transform.position):Vector3.zero;
            Vector3 spin=owner!=null?owner.angularVelocity:Vector3.zero;
            float releasedMass=0;
            foreach(var part in GetComponentsInChildren<P12PipelineSafeDamageable>())
                if(!part.IsDead&&part.GetComponentInParent<Rigidbody>()==owner)
                    releasedMass+=RoboRougelike.R6.R6VehicleDrive.PartMass(part);
            dead = true;
            if(flashRoutine!=null)StopCoroutine(flashRoutine);
            VisualFlashing=false;
            SetColor(new Color(.12f,.1f,.09f));
            // Reward snapshots must observe equipment before structural callbacks detach it.
            Died?.Invoke();

            if (!detachOnDeath)
            {
                Destroy(gameObject);
                return;
            }

            transform.SetParent(null, true);

            Rigidbody body = GetComponent<Rigidbody>();

            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();

            RoboRougelike.R6.R6DebrisRelease.Release(body,owner,inherited,spin,releasedMass);
            if(owner!=null)owner.GetComponent<RoboRougelike.R6.R6VehicleDrive>()?.RefreshMass();
            if(GetComponent<RoboRougelike.R3.R3Debris>()==null)gameObject.AddComponent<RoboRougelike.R3.R3Debris>();
            body.collisionDetectionMode =
                CollisionDetectionMode.Continuous;

            // Push destroyed blocks clear of the chassis and firing lane.
            RoboRougelike.R6.R6DebrisRelease.Eject(body,owner,hitDirection);

            Destroy(gameObject, debrisLifetime);
        }
    }
}
