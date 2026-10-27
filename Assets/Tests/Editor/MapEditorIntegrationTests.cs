using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.MapEditor;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;

public sealed class MapEditorIntegrationTests
{
    [UnityTest] public IEnumerator MenuEditorGarageVehicleTestAndReturnPreserveMap()
    {
        Assert.That(Application.isBatchMode,Is.True,"Run without touching the user's cursor");
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+30;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(R1RunController.Instance?.Progress,Is.Not.Null,"Real main menu initialized");
        var run=R1RunController.Instance;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Ready));
        Assert.That(run.OpenMapEditor(),Is.True);Assert.That(run.MapEditing,Is.True);
        var model=run.VehicleModel;var draft=run.EditorDraft;
        for(int z=-3;z<=3;z++)draft.Place(MapItemKind.Ground,0,z,0);
        draft.Place(MapItemKind.Spawn,0,-2,0);draft.Place(MapItemKind.Ramp,0,0,0);draft.Place(MapItemKind.Finish,0,2,0);
        var expected=draft.ToJson();
        run.MapEditorGarage();Assert.That(run.MapEditing,Is.False);Assert.That(run.VehicleModel,Is.SameAs(model));
        Assert.That(run.ReturnToMapEditorFromGarage(),Is.True);Assert.That(run.EditorDraft.ToJson(),Is.EqualTo(expected));
        Assert.That(run.TestEditedMap(),Is.True);
        end=Time.realtimeSinceStartup+30;while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(run.Transitioning,Is.False,"Custom test drive transition");
        Assert.That(run.MapEditorTesting,Is.True);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.TestDrive));
        Assert.That(run.Player.GetComponent<R6VehicleDrive>(),Is.Not.Null);
        Assert.That(run.VehicleModel,Is.Not.SameAs(model));
        Assert.That(run.Player.transform.position.z,Is.EqualTo(-8).Within(.5f));
        var part=run.Player.GetComponentsInChildren<Collider>().First(c=>c.attachedRigidbody==run.Player.GetComponent<Rigidbody>());
        run.EditorWorld.MarkFinish(part);Assert.That(run.EditorWorld.Finished,Is.True);
        run.TogglePause();Assert.That(run.Paused,Is.True);
        Assert.That(run.ReturnFromEditedMapTest(),Is.True);
        end=Time.realtimeSinceStartup+30;while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(run.Transitioning,Is.False,"Return from custom test");
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(run.MapEditing,Is.True);
        Assert.That(run.VehicleModel,Is.SameAs(model));Assert.That(run.EditorDraft.ToJson(),Is.EqualTo(expected));
        yield return new ExitPlayMode();
    }
}
