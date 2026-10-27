#if UNITY_EDITOR
using UnityEngine;
using RoboRougelike.Mechanics.MOV009;

// Test observer runs before the package's normal-order force producer.
[DefaultExecutionOrder(-100)]
public sealed class MOV009GuardProbe : MonoBehaviour
{
    public ActiveSuspensionController Controller;
    public bool SawFullSupport;
    public int Ticks;
    void FixedUpdate(){if(Controller==null)return;Ticks++;SawFullSupport|=Controller.SupportMode==ActiveSuspensionSupportMode.FullSupport;}
}
#endif
