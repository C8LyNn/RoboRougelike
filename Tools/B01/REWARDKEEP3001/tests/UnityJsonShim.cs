using System.Text.Json;
namespace UnityEngine {
 public static class JsonUtility {
  static readonly JsonSerializerOptions Options=new(){IncludeFields=true,WriteIndented=true};
  public static string ToJson(object o,bool pretty=false)=>JsonSerializer.Serialize(o,Options);
  public static T FromJson<T>(string json)=>JsonSerializer.Deserialize<T>(json,Options);
 }
}
