using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R6;
using RoboRougelike.Mechanics.StructuralScan;
using RoboRougelike.Mechanics.EMP;
using RogueMachine.P12PipelineSafe;

public class RRLW8ScanAdapterTests
{
    [Test] public void PartInReleasedBodyCannotRemainAnAttachedScanTargetUnderOldHierarchy()
    {
        var root=new GameObject("Actual owner");root.AddComponent<Rigidbody>();
        try
        {
            var part=new GameObject("Physical module");part.transform.SetParent(root.transform);
            var hp=part.AddComponent<P12PipelineSafeDamageable>();hp.Configure(100,false);
            var scan=part.AddComponent<COM001LiveScanPartAdapter>();scan.Bind(root.transform,hp,1,2,StructuralPartType.Armor);
            Assert.That(scan.IsAttached,Is.True);part.AddComponent<Rigidbody>();
            Assert.That(scan.IsAttached,Is.False,"Transform ancestry alone must not treat a released physical body as owned");
        }
        finally{Object.DestroyImmediate(root);}
    }
    [Test] public void PartScanReadsExistingEmpStateWithoutChangingItsHp()
    {
        var root=new GameObject("Actual owner");root.AddComponent<Rigidbody>();
        try
        {
            var part=new GameObject("Electronic module");part.transform.SetParent(root.transform);
            var hp=part.AddComponent<P12PipelineSafeDamageable>();hp.Configure(100,false);
            var receiver=part.AddComponent<EmpReceiver>();receiver.ConfigureOwnership(root.transform,1,2);
            var scan=part.AddComponent<COM001LiveScanPartAdapter>();scan.Bind(root.transform,hp,1,2,StructuralPartType.Weapon);
            Assert.That(scan.IsFunctional,Is.True);receiver.ApplyEmp(1);
            Assert.That(scan.IsFunctional,Is.False,"An EMP-disabled weapon must not be reported as functional");
            Assert.That(hp.CurrentHp,Is.EqualTo(100));Assert.That(scan.HealthFraction,Is.EqualTo(1));
            receiver.ClearEmp();Assert.That(scan.IsFunctional,Is.True);
        }
        finally{Object.DestroyImmediate(root);}
    }
}
