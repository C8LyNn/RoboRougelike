using UnityEngine;
namespace RoboRougelike.B01.Legendary {
 public sealed class LegendarySmokeExample : MonoBehaviour {
  [ContextMenu("Validate Legendary Catalogue")]
  public void ValidateCatalogue() {
   var definitions=LegendaryCatalog.CreatePilotDefinitions();
   if(LegendaryCatalog.Validate(definitions,out var reason))
    Debug.Log("LEGENDARY-UNITY-001 catalogue valid. This is NOT physics, ability or R3 integration.");
   else Debug.LogError(reason);
  }
 }
}
