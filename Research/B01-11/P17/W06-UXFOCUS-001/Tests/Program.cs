using System;
using System.Collections.Generic;
using B01.UXFocus;

static class Program
{
    static int passed, failed;
    static MenuSnapshot Ready() {return new MenuSnapshot{Screen=NavScreen.VehicleSelect};}
    static MenuSnapshot Reward(int n=3) {return new MenuSnapshot{Screen=NavScreen.RewardChoice,RewardCount=n};}
    static MenuSnapshot Results() {return new MenuSnapshot{Screen=NavScreen.Results};}
    static void Check(string name,Func<bool> test)
    {
        try
        {
            if(!test())throw new Exception("assertion false");
            Console.WriteLine("PASS "+name);passed++;
        }
        catch(Exception e){Console.WriteLine("FAIL "+name+": "+e.Message);failed++;}
    }
    static bool Reject(Action f)
    {
        try {f();return false;}catch(ArgumentException){return true;}
    }
    static bool Same(MenuSelection r,string focus,NavAction action=NavAction.None)
        {return r.FocusId==focus && r.Action==action;}
    static bool Has(FocusTarget[] t,string id)
    {foreach(var x in t)if(x.Id==id)return true;return false;}
    static MenuSelection N(MenuSnapshot s,string id,NavCommand cmd)
        {return MenuNavCore.Step(s,id,cmd);}
    static void Main()
    {
        var ready=Ready();
        Check("Vehicle initial focus is first vehicle; no phantom click",()=>Same(N(ready,null,NavCommand.Confirm),"ready/vehicle0"));
        Check("Vehicle initial focus changes",()=>N(ready,null,NavCommand.None).FocusChanged);
        ready.SelectedVehicleSlot=2;
        Check("Initial focus honors selected third vehicle",()=>Same(N(ready,"",NavCommand.Confirm),"ready/vehicle2"));
        ready.SelectedVehicleSlot=0;
        Check("Vehicle 0 real confirm returns source-safe action",()=>Same(N(ready,"ready/vehicle0",NavCommand.Confirm),"ready/vehicle0",NavAction.Vehicle0));
        Check("Vehicle 1 real confirm",()=>Same(N(ready,"ready/vehicle1",NavCommand.Confirm),"ready/vehicle1",NavAction.Vehicle1));
        Check("Vehicle 2 real confirm",()=>Same(N(ready,"ready/vehicle2",NavCommand.Confirm),"ready/vehicle2",NavAction.Vehicle2));
        Check("Right vehicle 0 to 1",()=>Same(N(ready,"ready/vehicle0",NavCommand.Right),"ready/vehicle1"));
        Check("Right vehicle 1 to 2",()=>Same(N(ready,"ready/vehicle1",NavCommand.Right),"ready/vehicle2"));
        Check("Right edge does not jump to lower quit button",()=>Same(N(ready,"ready/vehicle2",NavCommand.Right),"ready/vehicle2"));
        Check("Left vehicle 2 to 1",()=>Same(N(ready,"ready/vehicle2",NavCommand.Left),"ready/vehicle1"));
        Check("Left vehicle 1 to 0",()=>Same(N(ready,"ready/vehicle1",NavCommand.Left),"ready/vehicle0"));
        Check("Left edge stays",()=>Same(N(ready,"ready/vehicle0",NavCommand.Left),"ready/vehicle0"));
        Check("Selected first vehicle down to source seed field",()=>Same(N(ready,"ready/vehicle0",NavCommand.Down),"ready/seed"));
        Check("Seed down to prepare",()=>Same(N(ready,"ready/seed",NavCommand.Down),"ready/prepare"));
        Check("Second vehicle down to resume if checkpoint exists",()=>{
            var s=Ready();s.CheckpointAvailable=true;
            return Same(N(s,"ready/vehicle1",NavCommand.Down),"ready/resume");
        });
        Check("Third vehicle down to quit",()=>Same(N(ready,"ready/vehicle2",NavCommand.Down),"ready/quit"));
        Check("No accidental resume focus when checkpoint absent",()=>!Has(MenuNavCore.Targets(ready),"ready/resume"));
        Check("Resume target appears only with authoritative checkpoint",()=>{
            var s=Ready();s.CheckpointAvailable=true;
            return Has(MenuNavCore.Targets(s),"ready/resume");
        });
        Check("Confirm resume when valid",()=>{
            var s=Ready();s.CheckpointAvailable=true;
            return Same(N(s,"ready/resume",NavCommand.Confirm),"ready/resume",NavAction.ResumeCheckpoint);
        });
        Check("Checkpoint disappears: stale confirmation cannot resume",()=>Same(N(ready,"ready/resume",NavCommand.Confirm),"ready/vehicle0"));
        Check("Seed entry action does not mutate seed",()=>Same(N(ready,"ready/seed",NavCommand.Confirm),"ready/seed",NavAction.EditSeed));
        Check("Prepare action",()=>Same(N(ready,"ready/prepare",NavCommand.Confirm),"ready/prepare",NavAction.PrepareVehicle));
        Check("Sandbox action",()=>Same(N(ready,"ready/sandbox",NavCommand.Confirm),"ready/sandbox",NavAction.OpenSandbox));
        Check("Map editor action",()=>Same(N(ready,"ready/editor",NavCommand.Confirm),"ready/editor",NavAction.OpenMapEditor));
        Check("Quit action",()=>Same(N(ready,"ready/quit",NavCommand.Confirm),"ready/quit",NavAction.Quit));
        Check("Disable map editor removes target",()=>{
            var s=Ready();s.MapEditorAvailable=false;
            return !Has(MenuNavCore.Targets(s),"ready/editor");
        });
        Check("Disable sandbox removes target",()=>{
            var s=Ready();s.SandboxAvailable=false;
            return !Has(MenuNavCore.Targets(s),"ready/sandbox");
        });
        Check("Disable quit removes target",()=>{
            var s=Ready();s.QuitAvailable=false;
            return !Has(MenuNavCore.Targets(s),"ready/quit");
        });
        Check("Disabled map editor confirm fails closed",()=>{
            var s=Ready();s.MapEditorAvailable=false;
            return Same(N(s,"ready/editor",NavCommand.Confirm),"ready/vehicle0");
        });
        Check("Editing seed blocks navigation and action dispatch",()=>{
            var s=Ready();s.IsTextEditing=true;
            return Same(N(s,"ready/seed",NavCommand.Confirm),"ready/seed") &&
                   Same(N(s,"ready/seed",NavCommand.Right),"ready/seed");
        });
        Check("Exited seed text editing enables focus movement",()=>Same(N(ready,"ready/seed",NavCommand.Down),"ready/prepare"));
        Check("Transitioning means no targets",()=>{
            var s=Ready();s.IsTransitioning=true;
            return MenuNavCore.Targets(s).Length==0 && Same(N(s,"ready/prepare",NavCommand.Confirm),"");
        });
        Check("Reward default first card focus and no action",()=>Same(N(Reward(),"",NavCommand.Confirm),"reward/choice0"));
        Check("Reward first confirm yields offer index 0",()=>Same(N(Reward(),"reward/choice0",NavCommand.Confirm),"reward/choice0",NavAction.Reward0));
        Check("Reward second confirm yields offer index 1",()=>Same(N(Reward(),"reward/choice1",NavCommand.Confirm),"reward/choice1",NavAction.Reward1));
        Check("Reward third confirm yields offer index 2",()=>Same(N(Reward(),"reward/choice2",NavCommand.Confirm),"reward/choice2",NavAction.Reward2));
        Check("Reward arrow-right selection 0->1",()=>Same(N(Reward(),"reward/choice0",NavCommand.Right),"reward/choice1"));
        Check("Reward arrow-right selection 1->2",()=>Same(N(Reward(),"reward/choice1",NavCommand.Right),"reward/choice2"));
        Check("Reward rightmost right stays (no phantom wrap)",()=>Same(N(Reward(),"reward/choice2",NavCommand.Right),"reward/choice2"));
        Check("Reward 2 options removes third",()=>!Has(MenuNavCore.Targets(Reward(2)),"reward/choice2"));
        Check("Reward exhausted 0 options has no target and cannot confirm",()=>Same(N(Reward(0),"reward/choice0",NavCommand.Confirm),""));
        Check("Stale reward 2 focus on only 1 available never activates",()=>Same(N(Reward(1),"reward/choice2",NavCommand.Confirm),"reward/choice0"));
        Check("Stale Ready focus in Reward screen cannot run Ready action",()=>Same(N(Reward(),"ready/prepare",NavCommand.Confirm),"reward/choice0"));
        Check("Stale Reward focus in Ready screen cannot select reward",()=>Same(N(ready,"reward/choice0",NavCommand.Confirm),"ready/vehicle0"));
        Check("Results single restart focus",()=>Same(N(Results(),null,NavCommand.None),"results/restart"));
        Check("Results restart explicit confirm",()=>Same(N(Results(),"results/restart",NavCommand.Confirm),"results/restart",NavAction.Restart));
        Check("Results left/right navigation cannot invent actions",()=>Same(N(Results(),"results/restart",NavCommand.Left),"results/restart")&&Same(N(Results(),"results/restart",NavCommand.Right),"results/restart"));
        Check("Results old reward ID blocked",()=>Same(N(Results(),"reward/choice2",NavCommand.Confirm),"results/restart"));
        Check("Null snapshot rejected",()=>Reject(()=>MenuNavCore.Step(null,"",NavCommand.None)));
        var bad=Ready();bad.VehicleCount=0;
        Check("Invalid zero vehicles rejected",()=>Reject(()=>MenuNavCore.Targets(bad)));
        bad=Ready();bad.VehicleCount=4;
        Check("Invalid 4 vehicles rejected",()=>Reject(()=>MenuNavCore.Targets(bad)));
        bad=Ready();bad.SelectedVehicleSlot=3;
        Check("Invalid selected vehicle rejected",()=>Reject(()=>MenuNavCore.Step(bad,"",NavCommand.None)));
        bad=Reward(4);
        Check("Invalid 4 reward options rejected",()=>Reject(()=>MenuNavCore.Targets(bad)));
        bad=Ready();bad.Screen=(NavScreen)900;
        Check("Unknown menu screen rejected",()=>Reject(()=>MenuNavCore.Targets(bad)));
        Check("Unknown command rejected",()=>Reject(()=>MenuNavCore.Step(Ready(),"ready/prepare",(NavCommand)77)));
        Check("Ready exact source vehicle centers",()=>{
            var s=Ready();var a=MenuNavCore.Targets(s);
            return a[0].X==100&&a[0].Y==420&&a[1].X==480&&a[2].X==860;
        });
        Check("Reward exact source card centers",()=>{
            var a=MenuNavCore.Targets(Reward());
            return a[0].X==100&&a[0].Y==490&&a[2].X==860&&a[2].Height==52;
        });
        Check("Deterministic exact replay",()=>{
            var s=Ready();
            var a=N(s,"ready/vehicle1",NavCommand.Down);
            for(int i=0;i<25;i++)if(N(s,"ready/vehicle1",NavCommand.Down).FocusId!=a.FocusId)return false;
            return true;
        });
        Check("No duplicate target identifiers on Ready",()=>{
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var item in MenuNavCore.Targets(Ready()))if(!ids.Add(item.Id))return false;
            return true;
        });
        Check("No duplicate target action IDs on Ready",()=>{
            var ids=new HashSet<NavAction>();
            foreach(var item in MenuNavCore.Targets(Ready()))if(!ids.Add(item.Action))return false;
            return true;
        });
        Console.WriteLine("RESULT "+passed+" PASS, "+failed+" FAIL");
        if(failed>0)Environment.Exit(1);
    }
}
