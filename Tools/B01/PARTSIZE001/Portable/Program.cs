using B01.PARTSIZE001;
using System;
using System.Collections.Generic;
using System.Linq;

static class Test
{
    static int checks;
    static void Must(bool ok,string label)
    {
        checks++;
        if(!ok)throw new Exception("PARTSIZE_R1_FAIL: "+label);
    }
    static bool Same(Cell3[] a,Cell3[] b) =>
        new HashSet<Cell3>(a).SetEquals(b);
    static void Main()
    {
        var cube=PartSizeReferenceFixtures.LegacyCube();
        var wide=PartSizeReferenceFixtures.LegacyWide();
        Must(PartSizeGrid.EnvelopeError(cube,new Cell3(0,0,0),new Cell3(0,0,0))==null,"cube baseline source");
        Must(PartSizeGrid.EnvelopeError(wide,new Cell3(0,0,0),new Cell3(0,0,0))==null,"wide baseline source");
        Must(wide.LocalBoxSize!.Value.X==1.07&&wide.LocalBoxCenter!.Value.Z==.275,
            "derive wide hull from canonical R3 source");

        for(int x=-4;x<8;x++)
        for(int y=-4;y<8;y++)
        for(int z=-4;z<8;z++)
        {
            var t=new Cell3(x,y,z);
            var anchor=new Cell3(2,-1,3);
            Must(PartSizeGrid.EnvelopeError(wide,anchor,t)==null,"wide rotated envelope "+t);
            Must(PartSizeGrid.Occupied(wide,anchor,t).Distinct().Count()==4,
                "unique wide four cells "+t);
            Must(Same(PartSizeGrid.Occupied(wide,anchor,t),
                PartSizeGrid.Occupied(wide,anchor,new Cell3(x+4,y-8,z+12))),
                "rotation periodic "+t);
            var p=PartSizeGrid.Rotate(new Cell3(1,0,0),t);
            Must(Math.Abs(p.X)+Math.Abs(p.Y)+Math.Abs(p.Z)==1,"unit vectors preserved "+t);
        }

        var actualWide=PartSizeGrid.PhysicalBounds(wide,new Cell3(0,0,0),new Cell3(0,0,0));
        Must(Math.Abs(actualWide.Max.X-actualWide.Min.X-1.07)<1e-8,"real 1.07m bounding width");
        Must(Math.Abs(actualWide.Max.Z-actualWide.Min.Z-1.07)<1e-8,"real 1.07m bounding depth");
        Must(PartSizeGrid.IsExposedFace(wide,new Cell3(0,0,0),new Cell3(-1,0,0)),
            "external mount left");
        Must(!PartSizeGrid.IsExposedFace(wide,new Cell3(0,0,0),new Cell3(1,0,0)),
            "internal mount forbidden");
        Must(!PartSizeGrid.IsExposedFace(wide,new Cell3(77,0,0),new Cell3(1,0,0)),
            "non-owned face forbidden");

        var beside=PartSizeGrid.Occupied(cube,new Cell3(0,0,-1),new Cell3(0,0,0));
        Must(PartSizeGrid.Overlaps(PartSizeGrid.Occupied(wide,new Cell3(0,0,0),
            new Cell3(0,1,0)),beside),"yaw rotated collision");
        var far=PartSizeGrid.Occupied(cube,new Cell3(20,0,0),new Cell3(0,0,0));
        Must(!PartSizeGrid.Overlaps(PartSizeGrid.Occupied(wide,new Cell3(0,0,0),
            new Cell3(0,1,0)),far),"separate owner");

        var oversize=new PartSizeDefinition("Oversize",new Cell3(1,1,1),
            new Meters3(0,0,0),new Meters3(1.2,.52,.52),"NEGATIVE");
        Must(PartSizeGrid.EnvelopeError(oversize,new Cell3(0,0,0),new Cell3(0,1,0))==
            "PHYSICAL_ENVELOPE_OUTSIDE_OWNED_CELLS","oversized body negative");

        var hidden=PartSizeReferenceFixtures.Unmeasured("Wheel",new Cell3(1,1,1));
        Must(PartSizeGrid.EnvelopeError(hidden,new Cell3(0,0,0),new Cell3(0,0,0))==
            "UNMEASURED_PHYSICAL_ENVELOPE","unmeasured never accepted");
        Must(PartSizeGrid.Occupied(hidden,new Cell3(0,0,0),new Cell3(0,0,0)).Length==1,
            "unmeasured logical footprint stays inspectable");
        foreach(var invalid in new[]{double.NaN,double.PositiveInfinity,0,-1})
        {
            var bad=new PartSizeDefinition("Bad",new Cell3(1,1,1),
                new Meters3(0,0,0),new Meters3(invalid,1,1),"NEGATIVE");
            Must(PartSizeGrid.EnvelopeError(bad,new Cell3(0,0,0),new Cell3(0,0,0))==
                "INVALID_PHYSICAL_ENVELOPE","NaN/infinity/zero/negative");
        }
        var huge=new PartSizeDefinition("Huge",new Cell3(int.MaxValue,1,int.MaxValue),
            new Meters3(0,0,0),new Meters3(1,1,1),"NEGATIVE");
        Must(PartSizeGrid.EnvelopeError(huge,new Cell3(0,0,0),new Cell3(0,0,0))==
            "INVALID_LOGICAL_FOOTPRINT","overflow guarded by long");

        Must(PartSizeBaselineCatalog.CanonicalNames.Length==19,"all 19 actual R3 kinds listed");
        var catalogSeen=new HashSet<string>();
        int wideCount=0, unmeasuredCount=0;
        foreach(var kind in PartSizeBaselineCatalog.CanonicalNames)
        {
            Must(catalogSeen.Add(kind),"no duplicate canonical names");
            var spec=PartSizeBaselineCatalog.ForSourceKind(kind);
            var cells=PartSizeGrid.Occupied(spec,new Cell3(0,0,0),new Cell3(0,0,0));
            if(cells.Length==4)wideCount++;
            else Must(cells.Length==1,"only source-observed 1 and 2x1x2 occupancy");
            var error=PartSizeGrid.EnvelopeError(spec,new Cell3(0,0,0),new Cell3(0,0,0));
            if(error=="UNMEASURED_PHYSICAL_ENVELOPE")unmeasuredCount++;
            else Must(error==null,"source-derived physics boxes fit nominal footprint");
        }
        Must(wideCount==6,"canonical R3 source declares six wide kinds");
        Must(unmeasuredCount==11,"eleven moving/decorative kinds require native measurement");
        try
        {
            PartSizeBaselineCatalog.ForSourceKind("AngledArmor_proposed");
            Must(false,"unapproved kind cannot silently become canonical");
        }
        catch(ArgumentException) { Must(true,"unapproved kind rejected"); }

        Console.WriteLine("PARTSIZE_PURE_CSHARP_PASS "+checks+
            "/"+checks+" REAL_PRODUCTION_SOURCE_COMPILED; Unity/PhysX NOT_RUN; G2 HOLD");
    }
}
