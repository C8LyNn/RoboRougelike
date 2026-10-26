using System;
using System.Text.Json;
using B01.GameDesign.RC2GarageWarnings;
internal sealed class CaseFile
{
    public string Contract { get; set; } = "";
    public int FixtureCount { get; set; }
    public List<OneCase> Scenarios { get; set; } = new();
}
internal sealed class OneCase
{
    public string Id { get; set; }="";
    public string Description { get; set; }="";
    public GarageSnapshot Input { get; set; }=new();
    public List<string> ExpectedCritical { get; set; }=new();
    public string ExpectedLaunchDecision { get; set; }="";
}
static class Program
{
    static int failures;
    static void Check(bool okay,string name)
    {
        if(!okay){Console.Error.WriteLine("FAIL "+name);failures++;}
    }
    static GaragePart P(string id,string kind,int x,float hp=1,bool? con=true,bool? op=true)
        =>new(){Id=id,Kind=kind,CellX=x,Health=hp,ConnectedToTrustedCore=con,Operational=op};
    static GarageSnapshot Healthy()
        =>new(){SelectedDrive="Wheels",ActivityRequiresWeapon=true,ActivityId="Combat",
            Parts=new(){P("core","Core",0),P("gun","Gun",0),P("left","Wheel",-1),P("right","Wheel",1)}};
    static int Main(string[] args)
    {
        var file=args.Length>0?args[0]:System.IO.Path.GetFullPath(
            System.IO.Path.Combine(AppContext.BaseDirectory,"../../../../fixtures.json"));
        var fixture=JsonSerializer.Deserialize<CaseFile>(System.IO.File.ReadAllText(file),
            new JsonSerializerOptions{PropertyNameCaseInsensitive=true})
            ??throw new Exception("Empty fixtures");
        Check(fixture.Contract=="B01-W14-RC2-GARAGE-WARNINGS-R1"&&
              fixture.Scenarios.Count==fixture.FixtureCount,"contract manifest");
        foreach(var item in fixture.Scenarios)
        {
            var got=GarageWarningPolicy.Evaluate(item.Input);
            Check(got.Critical.SequenceEqual(item.ExpectedCritical),
                item.Id+": EXPECT ["+string.Join(",",item.ExpectedCritical)+
                "] OBSERVED ["+string.Join(",",got.Critical)+"]");
            Check(got.LaunchDecision==item.ExpectedLaunchDecision,item.Id+" launch authority");
            var off=new GarageSnapshot{SelectedDrive=item.Input.SelectedDrive,
                ActivityRequiresWeapon=item.Input.ActivityRequiresWeapon,
                ActivityId=item.Input.ActivityId,SuggestionsEnabled=false,
                RiskyCenterOfMass=item.Input.RiskyCenterOfMass,Parts=item.Input.Parts};
            Check(GarageWarningPolicy.Evaluate(off).Critical.SequenceEqual(got.Critical),
                item.Id+" critical hidden by suggestions");
        }
        // Deterministic property proof: advice toggle never changes critical result;
        // 2000 combinations of damage, disconnected support, 2 drive sides, activity.
        var random=new Random(60414);
        for(int i=0;i<2000;i++)
        {
            var b=Healthy(); b.ActivityRequiresWeapon=(i%11==0)?null:i%2==0;
            b.Parts[1].Health=random.Next(3)==0?0:1;
            b.Parts[2].Operational=random.Next(2)==0?false:true;
            b.Parts[3].ConnectedToTrustedCore=random.Next(4)==0?false:true;
            b.RiskyCenterOfMass=random.Next(2)==0;
            b.SuggestionsEnabled=true;
            var a=GarageWarningPolicy.Evaluate(b);
            b.SuggestionsEnabled=false;
            var x=GarageWarningPolicy.Evaluate(b);
            Check(a.Critical.SequenceEqual(x.Critical),"seed "+i+" critical toggle invariance");
            Check(a.LaunchDecision=="UNDECIDED_OWNER_APPROVAL_REQUIRED"&&
                  x.LaunchDecision=="UNDECIDED_OWNER_APPROVAL_REQUIRED",
                  "seed "+i+" forbidden automatic launch decision");
        }
        // Negative controls must demonstrably detect changed expected status, not only schema.
        var wrong=fixture.Scenarios.First(x=>x.Id=="W14-GW-07");
        Check(!GarageWarningPolicy.Evaluate(wrong.Input).Critical.SequenceEqual(
            new []{"WEAPON_NONFUNCTIONAL"}),"negative oracle control wrong code");
        var hd=Healthy();hd.Parts[3].Health=0;
        Check(GarageWarningPolicy.Evaluate(hd).Critical.Contains("MOBILITY_NONFUNCTIONAL"),
            "negative R3 0HP wheel suppression");
        var healthy=GarageWarningPolicy.Evaluate(Healthy());
        Check(healthy.Critical.Count==0,"positive original R3 wheels");
        Console.WriteLine(JsonSerializer.Serialize(new {
          contract=fixture.Contract,fixtures=fixture.Scenarios.Count,
          deterministicIterations=2000,negativeControls=3,
          detectedFailures=failures,verdict=failures==0?"PASS":"FAIL",
          nativeUnity="NOT_RUN",hostWiring="NOT_RUN",g2="HOLD"
        }));
        return failures==0?0:1;
    }
}
