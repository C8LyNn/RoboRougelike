// TEST ONLY: .NET8 JsonUtility surrogate. NOT Unity Engine or PhysX.
using System.Text.Json;
namespace UnityEngine {
 public static class JsonUtility {
  static readonly JsonSerializerOptions Opt=new JsonSerializerOptions {IncludeFields=true,PropertyNameCaseInsensitive=true,WriteIndented=true};
  public static string ToJson(object value,bool prettyPrint=false)=>JsonSerializer.Serialize(value,Opt);
  public static T FromJson<T>(string value)=>JsonSerializer.Deserialize<T>(value,Opt);
 }
}
