using UnityEngine;
using UnityEngine.Events;

namespace RoboRougelike.Mechanics.COV001
{
    /// <summary>Isolated breakable arena obstacle; never touches vehicle part durability.</summary>
    [DisallowMultipleComponent]
    public sealed class DestructibleArenaCover : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxDurability = 50f;
        [SerializeField] private UnityEvent onDamaged = new UnityEvent();
        [SerializeField] private UnityEvent onBroken = new UnityEvent();
        private Collider[] obstacleColliders;
        private bool[] initialColliderStates;

        public float Durability { get; private set; }
        public bool Broken => Durability <= 0f;

        private void Awake()
        {
            obstacleColliders = GetComponentsInChildren<Collider>(true);
            initialColliderStates = new bool[obstacleColliders.Length];
            for (int i = 0; i < obstacleColliders.Length; i++)
                initialColliderStates[i] = obstacleColliders[i].enabled;
            ResetCover();
        }

        public void ResetCover()
        {
            Durability = Mathf.Max(1f, maxDurability);
            if (obstacleColliders == null) return;
            for (int i = 0; i < obstacleColliders.Length; i++)
                if (obstacleColliders[i] != null)
                    obstacleColliders[i].enabled = initialColliderStates[i];
        }

        public bool ApplyDamage(float amount)
        {
            if (Broken || float.IsNaN(amount) || amount <= 0f) return false;
            Durability = Mathf.Max(0f, Durability - amount);
            onDamaged.Invoke();
            if (!Broken) return true;
            if (obstacleColliders != null)
                foreach (var collider in obstacleColliders)
                    if (collider != null) collider.enabled = false;
            onBroken.Invoke();
            return true;
        }
    }
}
