using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.Defenses.DirectionalShield
{
    /// <summary>
    /// Mostek od prawdziwego hit pipeline R6. Jeden receiver na konstrukcję.
    /// R6 nadal decyduje o drużynach, HP, penetracji, odłączaniu i salwage.
    /// </summary>
    public sealed class DirectionalShieldReceiver : MonoBehaviour
    {
        [SerializeField] private Rigidbody ownerBody;
        [SerializeField] private bool scanChildrenOnAwake = true;
        private readonly List<DirectionalShieldGenerator> _generators = new List<DirectionalShieldGenerator>();
        private bool _dirty;
        public Rigidbody OwnerBody => ownerBody;
        public int RegisteredCount => _generators.Count;

        private void Awake()
        {
            if (ownerBody == null) ownerBody = GetComponent<Rigidbody>();
            if (scanChildrenOnAwake)
            {
                var found = GetComponentsInChildren<DirectionalShieldGenerator>(true);
                foreach (var item in found) Register(item);
            }
        }

        public void Bind(Rigidbody body) => ownerBody = body;

        public void Register(DirectionalShieldGenerator generator)
        {
            if (generator == null || _generators.Contains(generator)) return;
            _generators.Add(generator);
            _dirty = true;
        }

        public void Unregister(DirectionalShieldGenerator generator)
        {
            if (generator == null) return;
            if (_generators.Remove(generator)) _dirty = true;
        }

        private void SortIfNeeded()
        {
            // Priorytet + stały tie-break wewnątrz uruchomienia. Zmianę Priority zgłoś ponowną rejestracją.
            if (!_dirty) return;
            _generators.Sort((a, b) =>
            {
                if (a == null) return b == null ? 0 : 1;
                if (b == null) return -1;
                int order = b.Priority.CompareTo(a.Priority);
                return order != 0 ? order : a.GetInstanceID().CompareTo(b.GetInstanceID());
            });
            _dirty = false;
        }

        /// <summary>
        /// Wywołać RAZ dla unikatowego trafienia po rozstrzygnięciu friendly fire, przed ApplyDamage R6.
        /// Zwraca resztkowe obrażenia dla R6; NIE aplikuje HP.
        /// </summary>
        public float Mitigate(float rawDamage, Vector3 hitPoint, Vector3 projectileTravelDirection,
            out float absorbedDamage)
        {
            absorbedDamage = 0f;
            if (!ShieldMath.Finite(rawDamage) || rawDamage <= 0f ||
                !ShieldMath.Finite(hitPoint) || !ShieldMath.Finite(projectileTravelDirection))
                return rawDamage;
            if (ownerBody == null) return rawDamage;

            SortIfNeeded();
            float remaining = rawDamage;
            for (int i = 0; i < _generators.Count && remaining > 0f; i++)
            {
                var shield = _generators[i];
                if (shield == null || !shield.Operational || shield.OwnerBody != ownerBody) continue;
                float stopped = shield.Absorb(remaining, hitPoint, projectileTravelDirection);
                stopped = Mathf.Clamp(stopped, 0f, remaining);
                remaining = Mathf.Max(0f, remaining - stopped);
                absorbedDamage += stopped;
            }
            return remaining;
        }
    }
}
