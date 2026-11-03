#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace B01.WeaponFeedback001.Editor{
 public static class WeaponFeedbackSceneBuilder{
  const string Root="Assets/B01Packages/WeaponFeedback001";
  [MenuItem("B01/Weapon Feedback 001/Generate Prefabs And Demo Scene")]
  public static void Generate(){
   Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Root+"/Scenes");
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var impactGo=new GameObject("ImpactSparks_VisualOnly");
   var impactPs=impactGo.AddComponent<ParticleSystem>();
   var impactMain=impactPs.main;impactMain.loop=false;impactMain.duration=.2f;impactMain.startLifetime=.25f;
   PrefabUtility.SaveAsPrefabAsset(impactGo,Root+"/Prefabs/WeaponImpactFX001.prefab");
   Object.DestroyImmediate(impactGo);
   var impactPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/WeaponImpactFX001.prefab").GetComponent<ParticleSystem>();

   var weapon=new GameObject("WeaponFeedback001_Root");
   var pivot=new GameObject("VisualRecoilPivot").transform;pivot.SetParent(weapon.transform,false);
   var barrel=GameObject.CreatePrimitive(PrimitiveType.Cube);
   barrel.name="VisibleBarrel_VisualOnly";barrel.transform.SetParent(pivot,false);
   barrel.transform.localScale=new Vector3(.2f,.2f,1.4f);
   Object.DestroyImmediate(barrel.GetComponent<BoxCollider>());
   var muzzle=new GameObject("MuzzleFlash");muzzle.transform.SetParent(pivot,false);
   muzzle.transform.localPosition=new Vector3(0,0,.7f);
   var muzzlePs=muzzle.AddComponent<ParticleSystem>();
   var muzzleEmission=muzzlePs.emission;muzzleEmission.rateOverTime=0f;
   var muzzleBurst=muzzlePs.emission;muzzleBurst.SetBursts(new[]{new ParticleSystem.Burst(0f,8)});
   var main=muzzlePs.main;main.loop=false;main.duration=.12f;main.startLifetime=.08f;
   muzzlePs.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var flash=muzzle.AddComponent<Light>();flash.range=3f;flash.intensity=2f;flash.enabled=false;
   var smokeGo=new GameObject("DamageSmoke_VisualOnly");
   smokeGo.transform.SetParent(pivot,false);
   smokeGo.transform.localPosition=new Vector3(0f,.13f,-.1f);
   var smoke=smokeGo.AddComponent<ParticleSystem>();
   var smokeMain=smoke.main;smokeMain.loop=true;smokeMain.startLifetime=.7f;
   smokeMain.startSpeed=.3f;smokeMain.startSize=.15f;
   smokeMain.playOnAwake=false;
   var smokeEmission=smoke.emission;smokeEmission.enabled=false;
   smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var visual=weapon.AddComponent<WeaponFeedbackVisual>();
   visual.BindVisuals(pivot,muzzlePs,flash,impactPrefab,smoke);
   PrefabUtility.SaveAsPrefabAsset(weapon,Root+"/Prefabs/WeaponFeedback001.prefab");
   Object.DestroyImmediate(weapon);

   // A genuinely populated, isolated visual demo scene; never attaches to
   // real gun firing, health, movement, or combat authority.
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/WeaponFeedback001.prefab");
   if(!prefab)throw new System.InvalidOperationException("Weapon feedback prefab not generated");
   var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
   instance.transform.position=Vector3.zero;
   var cameraGo=new GameObject("WeaponFeedback_DemoCamera");
   var camera=cameraGo.AddComponent<Camera>();
   camera.transform.position=new Vector3(2.1f,1.2f,-3.8f);
   camera.transform.LookAt(new Vector3(0f,0f,.4f));
   camera.clearFlags=CameraClearFlags.SolidColor;
   camera.backgroundColor=new Color(.08f,.1f,.14f);
   var keyGo=new GameObject("WeaponFeedback_DemoKeyLight");
   var key=keyGo.AddComponent<Light>();key.type=LightType.Directional;
   key.intensity=1.4f;keyGo.transform.rotation=Quaternion.Euler(38f,-25f,0f);
   EditorSceneManager.MarkSceneDirty(scene);
   EditorSceneManager.SaveScene(scene,Root+"/Scenes/WeaponFeedback001_Demo.unity");
   AssetDatabase.Refresh();
  }
 }
}
#endif
