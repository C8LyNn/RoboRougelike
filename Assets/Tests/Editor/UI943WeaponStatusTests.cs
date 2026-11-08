using System.Collections;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R3;
using RoboRougelike.WeaponIntegration;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;

public sealed class UI943WeaponStatusTests
{
    [TestCase(10,0f,0f,10f,"10/10 • gotowa")]
    [TestCase(2,0f,10.5f,10f,"2/10 • 0,5 s")]
    [TestCase(0,0f,0f,10f,"0/10 • ładowanie")]
    [TestCase(0,11f,0f,10f,"0/10 • ładowanie")]
    public void RocketStatusReflectsAutomaticReload(int ammo,float reload,float ready,float now,string expected)
    {
        var method=typeof(R1RunHUD).GetMethod("RocketWeaponStatus",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);string actual=(string)method.Invoke(null,new object[]{ammo,reload,ready,now});
        Assert.That(actual.Replace('.',','),Is.EqualTo(expected));
    }
    [TestCase(3,false,false,"niedostępne")]
    [TestCase(40,false,false,"niedostępne")]
    [TestCase(3,true,true,"ładowanie")]
    [TestCase(40,true,false,"pełny")]
    [TestCase(3,true,false,"T przeładuj")]
    public void ManualReloadHintRespectsDeviceAvailability(int ammo,bool available,bool reloading,string expected)
    {
        var method=typeof(R1RunHUD).GetMethod("ManualReloadAction",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((string)method.Invoke(null,new object[]{ammo,40,reloading?11f:0f,10f,available}),Is.EqualTo(expected));
    }
    [TestCase(0,1)] [TestCase(1,1)] [TestCase(9,1)] [TestCase(10,2)] [TestCase(180,20)]
    public void DefensePagesAreBoundedAndAllEntriesReachable(int count,int expectedPages)
    {
        var flags=BindingFlags.Static|BindingFlags.NonPublic;
        var pageCount=typeof(R1RunHUD).GetMethod("DefensePageCount",flags);
        var start=typeof(R1RunHUD).GetMethod("DefensePageStart",flags);
        Assert.That(pageCount,Is.Not.Null);Assert.That(start,Is.Not.Null);
        int pages=(int)pageCount.Invoke(null,new object[]{count});Assert.That(pages,Is.EqualTo(expectedPages));
        var seen=new System.Collections.Generic.HashSet<int>();
        for(int p=0;p<pages;p++)
        {
            int first=(int)start.Invoke(null,new object[]{count,p});
            for(int i=first;i<System.Math.Min(first+9,count);i++)Assert.That(seen.Add(i),Is.True);
        }
        Assert.That(seen.Count,Is.EqualTo(count));
        Assert.That((int)start.Invoke(null,new object[]{count,999}),Is.EqualTo((pages-1)*9));
    }
    [Test] public void DefensePagingUsesPWithoutConsumingWeaponOrMouseInputsAndClampsAfterLoss()
    {
        var go=new GameObject("Defense HUD input test");
        try
        {
            var hud=go.AddComponent<R1RunHUD>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var update=typeof(R1RunHUD).GetMethod("UpdateDefensePaging",flags);
            var page=typeof(R1RunHUD).GetField("defenseStatusPage",flags);
            Assert.That(update,Is.Not.Null);Assert.That(page,Is.Not.Null);
            var next=new Event{type=EventType.KeyDown,keyCode=KeyCode.P};update.Invoke(hud,new object[]{next,10});
            Assert.That((int)page.GetValue(hud),Is.EqualTo(1));Assert.That(next.type,Is.EqualTo(EventType.Used));
            foreach(var input in new[]{new Event{type=EventType.KeyDown,keyCode=KeyCode.Tab},
                new Event{type=EventType.MouseDown,button=0},new Event{type=EventType.MouseDown,button=1}})
            {var before=input.type;update.Invoke(hud,new object[]{input,10});Assert.That(input.type,Is.EqualTo(before));Assert.That((int)page.GetValue(hud),Is.EqualTo(1));}
            update.Invoke(hud,new object[]{new Event{type=EventType.Layout},1});Assert.That((int)page.GetValue(hud),Is.Zero);
            update.Invoke(hud,new object[]{new Event{type=EventType.KeyDown,keyCode=KeyCode.P},10});
            update.Invoke(hud,new object[]{new Event{type=EventType.KeyDown,keyCode=KeyCode.P},10});Assert.That((int)page.GetValue(hud),Is.Zero);
        }
        finally{Object.DestroyImmediate(go);}
    }
    [UnityTest] public IEnumerator DefenseRowsUseActualMountedDevicesInOrderAcrossPages()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();
        var paletteObject=new GameObject("Defense page test palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var design=R3VehicleCatalog.Create(R3VehicleKind.Standard);
        for(int i=0;i<5;i++)
        {
            design.Parts.Add(new R3PartState(new Vector3Int(i-2,2,-2),R3PartKind.DirectionalShield){DefenseCharge=20+i});
            design.Parts.Add(new R3PartState(new Vector3Int(i-2,2,-1),R3PartKind.PointDefense){Ammo=10+i});
        }
        var rig=R3VehicleAssembly.Build(new R3VehicleModel(design),new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        try
        {
            var shields=rig.GetComponentsInChildren<RRLShieldBinding>();var defenses=rig.GetComponentsInChildren<RRLPointDefenseBinding>();
            Assert.That(shields.Length,Is.EqualTo(5));Assert.That(defenses.Length,Is.EqualTo(5));
            var method=typeof(R1RunHUD).GetMethod("DefensePageRows",BindingFlags.Static|BindingFlags.NonPublic);Assert.That(method,Is.Not.Null);
            string[] Page(int p)=>(string[])method.Invoke(null,new object[]{shields,defenses,p});
            var first=Page(0);var second=Page(1);Assert.That(first.Length,Is.EqualTo(9));Assert.That(second.Length,Is.EqualTo(1));
            var rows=first.Concat(second).ToArray();
            for(int i=0;i<5;i++)Assert.That(rows[i],Is.EqualTo($"DEF-001 • {shields[i].Generator.Charge:0}/100 • "+(shields[i].Generator.Operational?"ON":"OFF")));
            for(int i=0;i<5;i++)Assert.That(rows[i+5],Is.EqualTo($"DEF-002 • {defenses[i].State.Ammo}/40 • "+(defenses[i].Controller.IsOperational?"ON":"OFF")+" • T przeładuj"));
            defenses[4].State.Ammo=40;Assert.That(Page(1)[0],Does.Contain("40/40").And.Contain("pełny"));
            defenses[4].State.Ammo=3;Assert.That(Page(1)[0],Does.Contain("3/40").And.Contain("T przeładuj"));
            defenses[4].State.ReloadReadyAt=Time.time+10;Assert.That(Page(1)[0],Does.Contain("ładowanie"));
            var remaining=defenses.Take(3).ToArray();
            var clamped=(string[])method.Invoke(null,new object[]{shields,remaining,1});Assert.That(clamped.Length,Is.EqualTo(8));
        }
        finally{Object.Destroy(rig.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);}
        yield return new ExitPlayMode();
    }
    [Test] public void PagingCyclesAndClampsAfterWeaponsAreLost()
    {
        var go=new UnityEngine.GameObject("HUD test");
        try
        {
            var hud=go.AddComponent<R1RunHUD>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var advance=typeof(R1RunHUD).GetMethod("AdvanceWeaponStatusPage",flags);
            var page=typeof(R1RunHUD).GetField("weaponStatusPage",flags);
            Assert.That(advance,Is.Not.Null);Assert.That(page,Is.Not.Null);
            for(int expected=1;expected<=6;expected++)
            {
                advance.Invoke(hud,new object[]{12});
                Assert.That((int)page.GetValue(hud),Is.EqualTo(expected%6));
            }
            page.SetValue(hud,5);advance.Invoke(hud,new object[]{1});
            Assert.That((int)page.GetValue(hud),Is.Zero,"Po utracie broni nie może pozostać pusta strona.");
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [TestCase(0,1)] [TestCase(1,1)] [TestCase(2,1)] [TestCase(4,2)] [TestCase(12,6)]
    public void EveryMountedWeaponCanBeReachedByPaging(int weapons,int expectedPages)
    {
        var flags=BindingFlags.Static|BindingFlags.NonPublic;
        var pagesMethod=typeof(R1RunHUD).GetMethod("WeaponStatusPageCount",flags);
        var startMethod=typeof(R1RunHUD).GetMethod("WeaponStatusPageStart",flags);
        Assert.That(pagesMethod,Is.Not.Null,"HUD musi udostępniać stan większej liczby dział.");
        Assert.That(startMethod,Is.Not.Null);
        int pages=(int)pagesMethod.Invoke(null,new object[]{weapons});
        Assert.That(pages,Is.EqualTo(expectedPages));
        var seen=new System.Collections.Generic.HashSet<int>();
        for(int page=0;page<pages;page++)
        {
            int start=(int)startMethod.Invoke(null,new object[]{weapons,page});
            for(int index=start;index<System.Math.Min(start+2,weapons);index++)
                Assert.That(seen.Add(index),Is.True,"Egzemplarz nie powinien znikać ani występować dwukrotnie.");
        }
        Assert.That(seen.Count,Is.EqualTo(weapons));
        Assert.That((int)startMethod.Invoke(null,new object[]{weapons,99}),Is.EqualTo((pages-1)*2),"Po utracie broni strona musi pozostać poprawna.");
    }
}
