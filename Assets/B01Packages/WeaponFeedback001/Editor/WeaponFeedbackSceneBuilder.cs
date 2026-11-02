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
   var main=muzzlePs.main;main.loop=false;main.duration=.12f;main.startLifetime=.08f;
   muzzlePs.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var flash=muzzle.AddComponent<Light>();flash.range=3f;flash.intensity=2f;flash.enabled=false;
   var visual=weapon.AddComponent<WeaponFeedbackVisual>();
   visual.BindVisuals(pivot,muzzlePs,flash,impactPrefab,null);
   PrefabUtility.SaveAsPrefabAsset(weapon,Root+"/Prefabs/WeaponFeedback001.prefab");
   EditorSceneManager.SaveScene(scene,Root+"/Scenes/WeaponFeedback001_Demo.unity");
   AssetDatabase.Refresh();
  }
 }
}
#endif
