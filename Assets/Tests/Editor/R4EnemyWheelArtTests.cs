using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.R4_1;
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
        var root=new GameObject("enemy visual test");root.transform.rotation=Quaternion.Euler(0,37,0);
        var body=root.AddComponent<Rigidbody>();body.useGravity=false;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var method=typeof(P12PipelineSafeEnemyTurret).GetMethod("MakeProfileBlock",BindingFlags.NonPublic|BindingFlags.Static);
        Assert.That(method,Is.Not.Null);
        var wheel=(P12PipelineSafeDamageable)method.Invoke(null,new object[]{root.transform,new Vector3Int(-2,0,1),material,true});
        var collider=wheel.GetComponent<Collider>();var originalRotation=wheel.transform.localRotation;
        var rotor=wheel.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4M_WheelM_Side_Offroad_Rotor");
        Assert.That(rotor,Is.Not.Null,"Every profiled enemy wheel should display the supplied model.");
        Assert.That(rotor.GetComponentsInChildren<Collider>(true),Is.Empty,"Visual art must not create physics colliders.");
        Assert.That(collider,Is.Not.Null);Assert.That(collider.enabled,Is.True);
        Assert.That(Vector3.Distance(rotor.position,wheel.transform.position),Is.LessThan(.005f),"Rotor center must match the existing physical wheel.");
        Physics.SyncTransforms();
        Assert.That(Mathf.Abs(Vector3.Dot(rotor.right,wheel.transform.up)),Is.GreaterThan(.99f),"The visible axle must align with the physical wheel axle.");
        var renderers=rotor.GetComponentsInChildren<Renderer>(true);Assert.That(renderers,Is.Not.Empty);
        var visibleBounds=renderers[0].bounds;
        foreach(var item in renderers.Skip(1))visibleBounds.Encapsulate(item.bounds);
        Debug.Log("R4_ENEMY_WHEEL_BOUNDS visual="+visibleBounds.size.ToString("F4")+" collider="+collider.bounds.size.ToString("F4"));
        Assert.That(visibleBounds.size.y,Is.EqualTo(.53f).Within(.04f),"Visual diameter must match the intended wheel diameter.");
        var oldWheel=wheel.GetComponentInChildren<P12EnemyWheelVisual>(true);
        Assert.That(oldWheel,Is.Not.Null,"The original visual remains as a fallback.");
        Assert.That(oldWheel.GetComponent<Renderer>().enabled,Is.False,"Do not draw both old and new wheel art.");
        var start=rotor.localRotation;body.linearVelocity=root.transform.forward*4;
        yield return null;yield return null;
        Assert.That(Quaternion.Angle(start,rotor.localRotation),Is.GreaterThan(.01f),"Visual rotor must roll with motion.");
        Assert.That(Quaternion.Angle(originalRotation,wheel.transform.localRotation),Is.LessThan(.01f),"Visual animation must not rotate the collider.");
        wheel.ApplyDamage(10000,wheel.transform.position,Vector3.forward);yield return null;
        Assert.That(wheel.IsDead,Is.True);Assert.That(wheel.transform.parent,Is.Null,"Existing wheel detachment remains in control.");
        Assert.That(rotor.IsChildOf(wheel.transform),Is.True,"Art must follow the detached debris.");
        Assert.That(wheel.GetComponent<Collider>(),Is.SameAs(collider));
        Object.Destroy(wheel.gameObject);Object.Destroy(root);Object.Destroy(material);
    }

    [UnityTest] public IEnumerator BothEnemyTiresRollForwardInWorldSpace()
    {
        var root=new GameObject("enemy two wheel direction");
        var body=root.AddComponent<Rigidbody>();body.useGravity=false;body.constraints=RigidbodyConstraints.FreezeRotation;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var make=typeof(P12PipelineSafeEnemyTurret).GetMethod("MakeProfileBlock",BindingFlags.NonPublic|BindingFlags.Static);
        var left=(P12PipelineSafeDamageable)make.Invoke(null,new object[]{root.transform,new Vector3Int(-2,0,1),material,true});
        var right=(P12PipelineSafeDamageable)make.Invoke(null,new object[]{root.transform,new Vector3Int(2,0,1),material,true});
        Transform Rotor(P12PipelineSafeDamageable wheel)=>wheel.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R4M_WheelM_Side_Offroad_Rotor");
        var l=Rotor(left);var r=Rotor(right);var axis=root.transform.right;
        Vector3 Mark(Transform rotor){var v=Vector3.ProjectOnPlane(rotor.TransformVector(Vector3.up),axis);return v.sqrMagnitude>.001f?v:Vector3.ProjectOnPlane(rotor.TransformVector(Vector3.forward),axis);}
        body.linearVelocity=root.transform.forward*2;var l0=Mark(l);var r0=Mark(r);yield return null;
        float ld=Vector3.Dot(Vector3.Cross(l0,Mark(l)),axis),rd=Vector3.Dot(Vector3.Cross(r0,Mark(r)),axis);
        Assert.That(ld,Is.GreaterThan(0),"The left enemy tire must roll forwards.");
        Assert.That(rd,Is.GreaterThan(0),"The right enemy tire must roll forwards.");
        Object.Destroy(root);Object.Destroy(material);
    }

    [UnityTest] public IEnumerator InvalidMountKeepsFallbackAndDuplicateAttachIsRejected()
    {
        var parent=new GameObject("visual parent");
        var wheel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);wheel.transform.SetParent(parent.transform,false);
        wheel.transform.localScale=new Vector3(.53f,.15f,.53f);
        var source=wheel.GetComponent<Renderer>();
        var old=GameObject.CreatePrimitive(PrimitiveType.Cube);old.transform.SetParent(wheel.transform,false);
        var fallback=old.GetComponent<Renderer>();
        Assert.That(R4EnemyWheelArt.TryAttach(wheel.transform,source,fallback,.265f),Is.False,"Unrotated mount must fail safely.");
        Assert.That(fallback.enabled,Is.True,"A failed attach must preserve the old appearance.");
        wheel.transform.localRotation=Quaternion.Euler(0,0,90);
        Assert.That(R4EnemyWheelArt.TryAttach(wheel.transform,source,fallback,.265f),Is.True);
        Assert.That(R4EnemyWheelArt.TryAttach(wheel.transform,source,fallback,.265f),Is.False,"A second attach cannot duplicate the model.");
        Assert.That(wheel.GetComponents<R4EnemyWheelArt>().Length,Is.EqualTo(1));
        Assert.That(wheel.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="R4M_WheelM_Side_Offroad_Rotor"),Is.EqualTo(1));
        Object.Destroy(parent);yield return null;
    }

    [UnityTearDown] public IEnumerator Cleanup()
    { if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();} }
}
