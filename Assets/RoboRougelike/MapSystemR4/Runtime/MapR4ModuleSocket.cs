using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    // Attach to child transform facing OUTWARD from a modular prefab.
    public sealed class MapR4ModuleSocket : MonoBehaviour
    {
        [Min(0.1f)] public float clearWidth = 7f;
        [Min(0.1f)] public float clearHeight = 5f;
        [Min(0f)] public float maxSlopeDifferenceDeg = 5f;
    }
}
