using System;
using B01.LevelChallenges.MagnetLane.R2;
static class ActualOriginalR2ExpectedRedWitness
{
    static void Main()
    {
        var c=new MagnetLaneController(new MagnetConfig(.6,2.4,1,60,3,1.5));
        for(int i=0;i<11;i++)c.Advance(new MagnetSample(.05,0,0,0,0,12));
        double before=c.Clock;
        if(Math.Abs(before-.55)>1e-9)throw new Exception("Invalid R2 witness setup");
        bool thrown=false;
        try { c.Advance(new MagnetSample(.1,-1e308,0,1e308,0,12)); }
        catch(ArgumentException){thrown=true;}
        if(!thrown)throw new Exception("R2 RED witness did not throw");
        // This is expected RED behavior in original immutable R2, not a PASS of R2 release.
        if(c.Clock<=before+1e-6)throw new Exception("Expected R2 partial-mutation regression not reproduced");
        Console.WriteLine("R2_EXPECTED_RED_CONFIRMED; CLOCK_BEFORE="+before+" CLOCK_AFTER="+c.Clock+"; NO_RELEASE_PASS");
    }
}
