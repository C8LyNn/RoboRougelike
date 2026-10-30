using System;
using System.Collections.Generic;

namespace B01.UXViewport
{
    // Pixel-space rectangle. No UnityEngine dependency: Codex may adapt it into Unity Rects.
    public readonly struct HudRect
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;
        public int Right { get { return checked(X + Width); } }
        public int Bottom { get { return checked(Y + Height); } }
        public bool IsEmpty { get { return Width <= 0 || Height <= 0; } }
        public HudRect(int x, int y, int width, int height)
        {
            X=x;Y=y;Width=width;Height=height;
        }
        public bool Contains(HudRect other)
        {
            return !other.IsEmpty && !IsEmpty &&
                other.X>=X && other.Y>=Y && other.Right<=Right && other.Bottom<=Bottom;
        }
        public bool Intersects(HudRect other)
        {
            return !IsEmpty && !other.IsEmpty &&
                X<other.Right && Right>other.X && Y<other.Bottom && Bottom>other.Y;
        }
    }

    public sealed class HudViewport
    {
        public readonly int Width,Height,InsetLeft,InsetTop,InsetRight,InsetBottom;
        public readonly int UserScalePercent;
        public readonly bool ShowObjectives,ShowTips;
        public HudViewport(int width,int height,int left=0,int top=0,int right=0,
            int bottom=0,int userScalePercent=100,bool showObjectives=true,bool showTips=true)
        {
            if(width<320 || height<420 || width>16384 || height>16384 ||
                left<0 || top<0 || right<0 || bottom<0 ||
                left+right>width-280 || top+bottom>height-320 ||
                userScalePercent<100 || userScalePercent>150)
                throw new ArgumentOutOfRangeException("viewport");
            Width=width;Height=height;InsetLeft=left;InsetTop=top;
            InsetRight=right;InsetBottom=bottom;UserScalePercent=userScalePercent;
            ShowObjectives=showObjectives;ShowTips=showTips;
        }
    }

    public sealed class HudLayout
    {
        public readonly HudRect SafeArea,Critical,Vitals,Threats,Objectives,Tips,Pause;
        public readonly int FontPixels;
        public readonly bool TwoColumn;
        internal HudLayout(HudRect safe,HudRect critical,HudRect vitals,HudRect threats,
            HudRect objectives,HudRect tips,HudRect pause,int fontPixels,bool twoColumn)
        {
            SafeArea=safe;Critical=critical;Vitals=vitals;Threats=threats;
            Objectives=objectives;Tips=tips;Pause=pause;
            FontPixels=fontPixels;TwoColumn=twoColumn;
        }
        public HudRect[] VisiblePanels()
        {
            var a=new List<HudRect>();
            foreach(var r in new[]{Critical,Vitals,Threats,Objectives,Tips,Pause})
                if(!r.IsEmpty)a.Add(r);
            return a.ToArray();
        }
    }

    // Deterministic, screen-size-aware combat HUD geometry, not a renderer or input handler.
    // Mandatory: Critical, Vitals, Threats, Pause. Optional: Objectives and Tips.
    public static class HudLayoutCore
    {
        static int Ceiling(int numerator,int denominator)
        {return (numerator+denominator-1)/denominator;}
        public static HudLayout Build(HudViewport v)
        {
            if(v==null)throw new ArgumentNullException("v");
            var safe=new HudRect(v.InsetLeft,v.InsetTop,
                v.Width-v.InsetLeft-v.InsetRight,v.Height-v.InsetTop-v.InsetBottom);
            int scale=v.UserScalePercent;
            int font=Math.Max(16,Ceiling(18*scale,100));
            int line=font+6;
            int pad=Ceiling(14*scale,100);
            int gap=Ceiling(10*scale,100);
            int criticalH=2*line+2*pad;
            int vitalsH=4*line+2*pad;
            int threatsH=3*line+2*pad;
            int objectivesH=2*line+2*pad;
            int tipsH=2*line+pad;
            int pauseH=Math.Max(48,2*line);
            int pauseW=Math.Max(96,4*line);
            bool two=safe.Width>=980 && safe.Height>=580 &&
                safe.Width*10>=safe.Height*13;
            int left=safe.X+gap, top=safe.Y+gap, right=safe.Right-gap;
            int bottom=safe.Bottom-gap;
            var missing=new HudRect(0,0,0,0);
            HudRect critical,vitals,threats,objectives=missing,tips=missing,pause;
            if(two)
            {
                int colW=(safe.Width-3*gap)/2;
                int rx=left+colW+gap;
                vitals=new HudRect(left,top,colW,vitalsH);
                threats=new HudRect(rx,top,colW,threatsH);
                critical=new HudRect(rx,threats.Bottom+gap,colW,criticalH);
                pause=new HudRect(rx+colW-pauseW,bottom-pauseH,pauseW,pauseH);
                int reservedTop=pause.Y-gap;
                if(critical.Bottom>reservedTop || vitals.Bottom>reservedTop)
                    throw new InvalidOperationException("Viewport cannot fit essential HUD panels");
                int objectiveY=vitals.Bottom+gap;
                if(v.ShowObjectives && objectiveY+objectivesH<=reservedTop)
                    objectives=new HudRect(left,objectiveY,colW,objectivesH);
                int tipY=bottom-tipsH;
                int currentBottom=objectives.IsEmpty?vitals.Bottom:objectives.Bottom;
                if(v.ShowTips && tipY>=currentBottom+gap)
                    tips=new HudRect(left,tipY,colW,tipsH);
            }
            else
            {
                int colW=safe.Width-2*gap;
                critical=new HudRect(left,top,colW,criticalH);
                vitals=new HudRect(left,critical.Bottom+gap,colW,vitalsH);
                threats=new HudRect(left,vitals.Bottom+gap,colW,threatsH);
                pause=new HudRect(right-pauseW,bottom-pauseH,pauseW,pauseH);
                int reservedTop=pause.Y-gap;
                if(threats.Bottom>reservedTop)
                    throw new InvalidOperationException("Viewport cannot fit essential HUD panels");
                int objectiveY=threats.Bottom+gap;
                if(v.ShowObjectives && objectiveY+objectivesH<=reservedTop)
                    objectives=new HudRect(left,objectiveY,colW,objectivesH);
                int tipY=pause.Y-gap-tipsH;
                int currentBottom=objectives.IsEmpty?threats.Bottom:objectives.Bottom;
                if(v.ShowTips && tipY>=currentBottom+gap)
                    tips=new HudRect(left,tipY,colW,tipsH);
            }
            var layout=new HudLayout(safe,critical,vitals,threats,objectives,
                tips,pause,font,two);
            var panels=layout.VisiblePanels();
            foreach(var p in panels)
                if(!safe.Contains(p))
                    throw new InvalidOperationException("HUD panel clips outside safe area");
            for(int i=0;i<panels.Length;i++)
                for(int j=i+1;j<panels.Length;j++)
                    if(panels[i].Intersects(panels[j]))
                        throw new InvalidOperationException("HUD panels overlap");
            return layout;
        }
    }
}
