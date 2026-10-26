// COMPILE ONLY: combined API *test doubles* for OBJ #210. No licensed Unity/PhysX.
using System;
using System.Collections.Generic;
namespace UnityEngine {
 public struct Vector3Int {
  public int x,y,z;
  public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}
 }
}
namespace RoboRougelike.R4 {
 public enum R4WeaponKind {Basic,Automatic,Heavy,Shotgun,Railgun,BeamCutter,RocketLauncher,RotarySaw,EmpEmitter}
}
namespace RoboRougelike.R3 {
 using UnityEngine;
 using RoboRougelike.R4;
 public enum R3PartKind {Armor,Wheel,Gun,Core,Suspension,Booster,JumpDrive,Blink,
 Gyroscope,OmniDrive,MagneticGrapple,HoverEngine,CrawlerTrack,BonusModule,
 DirectionalShield,PointDefense,StructuralScanner,ScannerPulse,IndependentSteering}
 public class R3PartState {
  public string Id=Guid.NewGuid().ToString("N"),Source;
  public R3PartKind Kind;
  public R4WeaponKind Weapon;
  public float Health;
  public Vector3Int Cell;
  public int ContactMask;
  public R3PartState(Vector3Int cell,R3PartKind kind,float hp=1){Cell=cell;Kind=kind;Health=hp;}
 }
 public class R3VehicleModel {
  public const int InventoryCapacity=200;
  public List<R3PartState> Parts {get;}=new List<R3PartState>();
  public IReadOnlyList<R3PartState> Inventory=>new List<R3PartState>();
  public int Price(R3PartKind k,R4WeaponKind w=R4WeaponKind.Basic)=>100;
  public string PlacementError(Vector3Int cell,R3PartKind kind,bool owned=false,
   R4WeaponKind weapon=R4WeaponKind.Basic,Vector3Int rotation=default,int contactMask=63)=>null;
  public bool Mount(string id,Vector3Int cell,Vector3Int rotation)=>false;
  public bool Remove(Vector3Int cell)=>false;
  public bool Repair(Vector3Int cell)=>false;
  public bool RepairInventory(string id)=>false;
  public bool ReceiveSalvage(IEnumerable<R3PartState> ps)=>false;
 }
}
