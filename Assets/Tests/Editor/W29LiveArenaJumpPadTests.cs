using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R5;
using B01.MapJumpPad001;

public sealed class W29LiveArenaJumpPadTests
{
    [UnityTest]
    public IEnumerator SalvageYardActuallyLaunchesA10KgRigidbodyOnTheRealBuiltLevel()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var host=new GameObject("W29 live map integration test");
        var arena=host.AddComponent<R5Arena>();
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        Assert.That(shader,Is.Not.Null);
        var mat=new Material(shader);
        arena.Initialize(mat);
        arena.Build(R5ArenaKind.SalvageYard);
        var pads=arena.Layout.GetComponentsInChildren<MapJumpPad>();
        Assert.That(pads.Length,Is.EqualTo(1),"real R5 salvage map must contain exactly one pad");
        var pad=pads[0];
        Assert.That(pad.FootprintMeters,Is.EqualTo(Vector2.one));
        var trigger=pad.GetComponent<BoxCollider>();
        Assert.That(trigger,Is.Not.Null);Assert.That(trigger.isTrigger,Is.True);
        var robot=GameObject.CreatePrimitive(PrimitiveType.Cube);
        robot.name="W29 actual 10kg Rigidbody game-world witness";
        robot.transform.position=pad.transform.position+Vector3.up*1.4f;
        var body=robot.AddComponent<Rigidbody>();body.mass=10f;
        body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        Physics.SyncTransforms();
        float best=float.NegativeInfinity;
        for(int i=0;i<135;i++)
        {float previousFixed=Time.fixedTime;while(Time.fixedTime<=previousFixed)yield return null;
            best=Mathf.Max(best,body.linearVelocity.y);}
        Assert.That(best,Is.GreaterThan(5f),"actual PhysX jump pad must launch heavy body");
        Object.Destroy(robot);Object.Destroy(host);Object.Destroy(mat);
    }

    [UnityTest]
    public IEnumerator OtherRealArenaDoesNotAccidentallyHaveSalvageJumpPad()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var host=new GameObject("W29 other arena no-pad test");
        var arena=host.AddComponent<R5Arena>();
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        Assert.That(shader,Is.Not.Null);
        var mat=new Material(shader);
        arena.Initialize(mat);
        arena.Build(R5ArenaKind.Wasteland);
        Assert.That(arena.Layout.GetComponentsInChildren<MapJumpPad>().Length,Is.Zero);
        Object.Destroy(host);Object.Destroy(mat);
    }

    [UnityTearDown] public IEnumerator Cleanup()
    {if(Application.isPlaying)yield return new ExitPlayMode();}
}
