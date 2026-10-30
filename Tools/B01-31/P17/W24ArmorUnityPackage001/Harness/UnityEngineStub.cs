// DELIBERATELY SYNTHETIC STUB. This runner is NOT Unity, PhysX, imported Mesh or native G1.
using System;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute {}
    public class MonoBehaviour {}
    public sealed class GameObject
    {
        public bool activeSelf { get; private set; }
        public GameObject(bool active = false) { activeSelf = active; }
        public void SetActive(bool state) => activeSelf = state;
    }
}
