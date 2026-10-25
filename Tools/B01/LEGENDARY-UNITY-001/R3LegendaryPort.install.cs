// INSTALL-ONLY: copy to host Assembly-CSharp after B01-00 G2. No assets modified here.
// OBJ #210 verified API. Missing trusted BUILD callbacks => fail closed, no wildcards.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.B01.Legendary;

public sealed class R3LegendaryPort : IAuthoritativeR3Port {
 private readonly R3VehicleModel model;
 private readonly Func<string,string,LegendaryPlacement,bool> socketProof;
 private readonly Func<string,LegendaryPlacement,bool> fullFootprintProof;
 public string RobotId {get;private set;}
 public R3LegendaryPort(string robotId,R3VehicleModel existing,
   Func<string,string,LegendaryPlacement,bool> certifiedSocket,
   Func<string,LegendaryPlacement,bool> certifiedFullFootprint) {
  RobotId=robotId;model=existing;socketProof=certifiedSocket;fullFootprintProof=certifiedFullFootprint;
 }
 private static PartObservation View(R3PartState p) =>
  new PartObservation(p.Id,p.Kind.ToString(),p.Kind==R3PartKind.Gun?p.Weapon.ToString():null,p.Health);
 public IReadOnlyList<PartObservation> Mounted => model.Parts.Select(View).ToArray();
 public IReadOnlyList<PartObservation> Inventory => model.Inventory.Select(View).ToArray();
 private static Vector3Int Cell(LegendaryPlacement p)=>new Vector3Int(p.X,p.Y,p.Z);
 private static Vector3Int Rotation(LegendaryPlacement p)=>new Vector3Int(p.RotX,p.RotY,p.RotZ);
 public bool HasCertifiedSocket(string kind,string token,LegendaryPlacement p) =>
  (kind=="WeaponBase"||kind=="MobilityHub")&&socketProof!=null&&!string.IsNullOrEmpty(token)&&socketProof(kind,token,p);
 public bool CanMountExisting(string id,LegendaryPlacement p,out string e) {
  var part=model.Inventory.FirstOrDefault(x=>x.Id==id);
  if(part==null){e="R3_NOT_IN_INVENTORY";return false;}
  if(fullFootprintProof==null||!fullFootprintProof(id,p)){e="FOOTPRINT_ADAPTER_PENDING";return false;}
  e=model.PlacementError(Cell(p),part.Kind,true,part.Weapon,Rotation(p),part.ContactMask);
  if(e!=null)return false;e="OK";return true;
 }
 public bool MountExisting(string id,LegendaryPlacement p,out string e) {
  if(!CanMountExisting(id,p,out e))return false;
  if(!model.Mount(id,Cell(p),Rotation(p))){e="R3_MOUNT_REJECT";return false;}
  e="OK";return true;
 }
 public bool CanUnmountSafely(string id,out string e) {
  var part=model.Parts.FirstOrDefault(x=>x.Id==id);
  if(part==null){e="R3_NOT_MOUNTED";return false;}
  if(part.Health<=0){e="ZERO_HP_UNMOUNT_UNSAFE";return false;}
  if(model.Inventory.Count>=R3VehicleModel.InventoryCapacity){e="INVENTORY_FULL";return false;}
  e="OK";return true;
 }
 public bool UnmountExisting(string id,out string e) {
  if(!CanUnmountSafely(id,out e))return false;
  var part=model.Parts.First(x=>x.Id==id);
  if(!model.Remove(part.Cell)){e="R3_REMOVE_REJECT";return false;}
  e="OK";return true;
 }
 public bool RepairExisting(string id,out string e) {
  var p=model.Parts.FirstOrDefault(x=>x.Id==id);
  bool ok=p!=null?model.Repair(p.Cell):model.RepairInventory(id);
  e=ok?"OK":"R3_REPAIR_REJECT";return ok;
 }
 public bool ReceiveSalvage(PartObservation p,out string e) {
  if(!Enum.TryParse(p.BasePartKind,true,out R3PartKind kind)||kind==R3PartKind.Core ||
   model.Inventory.Count>=R3VehicleModel.InventoryCapacity){e="R3_REWARD_INVALID_OR_FULL";return false;}
  var state=new R3PartState(default,kind,p.Health){Id=p.InstanceId,Source="Legendary"};
  if(kind==R3PartKind.Gun&&!Enum.TryParse(p.WeaponFamily,true,out state.Weapon)){e="R3_UNKNOWN_WEAPON";return false;}
  if(!model.ReceiveSalvage(new[]{state})){e="R3_SALVAGE_REJECT";return false;}
  e="OK";return true;
 }
 public bool TransferInventoryAtomically(IAuthoritativeR3Port target,string id,out string e) {
  e="TRANSFER_HOST_API_PENDING";return false; // Private R3 inventory has no safe extract API.
 }
}
