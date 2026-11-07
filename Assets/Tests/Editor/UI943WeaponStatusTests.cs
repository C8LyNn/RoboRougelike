using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;

public sealed class UI943WeaponStatusTests
{
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
