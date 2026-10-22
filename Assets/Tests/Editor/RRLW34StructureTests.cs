using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

public class RRLW34StructureTests
{
    GameObject fixtures;
    R3VisualPalette palette;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;
        fixtures=new GameObject("W34 physical structure fixtures");
        palette=Object.FindFirstObjectByType<R3VisualPalette>();
    }
    static R3VehicleDesign Design()
    {
        var d=new R3VehicleDesign{Name="W34 structure",Speed=5,TurnSpeed=60,CoreHp=100};
        d.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        d.Parts.Add(new R3PartState(Vector3Int.up,R3PartKind.Gun));
        return d;
    }
    static R3PartState Bonus(Vector3Int anchor,Vector3Int rotation=default) =>
        new R3PartState(anchor,R3PartKind.BonusModule){RotationQuarterTurns=rotation,
            UpgradeId="physical-test",UpgradeEffect=R1UpgradeEffect.Damage,UpgradeAmount=.25f};
    R1VehicleState Build(R3VehicleDesign d)
    {
        var v=R3VehicleAssembly.Build(new R3VehicleModel(d),new R1RunStats(),palette,
            new Vector3(0,20,1000),Quaternion.identity);
        v.transform.SetParent(fixtures.transform,true);
        v.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
        return v;
    }
    static R3MountedPart Module(R1VehicleState v) =>
        v.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.BonusModule);
    static IEnumerator Fixed(float seconds)
    {
        float end=Time.fixedTime+seconds,timeout=Time.realtimeSinceStartup+5;
        while(Time.fixedTime<end&&Time.realtimeSinceStartup<timeout)yield return null;
        Assert.That(Time.fixedTime,Is.GreaterThanOrEqualTo(end));
    }
    [UnityTest] public IEnumerator All64QuarterTurnCombinationsUseOnePhysicalOwnerAndMatchingCollider()
    {
        for(int x=0;x<4;x++)for(int y=0;y<4;y++)for(int z=0;z<4;z++)
        {
            var turns=new Vector3Int(x,y,z);
            var offsets=R3VehicleModel.OccupiedCells(Vector3Int.zero,R3PartKind.BonusModule,turns).ToArray();
            var support=offsets.OrderBy(c=>c.x).ThenBy(c=>c.y).ThenBy(c=>c.z).First();
            var d=Design();d.Parts.Add(Bonus(Vector3Int.right-support,turns));
            var v=Build(d);var m=Module(v);
            Assert.That(v.GetComponentsInChildren<P12_PipelineSafePlayerBlock>().Length,Is.EqualTo(3),
                "A four-cell module has one real structural owner, never HP-less helper blocks: "+turns);
            Assert.That(m.GetComponentsInChildren<P12PipelineSafeDamageable>().Length,Is.EqualTo(1));
            Assert.That(m.GetComponentsInChildren<Collider>().Length,Is.EqualTo(1));
            Assert.That(v.Structure.ConnectedBlockCount,Is.EqualTo(3));
            Assert.That(m.Operational,Is.True);Assert.That(v.Structure.IsBlockConnected(m.Block),Is.True);
            var state=v.GetComponent<R3VehicleAssembly>().Model.Parts.Single(p=>p.Kind==R3PartKind.BonusModule);
            var cells=R3VehicleModel.OccupiedCells(state).ToArray();
            Assert.That(cells.Distinct().Count(),Is.EqualTo(4));
            var box=m.GetComponent<BoxCollider>();
            var center=v.transform.InverseTransformPoint(box.transform.TransformPoint(box.center));
            var expected=cells.Aggregate(Vector3.zero,(sum,c)=>sum+(Vector3)c)*(.55f/4);
            Assert.That(Vector3.Distance(center,expected),Is.LessThan(.001f));
            Physics.SyncTransforms();
            var bounds=box.bounds;
            foreach(var c in cells)
                Assert.That(bounds.Contains(v.transform.TransformPoint((Vector3)c*.55f)),Is.True,"Collider must span "+c);
            Object.Destroy(v.gameObject);yield return null;
        }
    }
    [UnityTest] public IEnumerator NonAnchorFaceConnectsButBridgeLossReleasesWholeModuleOnce()
    {
        var d=Design();d.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.Armor));
        d.Parts.Add(Bonus(new Vector3Int(2,0,-1)));
        var v=Build(d);var assembly=v.GetComponent<R3VehicleAssembly>();var module=Module(v);
        Assert.That(R3VehicleModel.Connected(assembly.Model.Parts),Is.True);
        Assert.That(v.Structure.IsBlockConnected(module.Block),Is.True,"Only a non-anchor footprint face touches the bridge.");
        var bridge=assembly.Parts.Single(p=>p.Kind==R3PartKind.Armor);
        bridge.Damageable.ApplyDamage(10000,bridge.transform.position,Vector3.right);
        v.Structure.EvaluateStructure();
        Assert.That(module.Operational,Is.False);Assert.That(module.transform.IsChildOf(v.transform),Is.False);
        var chunk=module.GetComponentInParent<Rigidbody>();
        Assert.That(chunk,Is.Not.SameAs(v.GetComponent<Rigidbody>()));
        Assert.That(chunk.GetComponentsInChildren<P12_PipelineSafePlayerBlock>().Length,Is.EqualTo(1),
            "No detached invisible sockets survive as independent blocks.");
        Assert.That(v.Structure.DetachedBlockCountTotal,Is.EqualTo(1));
        assembly.Capture();
        Assert.That(assembly.Model.Parts.Single(p=>p.Kind==R3PartKind.BonusModule).Health,Is.Zero);
        yield return Fixed(.04f);
        Assert.That(chunk.linearVelocity.x,Is.GreaterThan(5));
        Object.Destroy(chunk.gameObject);Object.Destroy(bridge.gameObject);
    }
    [UnityTest] public IEnumerator OwnerDestructionRemovesAllFourCellsAndDropsDependentPart()
    {
        var d=Design();d.Parts.Add(Bonus(Vector3Int.right));
        d.Parts.Add(new R3PartState(new Vector3Int(3,0,0),R3PartKind.Wheel));
        var v=Build(d);var module=Module(v);
        var leaf=v.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.Wheel);
        Assert.That(v.Structure.IsBlockConnected(leaf.Block),Is.True);
        module.Damageable.ApplyDamage(10000,module.transform.position,Vector3.right);
        v.Structure.EvaluateStructure();
        Assert.That(leaf.Operational,Is.False);
        Assert.That(v.GetComponentsInChildren<P12_PipelineSafePlayerBlock>().Length,Is.EqualTo(2));
        Assert.That(v.Structure.ConnectedBlockCount,Is.EqualTo(2));
        Assert.That(v.GetComponentsInChildren<P12_PipelineSafePlayerBlock>().All(b=>b.GetComponent<P12PipelineSafeDamageable>()!=null),Is.True);
        Object.Destroy(leaf.GetComponentInParent<Rigidbody>().gameObject);Object.Destroy(module.gameObject);
        yield return null;
    }
    [UnityTest] public IEnumerator ReciprocalPortsRejectTheSameConnectionInModelAndLiveP12()
    {
        var d=Design();var bonus=Bonus(Vector3Int.right);
        bonus.ContactMask=R3MountingGrid.FaceBit(Vector3Int.right);d.Parts.Add(bonus);
        var model=new R3VehicleModel(d);
        Assert.That(R3VehicleModel.Connected(model.Parts),Is.False);
        var v=Build(d);var module=Module(v);
        Assert.That(module.Operational,Is.False);Assert.That(v.Structure.IsBlockConnected(module.Block),Is.False);
        Object.Destroy(module.GetComponentInParent<Rigidbody>().gameObject);yield return null;
    }
    [UnityTest] public IEnumerator ProjectileOnNonAnchorCellHitsTheSingleModuleHealthPool()
    {
        var d=Design();d.Parts.Add(Bonus(Vector3Int.right));
        var v=Build(d);var module=Module(v);
        var target=v.transform.TransformPoint(new Vector3(2,0,1)*.55f);
        var shot=GameObject.CreatePrimitive(PrimitiveType.Sphere);shot.transform.SetParent(fixtures.transform,true);
        shot.transform.position=target+Vector3.down*2;shot.transform.localScale=Vector3.one*.1f;
        var body=shot.AddComponent<Rigidbody>();body.useGravity=false;
        body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        shot.AddComponent<P17PhysicalShell>().Configure(null,25,2,Vector3.up,null);
        body.linearVelocity=Vector3.up*30;Physics.SyncTransforms();yield return Fixed(.2f);
        Assert.That(module.Damageable.CurrentHp,Is.EqualTo(225).Within(.01f));
        Assert.That(module.GetComponentsInChildren<P12PipelineSafeDamageable>().Length,Is.EqualTo(1));
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.timeScale=1;if(fixtures!=null)Object.Destroy(fixtures);
        if(Application.isPlaying)yield return new ExitPlayMode();
    }
}
