using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;

public sealed class UnifiedRC2IntegrationTests
{
    R1RunController run;Keyboard keyboard;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);run.RequestedSeed=7;
    }
    bool Prepare()
    {
        var method=typeof(R1RunController).GetMethod("PrepareUnifiedRun",BindingFlags.Public|BindingFlags.Instance);
        Assert.That(method,Is.Not.Null,"The unified menu must prepare the canonical garage before choosing activities");
        return (bool)method.Invoke(run,null);
    }
    [UnityTest] public IEnumerator UnifiedEntryPreparesOneCanonicalActorBeforeAnyActivity()
    {
        Assert.That(run.SelectVehicle(R3VehicleKind.Heavy),Is.True);Assert.That(Prepare(),Is.True);yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(run.Player,Is.Not.Null);
        Assert.That(run.Player.GetComponent<R3VehicleAssembly>().Model,Is.SameAs(run.VehicleModel));
        Assert.That(run.VehicleModel.Scrap,Is.Zero);Assert.That(run.VehicleModel.Inventory,Is.Empty);
        Assert.That(run.Encounter.AliveCount,Is.Zero);Assert.That(run.GeneratedWorld,Is.Null);Assert.That(run.ReactorActive,Is.False);
        Assert.That(run.CanChooseGeneratedArena,Is.True);Assert.That(run.CanChooseReactorDefense,Is.True);
        Assert.That(Object.FindObjectsByType<R3VehicleAssembly>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator RepeatedPreparationDoesNotReplacePhysicalOwnership()
    {
        Assert.That(Prepare(),Is.True);var model=run.VehicleModel;var actor=run.Player;var ids=model.Parts.Select(p=>p.Id).ToArray();
        Assert.That(Prepare(),Is.False);yield return null;
        Assert.That(run.VehicleModel,Is.SameAs(model));Assert.That(run.Player,Is.SameAs(actor));
        Assert.That(model.Parts.Select(p=>p.Id).ToArray(),Is.EqualTo(ids));Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
    }
    [UnityTest] public IEnumerator OrdinaryEnterKeyUsesUnifiedPreparationInsteadOfSkippingGarage()
    {
        keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));
        yield return null;yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage),"Normal keyboard control must follow the same menu entry as the button");
        Assert.That(run.Player,Is.Not.Null);Assert.That(run.Encounter.AliveCount,Is.Zero);
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
