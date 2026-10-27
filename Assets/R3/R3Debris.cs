using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.R3
{
    public sealed class R3Debris : MonoBehaviour
    {
        private static readonly Queue<R3Debris> pieces=new Queue<R3Debris>();
        private void Awake()
        {
            pieces.Enqueue(this);
            while(pieces.Count>70){var old=pieces.Dequeue();if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}}
            Destroy(gameObject,10);
        }
    }
}
