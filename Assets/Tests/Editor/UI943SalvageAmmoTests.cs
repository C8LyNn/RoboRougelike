using System;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

public sealed class UI943SalvageAmmoTests
{
    static object Call(string name,params object[] args)
    {
        var method=typeof(R1RunHUD).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null,"Brakuje funkcji karty łupu: "+name);
        return method.Invoke(null,args);
    }

    [TestCase(R4WeaponKind.Shotgun,-1,"12/12 salw")]
    [TestCase(R4WeaponKind.Shotgun,5,"5/12 salw")]
    [TestCase(R4WeaponKind.Railgun,4,"4/12 nab.")]
    [TestCase(R4WeaponKind.Railgun,0,"0/12 nab.")]
    [TestCase(R4WeaponKind.RocketLauncher,-1,"10/10 rak.")]
    [TestCase(R4WeaponKind.RocketLauncher,0,"0/10 rak.")]
    public void RealWeaponMagazineAndUninitializedSentinelAreDisplayed(R4WeaponKind kind,int ammo,string expected)
    {
        var snapshot=new R3PartState(default,R3PartKind.Gun){Weapon=kind,Ammo=ammo};
        var loot=new R4SalvageItem(snapshot,.5f);
        Assert.That(Call("SalvageAmmoLabel",loot.ToPart()),Is.EqualTo(expected));
    }

    [Test] public void PointDefenseUsesItsOwnFortyRoundMagazine()
    {
        var state=new R3PartState(default,R3PartKind.PointDefense){Ammo=7};
        Assert.That(Call("SalvageAmmoLabel",state),Is.EqualTo("7/40 poc."));
        state.Ammo=-1;
        Assert.That(Call("SalvageAmmoLabel",state),Is.EqualTo("40/40 poc."));
    }

    [Test] public void NoAmmoIsShownForWeaponsAndPartsWithoutMagazines()
    {
        foreach(R4WeaponKind kind in Enum.GetValues(typeof(R4WeaponKind)))
        {
            if(kind==R4WeaponKind.Shotgun||kind==R4WeaponKind.Railgun||kind==R4WeaponKind.RocketLauncher)continue;
            var part=new R3PartState(default,R3PartKind.Gun){Weapon=kind,Ammo=0};
            Assert.That(Call("SalvageAmmoLabel",part),Is.Null,kind.ToString());
        }
        var wheel=new R3PartState(default,R3PartKind.Wheel){Weapon=R4WeaponKind.Shotgun,Ammo=0};
        Assert.That(Call("SalvageAmmoLabel",wheel),Is.Null,"Rodzaj części jest równie ważny jak pole Weapon.");
    }

    [Test] public void AmmoLabelDoesNotOverlapSelectionHpOrRepairOnAnyCard()
    {
        for(int i=0;i<6;i++)
        {
            float x=455+(i%2)*390,y=197+(i/2)*116;
            var label=(Rect)Call("SalvageAmmoRect",x,y);
            var card=new Rect(x,y,375,104);
            var select=new Rect(x+268,y+26,97,48);
            var conditionAndRepair=new Rect(x+10,y+64,255,37);
            Assert.That(card.Contains(label.min)&&card.Contains(label.max-Vector2.one),Is.True,"Poza kartą "+i);
            Assert.That(label.Overlaps(select),Is.False,"Nakryto przycisk "+i);
            Assert.That(label.Overlaps(conditionAndRepair),Is.False,"Nakryto HP/koszt "+i);
        }
    }
}
