// B01-61 / PASS 12 / QA-ONLY, ORIGINALLY AUTHORED IN PASS 11.
// Source base: Codex PR #310 @ 231005c6d0351945a8ee6684f3856af118f0e94f
// Not compiled or executed in Unity by B01-61: native test status NOT_RUN.
// Install only in an authorized isolated copy under an appropriate Editor test assembly.
// Do not add to Codex's canonical branch/worktree without the Codex owner.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.MapEditor;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.Mechanics.MOV009;

public sealed class B0161CrossFeatureRegressionTests
{
    [Test]
    public void DeepCopyPreservesABAndSuspensionButDoesNotShareMutableState()
    {
        var original=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        original.SwapWeaponGroups(); // One Basic weapon assigned to B/PPM.
        original.SuspensionEnabled=true;
        original.SuspensionMode=ActiveSuspensionSupportMode.AssistOnly;
        original.SuspensionHeightOffset=.2f;
        original.Award(123);
        var gun=original.Parts.First(p=>p.Kind==R3PartKind.Gun);
        var gunId=gun.Id;
        var originalScrap=original.Scrap;

        var temporary=original.DeepCopy();
        Assert.That(temporary,Is.Not.SameAs(original));
        Assert.That(temporary.Parts.First(p=>p.Id==gunId),Is.Not.SameAs(gun));
        Assert.That(temporary.WeaponGroupA,Is.Null);
        Assert.That(temporary.WeaponGroupB,Is.EqualTo(R4WeaponKind.Basic));
        Assert.That(temporary.SuspensionMode,Is.EqualTo(ActiveSuspensionSupportMode.AssistOnly));
        Assert.That(temporary.SuspensionHeightOffset,Is.EqualTo(.2f).Within(.001f));
        Assert.That(temporary.Scrap,Is.EqualTo(originalScrap));

        // Negative: changes to the disposable test model must never mutate garage state.
        temporary.SuspensionMode=ActiveSuspensionSupportMode.FullSupport;
        temporary.SuspensionHeightOffset=-.1f;
        temporary.SwapWeaponGroups();
        temporary.Parts.First(p=>p.Id==gunId).Health=0;
        temporary.Award(37);
        Assert.That(original.SuspensionMode,Is.EqualTo(ActiveSuspensionSupportMode.AssistOnly));
        Assert.That(original.SuspensionHeightOffset,Is.EqualTo(.2f).Within(.001f));
        Assert.That(original.WeaponGroupA,Is.Null);
        Assert.That(original.WeaponGroupB,Is.EqualTo(R4WeaponKind.Basic));
        Assert.That(gun.Health,Is.GreaterThan(0));
        Assert.That(original.Scrap,Is.EqualTo(originalScrap));
    }

    [UnityTest]
    public IEnumerator EditorGarageMapTestReturnPreservesGroupsSuspensionIdsAndScrap()
    {
        // Explicitly avoid automating the owner's desktop.
        Assert.That(Application.isBatchMode,Is.True,"Authorized disposable batch Unity only");
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        var limit=Time.realtimeSinceStartup+30;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<limit)
            yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That(run.OpenMapEditor(),Is.True);
        run.MapEditorGarage();
        var garageModel=run.VehicleModel;
        Assert.That(garageModel,Is.Not.Null);
        Assert.That(run.GarageBuy(MOV009ModelTests.Cells(R3VehicleKind.Light)[0],R3PartKind.Suspension),Is.True);
        Assert.That(run.ConfigureSuspension(true,ActiveSuspensionSupportMode.AssistOnly,.2f),Is.True);
        garageModel.SwapWeaponGroups();
        Assert.That(garageModel.WeaponGroupA,Is.Null);
        Assert.That(garageModel.WeaponGroupB,Is.EqualTo(R4WeaponKind.Basic));
        var originalGun=garageModel.Parts.First(p=>p.Kind==R3PartKind.Gun);
        var gunId=originalGun.Id;
        var originalGunHealth=originalGun.Health;
        var scrap=garageModel.Scrap;
        var partIds=garageModel.Parts.Select(p=>p.Id).OrderBy(id=>id).ToArray();
        Assert.That(run.ReturnToMapEditorFromGarage(),Is.True);

        var draft=run.EditorDraft;
        for(int z=-3;z<=3;z++)Assert.That(draft.Place(MapItemKind.Ground,0,z,0),Is.True);
        Assert.That(draft.Place(MapItemKind.Spawn,0,-2,0),Is.True);
        Assert.That(draft.Place(MapItemKind.Ramp,0,0,0),Is.True);
        Assert.That(draft.Place(MapItemKind.Finish,0,2,0),Is.True);
        var mapBefore=draft.ToJson();
        Assert.That(run.TestEditedMap(),Is.True);
        yield return null;
        limit=Time.realtimeSinceStartup+30;
        while(run.Transitioning&&Time.realtimeSinceStartup<limit)yield return null;
        Assert.That(run.Transitioning,Is.False);
        Assert.That(run.MapEditorTesting,Is.True);
        Assert.That(run.VehicleModel,Is.Not.SameAs(garageModel));
        Assert.That(run.VehicleModel.WeaponGroupA,Is.Null);
        Assert.That(run.VehicleModel.WeaponGroupB,Is.EqualTo(R4WeaponKind.Basic));
        Assert.That(run.VehicleModel.SuspensionMode,Is.EqualTo(ActiveSuspensionSupportMode.AssistOnly));
        Assert.That(run.VehicleModel.SuspensionHeightOffset,Is.EqualTo(.2f).Within(.001f));

        // Negative: settings changed during test drive may affect that test only,
        // never the original garage build restored after returning to editor.
        Assert.That(run.ConfigureSuspension(true,ActiveSuspensionSupportMode.FullSupport,-.1f),Is.True);
        Assert.That(run.VehicleModel.SuspensionMode,Is.EqualTo(ActiveSuspensionSupportMode.FullSupport));

        // Cross-feature negative missed by separate model-only tests:
        // mutate BOTH firing assignment and owned-part health in the active map test.
        // Neither test-only change may leak into the original garage model.
        run.VehicleModel.SwapWeaponGroups();
        Assert.That(run.VehicleModel.WeaponGroupA,Is.EqualTo(R4WeaponKind.Basic));
        Assert.That(run.VehicleModel.WeaponGroupB,Is.Null);
        var temporaryGun=run.VehicleModel.Parts.First(p=>p.Id==gunId);
        Assert.That(temporaryGun,Is.Not.SameAs(originalGun));
        temporaryGun.Health=0;
        Assert.That(temporaryGun.Health,Is.Zero);
        Assert.That(originalGun.Health,Is.EqualTo(originalGunHealth));
        run.TogglePause();
        Assert.That(run.Paused,Is.True);
        Assert.That(run.ReturnFromEditedMapTest(),Is.True);
        yield return null;
        limit=Time.realtimeSinceStartup+30;
        while(run.Transitioning&&Time.realtimeSinceStartup<limit)yield return null;
        Assert.That(run.Transitioning,Is.False);
        Assert.That(run.MapEditing,Is.True);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        Assert.That(run.VehicleModel,Is.SameAs(garageModel));
        Assert.That(run.VehicleModel.WeaponGroupA,Is.Null);
        Assert.That(run.VehicleModel.WeaponGroupB,Is.EqualTo(R4WeaponKind.Basic));
        Assert.That(run.VehicleModel.Parts.First(p=>p.Id==gunId).Health,Is.EqualTo(originalGunHealth),
            "Test-only gun damage must not overwrite the original garage part state");
        Assert.That(run.VehicleModel.SuspensionMode,Is.EqualTo(ActiveSuspensionSupportMode.AssistOnly));
        Assert.That(run.VehicleModel.SuspensionHeightOffset,Is.EqualTo(.2f).Within(.001f));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.VehicleModel.Parts.Select(p=>p.Id).OrderBy(id=>id).ToArray(),Is.EqualTo(partIds));
        Assert.That(run.EditorDraft.ToJson(),Is.EqualTo(mapBefore));
        run.MapEditorGarage();
        Assert.That(run.Player.gameObject.activeInHierarchy,Is.True);
        Assert.That(run.Player.GetComponent<R3VehicleAssembly>(),Is.Not.Null);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if(Application.isPlaying)
        {
            Time.timeScale=1;
            yield return new ExitPlayMode();
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    }
}