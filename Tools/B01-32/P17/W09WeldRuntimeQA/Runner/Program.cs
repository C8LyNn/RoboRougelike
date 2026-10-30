// REAL original W06 code and two authored PlayMode IEnumerator sources are compiled
// unchanged; only UnityEngine API semantics here are TEST STUB, not real Unity.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using RoboRougelike.B0132P17.RepairWeld;
using RoboRougelike.B0132P17.RepairWeld.Tests;
using UnityEngine;
using UnityEngine.TestTools;
int checks=0;
void Check(bool ok,string name){if(!ok)throw new Exception("W09_UNCHANGED_WELD_RUNTIME_RED "+name);checks++;}
void Near(float a,float b,float eps,string name)=>Check(Math.Abs(a-b)<=eps,name);
void Tick(B0132RepairWeldVfx fx,float dt){Time.deltaTime=dt;fx.InvokeLifecycle("Update");}
bool AllClear(GameObject x)=>x.GetComponentsInChildren<LineRenderer>().All(l=>!l.enabled);
LineRenderer[] Lines(GameObject x)=>x.GetComponentsInChildren<LineRenderer>();
var prefab=Path.GetFullPath("build/W09WeldReceiver/Original/B0132_RepairWeld.prefab");
Resources.VerifiedPrefabricatedPath=prefab;
Check(File.Exists(prefab),"real original prefab must exist before mock runtime");

// Run both original *unaltered* authored UnityTest coroutine method bodies
// inside surrogate harness, not native Unity TestRunner.
var author=new RepairWeldPlayModeTests();
var originalMethods=typeof(RepairWeldPlayModeTests).GetMethods()
 .Where(m=>m.GetCustomAttribute<UnityTestAttribute>()!=null).OrderBy(m=>m.Name).ToArray();
Check(originalMethods.Length==2,"exact two original authored UnityTests compiled");
foreach(var m in originalMethods)
{
 var result=(IEnumerator)m.Invoke(author,null);
 int steps=0;
 while(result.MoveNext()){Check(++steps<8,"original coroutine finite "+m.Name);}
 Check(steps==2,"original coroutine yielded twice "+m.Name);
 Console.WriteLine("W09_ORIGINAL_AUTHORED_PLAYMODE_BODY_SURROGATE_PASS "+m.Name);
}
Check(NUnit.Framework.Assert.Calls>=19,"authored assertions genuinely executed");

// Exercising original unchanged MonoBehaviour methods with strict synthetic API.
GameObject New(){
 var go=new GameObject("W09_UNITYENGINE_TEST_STUB");
 go.AddComponent<B0132RepairWeldVfx>();
 return go;
}
var go1=New();var fx1=go1.GetComponent<B0132RepairWeldVfx>();
Check(Lines(go1).Length==9,"actual original Awake creates nine components");
Check(Lines(go1).Count(x=>x.gameObject.name=="WeldArc")==1,"one actual arc GameObject");
Check(Lines(go1).Count(x=>x.gameObject.name.StartsWith("WeldSpark_"))==8,"eight spark children");
Check(AllClear(go1),"Awake disables nine original renderers");
Check(Lines(go1).All(x=>x.positionCount==2&&!x.useWorldSpace),"two local positions each");
Check(Lines(go1).All(x=>x.sharedMaterial!=null),"material bound original shader");
Check(Lines(go1).All(x=>x.sharedMaterial.shader.name=="Sprites/Default"),"primary shader used");
var arc=Lines(go1).Single(x=>x.gameObject.name=="WeldArc");
fx1.BeginWeld(1337u);
Check(fx1.Phase==WeldPhase.Welding&&arc.enabled,"start actual original visible arc");
var start=arc.Positions[0].x;
fx1.SetVisualProgress(.5f);
Check(arc.Positions[0].x>start,"original authored progress moves arc");
Near(0f,arc.Positions[0].x,0.001f,"original arc halfway");
Tick(fx1,.04f);
Check(Lines(go1).Any(x=>x.enabled),"actual source update draws nonempty renderer");
Check(Lines(go1).All(x=>x.Positions.All(p=>WeldVisualCore.Finite(p.x)&&WeldVisualCore.Finite(p.y))),"original samples finite");
fx1.SetVisualProgress(float.NaN);
Near(0f,arc.Positions[0].x,.001f,"NaN cannot pollute original render");
fx1.FinishWeld();
Check(fx1.Phase==WeldPhase.Cooling,"actual original MonoBehaviour cooling");
Tick(fx1,.30f);
Check(fx1.IsVisualActive&&arc.enabled,"mid cooling remains visible");
Check(arc.startColor.a>0f&&arc.startColor.a<1f,"real source material color alpha fades");
Tick(fx1,.40f);
Check(fx1.Phase==WeldPhase.Idle&&AllClear(go1),"cool completion removes nine visuals");
fx1.BeginWeld(9001u);
fx1.SetVisualsEnabled(false);
Check(!fx1.IsVisualActive&&fx1.Phase==WeldPhase.Idle&&AllClear(go1),"VFX OFF clears core and renderer");
fx1.SetVisualsEnabled(true);
Check(!fx1.IsVisualActive&&AllClear(go1),"ON does not resurrect old state");
fx1.BeginWeld(9002u);
Check(fx1.IsVisualActive,"fresh event required after ON");
go1.SetActive(false);
Check(fx1.Phase==WeldPhase.Idle&&AllClear(go1),"original OnDisable resets both");
go1.SetActive(true);
Tick(fx1,.03f);
Check(!fx1.IsVisualActive&&AllClear(go1),"no stale visual on enable");
// shader failover must still create nine lines with fallback URP material:
Shader.PrimaryAvailable=false;
var alt=New();var altFx=alt.GetComponent<B0132RepairWeldVfx>();
Check(Lines(alt).Length==9&&Lines(alt).All(x=>x.sharedMaterial!=null),"fallback 9 material assignments");
Check(Lines(alt).All(x=>x.sharedMaterial.shader.name=="Universal Render Pipeline/Unlit"),"fallback selected");
altFx.BeginWeld(3u);Check(altFx.IsVisualActive,"fallback starts");
altFx.CancelWeld();Check(AllClear(alt),"fallback cancelled");
// one effect instance cannot affect separate instance
Shader.PrimaryAvailable=true;
var second=New();var secondFx=second.GetComponent<B0132RepairWeldVfx>();
secondFx.BeginWeld(22u);secondFx.SetVisualProgress(.7f);
Check(!fx1.IsVisualActive&&AllClear(go1),"separate instance independent of first");
Check(secondFx.IsVisualActive,"other instance remains active");
var material=Lines(second)[0].sharedMaterial;
Object.Destroy(second);
Check(material.WasDestroyed,"original OnDestroy disposes visual material");
Object.Destroy(alt);
Object.Destroy(go1);
Check(Lines(second).All(l=>!l.enabled),"original source destruction hides lines");
// 512 deterministic actual Unity adapter interactions with independent instances
uint seed=0xA43F1234u;
for(int i=0;i<512;i++)
{
 unchecked{seed=seed*1664525u+1013904223u;}
 var sample=New();var fx=sample.GetComponent<B0132RepairWeldVfx>();
 fx.BeginWeld(seed);
 float p=(seed%101u)/100f;
 fx.SetVisualProgress(p);
 Tick(fx,.01f+(seed%17u)/1000f);
 Check(fx.IsVisualActive,"seeded active "+i);
 Check(Lines(sample).Length==9&&Lines(sample).All(l=>WeldVisualCore.Finite(l.Positions[0].x)),"seeded geometry finite "+i);
 if(i%3==0)fx.CancelWeld();else if(i%3==1)fx.SetVisualsEnabled(false);else sample.SetActive(false);
 Check(fx.Phase==WeldPhase.Idle&&AllClear(sample),"seeded cancellation removes render lines "+i);
 Object.Destroy(sample);
}
Console.WriteLine("W09_ACTUAL_UNMODIFIED_WELD_MONOBEHAVIOUR_SURROGATE_PASS "+checks+"/"+checks+
" original compiled C#9 against test-only UnityEngine; ORIGINAL_AUTHOR_ASSERTS="+NUnit.Framework.Assert.Calls);
Console.WriteLine("W09_NATIVE_UNITY_NOT_RUN: surrogate GameObject, Resources and Renderer are not engine objects");
