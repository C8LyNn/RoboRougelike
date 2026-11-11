using System.Collections;
using NUnit.Framework;
using RoboRougelike.R1;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

public class LightStylePrototypeTests
{
    [UnitySetUp]
    public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        float deadline = Time.realtimeSinceStartup + 30;
        while ((R1RunController.Instance == null || LightStylePrototype.Instance == null || Camera.main == null) && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(LightStylePrototype.Instance, Is.Not.Null, "Prototyp nie został podłączony do właściwej sceny.");
        Assert.That(Camera.main, Is.Not.Null);
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }

    [UnityTest]
    public IEnumerator StartsOffAtZeroAndCanStillBeEnabledExplicitly()
    {
        var filter=LightStylePrototype.Instance;
        Assert.That(filter.Enabled,Is.False);
        Assert.That(filter.Strength,Is.Zero,"Początkowa siła filtra musi wynosić 0%.");
        var data=Camera.main.GetUniversalAdditionalCameraData();
        bool original=data.renderPostProcessing;int originalMask=data.volumeLayerMask.value;
        filter.SetEnabled(true);
        Assert.That(data.renderPostProcessing,Is.EqualTo(original));
        Assert.That(data.volumeLayerMask.value,Is.EqualTo(originalMask));
        filter.SetStrength(.6f);
        Assert.That(data.renderPostProcessing,Is.True,"Ręczne włączenie filtra nadal musi działać.");
        filter.SetEnabled(false);
        Assert.That(data.renderPostProcessing,Is.EqualTo(original));
        yield return null;
    }

    [UnityTest]
    public IEnumerator ProfileContainsOnlyColorAndWhiteBalance()
    {
        var profile = Resources.Load<VolumeProfile>("VisualPrototype/LekkaStylizacja");
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile.components.Count, Is.EqualTo(2));
        Assert.That(profile.TryGet<ColorAdjustments>(out var color), Is.True);
        Assert.That(profile.TryGet<WhiteBalance>(out var white), Is.True);
        Assert.That(profile.TryGet<Bloom>(out _), Is.False);
        Assert.That(color.saturation.overrideState && color.contrast.overrideState, Is.True);
        Assert.That(color.postExposure.overrideState, Is.False);
        Assert.That(white.temperature.overrideState, Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator DisabledAndZeroRestorePreviouslyDisabledPostProcessing()
    {
        var filter = LightStylePrototype.Instance;
        var data = Camera.main.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = false;
        data.volumeLayerMask = 5;
        filter.SetStrength(.6f); filter.SetEnabled(true);
        Assert.That(filter.Status, Is.Null);
        Assert.That(data.renderPostProcessing, Is.True);
        Assert.That(data.volumeLayerMask.value, Is.Not.EqualTo(5));
        filter.SetStrength(0);
        Assert.That(data.renderPostProcessing, Is.False);
        Assert.That(data.volumeLayerMask.value, Is.EqualTo(5));
        filter.SetStrength(.6f);
        Assert.That(data.renderPostProcessing, Is.True);
        filter.SetEnabled(false);
        Assert.That(data.renderPostProcessing, Is.False);
        Assert.That(data.volumeLayerMask.value, Is.EqualTo(5));
        yield return null;
    }

    [UnityTest]
    public IEnumerator ExistingEffectsAndVolumeMaskRemainWhenAlreadyEnabled()
    {
        var filter = LightStylePrototype.Instance;
        var data = Camera.main.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        data.volumeLayerMask = 5;
        var existingObject = new GameObject("Existing test volume") { layer = 2 };
        var existingVolume = existingObject.AddComponent<Volume>();
        var existingProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = existingProfile.Add<Bloom>(true); bloom.intensity.Override(.37f);
        existingVolume.sharedProfile = existingProfile; existingVolume.isGlobal = true;
        filter.SetStrength(.5f); filter.SetEnabled(true);
        Assert.That(filter.Status, Is.Null);
        Assert.That(data.renderPostProcessing, Is.True);
        Assert.That(data.volumeLayerMask.value & 5, Is.EqualTo(5));
        filter.SetEnabled(false);
        Assert.That(data.renderPostProcessing, Is.True);
        Assert.That(data.volumeLayerMask.value, Is.EqualTo(5));
        // Existing volumes are not edited by the separate asset.
        Assert.That(existingVolume.sharedProfile, Is.SameAs(existingProfile));
        Assert.That(bloom.intensity.value, Is.EqualTo(.37f));
        Object.Destroy(existingObject); Object.Destroy(existingProfile);
        yield return null;
    }
}
