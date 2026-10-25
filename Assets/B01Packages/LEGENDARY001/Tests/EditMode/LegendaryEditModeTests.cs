using System;
using System.Collections.Generic;
using NUnit.Framework;
using RoboRougelike.B01.Legendary;
namespace RoboRougelike.B01.Legendary.Tests
{
    // Real Unity Editor tests, require Unity 6000.3.14f1 licensed runner.
    public class LegendaryEditModeTests
    {
        sealed class Host : IAuthoritativeR3Port
        {
            public string RobotId => "editor-test";
            public readonly List<PartObservation> Live=new List<PartObservation>();
            public readonly List<PartObservation> Stash=new List<PartObservation>();
            public IReadOnlyList<PartObservation> Mounted => Live;
            public IReadOnlyList<PartObservation> Inventory => Stash;
            public bool HasCertifiedSocket(string kind,string token,LegendaryPlacement placement) =>
                kind=="WeaponBase"&&token=="w";
            public bool CanMountExisting(string id,LegendaryPlacement p,out string e)
            {e=Stash.Exists(x=>x.InstanceId==id)?"OK":"MISSING";return e=="OK";}
            public bool MountExisting(string id,LegendaryPlacement p,out string e)
            {if(!CanMountExisting(id,p,out e))return false;var part=Stash.Find(x=>x.InstanceId==id);Stash.Remove(part);Live.Add(part);return true;}
            public bool CanUnmountSafely(string id,out string e){e="UNAVAILABLE";return false;}
            public bool UnmountExisting(string id,out string e){e="UNAVAILABLE";return false;}
            public bool RepairExisting(string id,out string e){e="UNAVAILABLE";return false;}
            public bool ReceiveSalvage(PartObservation p,out string e)
            {Stash.Add(p);e="OK";return true;}
            public bool TransferInventoryAtomically(IAuthoritativeR3Port to,string id,out string e)
            {e="UNAVAILABLE";return false;}
        }
        static PartObservation NewGun()=>new PartObservation(Guid.NewGuid().ToString("N"),"Gun","Railgun",1);
        [Test] public void FirstEquipAndDuplicateRefusal() {
            var h=new Host();var service=new LegendaryUnityService(new[]{h},LegendaryCatalog.CreatePilotDefinitions());
            var a=NewGun();var b=NewGun();string e;
            Assert.IsTrue(service.TryGrant(h.RobotId,"a",a,"LEG-WPN-001",out e),e);
            Assert.IsTrue(service.TryGrant(h.RobotId,"b",b,"LEG-WPN-001",out e),e);
            Assert.IsTrue(service.TryEquip(h.RobotId,a.InstanceId,new LegendaryPlacement(0,0,0,"w"),out e),e);
            Assert.IsFalse(service.TryEquip(h.RobotId,b.InstanceId,new LegendaryPlacement(0,0,0,"w"),out e));
            Assert.AreEqual("LEGEND_ALREADY_EQUIPPED",e);
            Assert.AreEqual(1,h.Stash.Count);
        }
        [Test] public void MobilityHubWithoutAdapterIsRejected() {
            var h=new Host();var s=new LegendaryUnityService(new[]{h},LegendaryCatalog.CreatePilotDefinitions());
            var p=new PartObservation(Guid.NewGuid().ToString("N"),"HoverEngine",null,1);string e;
            Assert.IsTrue(s.TryGrant(h.RobotId,"hover",p,"LEG-MOV-001",out e),e);
            Assert.IsFalse(s.TryEquip(h.RobotId,p.InstanceId,new LegendaryPlacement(0,0,0,"structural"),out e));
            Assert.AreEqual("MOUNT_ADAPTER_PENDING_OR_SOCKET_MISMATCH",e);
            Assert.AreEqual(1,h.Stash.Count);
        }
        [Test] public void SaveRestorePreservesInstance() {
            var h=new Host();var s=new LegendaryUnityService(new[]{h},LegendaryCatalog.CreatePilotDefinitions());
            var p=NewGun();string e,json;
            Assert.IsTrue(s.TryGrant(h.RobotId,"evt",p,"LEG-WPN-001",out e),e);
            Assert.IsTrue(s.TrySaveMetadata(out json,out e),e);
            var loaded=new LegendaryUnityService(new[]{h},LegendaryCatalog.CreatePilotDefinitions());
            Assert.IsTrue(loaded.TryRestoreMetadata(json,out e),e);
            Assert.IsTrue(loaded.TryGetDefinition(p.InstanceId,out var definition));
            Assert.AreEqual("LEG-WPN-001",definition.Id);
        }
    }
}
