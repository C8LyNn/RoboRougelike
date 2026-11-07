using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4EnemyWheelArtTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    [UnityTest] public IEnumerator SourceWheelArtMovesAndDetachesWithOriginalPhysicalWheel()
    {
        var root=new GameObject("enemy visual test");var body=root.AddComponent<Rigidbody>();body.useGravity=false;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var method=typeof(P12PipelineSafeEnemyTurret).GetMethod("MakeProfileBlock",BindingFlags.NonPublic|BindingFlags.Static);
        Assert.That(method,Is.Not.Null);
        var wheel=(P12PipelineSafeDamageable)method.Invoke(null,new object[]{root.transform,new Vector3Int(-2,0,1),material,true});
        var collider=wheel.GetComponent<Collider>();var originalRotation=wheel.transform.localRotation;
        var rotor=wheel.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4M_Wheel_Rotor");
        Assert.That(rotor,Is.Not.Null,"Every profiled enemy wheel should display the supplied model.");
        Assert.That(rotor.GetComponentsInChildren<Collider>(true),Is.Empty,"Visual art must not create physics colliders.");
        Assert.That(collider,Is.Not.Null);Assert.That(collider.enabled,Is.True);
        Assert.That(Vector3.Distance(rotor.position,wheel.transform.position),Is.LessThan(.005f),"Rotor center must match the existing physical wheel.");
        var oldWheel=wheel.GetComponentInChildren<P12EnemyWheelVisual>(true);
        Assert.That(oldWheel,Is.Not.Null,"The original visual remains as a fallback.");
        Assert.That(oldWheel.GetComponent<Renderer>().enabled,Is.False,"Do not draw both old and new wheel art.");
        var start=rotor.localRotation;body.linearVelocity=Vector3.forward*4;
        yield return null;yield return null;
        Assert.That(Quaternion.Angle(start,rotor.localRotation),Is.GreaterThan(.01f),"Visual rotor must roll with motion.");
        Assert.That(Quaternion.Angle(originalRotation,wheel.transform.localRotation),Is.LessThan(.01f),"Visual animation must not rotate the collider.");
        wheel.ApplyDamage(10000,wheel.transform.position,Vector3.forward);yield return null;
        Assert.That(wheel.IsDead,Is.True);Assert.That(wheel.transform.parent,Is.Null,"Existing wheel detachment remains in control.");
        Assert.That(rotor.IsChildOf(wheel.transform),Is.True,"Art must follow the detached debris.");
        Assert.That(wheel.GetComponent<Collider>(),Is.SameAs(collider));
        Object.Destroy(wheel.gameObject);Object.Destroy(root);Object.Destroy(material);
    }

    [UnityTearDown] public IEnumerator Cleanup()
    { if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();} }
}
