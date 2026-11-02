using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace B01.EnemyCharge001.Tests{
 public sealed class EnemyChargePhysicsTests{
  GameObject enemy,target,wall;
  EnemyChargeMotor Make(bool obstacle){
   enemy=GameObject.CreatePrimitive(PrimitiveType.Cube);enemy.name="ChargeActor";
   var body=enemy.AddComponent<Rigidbody>();body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
   var motor=enemy.AddComponent<EnemyChargeMotor>();motor.Configure(.08f,1f,1.8f,65f,12f,20f,14f);
   target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=new Vector3(4,0,0);
   target.AddComponent<ChargeDemoHealth>();motor.AssignTarget(target.transform);
   if(obstacle){wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(2,0,0);wall.transform.localScale=new Vector3(.5f,3,3);}
   return motor;
  }
  [TearDown]public void Cleanup(){if(enemy)Object.DestroyImmediate(enemy);if(target)Object.DestroyImmediate(target);if(wall)Object.DestroyImmediate(wall);}
  [UnityTest]public IEnumerator FreeLane_OneRealPhysicsHit(){
   var motor=Make(false);int i=0;while(i++<150&&target.GetComponent<ChargeDemoHealth>().Hits==0)yield return new WaitForFixedUpdate();
   Assert.AreEqual(1,target.GetComponent<ChargeDemoHealth>().Hits);
   Assert.AreNotEqual(EnemyChargeMotor.Phase.Charging,motor.CurrentPhase);
  }
  [UnityTest]public IEnumerator SolidBlocker_DoesNotDamageTarget(){
   Make(true);for(int i=0;i<100;i++)yield return new WaitForFixedUpdate();
   Assert.AreEqual(0,target.GetComponent<ChargeDemoHealth>().Hits);
   Assert.Less(enemy.transform.position.x,target.transform.position.x);
  }
  [UnityTest]public IEnumerator CancelTelegraph_NoDamage(){
   var motor=Make(false);yield return new WaitForFixedUpdate();motor.SetArmed(false);
   for(int i=0;i<90;i++)yield return new WaitForFixedUpdate();
   Assert.AreEqual(0,target.GetComponent<ChargeDemoHealth>().Hits);
  }
 }
}
