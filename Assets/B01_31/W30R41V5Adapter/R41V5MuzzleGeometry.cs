// Original B01-31 W30 real production kinematic contract. Pure C# 9 source;
// used directly by the Unity Editor v5 importer and actual .NET8 source QA.
// Geometric QA envelopes are not game design firing-angle limits.
using System;

namespace RoboRougelike.B0131.W30
{
    public enum R41V5MuzzleSide { Left = 0, Right = 1 }

    public readonly struct R41V5Pose
    {
        public readonly double X, Y, Z;
        public readonly double ForwardX, ForwardY, ForwardZ;
        public R41V5Pose(double x, double y, double z,
                         double fx, double fy, double fz)
        {
            X=x; Y=y; Z=z;
            ForwardX=fx; ForwardY=fy; ForwardZ=fz;
        }
    }

    /// <summary>
    /// Actual neutral mounts measured in imported baked-v5 Unity axes:
    /// pivot (0,.135,0), left (-.06,.135,.70), right (+.06,.135,.70),
    /// intended +Z forward. We do not rewrite the incoming producer FBX;
    /// a separately generated adapter visual may use these measured points.
    /// </summary>
    public static class R41V5MuzzleGeometry
    {
        public const double GridMeters = 0.55;
        public const double PivotHeightMeters = 0.135;
        public const double HalfBarrelSpacingMeters = 0.060;
        public const double BarrelLengthMeters = 0.700;
        public const double NeutralPositionToleranceMeters = 0.010;

        public static bool TryPose(double yawDegrees, double pitchDegrees,
                                   R41V5MuzzleSide side, out R41V5Pose pose)
        {
            pose = default;
            if (double.IsNaN(yawDegrees) || double.IsInfinity(yawDegrees) ||
                double.IsNaN(pitchDegrees) || double.IsInfinity(pitchDegrees) ||
                yawDegrees < -180 || yawDegrees > 180 ||
                pitchDegrees < -45 || pitchDegrees > 45 ||
                (side != R41V5MuzzleSide.Left && side != R41V5MuzzleSide.Right))
                return false;

            double y = yawDegrees * Math.PI / 180;
            double p = pitchDegrees * Math.PI / 180;
            double cy = Math.Cos(y), sy = Math.Sin(y);
            double cp = Math.Cos(p), sp = Math.Sin(p);
            double lx = side == R41V5MuzzleSide.Left ?
                        -HalfBarrelSpacingMeters : HalfBarrelSpacingMeters;

            // Quaternion.Euler(pitch, yaw, 0) equivalent to parent
            // Yaw(Unity +Y) * child Pitch(Unity +X), on local (lx, 0, .70).
            pose = new R41V5Pose(
                lx * cy + BarrelLengthMeters * cp * sy,
                PivotHeightMeters - BarrelLengthMeters * sp,
                -lx * sy + BarrelLengthMeters * cp * cy,
                cp * sy, -sp, cp * cy);
            return true;
        }

        public static bool ValidateImportedNeutral(
            double leftX, double leftY, double leftZ,
            double rightX, double rightY, double rightZ,
            out string rejection)
        {
            rejection = "";
            double[] values = {leftX,leftY,leftZ,rightX,rightY,rightZ};
            for(int i=0;i<values.Length;i++)
                if(double.IsNaN(values[i]) || double.IsInfinity(values[i]))
                { rejection = "NON_FINITE_MUZZLE"; return false; }
            if (!TryPose(0,0,R41V5MuzzleSide.Left,out var l) ||
                !TryPose(0,0,R41V5MuzzleSide.Right,out var r))
            { rejection="INVALID_KINEMATIC_CONTRACT"; return false; }
            double le=Math.Sqrt(Math.Pow(leftX-l.X,2)+Math.Pow(leftY-l.Y,2)+Math.Pow(leftZ-l.Z,2));
            double re=Math.Sqrt(Math.Pow(rightX-r.X,2)+Math.Pow(rightY-r.Y,2)+Math.Pow(rightZ-r.Z,2));
            if (le > NeutralPositionToleranceMeters || re > NeutralPositionToleranceMeters)
            { rejection="MISSING_PRODUCER_AXIS_CONVERSION_OR_BAD_MUZZLE_POSITION"; return false; }
            double spacing=Math.Sqrt(Math.Pow(leftX-rightX,2)+Math.Pow(leftY-rightY,2)+Math.Pow(leftZ-rightZ,2));
            if (Math.Abs(spacing-2*HalfBarrelSpacingMeters) > NeutralPositionToleranceMeters)
            { rejection="WRONG_BARREL_SPACING"; return false; }
            return true;
        }

        public static bool ValidatePosePair(in R41V5Pose l, in R41V5Pose r)
        {
            double dx=l.X-r.X,dy=l.Y-r.Y,dz=l.Z-r.Z;
            double spacing=Math.Sqrt(dx*dx+dy*dy+dz*dz);
            double nl=l.ForwardX*l.ForwardX+l.ForwardY*l.ForwardY+l.ForwardZ*l.ForwardZ;
            double nr=r.ForwardX*r.ForwardX+r.ForwardY*r.ForwardY+r.ForwardZ*r.ForwardZ;
            double dot=l.ForwardX*r.ForwardX+l.ForwardY*r.ForwardY+l.ForwardZ*r.ForwardZ;
            return Math.Abs(spacing-2*HalfBarrelSpacingMeters)<1e-9 &&
                   Math.Abs(nl-1)<1e-9 && Math.Abs(nr-1)<1e-9 &&
                   Math.Abs(dot-1)<1e-9;
        }
    }
}
