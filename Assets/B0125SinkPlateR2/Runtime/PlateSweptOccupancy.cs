using System;
namespace B01.LevelChallenges.SinkPlate.R2
{
    /// <summary>Continuous segment vs authored plate rectangle expanded by world collider half-extents; no Unity dependency.</summary>
    public static class PlateSweptOccupancy
    {
        public static bool Intersects(double x0,double z0,double x1,double z1,double radiusX,double radiusZ)
        {
            if(!PlateConfig.Finite(x0)||!PlateConfig.Finite(z0)||!PlateConfig.Finite(x1)||
               !PlateConfig.Finite(z1)||!PlateConfig.Finite(radiusX)||!PlateConfig.Finite(radiusZ)||
               radiusX<0||radiusZ<0||radiusX>3||radiusZ>3)
                throw new ArgumentException("Invalid swept footprint");
            double first=0,last=1;
            return Clip(x0,x1-x0,-3-radiusX,3+radiusX,ref first,ref last) &&
                   Clip(z0,z1-z0,-1.5-radiusZ,1.5+radiusZ,ref first,ref last);
        }
        private static bool Clip(double p,double delta,double lo,double hi,ref double first,ref double last)
        {
            if(delta==0)return p>=lo&&p<=hi;
            double enter=(lo-p)/delta,exit=(hi-p)/delta;
            if(enter>exit){double t=enter;enter=exit;exit=t;}
            first=Math.Max(first,enter);last=Math.Min(last,exit);
            return first<=last;
        }
    }
}
