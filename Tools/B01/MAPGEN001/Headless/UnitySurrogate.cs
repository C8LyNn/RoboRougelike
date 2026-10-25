// MINIMAL UnityEngine API surrogate ONLY for independent .NET planner/validator tests.
// DO NOT ship as Unity runtime code or treat it as PhysX/Unity compilation proof.
using System;
namespace UnityEngine
{
    public class GameObject {}
    public struct Vector2
    {
        public float x,y;
        public Vector2(float xx,float yy) { x=xx; y=yy; }
        public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);
        public static Vector2 operator /(Vector2 a,float d)=>new Vector2(a.x/d,a.y/d);
        public static float Dot(Vector2 a,Vector2 b)=>a.x*b.x+a.y*b.y;
        public static float Distance(Vector2 a,Vector2 b)=>MathF.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float xx,float yy,float zz){x=xx;y=yy;z=zz;}
        public static Vector3 zero => new Vector3(0,0,0);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static float Distance(Vector3 a,Vector3 b)=>MathF.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));
        public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>new Vector3(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t,a.z+(b.z-a.z)*t);
        public override string ToString()=>$"({x:F3},{y:F3},{z:F3})";
    }
    public struct Quaternion
    {
        private float xAngle;
        public Vector3 eulerAngles => new Vector3(((xAngle%360)+360)%360,0,0);
        public static Quaternion Euler(float x,float y,float z)=>new Quaternion { xAngle=x };
        public static Quaternion identity=>new Quaternion();
    }
    public struct Bounds
    {
        public Vector3 center,size;
        public Bounds(Vector3 c,Vector3 s){center=c;size=s;}
        public Vector3 min => new Vector3(center.x-size.x/2,center.y-size.y/2,center.z-size.z/2);
        public Vector3 max => new Vector3(center.x+size.x/2,center.y+size.y/2,center.z+size.z/2);
    }
    public static class Mathf
    {
        public const float Deg2Rad=MathF.PI/180f;
        public static float Abs(float x)=>MathF.Abs(x);
        public static float Min(float a,float b)=>MathF.Min(a,b);
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Max(float a,float b)=>MathF.Max(a,b);
        public static float Clamp(float x,float lo,float hi)=>MathF.Max(lo,MathF.Min(hi,x));
        public static float Acos(float v)=>MathF.Acos(v);
        public static float Tan(float v)=>MathF.Tan(v);
        public static float Sin(float v)=>MathF.Sin(v);
        public static float Cos(float v)=>MathF.Cos(v);
        public static int FloorToInt(float x)=>(int)MathF.Floor(x);
        public static int CeilToInt(float x)=>(int)MathF.Ceiling(x);
        public static int RoundToInt(float x)=>(int)MathF.Round(x,MidpointRounding.ToEven);
    }
}
