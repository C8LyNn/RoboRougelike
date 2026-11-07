using System;
using System.IO;
using NUnit.Framework;
using RoboRougelike.R4_1;

public sealed class R4ColliderMeshCacheTests
{
    [Test] public void CacheSharesMeshesAndRejectsUseAfterDispose()
    {
        var json=File.ReadAllText("Tools/R4Blocks/generated/R4BlockColliderMeshes.json");
        Assert.That(R4ColliderMeshCache.TryOpen(json,out var cache,out var error),Is.True,error);
        try
        {
            Assert.That(cache.TryGet("R4B_Arch",out var first,out error),Is.True,error);
            Assert.That(first.Count,Is.EqualTo(3));
            Assert.That(cache.TryGet("R4B_Arch",out var second,out error),Is.True,error);
            Assert.That(ReferenceEquals(first,second),Is.True);
            Assert.That(cache.TryGet("missing",out _,out _),Is.False);
        }
        finally {cache.Dispose();}
        cache.Dispose();Assert.That(cache.TryGet("R4B_Arch",out _,out _),Is.False);
        Assert.That(R4ColliderMeshCache.TryOpen(json+" ",out _,out _),Is.False);
    }
}
