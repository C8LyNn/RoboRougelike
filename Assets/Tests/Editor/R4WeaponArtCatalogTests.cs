using System;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R4;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4WeaponArtCatalogTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();
    }
    [UnityTest] public IEnumerator CatalogLoadsEightRealModelsAndLeavesAutomaticAsFallback()
    {
        var type=Type.GetType("RoboRougelike.R4_1.R4WeaponArtCatalog, Assembly-CSharp");
        Assert.That(type,Is.Not.Null,"A runtime catalog is required for the Player to load the models.");
        var catalog=Resources.Load("R4Art/WeaponCatalog",type);Assert.That(catalog,Is.Not.Null);
        var lookup=type.GetMethod("ModelFor",new[]{typeof(R4WeaponKind)});Assert.That(lookup,Is.Not.Null);
        foreach(var weapon in new[]{R4WeaponKind.Basic,R4WeaponKind.Heavy,R4WeaponKind.Shotgun,R4WeaponKind.Railgun,
            R4WeaponKind.BeamCutter,R4WeaponKind.RocketLauncher,R4WeaponKind.RotarySaw,R4WeaponKind.EmpEmitter})
        {
            var model=(GameObject)lookup.Invoke(catalog,new object[]{weapon});
            Assert.That(model,Is.Not.Null,weapon.ToString());
            Assert.That(model.name,Is.EqualTo("R4W_"+weapon),weapon.ToString());
        }
        Assert.That(lookup.Invoke(catalog,new object[]{R4WeaponKind.Automatic}),Is.Null,"No Automatic model was supplied.");
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
