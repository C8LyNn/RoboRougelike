using System;
using System.Numerics;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R3;

public sealed class DeviceFireScheduleTests
{
    static object Create()
    {
        var type=typeof(R3FlyingDrone).Assembly.GetType("RoboRougelike.R3.DeviceFireSchedule");
        Assert.That(type,Is.Not.Null,"The schedule must retain shots that could not be emitted within the current work budget.");
        return Activator.CreateInstance(type);
    }
    static void Observe(object schedule,float now,float interval,bool canFire)
        =>schedule.GetType().GetMethod("Observe").Invoke(schedule,new object[]{now,interval,canFire});
    static BigInteger Pending(object schedule)=>(BigInteger)schedule.GetType().GetProperty("PendingShots").GetValue(schedule);
    static int Take(object schedule,int max)=>(int)schedule.GetType().GetMethod("Take").Invoke(schedule,new object[]{max});

    [Test] public void MultipleDueShotsCanBeDrainedAcrossBudgetsWithoutLoss()
    {
        var schedule=Create();Observe(schedule,0,.125f,true);Observe(schedule,.5f,.125f,true);
        Assert.That(Pending(schedule),Is.EqualTo(new BigInteger(5)));
        Assert.That(Take(schedule,2),Is.EqualTo(2));Assert.That(Pending(schedule),Is.EqualTo(new BigInteger(3)));
        Assert.That(Take(schedule,2),Is.EqualTo(2));Assert.That(Take(schedule,2),Is.EqualTo(1));
        Assert.That(Pending(schedule),Is.EqualTo(BigInteger.Zero));
    }
    [Test] public void InterruptedAimRetainsAlreadyDueShotsWithoutBankingNewOnes()
    {
        var schedule=Create();Observe(schedule,0,.125f,true);Observe(schedule,.5f,.125f,true);
        Observe(schedule,1,.125f,false);Observe(schedule,10,.125f,false);
        Assert.That(Pending(schedule),Is.EqualTo(new BigInteger(5)));
        Observe(schedule,11,.125f,true);Assert.That(Pending(schedule),Is.EqualTo(new BigInteger(6)));
    }
    [Test] public void SubnormalIntervalIsCountedExactlyWithoutLoopingOrOverflow()
    {
        var schedule=Create();Observe(schedule,0,float.Epsilon,true);Observe(schedule,1,float.Epsilon,true);
        var due=(BigInteger.One<<149)+BigInteger.One;
        Assert.That(Pending(schedule),Is.EqualTo(due));Assert.That(Take(schedule,3),Is.EqualTo(3));
        Assert.That(Pending(schedule),Is.EqualTo(due-3));
    }
    [Test] public void ZeroIntervalProducesAtMostOnePerDistinctTimeWithoutInfiniteLoop()
    {
        var schedule=Create();Observe(schedule,0,0,true);Observe(schedule,0,0,true);
        Observe(schedule,.125f,0,true);Assert.That(Pending(schedule),Is.EqualTo(new BigInteger(2)));
    }
    [Test] public void IntervalChangePreservesExistingDeadlineThenUsesNewInterval()
    {
        var schedule=Create();Observe(schedule,0,.5f,true);Observe(schedule,.5f,.125f,true);
        Observe(schedule,1,.125f,true);Assert.That(Pending(schedule),Is.EqualTo(new BigInteger(6)));
    }
}
