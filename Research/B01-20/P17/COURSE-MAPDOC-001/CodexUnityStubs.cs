// .NET-only substitutes for native UnityEngine classes. DO NOT install in Assets/.
using System;
using System.Text.Json;
namespace UnityEngine
{
 public readonly struct Vector2Int : IEquatable<Vector2Int>
 {
  public readonly int x,y;
  public Vector2Int(int x,int y){this.x=x;this.y=y;}
  public bool Equals(Vector2Int v)=>x==v.x&&y==v.y;
  public override bool Equals(object? obj)=>obj is Vector2Int v&&Equals(v);
  public override int GetHashCode()=>HashCode.Combine(x,y);
  public static bool operator ==(Vector2Int a,Vector2Int b)=>a.Equals(b);
  public static bool operator !=(Vector2Int a,Vector2Int b)=>!a.Equals(b);
 }
 public static class JsonUtility
 {
  public static T? FromJson<T>(string value)=>JsonSerializer.Deserialize<T>(value,new JsonSerializerOptions{IncludeFields=true});
  public static string ToJson(object v,bool prettyPrint=false)=>JsonSerializer.Serialize(v,new JsonSerializerOptions{IncludeFields=true,WriteIndented=prettyPrint});
 }
}
