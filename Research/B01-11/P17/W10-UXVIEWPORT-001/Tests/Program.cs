using System;
using B01.UXViewport;

static class Program
{
    static int passed,failed;
    static void Check(string name,Func<bool> fn)
    {
        try{if(!fn())throw new Exception("assert false");Console.WriteLine("PASS "+name);passed++;}
        catch(Exception e){Console.WriteLine("FAIL "+name+": "+e.Message);failed++;}
    }
    static bool Throws<T>(Action a) where T:Exception
    {try{a();return false;}catch(T){return true;}}
    static bool Valid(HudLayout l)
    {
        if(l.Critical.IsEmpty||l.Vitals.IsEmpty||l.Threats.IsEmpty||l.Pause.IsEmpty ||
            l.FontPixels<16)return false;
        var a=l.VisiblePanels();
        if(a.Length<4||a.Length>6)return false;
        for(int i=0;i<a.Length;i++)
        {
            if(!l.SafeArea.Contains(a[i]))return false;
            for(int j=i+1;j<a.Length;j++)
                if(a[i].Intersects(a[j]))return false;
        }
        return true;
    }

    // R2 production test: compile and call the original strongly-typed source API.
    static HudLayout BuildMeasured(HudViewport v,int critical,int vitals,int threats,int objectives=0,int tips=0)
    {
        return HudLayoutCore.Build(v,new HudContentMetrics(critical,vitals,threats,objectives,tips));
    }
    static void Main()
    {
        var resolutions=new[]{
            new[]{320,568}, new[]{360,640}, new[]{768,1024}, new[]{1024,768},
            new[]{1280,720}, new[]{1366,768}, new[]{1920,1080},new[]{3840,2160}
        };
        foreach(var size in resolutions)
        {
            foreach(int uiScale in new[]{100,125,150})
            {
                int width=size[0],height=size[1],percent=uiScale;
                Check("W10 viewport "+width+"x"+height+" text scale "+percent+" geometry",()=>{
                    try{return Valid(HudLayoutCore.Build(new HudViewport(width,height,userScalePercent:percent)));}
                    catch(InvalidOperationException){return percent==150 && width<=360;}
                });
            }
        }

        Check("720p landscape chooses two-column",()=>HudLayoutCore.Build(new HudViewport(1280,720)).TwoColumn);
        Check("1080p landscape chooses two-column",()=>HudLayoutCore.Build(new HudViewport(1920,1080)).TwoColumn);
        Check("4K landscape chooses two-column",()=>HudLayoutCore.Build(new HudViewport(3840,2160)).TwoColumn);
        Check("1024x768 4:3 chooses two-column",()=>HudLayoutCore.Build(new HudViewport(1024,768)).TwoColumn);
        Check("1366x768 wide layout",()=>HudLayoutCore.Build(new HudViewport(1366,768)).TwoColumn);
        Check("768x1024 portrait chooses stacked",()=>!HudLayoutCore.Build(new HudViewport(768,1024)).TwoColumn);
        Check("320x568 portrait chooses stacked",()=>!HudLayoutCore.Build(new HudViewport(320,568)).TwoColumn);
        Check("stacked critical above vitals",()=>{
            var l=HudLayoutCore.Build(new HudViewport(768,1024));
            return l.Critical.Bottom<l.Vitals.Y&&l.Vitals.Bottom<l.Threats.Y;
        });
        Check("two-column threats right of vitals",()=>{
            var l=HudLayoutCore.Build(new HudViewport(1280,720));
            return l.Threats.X>=l.Vitals.Right&&l.Critical.X==l.Threats.X;
        });
        Check("portrait visible urgent critical",()=>{
            var l=HudLayoutCore.Build(new HudViewport(360,640));
            return !l.Critical.IsEmpty&&l.Critical.Y<l.Vitals.Y;
        });
        Check("portrait visible pause bottom",()=>{
            var l=HudLayoutCore.Build(new HudViewport(360,640));
            return l.Pause.Bottom<=l.SafeArea.Bottom&&l.Pause.Y>l.Threats.Bottom;
        });
        Check("landscape visible pause bottom-right",()=>{
            var l=HudLayoutCore.Build(new HudViewport(1920,1080));
            return l.Pause.X>l.Threats.X&&l.Pause.Y>l.Critical.Bottom;
        });
        Check("landscape optional objectives exist",()=>!HudLayoutCore.Build(new HudViewport(1280,720)).Objectives.IsEmpty);
        Check("landscape optional tips exist",()=>!HudLayoutCore.Build(new HudViewport(1280,720)).Tips.IsEmpty);
        Check("optional objectives off",()=>{
            var l=HudLayoutCore.Build(new HudViewport(1280,720,showObjectives:false));
            return l.Objectives.IsEmpty&&Valid(l);
        });
        Check("optional tips off",()=>{
            var l=HudLayoutCore.Build(new HudViewport(1280,720,showTips:false));
            return l.Tips.IsEmpty&&Valid(l);
        });
        Check("optional both off still all mandatory panels",()=>{
            var l=HudLayoutCore.Build(new HudViewport(768,1024,showObjectives:false,showTips:false));
            return l.VisiblePanels().Length==4&&Valid(l);
        });
        Check("100% font baseline",()=>HudLayoutCore.Build(new HudViewport(1280,720)).FontPixels==18);
        Check("125% font grows",()=>HudLayoutCore.Build(new HudViewport(1280,720,userScalePercent:125)).FontPixels==23);
        Check("150% font grows",()=>HudLayoutCore.Build(new HudViewport(1280,720,userScalePercent:150)).FontPixels==27);
        Check("more text scale expands critical hit target",()=>{
            var a=HudLayoutCore.Build(new HudViewport(1920,1080,userScalePercent:100));
            var b=HudLayoutCore.Build(new HudViewport(1920,1080,userScalePercent:150));
            return b.Critical.Height>a.Critical.Height&&b.Pause.Height>a.Pause.Height;
        });
        Check("safe insets respected exactly",()=>{
            var l=HudLayoutCore.Build(new HudViewport(1920,1080,70,90,110,80));
            return l.SafeArea.X==70&&l.SafeArea.Y==90&&l.SafeArea.Width==1740&&l.SafeArea.Height==910&&Valid(l);
        });
        Check("asymmetric edge inset safe",()=>Valid(HudLayoutCore.Build(new HudViewport(1366,768,80,10,0,40))));
        Check("portrait notch/top inset safe",()=>Valid(HudLayoutCore.Build(new HudViewport(768,1024,0,90,0,50))));
        Check("4K TV overscan-safe inset",()=>Valid(HudLayoutCore.Build(new HudViewport(3840,2160,120,80,120,80))));
        Check("4:3 asymmetric safe area",()=>Valid(HudLayoutCore.Build(new HudViewport(1024,768,10,25,30,25))));
        Check("panel output is detached copy",()=>{
            var l=HudLayoutCore.Build(new HudViewport(1280,720));
            var a=l.VisiblePanels();int n=a.Length;a[0]=default(HudRect);
            return l.VisiblePanels().Length==n&&!l.VisiblePanels()[0].IsEmpty;
        });
        Check("rect touching edges does not intersect",()=>{
            var a=new HudRect(0,0,100,100);return !a.Intersects(new HudRect(100,0,100,100));
        });
        Check("rect overlapping intersects",()=>{
            var a=new HudRect(0,0,100,100);return a.Intersects(new HudRect(99,99,2,2));
        });
        Check("rect containment accepts equal bounds",()=>{
            var a=new HudRect(5,5,100,100);return a.Contains(new HudRect(5,5,100,100));
        });
        Check("rect containment refuses clipping",()=>{
            var a=new HudRect(5,5,100,100);return !a.Contains(new HudRect(4,5,100,100));
        });
        Check("zero-size optional rect is empty",()=>new HudRect(0,0,0,0).IsEmpty);
        Check("zero-size rect never intersects",()=>!new HudRect(0,0,0,0).Intersects(new HudRect(0,0,5,5)));
        Check("reject null viewport",()=>Throws<ArgumentNullException>(()=>HudLayoutCore.Build(null)));
        Check("reject width below minimum",()=>Throws<ArgumentOutOfRangeException>(()=>new HudViewport(319,768)));
        Check("reject height below minimum",()=>Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,419)));
        Check("reject huge screen width",()=>Throws<ArgumentOutOfRangeException>(()=>new HudViewport(16385,900)));
        Check("reject huge screen height",()=>Throws<ArgumentOutOfRangeException>(()=>new HudViewport(900,16385)));
        Check("reject negative inset",()=>Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,720,left:-1)));
        Check("reject negative top inset",()=>Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,720,top:-1)));
        Check("reject negative right inset",()=>Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,720,right:-1)));
        Check("reject negative bottom inset",()=>Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,720,bottom:-1)));
        Check("reject safe area narrower than 280px",()=>Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,720,left:900,right:101)));
        Check("reject safe area shorter than 320px",()=>Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,720,top:300,bottom:101)));
        Check("reject scale below 100%",()=>Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,720,userScalePercent:99)));
        Check("reject scale above 150%",()=>Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,720,userScalePercent:151)));
        Check("too little vertical space fails closed rather than clipping",()=>{
            return Throws<InvalidOperationException>(()=>HudLayoutCore.Build(new HudViewport(360,420,userScalePercent:150)));
        });
        Check("small safe area with too much text fails closed",()=>{
            return Throws<InvalidOperationException>(()=>HudLayoutCore.Build(new HudViewport(768,800,0,170,0,170,userScalePercent:150)));
        });
        Check("deterministic geometry repeated calls",()=>{
            var v=new HudViewport(1366,768,12,17,21,30,125);
            var a=HudLayoutCore.Build(v);var b=HudLayoutCore.Build(v);
            return a.Vitals.X==b.Vitals.X&&a.Vitals.Y==b.Vitals.Y&&
                a.Critical.Y==b.Critical.Y&&a.Pause.Bottom==b.Pause.Bottom;
        });
        Check("changing tips does not move critical",()=>{
            var a=HudLayoutCore.Build(new HudViewport(1280,720,showTips:true));
            var b=HudLayoutCore.Build(new HudViewport(1280,720,showTips:false));
            return a.Critical.Y==b.Critical.Y&&a.Critical.Height==b.Critical.Height;
        });
        Check("changing objectives does not move critical",()=>{
            var a=HudLayoutCore.Build(new HudViewport(768,1024,showObjectives:true));
            var b=HudLayoutCore.Build(new HudViewport(768,1024,showObjectives:false));
            return a.Critical.Y==b.Critical.Y&&a.Critical.Height==b.Critical.Height;
        });

        // W11 measured text (host supplies real pixel extents, not guessed glyph widths).
        Check("W11 measured-content API exists",()=>typeof(HudLayoutCore).Assembly.GetType("B01.UXViewport.HudContentMetrics")!=null);
        Check("W11 portrait taller critical copy expands reserved panel",()=>{
            var baseline=HudLayoutCore.Build(new HudViewport(768,1024));
            var measured=BuildMeasured(new HudViewport(768,1024),250,0,0);
            return measured!=null&&Valid(measured)&&measured.Critical.Height>=278&&measured.Critical.Height>baseline.Critical.Height;
        });
        Check("W11 measured critical also pushes following portrait vitals",()=>{
            var baseline=HudLayoutCore.Build(new HudViewport(768,1024));
            var measured=BuildMeasured(new HudViewport(768,1024),250,0,0);
            return measured!=null&&measured.Vitals.Y>baseline.Vitals.Y&&Valid(measured);
        });
        Check("W11 measured vitals extends left column without overlapping objectives",()=>{
            var baseline=HudLayoutCore.Build(new HudViewport(1920,1080));
            var measured=BuildMeasured(new HudViewport(1920,1080),0,330,0);
            return measured!=null&&measured.Vitals.Height>baseline.Vitals.Height&&Valid(measured);
        });
        Check("W11 measured threats moves critical downward on wide screen",()=>{
            var baseline=HudLayoutCore.Build(new HudViewport(1920,1080));
            var measured=BuildMeasured(new HudViewport(1920,1080),0,0,285);
            return measured!=null&&measured.Critical.Y>baseline.Critical.Y&&Valid(measured);
        });
        Check("W11 huge optional objectives are omitted without hiding essentials",()=>{
            var measured=BuildMeasured(new HudViewport(1280,720),0,0,0,500);
            return measured!=null&&measured.Objectives.IsEmpty&&Valid(measured);
        });
        Check("W11 huge optional tips are omitted without hiding essentials",()=>{
            var measured=BuildMeasured(new HudViewport(1280,720),0,0,0,0,600);
            return measured!=null&&measured.Tips.IsEmpty&&Valid(measured);
        });
        Check("W11 measured small text never shrinks existing panels",()=>{
            var baseline=HudLayoutCore.Build(new HudViewport(1280,720));
            var measured=BuildMeasured(new HudViewport(1280,720),1,1,1,1,1);
            return measured!=null&&Valid(measured)&&measured.Critical.Height==baseline.Critical.Height&&
                   measured.Vitals.Height==baseline.Vitals.Height&&measured.Threats.Height==baseline.Threats.Height;
        });
        Check("W11 measured zero is legacy-equivalent geometry",()=>{
            var baseline=HudLayoutCore.Build(new HudViewport(1024,768));
            var measured=BuildMeasured(new HudViewport(1024,768),0,0,0);
            return measured!=null&&Valid(measured)&&measured.Critical.Height==baseline.Critical.Height&&
                   measured.Vitals.Height==baseline.Vitals.Height&&measured.Threats.Height==baseline.Threats.Height;
        });
        Check("W11 huge measured mandatory critical fails closed, never clips",()=>
            Throws<InvalidOperationException>(()=>BuildMeasured(new HudViewport(1280,720),900,0,0)));
        Check("W11 measured complex portrait input still has nonoverlapping rectangles",()=>{
            var measured=BuildMeasured(new HudViewport(768,1024,0,32,0,48,125),180,140,105);
            return measured!=null&&Valid(measured);
        });
        Check("W11 measured font height does not double apply user scaling",()=>{
            var m=BuildMeasured(new HudViewport(1920,1080,userScalePercent:150),200,0,0);
            return m!=null&&m.Critical.Height==200+2*21&&Valid(m);
        });
        Check("W11 overlong objective copy may disappear without moving critical",()=>{
            var a=BuildMeasured(new HudViewport(1920,1080),0,0,0,0);
            var b=BuildMeasured(new HudViewport(1920,1080),0,0,0,1000);
            return a!=null&&b!=null&&a.Critical.Y==b.Critical.Y&&b.Objectives.IsEmpty&&Valid(b);
        });
        Check("W11 left/right integer overflow insets must be rejected at input",()=>
            Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,720,left:int.MaxValue,right:1)));
        Check("W11 top/bottom integer overflow insets must be rejected at input",()=>
            Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,720,top:int.MaxValue,bottom:1)));

        Check("W11 zero metrics constructor accepted",()=>Valid(HudLayoutCore.Build(
            new HudViewport(1280,720),new HudContentMetrics(0,0,0))));
        Check("W11 null measured metrics preserves legacy R1",()=>{
            var v=new HudViewport(1024,768);var a=HudLayoutCore.Build(v);var b=HudLayoutCore.Build(v,null);
            return a.Critical.Height==b.Critical.Height&&a.Vitals.Height==b.Vitals.Height&&Valid(b);
        });
        Check("W11 negative critical text metrics rejected",()=>
            Throws<ArgumentOutOfRangeException>(()=>new HudContentMetrics(-1,0,0)));
        Check("W11 negative optional text metrics rejected",()=>
            Throws<ArgumentOutOfRangeException>(()=>new HudContentMetrics(0,0,0,-1,0)));
        Check("W11 oversized text metrics rejected",()=>
            Throws<ArgumentOutOfRangeException>(()=>new HudContentMetrics(0,0,16385)));
        Check("W11 extreme double-overflow horizontal insets rejected",()=>
            Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,720,left:int.MaxValue,right:int.MaxValue)));
        Check("W11 extreme double-overflow vertical insets rejected",()=>
            Throws<ArgumentOutOfRangeException>(()=>new HudViewport(1280,720,top:int.MaxValue,bottom:int.MaxValue)));
        Console.WriteLine("RESULT "+passed+" PASS, "+failed+" FAIL");
        if(failed!=0)Environment.Exit(1);
    }
}
