using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using RoboRougelike.R1;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class LightStylePerformanceEvidenceTests
{
    [UnityTearDown] public IEnumerator Teardown()
    {
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
    [UnityTest] public IEnumerator OffscreenRenderAndReadbackAt1080And4K()
    {
        var output=Environment.GetEnvironmentVariable("RRL_LIGHT_STYLE_QA");
        if (string.IsNullOrEmpty(output)) Assert.Ignore("Set RRL_LIGHT_STYLE_QA for opt-in performance capture.");
        if (SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null || !SystemInfo.supportsAsyncGPUReadback)
            Assert.Ignore("A graphics device with async GPU readback is required.");
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        output=Environment.GetEnvironmentVariable("RRL_LIGHT_STYLE_QA");
        float deadline=Time.realtimeSinceStartup+45;
        while((R1RunController.Instance?.Progress==null || LightStylePrototype.Instance==null) && Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        deadline=Time.realtimeSinceStartup+45;
        while(run.Transitioning && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);yield return null;
        var camera=Camera.main;var filter=LightStylePrototype.Instance;
        Assert.That(camera,Is.Not.Null);Assert.That(filter,Is.Not.Null);
        Time.timeScale=0;
        var oldTarget=camera.targetTexture;
        var csv=new StringBuilder("width,height,mode,sample,render_and_readback_ms\n");
        try
        {
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(3840,2160)})
            {
                var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);
                try
                {
                    target.Create();camera.targetTexture=target;
                    // Paired alternating samples with a warm-up, same frozen camera.
                    for(int i=-2;i<8;i++) foreach(bool enabled in new[]{false,true})
                    {
                        filter.SetStrength(.6f);filter.SetEnabled(enabled);
                        var watch=Stopwatch.StartNew();camera.Render();
                        var request=AsyncGPUReadback.Request(target);request.WaitForCompletion();watch.Stop();
                        Assert.That(request.hasError,Is.False,"GPU readback failed.");
                        if(i>=0)csv.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4:F4}\n",size.x,size.y,enabled?"ON":"OFF",i,watch.Elapsed.TotalMilliseconds);
                    }
                }
                finally {target.Release();Object.Destroy(target);}
            }
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"render-readback-samples.csv"),csv.ToString());
            File.WriteAllText(Path.Combine(output,"render-readback-environment.txt"),$"Unity {Application.unityVersion}\nGPU: {SystemInfo.graphicsDeviceName}\nAPI: {SystemInfo.graphicsDeviceType}\nMode: Editor batch, camera.Render plus synchronous GPU readback; not gameplay FPS or isolated GPU time.\n");
        }
        finally {filter.SetEnabled(false);camera.targetTexture=oldTarget;Time.timeScale=1;}
    }
}
