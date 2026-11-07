using System.IO;
using NUnit.Framework;
using RoboRougelike.R4_1;
using UnityEngine;

public sealed class R4ColliderMeshFactoryTests
{
    [Test] public void TrustedArchCreatesThreeMeshesAndInvalidInputFailsClosed()
    {
        var json=File.ReadAllText("Tools/R4Blocks/generated/R4BlockColliderMeshes.json");
        Assert.That(R4ColliderMeshFactory.TryCreate(json,"R4B_Arch",out var meshes,out var error),Is.True,error);
        try
        {
            Assert.That(meshes.Length,Is.EqualTo(3));
            Assert.That(meshes[0].vertexCount,Is.GreaterThan(3));
            Assert.That(meshes[0].triangles.Length%3,Is.Zero);
            Assert.That(R4ColliderMeshFactory.TryCreate(json+" ","R4B_Arch",out _,out _),Is.False);
            Assert.That(R4ColliderMeshFactory.TryCreate(json,"missing",out _,out _),Is.False);
        }
        finally {foreach(var mesh in meshes)if(mesh!=null)Object.DestroyImmediate(mesh);}
    }
}
