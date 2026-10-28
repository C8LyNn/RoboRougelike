using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;

public sealed class WeaponTriggerSafetyTests
{
    [UnityTest] public IEnumerator LosingCameraReleasesRealBeamTrigger()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        var old=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Gun);
        Assert.That(run.GarageBuyWeapon(new Vector3Int(1,2,0),R4WeaponKind.BeamCutter),Is.True);
        Assert.That(run.GarageRemove(old.Cell),Is.True);Assert.That(run.EnterTestRange(),Is.True);
        while(run.Transitioning)yield return null;yield return null;
        var controller=run.Player.Controller;var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        var beam=assembly.Parts.Single(p=>p.Kind==R3PartKind.Gun).GetComponent<RRLBeamBinding>();
        var field=typeof(RRLBeamBinding).GetField("held",BindingFlags.NonPublic|BindingFlags.Instance);
        bool groupB=assembly.Model.WeaponGroupB==R4WeaponKind.BeamCutter;
        assembly.SetGroupTriggers(!groupB,groupB);Assert.That((bool)field.GetValue(beam),Is.True);
        var camera=Camera.main;Assert.That(camera,Is.Not.Null);camera.gameObject.SetActive(false);
        try
        {
            typeof(P12PipelineSafeTankController).GetField("aimCamera",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(controller,null);
            typeof(P12PipelineSafeTankController).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(controller,null);
            Assert.That((bool)field.GetValue(beam),Is.False,"no camera must release a real held beam immediately");
        }
        finally {if(camera!=null)camera.gameObject.SetActive(true);assembly.SetGroupTriggers(false,false);}
        yield return new ExitPlayMode();
    }
}
