using UnityEngine;

namespace B01.BUILD001
{
    /// <summary>Trusted prefab-author marker for exactly one collider/socket.
    /// Decorative colliders and renderer descendants must not carry this marker.</summary>
    [DisallowMultipleComponent]
    public sealed class BuildMountSurface : MonoBehaviour
    {
        public string PartInstanceId;
        public int SocketIndex = -1;
    }
}
