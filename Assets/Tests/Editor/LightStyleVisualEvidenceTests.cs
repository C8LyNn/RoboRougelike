using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class LightStyleVisualEvidenceTests
{
    R1RunController run;
    string output;
    [UnitySetUp] public IEnumerator Setup()
    {
        output = Environment.GetEnvironmentVariable("RRL_LIGHT_STYLE_QA");
        if (string.IsNullOrEmpty(output)) Assert.Ignore("Set RRL_LIGHT_STYLE_QA for opt-in image capture.");
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("A real graphics device is required for image evidence.");
        Directory.CreateDirectory(output);
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        output = Environment.GetEnvironmentVariable("RRL_LIGHT_STYLE_QA");
        Assert.That(output, Is.Not.Null.And.Not.Empty);
        float deadline = Time.realtimeSinceStartup + 45;
        while ((R1RunController.Instance?.Progress == null || LightStylePrototype.Instance == null) && Time.realtimeSinceStartup < deadline) yield return null;
        run = R1RunController.Instance;
        Assert.That(run?.Progress, Is.Not.Null);
        Assert.That(LightStylePrototype.Instance, Is.Not.Null);
    }
    [UnityTearDown] public IEnumerator Teardown()
    {
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
    IEnumerator WaitForTransition()
    {
        float deadline = Time.realtimeSinceStartup + 60;
        while (run.Transitioning && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(run.Transitioning, Is.False, "Transition timed out.");
        yield return null;
    }
    void CapturePair(string name, int width, int height)
    {
        var camera = Camera.main; var filter = LightStylePrototype.Instance;
        Assert.That(camera, Is.Not.Null);
        var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
        var target = new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
        var image = new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            target.Create(); camera.targetTexture = target;
            byte[] baseline = null;
            foreach (var state in new[] { "OFF", "ON", "ZERO" })
            {
                filter.SetEnabled(state != "OFF"); filter.SetStrength(state == "ZERO" ? 0 : .6f);
                Assert.That(filter.Status, Is.Null);
                camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
                var png = image.EncodeToPNG();
                File.WriteAllBytes(Path.Combine(output, name + "-" + state + ".png"), png);
                if (state == "OFF") baseline = png;
                if (state == "ZERO") CollectionAssert.AreEqual(baseline, png, "0% differs from OFF in this frozen camera capture.");
            }
            filter.SetEnabled(false);
        }
        finally { camera.targetTexture = oldTarget; RenderTexture.active = oldActive; target.Release(); Object.Destroy(target); Object.Destroy(image); }
    }
    [UnityTest] public IEnumerator GarageAndArenaSameCameraPairs()
    {
        Assert.That(run.OpenSandboxGarage(), Is.True);
        yield return WaitForTransition();
        Time.timeScale = 0;
        CapturePair("garaz",1920,1080);
        Time.timeScale = 1;
        run.RestartRun();
        float deadline = Time.realtimeSinceStartup + 45;
        while ((R1RunController.Instance == null || R1RunController.Instance == run || R1RunController.Instance.Progress == null) && Time.realtimeSinceStartup < deadline) yield return null;
        run = R1RunController.Instance;
        Assert.That(run?.Progress, Is.Not.Null);
        Assert.That(run.StartRun(), Is.True);
        Assert.That(run.ChooseRoute(0), Is.True);
        yield return WaitForTransition();
        Assert.That(run.Progress.State, Is.EqualTo(R1RunState.Combat));
        Time.timeScale = 0;
        CapturePair("arena",1920,1080);
        // Fire through the actual mounted-weapon system; do not synthesize visual effects.
        Assert.That(run.Encounter.Enemies.Count, Is.GreaterThan(0));
        Time.timeScale = 1;
        var assembly = run.Player.GetComponent<R3VehicleAssembly>();
        assembly.AimGuns(run.Encounter.Enemies[0].transform.position);
        assembly.FireGuns();
        yield return new WaitForFixedUpdate();
        Time.timeScale = 0;
        CapturePair("walka-stan",1920,1080);
        Time.timeScale = 1;
    }
}
