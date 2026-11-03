// Minimal compile-only host for the ORIGINAL immutable R3VehicleCatalog.Create.
// Not an implementation of R3VehicleModel, Unity, Save or game physics.
// Real catalog source is compiled unmodified from actual selected #877.
using System.Collections.Generic;
namespace UnityEngine
{
    public readonly struct Vector3Int
    {
        public readonly int x,y,z;
        public Vector3Int(int X,int Y,int Z){x=X;y=Y;z=Z;}
    }
}
namespace RoboRougelike.R3
{
    public enum R3VehicleKind {Standard,Light,Heavy}
    public enum R3PartKind {Armor,Wheel,Gun,Core}
    public sealed class R3PartState
    {
        public UnityEngine.Vector3Int Cell;
        public R3PartKind Kind;
        public R3PartState(UnityEngine.Vector3Int cell,R3PartKind kind)
        {Cell=cell;Kind=kind;}
    }
    public sealed class R3VehicleDesign
    {
        public string Name="",Description="";
        public float Speed,TurnSpeed,CoreHp;
        public readonly List<R3PartState> Parts=new List<R3PartState>();
    }
}
