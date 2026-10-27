using System;
using System.Collections.Generic;

namespace B01.PARTSIZE001
{
    /// <summary>Independent integer grid address. No R3 or Codex host dependency.</summary>
    public readonly struct Cell3 : IEquatable<Cell3>
    {
        public readonly int X,Y,Z;
        public Cell3(int x,int y,int z) { X=x;Y=y;Z=z; }
        public static Cell3 operator +(Cell3 a,Cell3 b) => new Cell3(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public bool Equals(Cell3 b) => X==b.X&&Y==b.Y&&Z==b.Z;
        public override bool Equals(object obj) => obj is Cell3 other&&Equals(other);
        public override int GetHashCode() { unchecked { return (X*73856093)^(Y*19349663)^(Z*83492791); } }
        public override string ToString() => "(" + X+","+Y+","+Z+")";
    }

    public readonly struct Meters3
    {
        public readonly double X,Y,Z;
        public Meters3(double x,double y,double z) {X=x;Y=y;Z=z;}
        public static Meters3 operator +(Meters3 a,Meters3 b) => new Meters3(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static Meters3 operator -(Meters3 a,Meters3 b) => new Meters3(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static Meters3 operator *(Meters3 a,double k) => new Meters3(a.X*k,a.Y*k,a.Z*k);
        public bool Finite => !(double.IsNaN(X)||double.IsNaN(Y)||double.IsNaN(Z)||
                                 double.IsInfinity(X)||double.IsInfinity(Y)||double.IsInfinity(Z));
    }

    public readonly struct Box3
    {
        public readonly Meters3 Min,Max;
        public Box3(Meters3 min,Meters3 max){Min=min;Max=max;}
        public bool Contains(Box3 inner,double allowance) =>
            inner.Min.X>=Min.X-allowance&&inner.Min.Y>=Min.Y-allowance&&inner.Min.Z>=Min.Z-allowance&&
            inner.Max.X<=Max.X+allowance&&inner.Max.Y<=Max.Y+allowance&&inner.Max.Z<=Max.Z+allowance;
    }

    /// <summary>
    /// Source-derived logical occupancy and an optional rectangular physical
    /// shape. Null physical dimensions are an UNMEASURED state, NOT a 0m box.
    /// One PartId owns all cells; mass, HP, mesh and swept effects are separate.
    /// </summary>
    public sealed class PartSizeDefinition
    {
        public readonly string PartId;
        public readonly Cell3 Dimensions;
        public readonly Meters3? LocalBoxCenter;
        public readonly Meters3? LocalBoxSize;
        public readonly string Provenance;
        public PartSizeDefinition(string partId,Cell3 dimensions,
            Meters3? boxCenter, Meters3? boxSize,string provenance)
        {
            PartId=partId;Dimensions=dimensions;LocalBoxCenter=boxCenter;
            LocalBoxSize=boxSize;Provenance=provenance;
        }
    }

    public static class PartSizeGrid
    {
        // Frozen as observed in original R4_1/R41PartGeometry.cs, not a scale proposal.
        public const double GridMeters=.55;
        public const double LegacyBlockMeters=.52;
        public const double MaximumWorldOvershootMeters=.005; // 5mm review limit.

        private static int Mod4(int n) => (n%4+4)%4;

        // Unity Quaternion.Euler applies rotations Z, then X, then Y.
        // Integer cell and fractional metric corners use the SAME exact transform.
        public static Cell3 Rotate(Cell3 v,Cell3 turns)
        {
            for(int n=0;n<Mod4(turns.Z);n++)v=new Cell3(-v.Y,v.X,v.Z);
            for(int n=0;n<Mod4(turns.X);n++)v=new Cell3(v.X,-v.Z,v.Y);
            for(int n=0;n<Mod4(turns.Y);n++)v=new Cell3(v.Z,v.Y,-v.X);
            return v;
        }
        public static Meters3 Rotate(Meters3 v,Cell3 turns)
        {
            for(int n=0;n<Mod4(turns.Z);n++)v=new Meters3(-v.Y,v.X,v.Z);
            for(int n=0;n<Mod4(turns.X);n++)v=new Meters3(v.X,-v.Z,v.Y);
            for(int n=0;n<Mod4(turns.Y);n++)v=new Meters3(v.Z,v.Y,-v.X);
            return v;
        }

        public static string DefinitionError(PartSizeDefinition def)
        {
            if(def==null||string.IsNullOrWhiteSpace(def.PartId)||
               def.Dimensions.X<=0||def.Dimensions.Y<=0||def.Dimensions.Z<=0||
               def.Dimensions.X>180||def.Dimensions.Y>180||def.Dimensions.Z>180||
               (long)def.Dimensions.X*def.Dimensions.Y*def.Dimensions.Z>180L)
                return "INVALID_LOGICAL_FOOTPRINT";
            if(def.LocalBoxCenter.HasValue!=def.LocalBoxSize.HasValue)
                return "INCOMPLETE_PHYSICAL_ENVELOPE";
            if(!def.LocalBoxCenter.HasValue)
                return "UNMEASURED_PHYSICAL_ENVELOPE";
            var size=def.LocalBoxSize.Value;
            if(!size.Finite||!def.LocalBoxCenter.Value.Finite||
               size.X<=0||size.Y<=0||size.Z<=0)
                return "INVALID_PHYSICAL_ENVELOPE";
            return null;
        }

        public static Cell3[] Occupied(PartSizeDefinition def,Cell3 anchor,Cell3 rotation)
        {
            var reason=DefinitionError(def);
            if(reason!=null&&reason!="UNMEASURED_PHYSICAL_ENVELOPE")
                throw new ArgumentException(reason,nameof(def));
            var cells=new List<Cell3>();
            for(int x=0;x<def.Dimensions.X;x++)
            for(int y=0;y<def.Dimensions.Y;y++)
            for(int z=0;z<def.Dimensions.Z;z++)
                cells.Add(anchor+Rotate(new Cell3(x,y,z),rotation));
            return cells.ToArray();
        }

        public static Box3 LogicalBounds(PartSizeDefinition def,Cell3 anchor,Cell3 rotation)
        {
            var cells=Occupied(def,anchor,rotation);
            double x0=double.PositiveInfinity,y0=x0,z0=x0;
            double x1=double.NegativeInfinity,y1=x1,z1=x1;
            foreach(var c in cells)
            {
                x0=Math.Min(x0,(c.X-.5)*GridMeters);
                y0=Math.Min(y0,(c.Y-.5)*GridMeters);
                z0=Math.Min(z0,(c.Z-.5)*GridMeters);
                x1=Math.Max(x1,(c.X+.5)*GridMeters);
                y1=Math.Max(y1,(c.Y+.5)*GridMeters);
                z1=Math.Max(z1,(c.Z+.5)*GridMeters);
            }
            return new Box3(new Meters3(x0,y0,z0),new Meters3(x1,y1,z1));
        }

        public static Box3 PhysicalBounds(PartSizeDefinition def,Cell3 anchor,Cell3 rotation)
        {
            if(DefinitionError(def)!=null)throw new ArgumentException("UNVERIFIED_DEFINITION");
            var c=def.LocalBoxCenter.Value;
            var h=def.LocalBoxSize.Value*.5;
            double x0=double.PositiveInfinity,y0=x0,z0=x0;
            double x1=double.NegativeInfinity,y1=x1,z1=x1;
            for(int sx=-1;sx<=1;sx+=2)
            for(int sy=-1;sy<=1;sy+=2)
            for(int sz=-1;sz<=1;sz+=2)
            {
                var p=Rotate(c+new Meters3(sx*h.X,sy*h.Y,sz*h.Z),rotation)+
                    new Meters3(anchor.X*GridMeters,anchor.Y*GridMeters,anchor.Z*GridMeters);
                x0=Math.Min(x0,p.X);y0=Math.Min(y0,p.Y);z0=Math.Min(z0,p.Z);
                x1=Math.Max(x1,p.X);y1=Math.Max(y1,p.Y);z1=Math.Max(z1,p.Z);
            }
            return new Box3(new Meters3(x0,y0,z0),new Meters3(x1,y1,z1));
        }

        public static string EnvelopeError(PartSizeDefinition def,Cell3 anchor,Cell3 rotation)
        {
            var reason=DefinitionError(def);
            if(reason!=null)return reason;
            var grid=LogicalBounds(def,anchor,rotation);
            var hull=PhysicalBounds(def,anchor,rotation);
            return grid.Contains(hull,MaximumWorldOvershootMeters)?null:"PHYSICAL_ENVELOPE_OUTSIDE_OWNED_CELLS";
        }

        public static bool Overlaps(Cell3[] existing,Cell3[] candidate)
        {
            if(existing==null||candidate==null)throw new ArgumentNullException("cells");
            var owner=new HashSet<Cell3>(existing);
            foreach(var cell in candidate)
                if(owner.Contains(cell))return true;
            return false;
        }

        public static bool IsExposedFace(PartSizeDefinition def,Cell3 localCell,Cell3 face)
        {
            int distance=Math.Abs(face.X)+Math.Abs(face.Y)+Math.Abs(face.Z);
            if(distance!=1) return false;
            if(localCell.X<0||localCell.Y<0||localCell.Z<0||
               localCell.X>=def.Dimensions.X||localCell.Y>=def.Dimensions.Y||
               localCell.Z>=def.Dimensions.Z)return false;
            var neighbor=localCell+face;
            return neighbor.X<0||neighbor.Y<0||neighbor.Z<0||
                   neighbor.X>=def.Dimensions.X||neighbor.Y>=def.Dimensions.Y||
                   neighbor.Z>=def.Dimensions.Z;
        }
    }

    public static class PartSizeReferenceFixtures
    {
        // Dimensions derived from canonical SOURCE constants, not a Unity
        // editor mesh/bounds measurement and not approval of decorative models.
        public static PartSizeDefinition LegacyCube(string id="Armor") =>
            new PartSizeDefinition(id,new Cell3(1,1,1),
                new Meters3(0,0,0),new Meters3(.52,.52,.52),
                "SOURCE_DERIVED_R41_0.52M_CUBE_NOT_NATIVE_MEASURED");

        public static PartSizeDefinition LegacyWide(string id="BonusModule") =>
            new PartSizeDefinition(id,new Cell3(2,1,2),
                new Meters3(.275,0,.275),new Meters3(1.07,.52,1.07),
                "SOURCE_DERIVED_R3BonusModuleGeometry_NOT_NATIVE_MEASURED");

        public static PartSizeDefinition Unmeasured(string id,Cell3 cells) =>
            new PartSizeDefinition(id,cells,null,null,
                "EXISTING_R3_LOGICAL_ONLY_NATIVE_COLLIDER_AUDIT_REQUIRED");
    }
}
