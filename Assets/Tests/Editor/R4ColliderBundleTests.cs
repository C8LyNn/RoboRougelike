using System.IO;
using NUnit.Framework;
using RoboRougelike.R4_1;
using UnityEngine;

public sealed class R4ColliderBundleTests
{
    [Test] public void ExactSourceParsesAndInvalidDataFailsClosed()
    {
        var json=File.ReadAllText("Tools/R4Blocks/generated/R4BlockColliderMeshes.json");
        Assert.That(R4ColliderBundle.IsTrustedSource(json),Is.True);
        Assert.That(R4ColliderBundle.IsTrustedSource(json+" "),Is.False);
        var data=JsonUtility.FromJson<R4ColliderBundle>(json);
        Assert.That(data,Is.Not.Null);Assert.That(data.Validate(out var reason),Is.True,reason);
        var piece=data.shapes[0].pieces[0];piece.indices[0]=int.MaxValue;
        Assert.That(data.Validate(out _),Is.False,"Out-of-range triangle indices must fail closed.");
    }
}
