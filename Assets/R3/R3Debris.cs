using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.R3
{
    public sealed class R3Debris : MonoBehaviour
    {
        private static readonly Queue<R3Debris> pieces=new Queue<R3Debris>();
        public static event System.Action<R3Debris> Separated;
        // Detached wreckage is briefly non-solid so it cannot shield live modules.
        private readonly List<Collider> temporaryColliders=new List<Collider>();
        private Rigidbody debrisBody;
        private bool originalGravity;
        private System.Collections.IEnumerator ReenableColliders()
        {
            yield return new WaitForSeconds(.45f);
            foreach(var collider in temporaryColliders)if(collider!=null)collider.enabled=true;
            temporaryColliders.Clear();
            if(debrisBody!=null)debrisBody.useGravity=originalGravity;
        }
        private void BeginSeparation()
        {
            debrisBody=GetComponent<Rigidbody>();
            if(debrisBody!=null){originalGravity=debrisBody.useGravity;debrisBody.useGravity=false;}
            foreach(var collider in GetComponentsInChildren<Collider>(true))
            {
                if(collider==null||!collider.enabled)continue;
                collider.enabled=false;temporaryColliders.Add(collider);
            }
            StartCoroutine(ReenableColliders());
        }
        private void OnDisable()
        {
            if(debrisBody!=null)debrisBody.useGravity=originalGravity;
            foreach(var collider in temporaryColliders)if(collider!=null)collider.enabled=true;
            temporaryColliders.Clear();
        }
        private void Awake()
        {
            // Awake runs immediately on detachment, before another collision in this frame.
            BeginSeparation();
            pieces.Enqueue(this);
            while(pieces.Count>70){var old=pieces.Dequeue();if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}}
            Destroy(gameObject,10);
            try{Separated?.Invoke(this);}catch(System.Exception error){Debug.LogException(error);}
        }
    }
}
