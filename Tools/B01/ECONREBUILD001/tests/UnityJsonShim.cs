// Explicit .NET8 serialization stand-in. NOT Unity engine.
using System.Text.Json;
namespace UnityEngine
{
 public static class JsonUtility
 {
  private static readonly JsonSerializerOptions Opt=new() { IncludeFields=true,WriteIndented=true };
  public static string ToJson(object x,bool pretty=false)=>JsonSerializer.Serialize(x,Opt);
  public static T FromJson<T>(string s)=>JsonSerializer.Deserialize<T>(s,Opt);
 }
}
