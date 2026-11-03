#!/usr/bin/env python3
"""Compile/run the EXACT production R3GarageView.ClickTarget method unchanged.
Original #877 RED behavior and W31 corrected mount+repair method GREEN, .NET8 source only.
The native real Unity PlayMode test lives in Assets/Tests/Editor and is NOT_RUN.
"""
from pathlib import Path
import hashlib, subprocess, sys, tempfile

ROOT = Path(__file__).resolve().parents[4]
FILE = "Assets/R3/R3GarageView.cs"
PARENT = "20273c3580079dfb299c30c85a67e76f8d893e7f"
OLD_BLOB = "1976a19516905be63259109ec1af0a92ef52d6f4"
original = subprocess.check_output(["git", "show", f"{PARENT}:{FILE}"], cwd=ROOT)
current = (ROOT / FILE).read_bytes()
def git_blob(raw):
    return hashlib.sha1(b"blob " + str(len(raw)).encode() + b"\0" + raw).hexdigest()
assert git_blob(original) == OLD_BLOB, "immutable original #877 Git blob changed"
assert current != original, "W30 original production method has not changed"

def method_from(actual):
    text = actual.decode("utf-8-sig")
    start = text.index("        public bool ClickTarget()")
    brace = text.index("{", start)
    depth = 0
    for i in range(brace,len(text)):
        if text[i] == "{": depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                return text[start:i+1]
    raise AssertionError("ClickTarget original brace mismatch")

PROJECT = """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
 <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
 <LangVersion>9.0</LangVersion><Nullable>disable</Nullable>
 <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
 <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
 </PropertyGroup><ItemGroup><Compile Include="Program.cs"/></ItemGroup></Project>
"""
WRAPPER = r"""using System;
enum R1RunState { Garage, Other }
enum R41GarageTool { Mount, Repair, Remove, Inspect }
enum R3PartKind { Armor, Gun }
sealed class FakeProgress { public R1RunState State = R1RunState.Garage; }
sealed class FakePart { public float Health = 1; }
sealed class FakeModel
{
    public FakePart Part = null;
    public int Scrap = 0;
    public int Quote = 0;
    public FakePart At(int i) => Part;
    public int RepairCost(FakePart part) => part == null ? 0 : Quote;
}
sealed class FakeRun
{
    public FakeProgress Progress = new FakeProgress();
    public bool Transitioning = false;
    public FakeModel VehicleModel = new FakeModel();
    public Func<bool> Purchase = () => false;
    public Func<bool> Fix = () => false;
    public bool GarageMount(string id,int cell,int rotation) => Purchase();
    public bool GarageBuy(int cell,R3PartKind kind,int rotation) => Purchase();
    public bool GarageBuyWeapon(int cell,int weapon,int rotation) => Purchase();
    public bool GarageRepair(int cell) => Fix();
    public bool GarageRemove(int cell) => false;
}
sealed class R3GarageView
{
    public bool HasTarget;
    public FakeRun run = new FakeRun();
    public R41GarageTool Tool = R41GarageTool.Mount;
    public string Message = "Zamontowano dokładnie w miejscu podglądu.";
    public string SelectedInventoryId = null;
    public int Cell = 0, SelectedRotation = 0, MountWeapon = 0;
    public R3PartKind MountKind = R3PartKind.Armor;
    public object Owned => null;
    public Func<string> LiveIssue = () => null;
    public string PlacementIssue => LiveIssue();
    private void RefreshMarker() {}
INSERT_ACTUAL_METHOD
}
static class Program
{
    static void Run(string name, bool target, Func<bool> purchase,
        Func<string> reason, bool active = true)
    {
        var v=new R3GarageView();
        v.HasTarget=target;v.run.Purchase=purchase;v.LiveIssue=reason;
        if(!active)v.run.Progress.State=R1RunState.Other;
        bool ok=v.ClickTarget();
        Console.WriteLine(name+"|"+(ok?"YES":"NO")+"|"+v.Message);
    }
    static void RepairScenario(string name, float? hp, int quote, int scrap, bool success=false, bool restoreNow=false)
    {
        var v=new R3GarageView();
        v.Tool=R41GarageTool.Repair;v.HasTarget=true;
        v.run.VehicleModel.Part=hp.HasValue?new FakePart{Health=hp.Value}:null;
        v.run.VehicleModel.Quote=quote;v.run.VehicleModel.Scrap=scrap;
        v.run.Fix=()=>{ if(success&&restoreNow&&v.run.VehicleModel.Part!=null) v.run.VehicleModel.Part.Health=1f; return success; };
        bool ok=v.ClickTarget();
        Console.WriteLine(name+"|"+(ok?"YES":"NO")+"|"+v.Message);
    }
    public static int Main()
    {
        Console.OutputEncoding=System.Text.Encoding.UTF8;
        Run("no-target",false,()=>false,()=>"Najedź na ścianę bloku pojazdu.");
        int n=0;
        Run("post-preview-rejection",true,()=>false,()=>++n==1?null:"Fizyczna kolizja po zmianie przeszkody.");
        Run("pre-validation-rejection",true,()=>false,()=>"Zajęte pole siatki.");
        Run("successful-purchase",true,()=>true,()=>null);
        Run("inactive-garage",true,()=>false,()=>null,false);
        RepairScenario("repair-no-part",null,0,0);
        RepairScenario("repair-healthy",1f,0,10);
        RepairScenario("repair-no-quote",.5f,0,10);
        RepairScenario("repair-zero-destroyed-rejected",0f,0,0);
        RepairScenario("repair-zero-destroyed-paid-success",0f,0,0,true);
        RepairScenario("repair-zero-destroyed-instant-success",0f,0,0,true,true);
        RepairScenario("repair-zero-partial-unknown",.5f,0,0);
        RepairScenario("repair-low-scrap",.5f,5,1);
        RepairScenario("repair-physical-reject",.5f,5,10);
        RepairScenario("repair-success",.5f,5,10,true);
        return 0;
    }
}
"""

def execute(method, folder):
    folder.mkdir(parents=True,exist_ok=True)
    (folder/"Program.cs").write_text(WRAPPER.replace("INSERT_ACTUAL_METHOD",method),encoding="utf-8")
    (folder/"ClickTargetSourceProbe.csproj").write_text(PROJECT,encoding="utf-8")
    result=subprocess.run(
       ["dotnet","run","--project",str(folder/"ClickTargetSourceProbe.csproj"),
        "--configuration","Release","--no-launch-profile"],
       stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding="utf-8",errors="strict",check=False
    )
    if result.returncode:
        print(result.stdout,result.stderr,file=sys.stderr)
        raise AssertionError("original production method failed direct source compile")
    return {line.lstrip("\ufeff").split("|",1)[0]:line.lstrip("\ufeff").split("|",2)[1:] for line in result.stdout.splitlines() if line.count("|")==2}
with tempfile.TemporaryDirectory(prefix="b0111_w30_garage_") as temp:
    folder=Path(temp)
    baseline=execute(method_from(original),folder/'old')
    current_result=execute(method_from(current),folder/'new')
expected_keys={"no-target","post-preview-rejection","pre-validation-rejection","successful-purchase","inactive-garage","repair-no-part","repair-healthy","repair-no-quote","repair-low-scrap","repair-physical-reject","repair-success","repair-zero-destroyed-rejected","repair-zero-destroyed-paid-success","repair-zero-destroyed-instant-success","repair-zero-partial-unknown"}
print("SOURCE_WINDOWS_UTF8_BASELINE",ascii(baseline))
print("SOURCE_WINDOWS_UTF8_FIXED",ascii(current_result))
assert set(baseline)==set(current_result)==expected_keys,(baseline,current_result)
assert baseline["no-target"]==["NO","Zamontowano dokładnie w miejscu podglądu."]
assert baseline["post-preview-rejection"]==["NO","Montaż nieudany."]
assert current_result["no-target"]==["NO","Najedź na ścianę bloku pojazdu."]
assert current_result["post-preview-rejection"]==["NO","Fizyczna kolizja po zmianie przeszkody."]
assert baseline["pre-validation-rejection"]==current_result["pre-validation-rejection"]==["NO","Zajęte pole siatki."]
assert baseline["successful-purchase"]==current_result["successful-purchase"]==["YES","Zamontowano dokładnie w miejscu podglądu."]
assert current_result["inactive-garage"][0]=="NO"
assert "nieaktywny" in current_result["inactive-garage"][1]
for name in ("repair-no-part","repair-healthy","repair-no-quote","repair-low-scrap","repair-physical-reject"):
    assert baseline[name]==["NO","Brak uszkodzeń, Złomu lub sprawnego połączenia."],(name,baseline[name])
    assert current_result[name][0]=="NO",(name,current_result[name])
assert "Nie znaleziono części" in current_result["repair-no-part"][1]
assert "nie wymaga naprawy" in current_result["repair-healthy"][1]
assert "Brak potwierdzonej wyceny" in current_result["repair-no-quote"][1]
assert baseline["repair-zero-destroyed-rejected"]==["NO","Brak uszkodzeń, Złomu lub sprawnego połączenia."]
assert current_result["repair-zero-destroyed-rejected"][0]=="NO"
assert "Odbudowa za 0 Scrap została odrzucona" in current_result["repair-zero-destroyed-rejected"][1]
assert "Brak dopuszczonej wyceny" not in current_result["repair-zero-destroyed-rejected"][1]
assert baseline["repair-zero-destroyed-paid-success"]==["YES","Naprawiono wskazaną część."]
assert current_result["repair-zero-destroyed-paid-success"][0]=="YES"
assert "część nadal ma 0 HP" in current_result["repair-zero-destroyed-paid-success"][1]
assert "Sprawdź status oczekiwania na kolejne zwycięstwo" in current_result["repair-zero-destroyed-paid-success"][1]
assert baseline["repair-zero-destroyed-instant-success"]==["YES","Naprawiono wskazaną część."]
assert current_result["repair-zero-destroyed-instant-success"][0]=="YES"
assert "Niezgodność odbudowy" in current_result["repair-zero-destroyed-instant-success"][1]
assert "przed kolejnym zwycięstwem" in current_result["repair-zero-destroyed-instant-success"][1]
assert "Naprawiono" not in current_result["repair-zero-destroyed-instant-success"][1]
assert baseline["repair-zero-partial-unknown"]==["NO","Brak uszkodzeń, Złomu lub sprawnego połączenia."]
assert "Brak potwierdzonej wyceny" in current_result["repair-zero-partial-unknown"][1]
assert "Koszt: 5 Scrap; dostępne: 1 Scrap" in current_result["repair-low-scrap"][1]
assert "kontroli konstrukcji" in current_result["repair-physical-reject"][1]
assert baseline["repair-success"]==current_result["repair-success"]==["YES","Naprawiono wskazaną część."]
print("ORIGINAL_R3_SOURCE_RED_WITNESS",ascii(baseline))
print("CORRECTED_R3_SOURCE_GREEN",ascii(current_result))
print("ORIGINAL_METHOD_COMPILED_RED_TO_GREEN PASS 30 old/new method executions; 15 named scenarios")
print("NATIVE_UNITY_PLAYMODE NOT_RUN (separate authored NUnit UnityTest)")
