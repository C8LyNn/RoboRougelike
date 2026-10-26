// Minimal named math surrogate for exact CAMERA diagnostic source C#.
// This does not execute UnityEngine, Physics, Test Framework or rendering.
using System;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float a,float b,float c){x=a;y=b;z=c;}
        public static Vector3 zero => new Vector3(0f,0f,0f);
        public static Vector3 forward => new Vector3(0f,0f,1f);
        public static Vector3 right => new Vector3(1f,0f,0f);
        public float sqrMagnitude => x*x+y*y+z*z;
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static float Angle(Vector3 a,Vector3 b)
        {
            double aa=Math.Sqrt(a.sqrMagnitude),bb=Math.Sqrt(b.sqrMagnitude);
            if(aa<1e-12||bb<1e-12)return 0f;
            double dot=(a.x*(double)b.x+a.y*(double)b.y+a.z*(double)b.z)/(aa*bb);
            return (float)(Math.Acos(Math.Clamp(dot,-1d,1d))*180d/Math.PI);
        }
    }
    public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);}
}
