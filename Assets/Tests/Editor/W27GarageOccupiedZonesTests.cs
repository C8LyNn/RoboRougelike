using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class W27GarageOccupiedZonesTests
{
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    [UnityTest] public IEnumerator OccupiedGridIsVisibleAndTracksActualGarageConstructionWithoutColliders()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.OpenSandboxGarage(),Is.True);
        var lines=run.Player.GetComponentsInChildren<LineRenderer>().Where(l=>l.name=="W27 occupied cell").ToArray();
        Assert.That(lines.Length,Is.EqualTo(20),"Light construction has 15 armor/core cells, 4 wheels and 1 gun.");
        Assert.That(lines.All(l=>l.GetComponent<Collider>()==null),Is.True,"The overlay cannot affect hit tests or physics.");
        Assert.That(lines.All(l=>l.positionCount>=16&&!l.useWorldSpace),Is.True);
        Assert.That(lines.All(l=>l.sharedMaterial.color.r>l.sharedMaterial.color.g),Is.True);
        Assert.That(lines.All(l=>l.sharedMaterial.GetColor("_BaseColor").a>=.9f&&l.startWidth>=.02f),Is.True,
            "Occupied-cell outlines must remain visible against both bright and dark vehicle surfaces.");
        Assert.That(lines.Any(l=>Vector3.Distance(l.transform.localPosition,new Vector3(.55f,0,1.1f))<.001f),Is.True,"The displayed cell uses the real 0.55m grid.");
        Assert.That(run.GarageBuy(new Vector3Int(1,1,1),R3PartKind.Armor),Is.True);
        lines=run.Player.GetComponentsInChildren<LineRenderer>().Where(l=>l.name=="W27 occupied cell").ToArray();
        Assert.That(lines.Length,Is.EqualTo(21),"The overlay must refresh after an actual successful purchase.");
        Assert.That(lines.Any(l=>Vector3.Distance(l.transform.localPosition,new Vector3(.55f,.55f,.55f))<.001f),Is.True);
        Assert.That(run.GarageBuy(new Vector3Int(1,1,2),R3PartKind.DirectionalShield,new Vector3Int(3,0,0)),Is.True);
        lines=run.Player.GetComponentsInChildren<LineRenderer>().Where(l=>l.name=="W27 occupied cell").ToArray();
        Assert.That(lines.Length,Is.EqualTo(22),"The rotated shield reserves exactly one cell.");
        Assert.That(lines.Any(l=>Vector3.Distance(l.transform.localPosition,new Vector3(.55f,.55f,1.1f))<.001f),Is.True,"The rotated shield's occupied cell is shown.");
        run.Garage.Close();
        Assert.That(run.Player.GetComponentsInChildren<LineRenderer>().Any(l=>l.name=="W27 occupied cell"),Is.False,"Closing the garage must immediately hide its overlay.");
    }
    [UnityTest] public IEnumerator WheelGhostDistinguishesItsProtrudingGeometryDuringRotation()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.OpenSandboxGarage(),Is.True);
        var garage=run.Garage;garage.SelectedPart=R3PartKind.Wheel;garage.Select(new Vector3Int(1,1,1));
        foreach(var turn in new[]{0,1})
        {
            if(turn!=0)garage.RotateSelected(0,turn);
            var visual=garage.Preview.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name=="Koło");
            Assert.That(visual,Is.Not.Null,"The ghost uses the actual wheel geometry.");
            var color=visual.sharedMaterial.color;
            Assert.That(color.b,Is.GreaterThan(color.r));Assert.That(color.b,Is.GreaterThan(color.g));
            Assert.That(visual.sharedMaterial.GetColor("_BaseColor").a,Is.GreaterThanOrEqualTo(.65f),
                "The protruding blue geometry must remain distinguishable over an invalid red ghost.");
            Assert.That(visual.sharedMaterial.renderQueue,Is.GreaterThan(garage.Preview.GetComponent<Renderer>().sharedMaterial.renderQueue),
                "The blue protruding geometry must render after the translucent placement ghost.");
            Assert.That(visual.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True,"Ghost geometry cannot affect physics.");
        }
    }
}
