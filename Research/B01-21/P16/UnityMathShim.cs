// Only the minimal UnityEngine mathematical API needed to compile and execute the
// unchanged original B01.MAPGEN001 planner, route validator and MAPGENWORLD policy.
// THIS IS NOT UNITY, URP, PHYSX, VEHICLE DYNAMICS OR NATIVE G1.
using System;
namespace UnityEngine
{
  public sealed class GameObject {}
  public struct Vector2
  {
    public float x,y;
    public Vector2(float x,float y){this.x=x;this.y=y;}
    public static Vector2 operator -(Vector2 a,Vector2 b)=>new(a.x-b.x,a.y-b.y);
    public static Vector2 operator /(Vector2 a,float f)=>new(a.x/f,a.y/f);
    public static float Distance(Vector2 a,Vector2 b) => MathF.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));
    public static float Dot(Vector2 a,Vector2 b)=>a.x*b.x+a.y*b.y;
  }
  public struct Vector3
  {
    public float x,y,z;
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public static Vector3 zero=>new(0,0,0);
    public static float Distance(Vector3 a,Vector3 b)=>
      MathF.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));
    public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>
      new(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t,a.z+(b.z-a.z)*t);
  }
  public struct Quaternion
  {
    private Vector3 angles;
    public Vector3 eulerAngles=>angles;
    public static Quaternion Euler(float x,float y,float z)=>new(){angles=new(x,y,z)};
  }
  public struct Bounds
  {
    public Vector3 center,size;
    public Bounds(Vector3 c,Vector3 s){center=c;size=s;}
    public Vector3 min=>new(center.x-size.x*.5f,center.y-size.y*.5f,center.z-size.z*.5f);
    public Vector3 max=>new(center.x+size.x*.5f,center.y+size.y*.5f,center.z+size.z*.5f);
  }
  public static class Mathf
  {
    public const float Deg2Rad=MathF.PI/180f;
    public static float Abs(float x)=>MathF.Abs(x);
    public static float Min(float a,float b)=>MathF.Min(a,b);
    public static float Max(float a,float b)=>MathF.Max(a,b);
    public static int Max(int a,int b)=>Math.Max(a,b);
    public static float Clamp(float x,float lo,float hi)=>Math.Clamp(x,lo,hi);
    public static float Acos(float x)=>MathF.Acos(x);
    public static float Tan(float x)=>MathF.Tan(x);
    public static float Cos(float x)=>MathF.Cos(x);
    public static float Sin(float x)=>MathF.Sin(x);
    public static int FloorToInt(float x)=>(int)MathF.Floor(x);
    public static int CeilToInt(float x)=>(int)MathF.Ceiling(x);
    // Unity's Mathf.RoundToInt rounds ties to even.
    public static int RoundToInt(float x)=>(int)MathF.Round(x,MidpointRounding.ToEven);
  }
}
