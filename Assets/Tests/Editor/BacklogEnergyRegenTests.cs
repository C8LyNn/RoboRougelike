using System;
using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R1;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogEnergyRegenTests
{
    [Test]
    public void MountedRegeneratorReservesEnergyAndIncreasesRecovery()
    {
        Assert.That(Enum.TryParse("EnergyRegenerator",out R3PartKind kind),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Award(500);float maximum=model.Energy.Maximum;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,kind,false)==null);
        Assert.That(model.Buy(cell,kind),Is.True);model.RefreshEnergyLayout();
        Assert.That(model.Energy.Maximum,Is.EqualTo(maximum-25));
        Assert.That(model.Energy.TrySpend(model.Energy.Current),Is.True);
        model.Energy.AdvanceWithMotion(1,false);
        Assert.That(model.Energy.Current,Is.EqualTo(15).Within(.001));
        model.Energy.AdvanceWithMotion(1,true);
        Assert.That(model.Energy.Current,Is.EqualTo(75).Within(.001));
    }
    [Test]
    public void MultipleDevicesStackAndDestroyedDevicesStopContributing()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        for(int i=0;i<2;i++)
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.PlacementError(c,R3PartKind.EnergyRegenerator,false)==null);
            Assert.That(model.Buy(cell,R3PartKind.EnergyRegenerator),Is.True);
        }
        model.RefreshEnergyLayout();Assert.That(model.Energy.Maximum,Is.EqualTo(450));
        Assert.That(model.Energy.TrySpend(model.Energy.Current),Is.True);model.Energy.Advance(1);
        Assert.That(model.Energy.Current,Is.EqualTo(20).Within(.001));
        model.Parts.First(p=>p.Kind==R3PartKind.EnergyRegenerator).Health=0;model.RefreshEnergyLayout();
        Assert.That(model.Energy.Maximum,Is.EqualTo(475));model.Energy.Advance(1);
        Assert.That(model.Energy.Current,Is.EqualTo(35).Within(.001));
    }
}

public sealed class BacklogEnergyRegenIntegrationTests
{
    [UnitySetUp] public IEnumerator Setup()
    {EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}

    [UnityTest] public IEnumerator DestroyingRealRegeneratorRemovesItsRateAndReservation()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.EnergyRegenerator,false)==null);
        Assert.That(model.Buy(cell,R3PartKind.EnergyRegenerator),Is.True);
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        var bank=vehicle.GetComponent<VehicleEnergyBank>();var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(475));
        Assert.That(bank.TrySpend(bank.Current),Is.True);model.Energy.Advance(1);
        Assert.That(bank.Current,Is.EqualTo(15).Within(.001f));
        var device=assembly.Parts.Single(p=>p.Kind==R3PartKind.EnergyRegenerator);
        device.Damageable.ApplyDamage(100000,device.transform.position,Vector3.up);bank.Refresh();
        Assert.That(bank.Maximum,Is.EqualTo(500));model.Energy.Advance(1);
        Assert.That(bank.Current,Is.EqualTo(25).Within(.001f));
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
