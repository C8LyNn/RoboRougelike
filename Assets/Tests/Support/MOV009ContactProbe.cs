#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class MOV009ContactProbe : MonoBehaviour
{
    readonly Dictionary<int,string> contacts=new Dictionary<int,string>();
    public string Summary=>string.Join(" | ",contacts.Values);
    void OnCollisionStay(Collision hit){contacts[hit.collider.GetInstanceID()]=$"{hit.collider.name} impulse={hit.impulse:F2}:"+string.Join(";",hit.contacts.Select(p=>$"{p.thisCollider.name} material={p.thisCollider.sharedMaterial?.name} mu={p.thisCollider.sharedMaterial?.staticFriction} at {p.point:F2} n={p.normal:F2} gap={p.separation:F4}"));}
    void OnCollisionExit(Collision hit){contacts.Remove(hit.collider.GetInstanceID());}
}
#endif
