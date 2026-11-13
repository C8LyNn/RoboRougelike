using System;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R3;
using UnityEngine;

public sealed class BacklogChargeStateTests
{
    object state;
    Type type;
    [SetUp] public void Setup()
    {
        type=Type.GetType("RoboRougelike.R3.VehicleChargeState, Assembly-CSharp");
        Assert.That(type,Is.Not.Null,"The shared vehicle charge state must exist");
        state=Activator.CreateInstance(type);
    }
    object Call(string name,params object[] args)=>type.GetMethod(name,BindingFlags.Instance|BindingFlags.Public).Invoke(state,args);
    [Test] public void CapacityLossPreservesExcessButDoesNotRegenerateUntilBelowTheNewLimit()
    {
        Call("EnsurePool","emp",3);
        Call("SetCapacityBonus",2);
        Call("RefillAll");
        Assert.That(Call("Current","emp"),Is.EqualTo(5));
        Call("SetCapacityBonus",0);
        Assert.That(Call("Current","emp"),Is.EqualTo(5));
        Call("Advance",5f,1f);Assert.That(Call("Current","emp"),Is.EqualTo(5));
        Assert.That(Call("TrySpend","emp",2),Is.EqualTo(true));
        Call("Advance",1f,1f);Assert.That(Call("Current","emp"),Is.EqualTo(3));
        Assert.That(Call("TrySpend","emp",1),Is.EqualTo(true));
        Call("Advance",1f,1f);Assert.That(Call("Current","emp"),Is.EqualTo(3));
    }
    [Test] public void PoolsAreIndependentAndReregistrationDoesNotGiveFreeCharges()
    {
        Call("EnsurePool","target",3);Call("EnsurePool","area",3);
        Assert.That(Call("TrySpend","target",2),Is.EqualTo(true));
        Call("EnsurePool","target",3);
        Assert.That(Call("Current","target"),Is.EqualTo(1));Assert.That(Call("Current","area"),Is.EqualTo(3));
        Assert.That(Call("TrySpend","target",2),Is.EqualTo(false));Assert.That(Call("Current","target"),Is.EqualTo(1));
    }
    [Test] public void SafeZoneRefillNeverDestroysOverflow()
    {
        Call("EnsurePool","one",2);Call("SetCapacityBonus",2);Call("RefillAll");Call("SetCapacityBonus",0);
        Call("EnsurePool","two",2);Call("TrySpend","two",2);Call("RefillAll");
        Assert.That(Call("Current","one"),Is.EqualTo(4));Assert.That(Call("Current","two"),Is.EqualTo(2));
    }
    [Test] public void VehicleCheckpointRoundTripKeepsChargesAndFractionalProgress()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var property=typeof(R3VehicleModel).GetProperty("Charges");Assert.That(property,Is.Not.Null);
        var charges=(VehicleChargeState)property.GetValue(model);charges.EnsurePool("emp",3);charges.TrySpend("emp",2);charges.Advance(.5f,1);
        var saved=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(10)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,10,out var loaded,out var error),Is.True,error);
        var restored=(VehicleChargeState)property.GetValue(loaded);Assert.That(restored.Current("emp"),Is.EqualTo(1));
        restored.Advance(.5f,1);Assert.That(restored.Current("emp"),Is.EqualTo(2));
        var copy=(VehicleChargeState)property.GetValue(model.DeepCopy());copy.TrySpend("emp",1);
        Assert.That(charges.Current("emp"),Is.EqualTo(1),"A copied model must not share its charge state");
    }
    [Test] public void CorruptChargeCheckpointIsRejectedWithoutLosingPriorState()
    {
        var charges=(VehicleChargeState)state;charges.EnsurePool("emp",3);charges.TrySpend("emp",1);
        var bad=charges.Snapshot();bad[0].progress=float.NaN;
        Assert.That(charges.Restore(bad),Is.False);Assert.That(charges.Current("emp"),Is.EqualTo(2));
    }
}
