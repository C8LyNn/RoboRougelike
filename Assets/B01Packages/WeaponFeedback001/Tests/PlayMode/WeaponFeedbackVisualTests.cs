using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace B01.WeaponFeedback001.Tests
{
 public sealed class WeaponFeedbackVisualTests
 {
  GameObject host,impactOriginal;
  [TearDown]public void Cleanup(){if(host)Object.DestroyImmediate(host);if(impactOriginal)Object.DestroyImmediate(impactOriginal);}
  WeaponFeedbackVisual Make(out Transform pivot,out Light light)
  {
   host=new GameObject("VisualOnlyHost");
   var body=new GameObject("VisualRecoil").transform;body.SetParent(host.transform,false);
   pivot=body;
   var flash=new GameObject("MuzzleLight");flash.transform.SetParent(host.transform,false);
   light=flash.AddComponent<Light>();light.enabled=false;
   impactOriginal=new GameObject("IsolatedImpactFX");impactOriginal.SetActive(false);
   var impact=impactOriginal.AddComponent<ParticleSystem>();
   var visual=host.AddComponent<WeaponFeedbackVisual>();
   visual.BindVisuals(pivot,null,light,impact,null);
   return visual;
  }
  [UnityTest]public IEnumerator ConfirmedShot_OnlyVisualsAndRecovers()
  {
   var v=Make(out Transform pivot,out Light light);
   Vector3 start=pivot.localPosition;
   v.NotifyShot();Assert.AreEqual(1,v.ShotFeedbackCount);
   Assert.IsTrue(light.enabled);
   Assert.Less(pivot.localPosition.z,start.z);
   yield return new WaitForSeconds(.35f);
   Assert.IsFalse(light.enabled);
   Assert.Less(Vector3.Distance(pivot.localPosition,start),.03f);
  }
  [UnityTest]public IEnumerator Impact_InstantiatesIndependentParticle()
  {
   var v=Make(out Transform pivot,out Light light);
   v.NotifyImpact(new Vector3(2,1,0),Vector3.up);
   Assert.AreEqual(1,v.ImpactFeedbackCount);
   yield return null;
   Assert.AreEqual(1, v.ImpactFeedbackCount);
  }
  [UnityTest]public IEnumerator NoConfirmedShot_MuzzleDoesNotAutoPlay()
  {
   var v=Make(out Transform pivot,out Light light);
   var muzzleGo=new GameObject("MuzzleFixture");
   muzzleGo.transform.SetParent(host.transform,false);
   var muzzle=muzzleGo.AddComponent<ParticleSystem>();
   v.BindVisuals(pivot,muzzle,light,null,null);
   yield return null;
   Assert.IsFalse(muzzle.isPlaying,"Binding must clear playOnAwake.");
   Assert.IsFalse(light.enabled,"No confirmed shot means no muzzle light.");
   Assert.AreEqual(0,v.ShotFeedbackCount);
   v.NotifyShot();
   Assert.AreEqual(1,v.ShotFeedbackCount);
   Assert.IsTrue(muzzle.isPlaying);
  }
  [UnityTest]public IEnumerator DisableStopsAllEffects_AndReusesImpactPoolAfterEnable()
  {
   var v=Make(out Transform pivot,out Light light);
   var smokeGo=new GameObject("DamageSmokeFixture");
   smokeGo.transform.SetParent(host.transform,false);
   var smoke=smokeGo.AddComponent<ParticleSystem>();
   var main=smoke.main;main.playOnAwake=false;
   var muzzleGo=new GameObject("MuzzleFixture");
   muzzleGo.transform.SetParent(host.transform,false);
   var muzzle=muzzleGo.AddComponent<ParticleSystem>();
   v.BindVisuals(pivot,muzzle,light,impactOriginal.GetComponent<ParticleSystem>(),smoke);
   v.NotifyImpact(Vector3.zero,Vector3.up);
   v.NotifyShot();
   v.SetWeaponDamageVisual(.7f);
   Assert.AreEqual(1,v.ImpactPoolSize);
   Assert.IsTrue(light.enabled);
   Assert.IsTrue(smoke.emission.enabled);
   v.enabled=false;
   Assert.IsFalse(light.enabled);
   Assert.IsFalse(muzzle.isPlaying);
   Assert.IsFalse(smoke.emission.enabled);
   Assert.AreEqual(1,v.ImpactPoolSize,"Disabling must retain pool for reuse.");
   v.NotifyImpact(Vector3.one,Vector3.up);
   v.NotifyShot();
   v.SetWeaponDamageVisual(.8f);
   Assert.AreEqual(1,v.ImpactFeedbackCount);
   Assert.AreEqual(1,v.ShotFeedbackCount);
   Assert.IsFalse(smoke.emission.enabled,"Disabled component must not restart smoke.");
   v.enabled=true;
   Assert.IsTrue(smoke.emission.enabled,"Visual damage state resumes when enabled.");
   v.NotifyImpact(Vector3.one,Vector3.up);
   Assert.AreEqual(1,v.ImpactPoolSize,"Re-enabling should not allocate again.");
   yield return null;
  }
    [UnityTest]public IEnumerator RebindVisuals_ResetsOldModelAndDetachedImpactPool()
  {
   var v=Make(out Transform oldPivot,out Light oldLight);
   var oldSmokeGo=new GameObject("OldDamageSmoke");
   oldSmokeGo.transform.SetParent(host.transform,false);
   var oldSmoke=oldSmokeGo.AddComponent<ParticleSystem>();
   var smokeMain=oldSmoke.main;smokeMain.playOnAwake=false;
   v.BindVisuals(oldPivot,null,oldLight,
    impactOriginal.GetComponent<ParticleSystem>(),oldSmoke);
   Vector3 oldRest=oldPivot.localPosition;
   v.NotifyShot();
   v.NotifyImpact(Vector3.one,Vector3.up);
   v.SetWeaponDamageVisual(.5f);
   Assert.Less(oldPivot.localPosition.z,oldRest.z);
   Assert.IsTrue(oldLight.enabled);
   Assert.AreEqual(1,v.ImpactPoolSize);
   Assert.IsTrue(oldSmoke.emission.enabled);

   var nextPivotGo=new GameObject("NewVisualPivot");
   nextPivotGo.transform.SetParent(host.transform,false);
   nextPivotGo.transform.localPosition=new Vector3(.2f,0f,.3f);
   Vector3 nextRest=nextPivotGo.transform.localPosition;
   var nextLightGo=new GameObject("NewMuzzleLight");
   nextLightGo.transform.SetParent(host.transform,false);
   var nextLight=nextLightGo.AddComponent<Light>();
   var nextImpactGo=new GameObject("NewImpactFX");
   nextImpactGo.transform.SetParent(host.transform,false);
   nextImpactGo.SetActive(false);
   var nextImpact=nextImpactGo.AddComponent<ParticleSystem>();
   v.BindVisuals(nextPivotGo.transform,null,nextLight,nextImpact,null);
   Assert.Less(Vector3.Distance(oldPivot.localPosition,oldRest),.0001f);
   Assert.IsFalse(oldLight.enabled);
   Assert.IsFalse(oldSmoke.emission.enabled);
   Assert.AreEqual(0,v.ImpactPoolSize,"Previous detached hit particles are released.");
   Assert.AreEqual(nextRest,nextPivotGo.transform.localPosition);
   v.NotifyShot();
   v.NotifyImpact(Vector3.right,Vector3.up);
   Assert.Less(nextPivotGo.transform.localPosition.z,nextRest.z);
   Assert.AreEqual(oldRest,oldPivot.localPosition);
   Assert.AreEqual(1,v.ImpactPoolSize);
   yield return null;
  }

  [UnityTest]public IEnumerator SameVisualsRebind_DoesNotMakeRecoilPermanent()
  {
   var v=Make(out Transform pivot,out Light light);
   var fx=impactOriginal.GetComponent<ParticleSystem>();
   Vector3 rest=pivot.localPosition;
   v.NotifyShot();
   Assert.Less(pivot.localPosition.z,rest.z);
   v.BindVisuals(pivot,null,light,fx,null);
   yield return new WaitForSecondsRealtime(.35f);
   Assert.Less(Vector3.Distance(pivot.localPosition,rest),.03f,
    "A second BindVisuals call cannot adopt the recoiled pose as rest.");
  }

    [UnityTest]public IEnumerator PausedGame_FlashAndImpactExpireAndRecoilRecovers()
  {
   var v=Make(out Transform pivot,out Light light);
   var field=typeof(WeaponFeedbackVisual).GetField("impactLifetimeSeconds",
     System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
   Assert.IsNotNull(field);
   field.SetValue(v,.05f);
   var initial=pivot.localPosition;
   v.NotifyShot();
   v.NotifyImpact(Vector3.one,Vector3.up);
   Assert.IsTrue(light.enabled);
   Assert.AreEqual(1,v.ImpactPoolSize);
   float priorScale=Time.timeScale;
   Time.timeScale=0f;
   try
   {
    yield return new WaitForSecondsRealtime(.25f);
    Assert.IsFalse(light.enabled,"Muzzle flash must not stick during pause.");
    Assert.Less(Vector3.Distance(initial,pivot.localPosition),.03f,
      "Visual recoil must recover in unscaled time.");
    int activeImpacts=0;
    foreach(var ps in Object.FindObjectsByType<ParticleSystem>(
      FindObjectsInactive.Include,FindObjectsSortMode.None))
     if(ps.name.StartsWith("IsolatedImpactFX(Clone)")&&ps.gameObject.activeSelf)
      activeImpacts++;
    Assert.AreEqual(0,activeImpacts,"Hit particles should expire during pause.");
    Assert.AreEqual(1,v.ImpactPoolSize,"Expired FX stays pooled.");
   }
   finally { Time.timeScale=priorScale; }
  }
  [UnityTest]public IEnumerator InvalidSerializedImpactBudget_IsBoundedAndSafe()
  {
   var v=Make(out Transform pivot,out Light light);
   var field=typeof(WeaponFeedbackVisual).GetField("maximumImpactInstances",
     System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
   Assert.IsNotNull(field);
   field.SetValue(v,0);
   for(int i=0;i<40;i++)v.NotifyImpact(Vector3.right*i,Vector3.up);
   Assert.AreEqual(1,v.ImpactPoolSize,
    "A zero-valued serialized capacity must fail safely without divide by zero.");
   field.SetValue(v,999);
   for(int i=0;i<64;i++)v.NotifyImpact(Vector3.right*i,Vector3.up);
   Assert.AreEqual(32,v.ImpactPoolSize,"A corrupted oversized capacity must cap at 32.");
   yield return null;
  }
    [UnityTest]public IEnumerator RapidBurst_ReusesBoundedImpactPool()
  {
   var v=Make(out Transform pivot,out Light light);
   for(int i=0;i<48;i++)
    v.NotifyImpact(new Vector3(i,1,0),Vector3.up);
   Assert.AreEqual(48,v.ImpactFeedbackCount);
   Assert.AreEqual(12,v.ImpactPoolSize,
    "FX pool must cap the number of created particle GameObjects.");
   yield return null;
   Assert.AreEqual(12,v.ImpactPoolSize);
   for(int i=0;i<24;i++)
    v.NotifyImpact(new Vector3(i,2,0),Vector3.forward);
   Assert.AreEqual(12,v.ImpactPoolSize,
    "Further hits must reuse the existing GameObjects.");
  }
    [UnityTest]public IEnumerator DamageSmoke_EmitsOnlyWhileDamaged()
  {
   var v=Make(out Transform pivot,out Light light);
   var smokeGo=new GameObject("SmokeFixture");
   smokeGo.transform.SetParent(host.transform,false);
   var smoke=smokeGo.AddComponent<ParticleSystem>();
   var main=smoke.main;main.playOnAwake=false;
   smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   v.BindVisuals(pivot,null,light,null,smoke);
   v.SetWeaponDamageVisual(.65f);
   Assert.IsTrue(smoke.emission.enabled);
   yield return null;
   v.SetWeaponDamageVisual(0f);
   Assert.IsFalse(smoke.emission.enabled);
  }
    [UnityTest]public IEnumerator WeaponDamageVisual_ClampsWithoutGameplayDamage()
  {
   var v=Make(out Transform pivot,out Light light);
   v.SetWeaponDamageVisual(5f);
   Assert.AreEqual(1f,v.DamageVisualFraction);
   v.SetWeaponDamageVisual(-3f);
   Assert.AreEqual(0f,v.DamageVisualFraction);
   yield return null;
  }
 }
}
