using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R5;
using RogueMachine.P12PipelineSafe;
public class R5ArenaTests
{
    [UnityTest] public IEnumerator SolidLayoutsHaveSafeSpawnsAndAiTraversesIndustrialCover()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run,Is.Not.Null);run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;
        run.Encounter.SetCombatActive(false);var enemy=run.Encounter.Enemies.First();
        foreach(R5ArenaKind kind in System.Enum.GetValues(typeof(R5ArenaKind)))
        {
            run.Arena.Build(kind);yield return null;
            var colliders=run.Arena.Layout.GetComponentsInChildren<Collider>();Assert.That(colliders.Length,Is.GreaterThanOrEqualTo(6));
            Assert.That(colliders.All(c=>c.enabled&&!c.isTrigger),Is.True);
            foreach(var c in colliders){Assert.That(c.bounds.Contains(run.Arena.PlayerSpawn),Is.False);Assert.That(c.bounds.Contains(new Vector3(0,.8f,17)),Is.False);}
        }
        run.Player.MoveTo(new Vector3(-17,.7f,23),Quaternion.identity);
        var rb=enemy.GetComponent<Rigidbody>();rb.position=new Vector3(-17,0,-19);Physics.SyncTransforms();enemy.SetCombatActive(true);
        float end=Time.time+18;float maximumZ=rb.position.z;float nextLog=Time.time;
        while(Time.time<end&&run.Progress.State==R1RunState.Combat)
        {
            if(Time.time>=nextLog){Debug.Log($"R6 AI obstacle trace position={rb.position}, velocity={rb.linearVelocity}, yaw={rb.rotation.eulerAngles.y}");nextLog=Time.time+2;}
            maximumZ=Mathf.Max(maximumZ,rb.position.z);Assert.That(float.IsNaN(rb.position.x),Is.False);Assert.That(rb.position.y,Is.InRange(-.6f,1f),"Real gravity body may settle vertically, but cannot fall through the arena");yield return null;
        }
        Assert.That(maximumZ,Is.GreaterThan(-8),"AI must progress around the south industrial hall, not oscillate forever.");
        yield return new ExitPlayMode();
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
