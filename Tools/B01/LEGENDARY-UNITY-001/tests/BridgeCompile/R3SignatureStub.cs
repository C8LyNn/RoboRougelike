// COMPILE-ONLY test doubles reflecting verified OBJ #210 signatures.
// Not Unity, not source copies, not a claim of runtime or host integration.
using System;
using System.Collections.Generic;
namespace UnityEngine {
 public struct Vector3Int {
  public int x,y,z;
  public Vector3Int(int a,int b,int c){x=a;y=b;z=c;}
 }
}
namespace RoboRougelike.R4 {
 public enum R4WeaponKind {Basic,Automatic,Heavy,Shotgun,Railgun,BeamCutter,RocketLauncher,RotarySaw,EmpEmitter}
}
namespace RoboRougelike.R3 {
 using UnityEngine;
 using RoboRougelike.R4;
 public enum R3PartKind { Armor, Wheel, Gun, Core, Suspension, Booster, JumpDrive, Blink,
  Gyroscope, OmniDrive, MagneticGrapple, HoverEngine, CrawlerTrack, BonusModule,
  DirectionalShield, PointDefense, StructuralScanner, ScannerPulse, IndependentSteering }
 public sealed class R3PartState {
  public Vector3Int Cell;
  public R3PartKind Kind;
  public float Health;
  public string Id=Guid.NewGuid().ToString("N"),Source;
  public R4WeaponKind Weapon;
  public int ContactMask;
  public R3PartState(Vector3Int cell,R3PartKind kind,float health=1){Cell=cell;Kind=kind;Health=health;}
 }
 public sealed class R3VehicleModel {
  public const int InventoryCapacity=200;
  public List<R3PartState> Parts {get;}=new List<R3PartState>();
  private readonly List<R3PartState> inventory=new List<R3PartState>();
  public IReadOnlyList<R3PartState> Inventory=>inventory;
  public string PlacementError(Vector3Int cell,R3PartKind kind,bool owned=false,
   R4WeaponKind weapon=R4WeaponKind.Basic,Vector3Int rotation=default,int contactMask=63)=>null;
  public bool Mount(string id,Vector3Int cell,Vector3Int rotation)=>false;
  public bool Remove(Vector3Int cell)=>false;
  public bool Repair(Vector3Int cell)=>false;
  public bool RepairInventory(string id)=>false;
  public bool ReceiveSalvage(IEnumerable<R3PartState> recovered)=>false;
 }
}
