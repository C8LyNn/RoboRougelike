// .NET8-only Unity JsonUtility SURROGATE: native Unity tests are NOT_RUN by this harness.
using System.Text.Json;
namespace UnityEngine {
 public static class JsonUtility {
   private static readonly JsonSerializerOptions Options=new JsonSerializerOptions { IncludeFields=true, WriteIndented=true, PropertyNameCaseInsensitive=true };
   public static string ToJson(object value,bool prettyPrint=false) => JsonSerializer.Serialize(value,Options);
   public static T FromJson<T>(string value) => JsonSerializer.Deserialize<T>(value,Options);
 }
}
