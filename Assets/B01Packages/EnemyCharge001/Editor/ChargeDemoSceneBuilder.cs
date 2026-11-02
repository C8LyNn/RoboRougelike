#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace B01.EnemyCharge001.Editor{
 public static class ChargeDemoSceneBuilder{
  const string Root="Assets/B01Packages/EnemyCharge001";
  [MenuItem("B01/Enemy Charge 001/Generate Prefab And Demo Scene")]
  public static void Generate(){
   Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Root+"/Scenes");
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
   floor.name="Floor";floor.transform.position=new Vector3(0,-.65f,0);floor.transform.localScale=new Vector3(14,.3f,8);
   var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.name="ChargeTestTarget";
   target.transform.position=new Vector3(4,0,0);target.AddComponent<ChargeDemoHealth>();
   var enemy=GameObject.CreatePrimitive(PrimitiveType.Cube);enemy.name="EnemyCharge001";
   enemy.transform.position=new Vector3(-4,0,0);
   var rb=enemy.AddComponent<Rigidbody>();rb.constraints=RigidbodyConstraints.FreezeRotation;
   rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
   var motor=enemy.AddComponent<EnemyChargeMotor>();
   // Do not serialize scene target reference into the reusable prefab.
   PrefabUtility.SaveAsPrefabAsset(enemy,Root+"/Prefabs/EnemyCharge001.prefab");
   motor.AssignTarget(target.transform);
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
   wall.name="MoveIntoChargeLane";wall.transform.position=new Vector3(0,0,2);
   wall.transform.localScale=new Vector3(.5f,2,2);
   EditorSceneManager.SaveScene(scene,Root+"/Scenes/EnemyCharge001_Demo.unity");
   AssetDatabase.Refresh();
  }
 }
}
#endif
