using System.IO;
using NUnit.Framework;
using RoboRougelike.R4_1;
using UnityEngine;

public sealed class R4ColliderStageTests
{
    [Test] public void DiagnoseDisabledChildColliderRigidbodyAssociation()
    {
        var owner=new GameObject("disabled collider diagnostic");
        var child=new GameObject("child");child.transform.SetParent(owner.transform,false);
        var body=owner.AddComponent<Rigidbody>();var collider=child.AddComponent<BoxCollider>();
        try
        {
            Assert.That(collider.attachedRigidbody,Is.SameAs(body),"Enabled baseline");
            collider.enabled=false;
            TestContext.WriteLine("Disabled attached Rigidbody: "+(collider.attachedRigidbody==null?"null":"present"));
            Assert.That(child.GetComponentInParent<Rigidbody>(),Is.SameAs(body),"Transform parent remains correct");
            collider.enabled=true;
            Assert.That(collider.attachedRigidbody,Is.SameAs(body),"Re-enabled association");
        }
        finally{Object.DestroyImmediate(owner);}
    }
    [Test] public void StagedArchCollidersRemainDisabledAndKeepOriginalCollider()
    {
        var json=File.ReadAllText("Tools/R4Blocks/generated/R4BlockColliderMeshes.json");
        Assert.That(R4ColliderMeshCache.TryOpen(json,out var cache,out var error),Is.True,error);
        var owner=new GameObject("R4 collider stage test");owner.transform.localScale=Vector3.one*.52f;
        var body=owner.AddComponent<Rigidbody>();var original=owner.AddComponent<BoxCollider>();
        R4ColliderStage stage=null;
        try
        {
            Assert.That(R4ColliderStage.TryCreate(owner,cache,"R4B_Arch",out stage,out error),Is.True,error);
            Assert.That(stage.Colliders.Count,Is.EqualTo(3));Assert.That(original.enabled,Is.True);
            foreach(var item in stage.Colliders)
            {
                Assert.That(item.enabled,Is.False);Assert.That(item.convex,Is.True);
                Assert.That(item.sharedMesh,Is.Not.Null);Assert.That(item.transform.parent,Is.SameAs(owner.transform));
                Assert.That(item.attachedRigidbody,Is.SameAs(body));
            }
            stage.Dispose();Assert.That(stage.IsDisposed,Is.True);Assert.That(original.enabled,Is.True);
        }
        finally {stage?.Dispose();cache.Dispose();Object.DestroyImmediate(owner);}
    }
}
