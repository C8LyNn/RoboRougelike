#if UNITY_EDITOR || R4_STANDALONE_QA
using System.Collections;
using System.Linq;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
namespace RoboRougelike.R4
{
    // Only the separate QA executable contains this fixture. Never included in the release.
    // Seeds a damaged victory using damage API, then leaves all UI interactions to the tester.
    public sealed class R4StandaloneQAFixture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-r4-qa-loot")<0)return;
            new GameObject("R4 QA fixture — not release gameplay").AddComponent<R4StandaloneQAFixture>();
        }
        private IEnumerator Start()
        {
            while(R1RunController.Instance?.Progress==null)yield return null;
            var run=R1RunController.Instance;run.StartRun();while(run.Transitioning)yield return null;
            run.Encounter.SetCombatActive(false);
            var assembly=run.Player.GetComponent<R3VehicleAssembly>();
            var wheel=assembly.Parts.First(p=>p.Kind==R3PartKind.Wheel);wheel.Damageable.ApplyDamage(10000,wheel.transform.position,Vector3.up);
            run.Player.Core.ApplyDamage(10,run.Player.Core.transform.position,Vector3.forward);
            var enemy=run.Encounter.Enemies.Single();enemy.WeaponBlock.ApplyDamage(enemy.WeaponBlock.MaxHp*0.35f,enemy.WeaponBlock.transform.position,Vector3.up);
            enemy.Core.ApplyDamage(10000,enemy.Core.transform.position,Vector3.up);
            while(run.Progress.State!=R1RunState.Salvage)yield return null;
            Debug.Log("[R4 QA FIXTURE] UI ready. First victory seeded using damage API; NOT evidence of a played combat run.");
            Destroy(gameObject);
        }
    }
}
#endif
