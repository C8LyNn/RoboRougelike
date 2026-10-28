// This file supplies NOT-production stand-ins solely for unrelated Unity API
// types/mounting math so ORIGINAL R3VehicleModel and R4WeaponDefinition compile.
// The executable cases exercise only original recovery, repair, dismantle,
// wallet & weapon price methods; NO physical placement/socket behavior is claimed.
namespace UnityEngine
{
    public struct Vector3Int : System.IEquatable<Vector3Int>
    {
        public int x,y,z;
        public Vector3Int(int a,int b,int c){x=a;y=b;z=c;}
        public static Vector3Int zero=>new(0,0,0);
        public static Vector3Int left=>new(-1,0,0);
        public static Vector3Int right=>new(1,0,0);
        public static Vector3Int up=>new(0,1,0);
        public static Vector3Int down=>new(0,-1,0);
        public static Vector3Int forward=>new(0,0,1);
        public static Vector3Int back=>new(0,0,-1);
        public static Vector3Int operator +(Vector3Int a,Vector3Int b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3Int operator -(Vector3Int a,Vector3Int b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3Int operator -(Vector3Int a)=>new(-a.x,-a.y,-a.z);
        public static bool operator ==(Vector3Int a,Vector3Int b)=>a.Equals(b);
        public static bool operator !=(Vector3Int a,Vector3Int b)=>!a.Equals(b);
        public bool Equals(Vector3Int other)=>x==other.x&&y==other.y&&z==other.z;
        public override bool Equals(object other)=>other is Vector3Int v&&Equals(v);
        public override int GetHashCode()=>System.HashCode.Combine(x,y,z);
    }
    public readonly struct Color
    {
        public readonly float r,g,b,a;
        public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
    }
    public static class Mathf
    {
        public static int CeilToInt(float x)=>(int)System.MathF.Ceiling(x);
        public static int FloorToInt(float x)=>(int)System.MathF.Floor(x);
        public static float Clamp01(float x)=>System.Math.Clamp(x,0f,1f);
    }
}
namespace RoboRougelike.Mechanics.IndependentSteering { public enum SteeringMode { None } }
namespace RoboRougelike.Mechanics.MOV009 { public enum ActiveSuspensionSupportMode { FullSupport } }
namespace RoboRougelike.R1
{
    public enum R1UpgradeEffect { Hull, Damage, Reload, Mobility, Repair, Core }
    public sealed class R1UpgradeDefinition
    {
        public string Id,Title,Description;
        public R1UpgradeEffect Effect;
        public float Amount;
    }
}
namespace RoboRougelike.R3
{
    // NOT REAL physical topology; tests must NOT exercise Mount/Buy/placement.
    // Signature doubles only to satisfy compilation of the original full R3 file.
    public static class R3MountingGrid
    {
        public const int AllFaces=63;
        public static UnityEngine.Vector3Int Rotate(UnityEngine.Vector3Int v,UnityEngine.Vector3Int r)=>v;
        public static UnityEngine.Vector3Int Normalize(UnityEngine.Vector3Int r)=>r;
        public static int WorldPortMask(UnityEngine.Vector3Int r,int c)=>c;
        public static int FaceBit(UnityEngine.Vector3Int d)=>1;
        public static bool PortOnFace(R3PartState p,UnityEngine.Vector3Int d)=>p!=null;
        public static bool Compatible(R3PartState a,R3PartState b,UnityEngine.Vector3Int d)=>a!=null&&b!=null;
    }
}
