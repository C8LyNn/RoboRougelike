using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;
public class R6FeedbackTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
        run.StartRun();run.ChooseRoute(0);while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator HitsLeaveReadableDamageAndBoundedEffectsWithoutCollisionShapes()
    {
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor);
        var renderer=part.GetComponent<Renderer>();Color original=renderer.sharedMaterial.GetColor("_BaseColor");
        part.Damageable.ApplyDamage(30,part.transform.position,Vector3.forward);
        float until=Time.time+.2f;while(Time.time<until)yield return null;
        var props=new MaterialPropertyBlock();renderer.GetPropertyBlock(props);
        Assert.That(Vector4.Distance(original,props.GetColor("_BaseColor")),Is.GreaterThan(.05f),"Damage must remain visible after the hit flash.");
        Assert.That(part.Damageable.CurrentHp,Is.EqualTo(70).Within(.01f));
        for(int i=0;i<100;i++)part.Damageable.ApplyDamage(.001f,part.transform.position,Vector3.forward);
        var feedback=run.GetComponentInChildren<RoboRougelike.VFX001.VfxPoolManager>();Assert.That(feedback,Is.Not.Null);
        Assert.That(feedback.ActiveCount,Is.InRange(1,48));
        Assert.That(feedback.GetComponentsInChildren<Collider>(true),Is.Empty);
        until=Time.time+1;while(Time.time<until)yield return null;
        Assert.That(feedback.ActiveCount,Is.Zero,"Short lived effects must return to the bounded pool.");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
