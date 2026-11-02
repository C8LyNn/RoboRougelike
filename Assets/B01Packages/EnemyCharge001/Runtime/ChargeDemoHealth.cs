using UnityEngine;
namespace B01.EnemyCharge001 {
 // Test-scene-only sink, not canonical combat health authority.
 public sealed class ChargeDemoHealth:MonoBehaviour,IChargeDamageSink{
  [SerializeField,Min(1f)] float maxHealth=100f;
  public float Health{get;private set;} public int Hits{get;private set;}
  void Awake(){Health=maxHealth;}
  public void ReceiveChargeDamage(float amount,Vector3 point,GameObject instigator){
   if(amount<=0||Health<=0)return;Health=Mathf.Max(0,Health-amount);Hits++;
  }
 }
}
