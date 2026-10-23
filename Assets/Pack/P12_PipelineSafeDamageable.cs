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
            maxHp = baseMaxHp * Mathf.Clamp(multiplier, 0.1f, 12f);
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

            currentHp -= Mathf.Max(0f, amount);
            RoboRougelike.R6.R6CombatFeedback.Hit(this,hitPoint,currentHp<=0);

            if (currentHp <= 0f)
            {
                Die(hitPoint, hitDirection);
                return amount;
            }

            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(DamageFlash());
            return amount;
        }

        private IEnumerator DamageFlash()
        {
            SetColor(
                Color.Lerp(
                    ReadBaseColor(),
                    Color.red,
                    0.72f
                )
            );

            yield return new WaitForSeconds(0.09f);

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
        }

        private void Die(
            Vector3 hitPoint,
            Vector3 hitDirection)
        {
            var owner=GetComponentInParent<Rigidbody>();
            Vector3 inherited=owner!=null?owner.GetPointVelocity(transform.position):Vector3.zero;
            Vector3 spin=owner!=null?owner.angularVelocity:Vector3.zero;
            float releasedMass=RoboRougelike.R6.R6VehicleDrive.PartMass(this);
            dead = true;
            if(flashRoutine!=null)StopCoroutine(flashRoutine);
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
            if(GetComponent<RoboRougelike.R3.R3Debris>()==null)gameObject.AddComponent<RoboRougelike.R3.R3Debris>();
            body.collisionDetectionMode =
                CollisionDetectionMode.Continuous;

            // Push destroyed blocks clear of the chassis and firing lane.
            RoboRougelike.R6.R6DebrisRelease.Eject(body,owner,hitDirection);

            Destroy(gameObject, debrisLifetime);
        }
    }
}
