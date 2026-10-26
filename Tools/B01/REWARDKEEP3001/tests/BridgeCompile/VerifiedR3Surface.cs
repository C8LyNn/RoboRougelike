// Minimal pinned OBJ #210 API signatures only; NOT real Unity/game runtime.
using System.Collections.Generic;
namespace RoboRougelike.R3 {
 public sealed class R3PartState { public string Id; }
 public sealed class R3VehicleModel {
   public List<R3PartState> Parts {get;}=new();
   public IReadOnlyList<R3PartState> Inventory=>new List<R3PartState>();
 }
}
