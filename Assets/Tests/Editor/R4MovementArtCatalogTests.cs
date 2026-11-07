using System;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4MovementArtCatalogTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}
    [UnityTest] public IEnumerator AllElevenModelsLoadAndUnavailablePartsFallBack()
    {
        var type=Type.GetType("RoboRougelike.R4_1.R4MovementArtCatalog, Assembly-CSharp");
        Assert.That(type,Is.Not.Null,"Movement art needs a Player-readable reference catalog.");
        var catalog=Resources.Load("R4Art/MovementCatalog",type);Assert.That(catalog,Is.Not.Null);
        var lookup=type.GetMethod("ModelFor",new[]{typeof(R3PartKind)});Assert.That(lookup,Is.Not.Null);
        foreach(var kind in new[]{R3PartKind.Wheel,R3PartKind.CrawlerTrack,R3PartKind.HoverEngine,R3PartKind.OmniDrive,
            R3PartKind.Suspension,R3PartKind.Booster,R3PartKind.JumpDrive,R3PartKind.Blink,R3PartKind.Gyroscope,
            R3PartKind.MagneticGrapple,R3PartKind.IndependentSteering})
        {
            var model=(GameObject)lookup.Invoke(catalog,new object[]{kind});
            Assert.That(model,Is.Not.Null,kind.ToString());Assert.That(model.name,Is.EqualTo("R4M_"+kind));
        }
        Assert.That(lookup.Invoke(catalog,new object[]{R3PartKind.Battery}),Is.Null);
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
