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
