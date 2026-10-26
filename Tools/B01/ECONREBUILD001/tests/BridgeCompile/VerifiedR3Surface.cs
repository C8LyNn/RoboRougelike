// .NET8 interface-signature doubles from OBJ #210 ONLY. NOT Unity native.
using System;
using System.Collections.Generic;
namespace RoboRougelike.R4
{
 public enum R4WeaponKind { Basic, Automatic, Heavy, Shotgun, Railgun, BeamCutter, RocketLauncher, RotarySaw, EmpEmitter }
}
namespace RoboRougelike.R3
{
 using RoboRougelike.R4;
 public enum R3PartKind { Armor, Wheel, Gun, Core, Suspension, Booster, JumpDrive, Blink,
 Gyroscope, OmniDrive, MagneticGrapple, HoverEngine, CrawlerTrack, BonusModule,
 DirectionalShield, PointDefense, StructuralScanner, ScannerPulse, IndependentSteering }
 public class R3PartState
 {
  public string Id;
  public R3PartKind Kind;
  public R4WeaponKind Weapon;
  public float Health;
 }
 public class R3VehicleModel
 {
  public List<R3PartState> Parts {get;}=new();
  public IReadOnlyList<R3PartState> Inventory=>new List<R3PartState>();
  public int Price(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic)=>1;
 }
}
