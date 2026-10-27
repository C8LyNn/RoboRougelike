using System;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.MapEditor;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;

public sealed class MapEditorDraftTests
{
    [Test] public void RuntimeMapDraftExistsForInGameEditing()
    {
        var type=typeof(RoboRougelike.R3.R3VehicleModel).Assembly.GetType("RoboRougelike.MapEditor.MapDraft");
        Assert.That(type,Is.Not.Null,"Map editing requires a real shared data model");
        Assert.That(Activator.CreateInstance(type),Is.Not.Null);
    }
    [Test] public void BuildRotateRemoveUndoRedoAndUniqueMarkers()
    {
        var m=new MapDraft();Assert.That(m.Items,Is.Empty);
        Assert.That(m.Place(MapItemKind.Ground,0,0,0),Is.True);
        Assert.That(m.Place(MapItemKind.Spawn,0,0,1),Is.True);
        Assert.That(m.Place(MapItemKind.Ground,1,0,0),Is.True);
        Assert.That(m.Place(MapItemKind.Ramp,1,0,3),Is.True);
        Assert.That(m.Place(MapItemKind.Spawn,2,0,2),Is.True);
        Assert.That(m.Items.Count(p=>p.kind==MapItemKind.Spawn),Is.EqualTo(1));
        Assert.That(m.Undo(),Is.True);Assert.That(m.Spawn.x,Is.EqualTo(0));
        Assert.That(m.Redo(),Is.True);Assert.That(m.Spawn.x,Is.EqualTo(2));
        Assert.That(m.Remove(1,0),Is.True);Assert.That(m.Items.Any(p=>p.kind==MapItemKind.Ramp),Is.False);
        Assert.That(m.Items.Any(p=>p.kind==MapItemKind.Ground&&p.x==1),Is.True);
        Assert.That(m.Undo(),Is.True);Assert.That(m.Items.Single(p=>p.kind==MapItemKind.Ramp).rotation,Is.EqualTo(3));
        Assert.That(m.Place(MapItemKind.Wall,3,0,0),Is.True);Assert.That(m.CanRedo,Is.False);
    }
    [Test] public void JsonLoadIsTransactionalAndTestsRequireSpawnFinishAndGround()
    {
        var m=new MapDraft();Assert.That(m.TestError(),Is.Not.Null);
        m.Place(MapItemKind.Ground,0,0,0);m.Place(MapItemKind.Spawn,0,0,0);m.Place(MapItemKind.Finish,1,0,0);
        Assert.That(m.TestError(),Is.Null);var json=m.ToJson();var clone=new MapDraft();
        Assert.That(clone.TryLoad(json,out var error),Is.True,error);Assert.That(clone.Items.Count,Is.EqualTo(3));
        Assert.That(clone.TryLoad("{\"version\":1,\"items\":[{\"kind\":99}]}",out error),Is.False);
        Assert.That(clone.Items.Count,Is.EqualTo(3));Assert.That(clone.Undo(),Is.True);Assert.That(clone.Items,Is.Empty);
        Assert.That(m.Place(MapItemKind.Ramp,51,0,0),Is.False);
    }
    [Test] public void LocalFileRoundTripAndNameValidation()
    {
        var directory=Path.Combine(Path.GetTempPath(),"rrl-map-test-"+Guid.NewGuid().ToString("N"));
        try
        {
            Assert.That(MapStorage.TryPath(directory,"../escape",out _),Is.False);
            Assert.That(MapStorage.TryPath(directory,"Tor 1",out var file),Is.True);
            var m=new MapDraft();m.Place(MapItemKind.Ground,0,0,0);MapStorage.Save(file,m);
            m.Place(MapItemKind.Wall,0,0,2);MapStorage.Save(file,m);
            var loaded=new MapDraft();Assert.That(MapStorage.Load(file,loaded,out var error),Is.True,error);
            Assert.That(loaded.Items.Count,Is.EqualTo(2));Assert.That(loaded.Items.Single(p=>p.kind==MapItemKind.Wall).rotation,Is.EqualTo(2));
            File.WriteAllText(file,"{bad");Assert.That(MapStorage.Load(file,loaded,out error),Is.False);Assert.That(loaded.Items.Count,Is.EqualTo(2));
        }
        finally {if(Directory.Exists(directory)){foreach(var f in Directory.GetFiles(directory))File.Delete(f);Directory.Delete(directory);}}
    }
    [UnityTest] public IEnumerator ActualGeometrySupportsRealR6WheelDrivenVehicle()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var paletteObject=new GameObject("Map test palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var root=new GameObject("Map test world");var world=root.AddComponent<MapWorld>();
        var draft=new MapDraft();for(int z=-5;z<=5;z++)draft.Place(MapItemKind.Ground,0,z,0);
        draft.Place(MapItemKind.Spawn,0,-3,0);draft.Place(MapItemKind.Ramp,0,0,0);draft.Place(MapItemKind.Finish,0,3,0);
        draft.Place(MapItemKind.Wall,1,0,0);draft.Place(MapItemKind.Obstacle,-1,0,0);world.Build(draft,palette);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,.7f,-12),Quaternion.identity);
        try
        {
            Physics.SyncTransforms();
            Assert.That(Physics.Raycast(new Vector3(0,5,0),Vector3.down,out var hit,10),Is.True);
            Assert.That(hit.collider.GetComponentInParent<MapWorld>(),Is.EqualTo(world));
            Assert.That(hit.point.y,Is.EqualTo(.5f).Within(.1f),"Ramp must be a sloped physical mesh");
            Assert.That(world.Layout.Find("Finish").GetComponent<BoxCollider>().isTrigger,Is.True);
            var body=vehicle.GetComponent<Rigidbody>();var drive=vehicle.GetComponent<R6VehicleDrive>();
            Assert.That(drive,Is.Not.Null);vehicle.Controller.enabled=false;
            int grounded=0;float maximumZ=body.position.z;
            for(int i=0;i<500;i++)
            {
                float previous=Time.fixedTime;while(Time.fixedTime<=previous)yield return null;
                drive.Step(1,0,10,4,90);
                if(drive.GroundedWheels>0)grounded++;maximumZ=Mathf.Max(maximumZ,body.position.z);
                if(i%100==0)Debug.Log($"[MapEditor physics] step={i} position={body.position} velocity={body.linearVelocity} grounded={drive.GroundedWheels} rotation={body.rotation.eulerAngles}");
            }
            Assert.That(grounded,Is.GreaterThan(0),"Real wheels must contact edited terrain");
            Assert.That(maximumZ,Is.GreaterThan(2),"Real wheel forces must carry the vehicle across the edited ramp");
        }
        finally
        {
            UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(root);
            for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=null;
            for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=null;
            UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
        }
        yield return new ExitPlayMode();
    }
}
