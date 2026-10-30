using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.LevelChallenges.MagnetLane.R3;

static class OriginalR3LevelScenarios
{
    const string PathToData="Research/B01-25/P17/W17_MAGNETLANE_SCENARIOS/fixtures.json";
    const string Frozen="c9663a8c50599bdb0f8954c42737d8e0d8afcaec";
    static int count,negatives;
    static readonly List<object> results=new();
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);}
    static void Case(string name,Action callback)
    {
        try{callback();count++;Console.WriteLine("PASS "+name);}
        catch(Exception error){Console.Error.WriteLine("FAIL "+name+" "+error);throw;}
    }
    static void Reject(string name,Action callback)
    {
        Case("NEGATIVE_"+name,()=>{
            bool denied=false;
            try{callback();}catch(ArgumentException){denied=true;}
            Check(denied,"Unsafe authored data must be rejected");
        });negatives++;
    }
    static double Val(JsonElement e,string key)=>e.GetProperty(key).GetDouble();
    static JsonElement Parse(string value)=>JsonDocument.Parse(value).RootElement.Clone();
    static void Validate(JsonElement p)
    {
        if(p.GetProperty("schemaVersion").GetInt32()!=1||
           p.GetProperty("immutableEngineSha").GetString()!=Frozen||
           p.GetProperty("packageId").GetString()!="B01-25-MAGNETLANE-W17-SCENARIOS")
            throw new ArgumentException("Invalid version/owner/source");
        var c=p.GetProperty("config");
        _=new MagnetConfig(Val(c,"warning"),Val(c,"active"),Val(c,"safe"),
                          Val(c,"force"),Val(c,"halfX"),Val(c,"halfZ"));
        var m=p.GetProperty("masses");
        if(Val(m,"light")!=1.5||Val(m,"heavy")!=12||
           Val(p,"safeBypassZ")<=Val(c,"halfZ")+1.5)
            throw new ArgumentException("Rigs/bypass inconsistent");
        var items=p.GetProperty("scenarios").EnumerateArray().ToList();
        if(items.Count!=7)throw new ArgumentException("Wrong scenario count");
        var ids=new HashSet<string>(StringComparer.Ordinal);
        foreach(var s in items)
        {
            string id=s.GetProperty("id").GetString()??"";
            string sign=s.GetProperty("forceSign").GetString()??"";
            if(!id.StartsWith("ML17_A",StringComparison.Ordinal)||!ids.Add(id)||
               (sign!="POSITIVE"&&sign!="NEGATIVE"&&sign!="ZERO"))
                throw new ArgumentException("Duplicate or invalid scenario/force expectation");
            int steps=s.GetProperty("warmupSteps").GetInt32();
            double warm=Val(s,"warmupDt"),dt=Val(s,"dt");
            if(steps<0||steps>200||!MagnetConfig.Finite(warm)||warm<=0||warm>.1||
               !MagnetConfig.Finite(dt)||dt<=0||dt>.1)
                throw new ArgumentException("Malformed step/time");
            var from=s.GetProperty("from").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
            var to=s.GetProperty("to").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
            if(from.Length!=2||to.Length!=2)throw new ArgumentException("Bad coordinates");
            for(int i=0;i<2;i++)
            {
                if(!MagnetConfig.Finite(from[i])||!MagnetConfig.Finite(to[i])||
                   Math.Abs(from[i])>MagnetLaneController.MaxAbsPositionMeters||
                   Math.Abs(to[i])>MagnetLaneController.MaxAbsPositionMeters||
                   Math.Abs(to[i]-from[i])>MagnetLaneController.MaxPathPerSampleMeters)
                    throw new ArgumentException("Overflow/out-of-range teleport");
            }
        }
    }
    static MagnetOutput Original(JsonElement c,JsonElement s,double mass)
    {
        var h=new MagnetLaneController(new MagnetConfig(Val(c,"warning"),Val(c,"active"),
            Val(c,"safe"),Val(c,"force"),Val(c,"halfX"),Val(c,"halfZ")));
        int steps=s.GetProperty("warmupSteps").GetInt32();
        for(int i=0;i<steps;i++)
            h.Advance(new MagnetSample(Val(s,"warmupDt"),0,0,0,0,mass));
        var f=s.GetProperty("from").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
        var t=s.GetProperty("to").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
        return h.Advance(new MagnetSample(Val(s,"dt"),f[0],f[1],t[0],t[1],mass));
    }
    // Independent 20k-step midpoint numerical oracle: no source StrengthIntegral reuse.
    static double Numerical(JsonElement c,JsonElement s)
    {
        const int N=20000;
        double cycle=Val(c,"warning")+Val(c,"active")+Val(c,"safe"),sum=0;
        double time=s.GetProperty("warmupSteps").GetInt32()*Val(s,"warmupDt");
        var f=s.GetProperty("from").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
        var end=s.GetProperty("to").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
        for(int j=0;j<N;j++)
        {
            double t=(j+.5)/N,now=time+t*Val(s,"dt");
            int cycleNumber=(int)Math.Floor(now/cycle);
            double phase=now-cycleNumber*cycle;
            if(phase<Val(c,"warning")||phase>=Val(c,"warning")+Val(c,"active"))continue;
            double x=f[0]+(end[0]-f[0])*t,z=f[1]+(end[1]-f[1])*t;
            if(Math.Abs(x)>Val(c,"halfX")||Math.Abs(z)>Val(c,"halfZ"))continue;
            sum+=Val(c,"force")*(1-Math.Abs(z)/Val(c,"halfZ"))*(cycleNumber%2==0?1:-1);
        }
        return sum/N;
    }
    static void Main(string[] args)
    {
        Check(args.Length==1,"Pass one output JSON path");
        string raw=File.ReadAllText(PathToData);
        JsonElement data=Parse(raw);
        Case("VALIDATE_SEVEN_SOURCE_PINNED_LEVEL_FIXTURES",()=>Validate(data));
        var config=data.GetProperty("config");
        foreach(var s in data.GetProperty("scenarios").EnumerateArray())
        {
            string id=s.GetProperty("id").GetString()??"";
            Case(id+"_SOURCE_REAL_LIGHT_HEAVY_ORACLE",()=>{
                var light=Original(config,s,1.5);
                var heavy=Original(config,s,12);
                double independent=Numerical(config,s);
                Check(Math.Abs(light.ForceZ-independent)<.035,"source versus independent integral");
                Check(Math.Abs(light.ForceZ-heavy.ForceZ)<1e-8,"Mass must not alter Newtons");
                Check(Math.Abs(light.ForceZ)<=Val(config,"force")+1e-9,"Field exceeds budget");
                Check(light.ActiveFraction>=0&&light.ActiveFraction<=1,"Bad fraction");
                Check(Math.Abs(light.AccelerationZ*1.5-light.ForceZ)<1e-8,"Light F=ma");
                Check(Math.Abs(heavy.AccelerationZ*12-heavy.ForceZ)<1e-8,"Heavy F=ma");
                string sign=s.GetProperty("forceSign").GetString()??"";
                Check(sign=="POSITIVE"?light.ForceZ>.5:
                      sign=="NEGATIVE"?light.ForceZ<-.5:Math.Abs(light.ForceZ)<1e-9,"Wrong scenario outcome");
                results.Add(new{id,originalForceNewton=light.ForceZ,
                    independentForceNewton=independent,lightAccel=light.AccelerationZ,
                    heavyAccel=heavy.AccelerationZ,activeFraction=light.ActiveFraction});
            });
            Case(id+"_DETERMINISTIC_REPLAY",()=>{
                var a=Original(config,s,1.5);var b=Original(config,s,1.5);
                Check(a.ForceZ==b.ForceZ&&a.Elapsed==b.Elapsed&&
                      a.CompletedCycles==b.CompletedCycles,"Replay divergence");
            });
        }
        Case("NORTH_BYPASS_NO_MAGNETIC_EXPOSURE_LIGHT_AND_HEAVY",()=>{
            var c=new MagnetLaneController(new MagnetConfig(.6,2.4,1,60,3,1.5));
            for(int i=0;i<6;i++)c.Advance(new MagnetSample(.1,0,0,0,0,1.5));
            var output=c.Advance(new MagnetSample(.1,-7,4,7,4,1.5));
            Check(output.ForceZ==0&&output.ActiveFraction==0,"Bypass failed");
        });
        Case("REJECTED_OVERFLOW_LEAVES_FROZEN_R3_STATE_ATOMIC",()=>{
            var c=new MagnetLaneController(new MagnetConfig(.6,2.4,1,60,3,1.5));
            for(int i=0;i<11;i++)c.Advance(new MagnetSample(.05,0,0,0,0,12));
            double before=c.Clock;int polarity=c.Polarity;bool rejected=false;
            try{c.Advance(new MagnetSample(.1,-1e308,0,1e308,0,12));}
            catch(ArgumentException){rejected=true;}
            Check(rejected&&c.Clock==before&&c.Polarity==polarity,"Atomic regression");
        });
        Reject("DUPLICATE_ROUTE_ID",()=>Validate(Parse(raw.Replace("ML17_A02_NORTH_BYPASS","ML17_A01_FAST_CENTER"))));
        Reject("FOREIGN_SOURCE_SHA",()=>Validate(Parse(raw.Replace(Frozen,new string('0',40)))));
        Reject("SAFE_BYPASS_OVERLAP",()=>Validate(Parse(raw.Replace("\"safeBypassZ\": 4","\"safeBypassZ\": 2"))));
        Reject("INVALID_LIGHT_HEAVY",()=>Validate(Parse(raw.Replace("\"heavy\": 12","\"heavy\": 1.5"))));
        Reject("OVERLONG_HOST_TIMESTEP",()=>Validate(Parse(raw.Replace("\"dt\": 0.1","\"dt\": 0.2"))));
        Reject("OVERSIZE_TELEPORT",()=>Validate(Parse(raw.Replace("-7,\n        0","-9999,\n        0"))));
        Reject("UNKNOWN_EXPECTED_OUTCOME",()=>Validate(Parse(raw.Replace("\"forceSign\": \"POSITIVE\"","\"forceSign\": \"WRONG\""))));
        Reject("MISSING_ONE_SCENARIO",()=>{
            var doc=Parse(raw);
            var dict=JsonSerializer.Deserialize<Dictionary<string,object>>(raw)!;
            dict["scenarios"]=doc.GetProperty("scenarios").EnumerateArray().Skip(1).ToArray();
            Validate(Parse(JsonSerializer.Serialize(dict)));
        });
        string path=System.IO.Path.GetFullPath(args[0]);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path,JsonSerializer.Serialize(new{
           package="B01-25-MAGNETLANE-W17-LEVEL-FIXTURES",
           frozenEngineSha=Frozen,sourceTestsPassed=count,negativeTests=negatives,
           nativeUnity="NOT_RUN",codexAck=false,scenarios=results
        },new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine("W17_FROZEN_R3_SCENARIOS_SOURCE_PASS "+count+"/"+count+" NEGATIVE="+negatives+" UNITY_NATIVE_NOT_RUN");
    }
}
