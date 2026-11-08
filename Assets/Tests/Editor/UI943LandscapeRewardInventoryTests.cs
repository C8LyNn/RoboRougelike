using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

public sealed class UI943LandscapeRewardInventoryTests
{
    static object Call(string name,params object[] args)
    {var method=typeof(R1RunHUD).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static);Assert.That(method,Is.Not.Null,name);return method.Invoke(null,args);}
    [TestCase(R3PartKind.Gun,R4WeaponKind.Shotgun,.5f,5,"Naprawa","5/12 salw")]
    [TestCase(R3PartKind.Gun,R4WeaponKind.Railgun,0f,0,"Odbudowa","0/12 nab.")]
    [TestCase(R3PartKind.Gun,R4WeaponKind.RocketLauncher,1f,-1,"niepotrzebna","10/10 rak.")]
    [TestCase(R3PartKind.PointDefense,R4WeaponKind.Basic,.5f,7,"Naprawa","7/40 poc.")]
    public void StatusShowsRealRepairQuoteAndApplicableAmmo(R3PartKind kind,R4WeaponKind weapon,float health,int ammo,string action,string ammoText)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var item=new R3PartState(default,kind,health){Weapon=weapon,Ammo=ammo};
        var actual=(string)Call("RewardInventoryStatusText",model,item);
        var expected=health>=1f?"Naprawa: niepotrzebna":action+": "+model.RepairCost(item)+" Z";
        Assert.That(actual,Does.Contain(expected));
        Assert.That(actual,Does.Contain(ammoText));
    }
    [Test] public void NoMagazineLabelForWheelOrUnmeteredWeapon()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        foreach(var item in new[]{new R3PartState(default,R3PartKind.Wheel,.5f){Ammo=0},new R3PartState(default,R3PartKind.Gun,.5f){Weapon=R4WeaponKind.BeamCutter,Ammo=0}})
        {
            var actual=(string)Call("RewardInventoryStatusText",model,item);
            Assert.That(actual,Does.Contain("Naprawa: "+model.RepairCost(item)+" Z"));
            Assert.That(actual,Does.Not.Contain("/"));
        }
    }
    [Test] public void ExistingNameHpAndSourceRemainInTheDetails()
    {
        var item=new R3PartState(default,R3PartKind.Gun,.5f){Weapon=R4WeaponKind.Railgun,Source="Zwiadowca 2 / uzbrojenie"};
        var text=(string)Call("RewardInventoryItemText",item);
        Assert.That(text,Does.Contain(R4WeaponDefinition.PartName(item)));
        Assert.That(text,Does.Contain("HP 50%"));
        Assert.That(text,Does.Contain(item.Source));
    }
    [Test] public void DetailAndStatusFitAllCardsWithoutCoveringTheUnchangedButton()
    {
        for(int i=0;i<5;i++)
        {
            float y=180+i*76;
            var card=new Rect(45,y,1180,68);
            var details=(Rect)Call("RewardInventoryDetailsRect",i);
            var status=(Rect)Call("RewardInventoryStatusRect",i);
            var button=new Rect(860,y+10,350,48);
            foreach(var rect in new[]{details,status,button})
                Assert.That(card.Contains(rect.min)&&card.Contains(rect.max-Vector2.one),Is.True,"Poza kartą "+i);
            Assert.That(details.Overlaps(status),Is.False,"Dane nie mogą nachodzić na koszt i amunicję.");
            Assert.That(details.Overlaps(button)||status.Overlaps(button),Is.False,"Przycisk pozostaje w obecnym miejscu i nie przykrywa danych.");
            Assert.That(details.height,Is.GreaterThanOrEqualTo(57));Assert.That(status.height,Is.GreaterThanOrEqualTo(50));
        }
    }
}
