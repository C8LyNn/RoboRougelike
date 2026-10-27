#if UNITY_EDITOR || R5_STANDALONE_QA
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using RoboRougelike.R1;
using RoboRougelike.R3;
namespace RoboRougelike.R5
{
    // QA-only API fixture. Absent from the release executable. Never evidence of a combat win.
    public sealed class R5StandaloneQA : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-r5-qa")<0)return;
            SceneManager.sceneLoaded-=Create;SceneManager.sceneLoaded+=Create;
        }
        private static void Create(Scene scene,LoadSceneMode mode){new GameObject("R5 QA API fixture").AddComponent<R5StandaloneQA>();}
        private void Update()
        {
            var run=R1RunController.Instance;
            if(run?.Progress?.State==R1RunState.Combat&&!run.Transitioning)run.Encounter.SetCombatActive(false);
        }
        private void OnGUI()
        {
            var run=R1RunController.Instance;if(run?.Progress?.State!=R1RunState.Combat||run.Transitioning)return;
            var previous=GUI.matrix;float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);
            if(GUI.Button(new Rect(1000,190,260,48),"QA: zakończ walkę przez API"))
            {
                run.Encounter.SetCombatActive(false);
                if(run.Progress.EncounterIndex==0)
                {
                    var gun=run.Encounter.Enemies[0].WeaponBlock;gun.ApplyDamage(gun.MaxHp*.35f,gun.transform.position,Vector3.up);
                    run.Player.Core.ApplyDamage(10,run.Player.Core.transform.position,Vector3.up);
                }
                foreach(var enemy in run.Encounter.Enemies)if(!enemy.IsDefeated)enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
                Debug.Log("[R5 QA FIXTURE] API victory seed="+run.Seed+" stage="+(run.Progress.EncounterIndex+1)+"; NOT a played combat win.");
            }
            GUI.Label(new Rect(1000,242,260,42),"Fixture UI • AI wstrzymane • tylko QA");GUI.matrix=previous;
        }
    }
}
#endif
