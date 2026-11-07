using System.Collections;
using System.Linq;
using NUnit.Framework;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ProjectileShellVisualTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}
    [UnityTest] public IEnumerator ShellUsesElongatedVisualButKeepsOnePhysicalCollider()
    {
        var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.transform.localScale=Vector3.one*.2f;
        var body=shell.AddComponent<Rigidbody>();body.useGravity=false;body.linearVelocity=Vector3.forward*10;
        var physical=shell.GetComponent<SphereCollider>();var source=shell.GetComponent<Renderer>();
        var projectile=shell.AddComponent<P17PhysicalShell>();projectile.Configure(null,20,5,Vector3.forward,null);
        var visual=shell.transform.Find("P17 shell visual");Assert.That(visual,Is.Not.Null);
        Assert.That(source.enabled,Is.False);Assert.That(physical.enabled,Is.True);
        Assert.That(shell.GetComponentsInChildren<Collider>(true).Count(c=>c.enabled),Is.EqualTo(1));
        Assert.That(visual.localScale.y,Is.GreaterThan(visual.localScale.x*2));
        yield return null;
        Assert.That(Vector3.Dot(visual.up.normalized,Vector3.forward),Is.GreaterThan(.99f));
        Object.Destroy(shell);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
