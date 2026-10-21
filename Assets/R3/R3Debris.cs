using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.R3
{
    public sealed class R3Debris : MonoBehaviour
    {
        private static readonly Queue<R3Debris> pieces=new Queue<R3Debris>();
        // Detached wreckage is briefly non-solid so it cannot shield live modules.
        private readonly List<Collider> temporaryColliders=new List<Collider>();
        private System.Collections.IEnumerator ReenableColliders()
        {
            yield return new WaitForSeconds(.45f);
            foreach(var collider in temporaryColliders)if(collider!=null)collider.enabled=true;
            temporaryColliders.Clear();
        }
        private void Start()
        {
            foreach(var collider in GetComponentsInChildren<Collider>(true))
            {
                if(collider==null||!collider.enabled)continue;
                collider.enabled=false;temporaryColliders.Add(collider);
            }
            StartCoroutine(ReenableColliders());
        }
        private void OnDisable()
        {
            foreach(var collider in temporaryColliders)if(collider!=null)collider.enabled=true;
            temporaryColliders.Clear();
        }
        private void Awake()
        {
            pieces.Enqueue(this);
            while(pieces.Count>70){var old=pieces.Dequeue();if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}}
            Destroy(gameObject,10);
        }
    }
}
