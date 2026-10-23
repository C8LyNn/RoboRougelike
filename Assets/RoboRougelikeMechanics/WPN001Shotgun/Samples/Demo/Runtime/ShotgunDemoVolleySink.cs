using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.WPN001Shotgun.Demo
{
    /// <summary>Only for an isolated demonstration scene. Do not wire into R6.</summary>
    [DisallowMultipleComponent]
    public sealed class ShotgunDemoVolleySink : MonoBehaviour, IShotgunVolleySink
    {
        [SerializeField, Range(0.04f, 0.3f)] private float pelletDiameter = 0.10f;
        private readonly List<GameObject> spawned = new List<GameObject>(64);

        public bool TrySpawnVolley(IReadOnlyList<ShotgunPelletSpawn> pellets)
        {
            if (!isActiveAndEnabled || pellets == null || pellets.Count == 0) return false;
            spawned.Clear();
            try
            {
                for (int i = 0; i < pellets.Count; i++)
                {
                    ShotgunPelletSpawn spawn = pellets[i];
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    spawned.Add(go);
                    go.name = "WPN001 DEMO pellet " + i;
                    go.transform.position = spawn.Origin;
                    go.transform.localScale = Vector3.one * pelletDiameter;
                    Rigidbody body = go.AddComponent<Rigidbody>();
                    body.mass = 0.05f;
                    Collider pelletCollider = go.GetComponent<Collider>();
                    if (spawn.OwnerRoot != null)
                    {
                        Collider[] own = spawn.OwnerRoot.GetComponentsInChildren<Collider>(true);
                        foreach (Collider collider in own)
                            if (collider != null && collider != pelletCollider)
                                Physics.IgnoreCollision(pelletCollider, collider, true);
                    }
                    // Pellets of one volley start close together: never collide with each other.
                    foreach (GameObject earlier in spawned)
                    {
                        if (earlier == null || earlier == go) continue;
                        Collider earlierCollider = earlier.GetComponent<Collider>();
                        if (earlierCollider != null)
                            Physics.IgnoreCollision(pelletCollider, earlierCollider, true);
                    }
                    ShotgunDemoProjectile demo = go.AddComponent<ShotgunDemoProjectile>();
                    demo.Initialize(spawn);
                }
                spawned.Clear();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
                foreach (GameObject go in spawned)
                    if (go != null) Destroy(go);
                spawned.Clear();
                return false;
            }
        }
    }
}
