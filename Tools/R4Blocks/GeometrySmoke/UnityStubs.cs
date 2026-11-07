// Minimal identity-pose stand-ins used only for the offline smoke test.
// Rotation is deliberately unsupported; no claim is made about Unity transforms.
using System;
namespace UnityEngine
{
    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y){this.x=x;this.y=y;}
    }
    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public int x,y,z;
        public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}
        public static Vector3Int zero=>new Vector3Int(0,0,0);
        public static Vector3Int right=>new Vector3Int(1,0,0);
        public static Vector3Int left=>new Vector3Int(-1,0,0);
        public static Vector3Int operator +(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3Int operator -(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3Int operator -(Vector3Int a)=>new Vector3Int(-a.x,-a.y,-a.z);
        public static bool operator ==(Vector3Int a,Vector3Int b)=>a.Equals(b);
        public static bool operator !=(Vector3Int a,Vector3Int b)=>!a.Equals(b);
        public bool Equals(Vector3Int other)=>x==other.x&&y==other.y&&z==other.z;
        public override bool Equals(object other)=>other is Vector3Int v&&Equals(v);
        public override int GetHashCode()=>HashCode.Combine(x,y,z);
        public static implicit operator Vector3(Vector3Int v)=>new Vector3(v.x,v.y,v.z);
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 up=>new Vector3(0,1,0);
        public static Vector3 right=>new Vector3(1,0,0);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float n)=>new Vector3(a.x*n,a.y*n,a.z*n);
        public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    }
    public struct Quaternion
    {
        public static Quaternion Euler(float x,float y,float z)
        {
            if(x!=0||y!=0||z!=0)throw new NotSupportedException("Only identity poses are supported in this smoke test.");
            return new Quaternion();
        }
        public static Vector3 operator *(Quaternion unused,Vector3 v)=>v;
    }
    public static class Mathf
    {
        public static int RoundToInt(float f)=>(int)Math.Round(f);
        public static int Abs(int value)=>Math.Abs(value);
        public static float Abs(float value)=>Math.Abs(value);
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static float Max(float a,float b)=>Math.Max(a,b);
    }
}
