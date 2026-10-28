// B01-25 PASS14 optional original-code-targeting native Unity fixture.
// AUTHORED/NOT_COMPILED/NOT_RUN. Codex owns adoption and execution.
// Copy immutable LD02_ARENA.json into disposable Application.persistentDataPath/Maps
// as B01_25_LD02_ARENA_P14.json before running in approved licensed Unity 6000.3.14f1.
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using RoboRougelike.MapEditor;
using RoboRougelike.R3;

public sealed class B0125LD02ArenaNativeAcceptance
{
    [UnityTest] public IEnumerator OriginalMapWorld_CreatesFourSolidPerimeterWallColliders()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        string file=Path.Combine(Application.persistentDataPath,"Maps","B01_25_LD02_ARENA_P14.json");
        Assert.That(File.Exists(file),Is.True,"Copy only into a disposable sandbox profile");
        var draft=new MapDraft();
        Assert.That(draft.TryLoad(File.ReadAllText(file),out var error),Is.True,error);
        Assert.That(draft.TestError(),Is.Null);
        Assert.That(draft.GlobalFloor,Is.False);
        Assert.That(draft.Items.Count(o=>o.kind==MapItemKind.Ground),Is.EqualTo(120));
        Assert.That(draft.Items.Count(o=>o.kind==MapItemKind.Wall),Is.EqualTo(40));

        var paletteGo=new GameObject("B0125 Arena Palette");
        var palette=paletteGo.AddComponent<R3VisualPalette>();
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        Assert.That(shader,Is.Not.Null);
        var material=new Material(shader);
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var root=new GameObject("B0125 Arena World");
        var world=root.AddComponent<MapWorld>();
        try
        {
            world.Build(draft,palette);
            Physics.SyncTransforms();
            AssertWall(new Vector3(4,1.5f,20),Vector3.left,"west");
            AssertWall(new Vector3(40,1.5f,20),Vector3.right,"east");
            AssertWall(new Vector3(24,1.5f,4),Vector3.back,"south");
            AssertWall(new Vector3(24,1.5f,32),Vector3.forward,"north");
        }
        finally
        {
            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(paletteGo);
            UnityEngine.Object.Destroy(material);
        }
        yield return new ExitPlayMode();
    }
    static void AssertWall(Vector3 origin,Vector3 direction,string side)
    {
        Assert.That(Physics.Raycast(origin,direction,out var hit,8f,~0,QueryTriggerInteraction.Ignore),Is.True,side);
        var tagged=hit.collider.GetComponentInParent<MapItemReference>();
        Assert.That(tagged,Is.Not.Null,side);
        Assert.That(tagged.Item.kind,Is.EqualTo(MapItemKind.Wall),side+" actual generated solid wall");
        Assert.That(hit.collider.isTrigger,Is.False,side+" is not a trigger");
    }
}
