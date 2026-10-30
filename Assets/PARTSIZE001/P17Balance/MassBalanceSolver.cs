// B01-23 / P17-MASSBALANCE-001 R1. Isolated source: never writes to Codex Garage/R3.
// A planar static tipping estimate from CALLER-MEASURED mass centers and grounded contacts.
// Not a PhysX collision, joint, traction, motor, suspension, or typed-socket acceptance.
using System;
using System.Collections.Generic;

namespace B01.PARTSIZE001.P17Balance
{
    public struct PointM
    {
        public readonly double X, Y, Z;
        public PointM(double x, double y, double z) { X=x; Y=y; Z=z; }
    }

    public sealed class PartMass
    {
        public readonly string Id;
        public readonly double Kilograms;
        public readonly PointM CenterMeters;
        public PartMass(string id, double kilograms, PointM centerMeters)
        { Id=id; Kilograms=kilograms; CenterMeters=centerMeters; }
    }

    public sealed class GroundContact
    {
        public readonly string Id;
        public readonly PointM PointMeters;
        public GroundContact(string id, PointM pointMeters)
        { Id=id; PointMeters=pointMeters; }
    }

    public sealed class StabilityResult
    {
        public readonly double TotalKilograms;
        public readonly PointM CenterMeters;
        public readonly double SupportPlaneMeters;
        public readonly int HullVertexCount;
        // Positive when projected CoM is inside the support polygon; negative outside.
        public readonly double SignedEdgeMarginMeters;
        // Ideal static planar tip angle; negative when already outside support polygon.
        public readonly double IdealTipAngleDegrees;
        public readonly bool ProjectionWithinSupport;
        internal StabilityResult(double mass, PointM center, double plane, int hull,
                                 double margin, double angle, bool inside)
        {
            TotalKilograms=mass; CenterMeters=center; SupportPlaneMeters=plane;
            HullVertexCount=hull; SignedEdgeMarginMeters=margin;
            IdealTipAngleDegrees=angle; ProjectionWithinSupport=inside;
        }
    }

    public sealed class BalanceInputException : ArgumentException
    {
        public readonly string Code;
        public BalanceInputException(string code) : base(code) { Code=code; }
    }

    public static class MassBalanceSolver
    {
        private const double Epsilon=1e-9;
        private const double MaxPlaneDeltaMeters=0.005;

        public static PointM RotateYawQuarterTurns(PointM point, int turns)
        {
            if(turns<0 || turns>3) throw new BalanceInputException("UNSUPPORTED_YAW_TURNS");
            CheckFinite(point);
            if(turns==1) return new PointM(point.Z,point.Y,-point.X);
            if(turns==2) return new PointM(-point.X,point.Y,-point.Z);
            if(turns==3) return new PointM(-point.Z,point.Y,point.X);
            return point;
        }

        public static StabilityResult Evaluate(IReadOnlyList<PartMass> parts,
                                                IReadOnlyList<GroundContact> contacts)
        {
            if(parts==null || parts.Count<1 || parts.Count>180)
                throw new BalanceInputException("MISSING_OR_EXCESS_PART_MASSES");
            if(contacts==null || contacts.Count<3 || contacts.Count>64)
                throw new BalanceInputException("INSUFFICIENT_OR_EXCESS_CONTACTS");

            var partIds=new HashSet<string>(StringComparer.Ordinal);
            double total=0;
            foreach(var p in parts)
            {
                if(p==null || string.IsNullOrWhiteSpace(p.Id) || !partIds.Add(p.Id))
                    throw new BalanceInputException("INVALID_OR_DUPLICATE_PART_ID");
                if(!Finite(p.Kilograms) || p.Kilograms<=0)
                    throw new BalanceInputException("UNMEASURED_OR_INVALID_PART_MASS");
                CheckFinite(p.CenterMeters);
                total+=p.Kilograms;
            }
            if(!Finite(total) || total<=0)
                throw new BalanceInputException("MASS_TOTAL_OVERFLOW");

            // Ratios reduce multiplication overflow. No default mass for unknown parts.
            double cx=0,cy=0,cz=0;
            foreach(var p in parts)
            {
                double fraction=p.Kilograms/total;
                cx+=fraction*p.CenterMeters.X;
                cy+=fraction*p.CenterMeters.Y;
                cz+=fraction*p.CenterMeters.Z;
            }
            var center=new PointM(cx,cy,cz);
            CheckFinite(center);

            var supportIds=new HashSet<string>(StringComparer.Ordinal);
            var points=new List<PointM>();
            double plane=contacts[0]==null ? double.NaN : contacts[0].PointMeters.Y;
            foreach(var contact in contacts)
            {
                if(contact==null || string.IsNullOrWhiteSpace(contact.Id) ||
                   !supportIds.Add(contact.Id))
                    throw new BalanceInputException("INVALID_OR_DUPLICATE_CONTACT_ID");
                CheckFinite(contact.PointMeters);
                if(Math.Abs(contact.PointMeters.Y-plane)>MaxPlaneDeltaMeters)
                    throw new BalanceInputException("CONTACTS_NOT_COPLANAR");
                foreach(var previous in points)
                    if(previous.X==contact.PointMeters.X && previous.Z==contact.PointMeters.Z)
                        throw new BalanceInputException("DUPLICATE_SUPPORT_POSITION");
                points.Add(contact.PointMeters);
            }
            if(center.Y<=plane)
                throw new BalanceInputException("COM_NOT_ABOVE_SUPPORT_PLANE");

            points.Sort((a,b)=> { int x=a.X.CompareTo(b.X); return x!=0?x:a.Z.CompareTo(b.Z); });
            var lower=new List<PointM>();
            foreach(var p in points)
            {
                while(lower.Count>=2 && Cross(lower[lower.Count-2],lower[lower.Count-1],p)<=0)
                    lower.RemoveAt(lower.Count-1);
                lower.Add(p);
            }
            var upper=new List<PointM>();
            for(int i=points.Count-1;i>=0;i--)
            {
                var p=points[i];
                while(upper.Count>=2 && Cross(upper[upper.Count-2],upper[upper.Count-1],p)<=0)
                    upper.RemoveAt(upper.Count-1);
                upper.Add(p);
            }
            lower.RemoveAt(lower.Count-1);
            upper.RemoveAt(upper.Count-1);
            lower.AddRange(upper);
            if(lower.Count<3)
                throw new BalanceInputException("COLLINEAR_CONTACTS");

            bool inside=true;
            double closest=double.PositiveInfinity;
            for(int i=0;i<lower.Count;i++)
            {
                var a=lower[i];
                var b=lower[(i+1)%lower.Count];
                if(Cross(a,b,center)<-Epsilon) inside=false;
                double dx=b.X-a.X, dz=b.Z-a.Z;
                double lengthSquared=dx*dx+dz*dz;
                if(!Finite(lengthSquared) || lengthSquared<=0)
                    throw new BalanceInputException("INVALID_SUPPORT_SPAN");
                double t=((center.X-a.X)*dx+(center.Z-a.Z)*dz)/lengthSquared;
                t=Math.Max(0,Math.Min(1,t));
                double edgeDistance=Math.Sqrt(
                    Math.Pow(center.X-(a.X+t*dx),2)+Math.Pow(center.Z-(a.Z+t*dz),2));
                if(!Finite(edgeDistance))
                    throw new BalanceInputException("INVALID_SUPPORT_DISTANCE");
                closest=Math.Min(closest,edgeDistance);
            }
            double signedMargin=inside?closest:-closest;
            double tipDegrees=Math.Atan2(signedMargin,center.Y-plane)*180.0/Math.PI;
            return new StabilityResult(total,center,plane,lower.Count,signedMargin,
                                       tipDegrees,inside);
        }

        private static double Cross(PointM a, PointM b, PointM c)
        { return (b.X-a.X)*(c.Z-a.Z)-(b.Z-a.Z)*(c.X-a.X); }

        private static bool Finite(double x)
        { return !double.IsNaN(x) && !double.IsInfinity(x); }

        private static void CheckFinite(PointM p)
        {
            if(!Finite(p.X)||!Finite(p.Y)||!Finite(p.Z))
                throw new BalanceInputException("NONFINITE_POSITION");
        }
    }
}
