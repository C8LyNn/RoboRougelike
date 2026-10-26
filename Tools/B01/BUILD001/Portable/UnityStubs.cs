// Narrow fake Unity API to compile and execute THE REAL BUILD001 runtime C#.
// Geometry and penetration here are synthetic and NOT native Unity/PhysX evidence.
using System;
using System.Collections.Generic;
using Numerics = System.Numerics;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x=x; this.y=y; }
        public static Vector2 zero => new Vector2(0,0);
    }

    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public int x, y, z;
        public Vector3Int(int x,int y,int z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3Int zero => new Vector3Int(0,0,0);
        public static Vector3Int right => new Vector3Int(1,0,0);
        public static Vector3Int left => new Vector3Int(-1,0,0);
        public static Vector3Int up => new Vector3Int(0,1,0);
        public static Vector3Int down => new Vector3Int(0,-1,0);
        public static Vector3Int forward => new Vector3Int(0,0,1);
        public static Vector3Int back => new Vector3Int(0,0,-1);
        public static Vector3Int operator +(Vector3Int a, Vector3Int b) =>
            new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3Int operator -(Vector3Int a, Vector3Int b) =>
            new Vector3Int(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3Int operator -(Vector3Int a) => new Vector3Int(-a.x,-a.y,-a.z);
        public static Vector3Int operator *(Vector3Int a,int k) =>
            new Vector3Int(a.x*k,a.y*k,a.z*k);
        public static bool operator ==(Vector3Int a,Vector3Int b) => a.Equals(b);
        public static bool operator !=(Vector3Int a,Vector3Int b) => !a.Equals(b);
        public bool Equals(Vector3Int b) => x==b.x && y==b.y && z==b.z;
        public override bool Equals(object o) => o is Vector3Int b && Equals(b);
        public override int GetHashCode() => HashCode.Combine(x,y,z);
        public override string ToString() => $"({x},{y},{z})";
        public static explicit operator Vector3(Vector3Int a) => new Vector3(a.x,a.y,a.z);
    }

    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 zero => new Vector3(0,0,0);
        public static Vector3 right => new Vector3(1,0,0);
        public static Vector3 left => new Vector3(-1,0,0);
        public static Vector3 up => new Vector3(0,1,0);
        public static Vector3 forward => new Vector3(0,0,1);
        public static Vector3 operator +(Vector3 a,Vector3 b) =>
            new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b) =>
            new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float v) =>
            new Vector3(a.x*v,a.y*v,a.z*v);
        public static Vector3 operator *(float v,Vector3 a) => a*v;
        public static float Dot(Vector3 a,Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a,Vector3 b) =>
            new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public override string ToString() => $"({x},{y},{z})";
    }

    public struct Quaternion
    {
        private Numerics.Quaternion q;
        private Quaternion(Numerics.Quaternion q) { this.q=q; }
        public static Quaternion identity => new Quaternion(Numerics.Quaternion.Identity);
        public static Quaternion Euler(float x,float y,float z)
        {
            var yaw = Numerics.Quaternion.CreateFromAxisAngle(Numerics.Vector3.UnitY,y*MathF.PI/180f);
            var pitch = Numerics.Quaternion.CreateFromAxisAngle(Numerics.Vector3.UnitX,x*MathF.PI/180f);
            var roll = Numerics.Quaternion.CreateFromAxisAngle(Numerics.Vector3.UnitZ,z*MathF.PI/180f);
            return new Quaternion(Numerics.Quaternion.Normalize(roll*pitch*yaw));
        }
        public static Vector3 operator *(Quaternion q,Vector3 v)
        {
            var r=Numerics.Vector3.Transform(new Numerics.Vector3(v.x,v.y,v.z),q.q);
            return new Vector3(r.X,r.Y,r.Z);
        }
    }

    public static class Mathf
    {
        public static float Abs(float x) => MathF.Abs(x);
        public static int Abs(int x) => Math.Abs(x);
        public static float Max(float a,float b) => MathF.Max(a,b);
        public static int RoundToInt(float x) => (int)MathF.Round(x);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisallowMultipleComponent : Attribute { }

    public class Component
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
    }

    public class MonoBehaviour : Component { }

    public class Transform
    {
        public GameObject gameObject;
        public Vector3 position=Vector3.zero;
        public Quaternion rotation=Quaternion.identity;
        public Transform parent;
        public bool IsChildOf(Transform root)
        {
            for(var node=this;node!=null;node=node.parent)
                if(node==root)return true;
            return false;
        }
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
    }

    public sealed class GameObject
    {
        public bool activeInHierarchy=true;
        public readonly Transform transform;
        private readonly Dictionary<Type,Component> components = new();
        public GameObject()
        { transform=new Transform { gameObject=this }; }
        public T AddComponent<T>() where T:Component,new()
        {
            var component=new T { gameObject=this };
            components.Add(typeof(T),component);
            return component;
        }
        public T GetComponent<T>() where T:Component
        {
            foreach(var component in components.Values)
                if(component is T match) return match;
            return null;
        }
    }

    public class Collider : Component
    {
        public bool enabled=true;
        public bool isTrigger;
        public Vector3 Half=new Vector3(.26f,.26f,.26f);
    }
    public sealed class BoxCollider : Collider { }
    public sealed class SphereCollider : Collider { }
    public sealed class CapsuleCollider : Collider { }
    public sealed class MeshCollider : Collider { public bool convex; }

    public struct RaycastHit
    {
        public Collider collider;
        public Vector3 point;
        public Vector3 normal;
    }

    public static class Physics
    {
        public static void SyncTransforms() { }
        // AABB-only fake depth (no rotation/meshes); never label native PhysX.
        public static bool ComputePenetration(Collider a,Vector3 posA,Quaternion rotA,
            Collider b,Vector3 posB,Quaternion rotB,out Vector3 direction,out float depth)
        {
            direction=Vector3.right;
            float px=a.Half.x+b.Half.x-MathF.Abs(posA.x-posB.x);
            float py=a.Half.y+b.Half.y-MathF.Abs(posA.y-posB.y);
            float pz=a.Half.z+b.Half.z-MathF.Abs(posA.z-posB.z);
            depth=MathF.Min(px,MathF.Min(py,pz));
            return depth>0f;
        }
    }
}
