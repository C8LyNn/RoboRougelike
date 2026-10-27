using System.Collections;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;

public class R5PhysicsTests
{
    [UnityTest] public IEnumerator TurningDoesNotInflateCollisionEnvelope()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity"); yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);while(run.Transitioning)yield return null;
        var controller=run.Player.Controller;var box=controller.GetComponent<BoxCollider>();
        var refresh=controller.GetType().GetMethod("RefreshCollisionEnvelope",BindingFlags.NonPublic|BindingFlags.Instance);
        controller.transform.rotation=Quaternion.identity;Physics.SyncTransforms();refresh.Invoke(controller,new object[]{true});
        Vector3 original=box.size;
        controller.transform.rotation=Quaternion.Euler(0,37,0);Physics.SyncTransforms();refresh.Invoke(controller,new object[]{true});
        Assert.That(Vector3.Distance(original,box.size),Is.LessThan(0.001f),"Turning must not grow the solid hull envelope.");
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator ExistingOverlapCannotTeleportHullInOneCorrection()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);while(run.Transitioning)yield return null;
        var controller=run.Player.Controller;var body=controller.GetComponent<Rigidbody>();
        var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=body.position+Vector3.right;
        obstacle.transform.localScale=new Vector3(3,20,3);Physics.SyncTransforms();var before=body.position;
        controller.GetType().GetMethod("ResolveExistingPenetration",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(controller,null);
        Assert.That(Vector3.Distance(before,body.position),Is.LessThanOrEqualTo(0.081f),"Recovery is bounded to 4m/s, never a multi-metre pop.");
        Assert.That(controller.GetComponent<BoxCollider>().enabled,Is.True);Assert.That(controller.GetComponent<BoxCollider>().isTrigger,Is.True);
        Assert.That(controller.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger),Is.True,"R6 keeps solid contacts on actual parts.");
        yield return new ExitPlayMode();
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
