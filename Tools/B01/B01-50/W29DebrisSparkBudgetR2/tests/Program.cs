// Executes the EXACT original Unity-facing production adapter compiled against an
// explicitly TEST-ONLY Unity API surface. NOT NATIVE UNITY / NO PhysX OR GPU.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using RoboRougelike.B01Performance;
using RoboRougelike.R3;
using RoboRougelike.VFX001;
using UnityEngine;

int passed = 0;
var failures = new List<string>();
void Test(string name, Action body)
{
    try { body(); ++passed; Console.WriteLine("ORIGINAL_W29_ADAPTER_SOURCE_PASS " + name); }
    catch (Exception ex) {
        failures.Add(name);
        Console.WriteLine("ORIGINAL_W29_ADAPTER_SOURCE_FAIL " + name + " :: " +
            (ex.InnerException ?? ex).Message);
    }
}
void Must(bool predicate, string reason) {
    if (!predicate) throw new InvalidOperationException(reason);
}
void Case(Action<SceneFixture> action, int cap=1)
{
    using var f = new SceneFixture(cap);
    action(f);
}
Test("defer_new_emitter_until_selected", () => Case(f => {
    var fast=f.Add(7);
    Must(fast.NewlyAllocatedVfxCount==0,"Observe should not allocate VFX");
    Must(f.Budget.TrackedCount==1,"physical R3 observation missing");
}));
Test("70_r3_debris_only_allocate_eight_emitters", () => Case(f => {
    var parts=new List<GameObject>();
    for (int i=0;i<70;i++) parts.Add(f.Add(3f+i*.07f));
    Must(parts.Sum(x=>x.NewlyAllocatedVfxCount)==0,"eager emitter allocation on Observe");
    f.Budget.RefreshNow();
    Must(f.Budget.ActiveEmitters==8,"active visual lease budget");
    Must(parts.Sum(x=>x.NewlyAllocatedVfxCount)==8,"allocated unused VFX components");
},8));
Test("cache_rigidbody_component_out_of_refresh", () => Case(f => {
    var g=f.Add(6);
    Must(g.BodyComponentLookups==1,"initial body lookup expected exactly once");
    for (int i=0;i<20;i++) f.Budget.RefreshNow();
    Must(g.BodyComponentLookups==1,"repeated native GetComponent in refresh");
}));
Test("budget_zero_adds_no_visual_components", () => Case(f => {
    var g=f.Add(9);
    f.Budget.RefreshNow();
    Must(g.NewlyAllocatedVfxCount==0,"budget=0 must create no visual");
    Must(f.Budget.ActiveEmitters==0,"budget=0 must render none");
},0));
Test("unchosen_debris_has_no_repeated_vfx_property_writes", () => Case(f => {
    f.Add(9);
    var slow=f.Add(5);
    f.Budget.RefreshNow();
    var emitter=slow.GetComponent<DetachedModuleVfxController>();
    int calls=emitter==null ? 0 : emitter.SetCalls;
    for (int i=0;i<25;i++) f.Budget.RefreshNow();
    emitter=slow.GetComponent<DetachedModuleVfxController>();
    Must(emitter==null||emitter.SetCalls==calls,"unselected emission repeatedly updated");
}));
Test("late_vfx_allocation_after_fastest_retirement", () => Case(f => {
    var fast=f.Add(9);
    var slow=f.Add(5);
    f.Budget.RefreshNow();
    Must(slow.GetComponent<DetachedModuleVfxController>()==null,"slow VFX allocated before lease");
    fast.activeInHierarchy=false;
    f.Budget.RefreshNow();
    Must(f.Budget.TrackedCount==1,"retirement missing");
    Must(slow.GetComponent<DetachedModuleVfxController>()?.EmissionRate>0,"late promotion never emitted");
}));
Test("fastest_r3_debris_wins_visual_lease", () => Case(f => {
    var fast=f.Add(10);
    var slow=f.Add(4);
    f.Budget.RefreshNow();
    Must(f.Budget.ActiveEmitters==1,"lease count");
    Must(fast.GetComponent<DetachedModuleVfxController>()?.EmissionRate>0,"fastest not selected");
    Must(slow.GetComponent<DetachedModuleVfxController>()?.EmissionRate is null or 0f,"slow must not emit");
}));
Test("kinematic_piece_never_enrolled", () => Case(f => {
    var p=f.Add(10,kinematic:true);
    f.Budget.RefreshNow();
    Must(f.Budget.TrackedCount==0 && p.NewlyAllocatedVfxCount==0,"kinematic incorrectly enrolled");
}));
Test("missing_rigidbody_never_enrolled", () => Case(f => {
    var p=f.Add(10,addBody:false);
    f.Budget.RefreshNow();
    Must(f.Budget.TrackedCount==0 && p.NewlyAllocatedVfxCount==0,"missing body enrolled");
}));
Test("duplicate_detach_event_does_not_duplicate_visual", () => Case(f => {
    var g=f.Add(10);
    R3Debris.SimulateSeparated(g.GetComponent<R3Debris>());
    f.Budget.RefreshNow();
    Must(f.Budget.TrackedCount==1 && g.NewlyAllocatedVfxCount==1,"duplicate event mutated budget");
}));
Test("invalid_config_fails_closed", () => Case(f => {
    try { f.Budget.Configure(-1,2,.25f); throw new Exception("negative cap accepted");}
    catch(ArgumentOutOfRangeException){}
    try { f.Budget.Configure(1,float.NaN,.25f);throw new Exception("NaN threshold accepted");}
    catch(ArgumentOutOfRangeException){}
    Must(f.Budget.MaximumEmitters==1,"invalid call changed existing configuration");
}));
Test("disable_visual_budget_does_not_destroy_debris", () => Case(f => {
    var g=f.Add(10);
    f.Budget.RefreshNow();
    f.Disable();
    Must(g.activeInHierarchy,"debris active altered");
    Must(g.GetComponent<Rigidbody>()!=null,"physical body destroyed");
    Must(g.GetComponent<DetachedModuleVfxController>()?.EmissionRate==0,"spark not stopped on disable");
}));
Test("inactive_debris_is_removed_without_destroy", () => Case(f => {
    var g=f.Add(8);
    f.Budget.RefreshNow();
    g.activeInHierarchy=false;
    f.Budget.RefreshNow();
    Must(f.Budget.TrackedCount==0 && f.Budget.ActiveEmitters==0,"retired debris not pruned");
    Must(g.GetComponent<Rigidbody>()!=null,"purge destroyed body");
}));
Test("active_body_velocity_remains_unchanged", () => Case(f => {
    var g=f.Add(8);
    var rb=g.GetComponent<Rigidbody>();
    int writes=rb.VelocityWrites;
    f.Budget.RefreshNow();
    f.Budget.RefreshNow();
    Must(rb.VelocityWrites==writes,"budget wrote Rigidbody velocity");
    Must(rb.linearVelocity.x==8,"body speed altered");
}));
Test("speed_reordering_moves_emission_to_new_fastest", () => Case(f => {
    var first=f.Add(6); var second=f.Add(4);
    f.Budget.RefreshNow();
    first.GetComponent<Rigidbody>().linearVelocity=new Vector3(3,0,0);
    second.GetComponent<Rigidbody>().linearVelocity=new Vector3(12,0,0);
    f.Budget.RefreshNow();
    Must(first.GetComponent<DetachedModuleVfxController>()?.EmissionRate==0,"old lease not revoked");
    Must(second.GetComponent<DetachedModuleVfxController>()?.EmissionRate>0,"reordered lease not granted");
    Must(f.Budget.ActiveEmitters==1,"active count invalid");
}));
Test("full_r3_limit_does_not_track_71st_live_body", () => Case(f => {
    for(int i=0;i<71;i++) f.Add(4+i*.02f);
    Must(f.Budget.TrackedCount==70,"actual 70 R3 limit bypassed");
}));
Test("duplicate_visual_budget_controller_is_disabled", () => Case(f => {
    var other=new GameObject().AddComponent<B0150DetachedDebrisSparkBudget>();
    SceneFixture.Call(other,"OnEnable");
    Must(!other.enabled,"duplicate visual authority not disabled");
    var g=f.Add(9);
    f.Budget.RefreshNow();
    Must(f.Budget.TrackedCount==1 && f.Budget.ActiveEmitters==1,"original owner stopped working");
    SceneFixture.Call(other,"OnDisable");
}));
Test("subthreshold_body_cannot_allocate_sparks", () => Case(f => {
    var g=f.Add(1);
    f.Budget.RefreshNow();
    Must(f.Budget.ActiveEmitters==0,"slow debris incorrectly emitted");
    Must(g.GetComponent<DetachedModuleVfxController>()?.EmissionRate is null or 0f,"slow debris VFX lit");
}));
Test("nan_velocity_is_not_chosen", () => Case(f => {
    var g=f.Add(float.NaN);
    f.Budget.RefreshNow();
    Must(f.Budget.ActiveEmitters==0,"NaN velocity emits sparks");
    Must(g.GetComponent<DetachedModuleVfxController>()?.EmissionRate is null or 0f,"NaN VFX emitted");
}));
Test("reuse_existing_vfx_without_duplicate", () => Case(f => {
    var g=f.Add(7,existingFx:true);
    Must(g.NewlyAllocatedVfxCount==1,"preexisting VFX fixture");
    f.Budget.RefreshNow();
    Must(g.NewlyAllocatedVfxCount==1,"existing VFX double-allocated");
    Must(g.GetComponent<DetachedModuleVfxController>()?.EmissionRate>0,"existing emitter never used");
}));
Test("enable_after_disable_reobserves_new_debris", () => Case(f => {
    f.Add(5);
    f.Disable();
    f.Enable();
    var later=f.Add(8);
    f.Budget.RefreshNow();
    Must(f.Budget.TrackedCount==1 && f.Budget.ActiveEmitters==1,"reenabled component not subscribed");
    Must(later.GetComponent<DetachedModuleVfxController>()?.EmissionRate>0,"new debris missing visuals");
}));
string receipt=null;
for (int i=0;i<args.Length-1;i++) if(args[i]=="--receipt")receipt=args[i+1];
var report=new {
    testSystem="ORIGINAL_W29_UNITY_FACING_ADAPTER_CSHARP_ON_EXPLICIT_TEST_SURFACE",
    originalUnityFacingSource=true,
    nativeUnity="NOT_RUN",nativePhysX="NOT_RUN",nativeGpu="NOT_MEASURED",
    total=passed+failures.Count,passed,failed=failures.Count,failures
};
if (receipt!=null) {
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(receipt))!);
    File.WriteAllText(receipt,JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
}
Console.WriteLine($"ORIGINAL_W29_ADAPTER_SOURCE_TESTS {passed}/{report.total} PASS, {failures.Count} FAIL; UNITY_G1_NOT_RUN");
if (failures.Count>0) Environment.ExitCode=1;

sealed class SceneFixture : IDisposable
{
    public GameObject Owner { get; } = new GameObject();
    public B0150DetachedDebrisSparkBudget Budget { get; }
    private bool disabled;
    public SceneFixture(int budget=1) {
        Budget=Owner.AddComponent<B0150DetachedDebrisSparkBudget>();
        Call(Budget,"OnEnable");
        Budget.Configure(budget,2f,.25f);
    }
    public GameObject Add(float speed,bool kinematic=false,bool addBody=true,bool existingFx=false)
    {
        var obj=new GameObject();
        if(addBody) {
            var b=obj.AddComponent<Rigidbody>();
            b.isKinematic=kinematic;
            b.linearVelocity=new Vector3(speed,0,0);
        }
        if(existingFx)obj.AddComponent<DetachedModuleVfxController>();
        var item=obj.AddComponent<R3Debris>();
        R3Debris.SimulateSeparated(item);
        return obj;
    }
    public void Disable() { if(disabled)return; disabled=true; Call(Budget,"OnDisable"); }
    public void Enable() { if(!disabled)return; disabled=false; Call(Budget,"OnEnable");}
    public void Dispose() { Disable(); }
    public static void Call(object o,string name) {
        var method=o.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance);
        if(method==null)throw new Exception("Cannot invoke actual source lifecycle "+name);
        method.Invoke(o,null);
    }
}
