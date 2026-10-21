using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RogueMachine.P12PipelineSafe;

public class R1CombatRegressionTests
{
    [Test]
    public void RunProgressSubsystemExists()
    {
        Assert.That(typeof(P12PipelineSafeTankController).Assembly.GetType(
            "RoboRougelike.R1.R1RunProgress"), Is.Not.Null,
            "R1 wymaga modelu wyprawy niezależnego od interfejsu.");
    }

    [UnityTest]
    public IEnumerator EnemyCannotResumeFireAfterItsDeadCoreDebrisExpires()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        Time.timeScale = 1;
        var target = new GameObject("Target");
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.transform.SetParent(target.transform);
        block.transform.position = new Vector3(0, 1, 10);
        block.AddComponent<P12_PipelineSafePlayerBlock>();
        block.AddComponent<P12PipelineSafeDamageable>().Configure(100000, false);
        var root = new GameObject("Enemy");
        var core = GameObject.CreatePrimitive(PrimitiveType.Cube);
        core.transform.SetParent(root.transform);
        core.transform.position = new Vector3(3, 1, 0);
        var damageable = core.AddComponent<P12PipelineSafeDamageable>();
        var yaw = new GameObject("Enemy_Yaw").transform;
        yaw.SetParent(root.transform);
        yaw.position = Vector3.up;
        var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrel.transform.SetParent(yaw);
        barrel.transform.localPosition = new Vector3(0, 0, 0.5f);
        barrel.AddComponent<P12PipelineSafeDamageable>();
        var muzzle = new GameObject("Enemy_Muzzle").transform;
        muzzle.SetParent(yaw);
        muzzle.localPosition = Vector3.forward;
        var enemy = root.AddComponent<P12PipelineSafeEnemyTurret>();
        enemy.Configure(target.transform, yaw, muzzle, root.transform, damageable, null);
        yield return null;
        damageable.ApplyDamage(1000, core.transform.position, Vector3.right);
        // EditMode's IEnumerator runner does not schedule WaitForSeconds as a game coroutine.
        float until = Time.time + 7.5f;
        while (Time.time < until) yield return null;
        Assert.That(damageable == null, Is.True, "Szczątki rdzenia muszą faktycznie zniknąć przed asercją.");
        Assert.That(Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None)
            .Count(shell => shell.name == "P17_EnemyPhysicalShell"), Is.Zero,
            "Zniszczony rdzeń nie może pozwolić wieżyczce wznowić ognia po usunięciu szczątków.");
    }

    [UnityTearDown]
    public IEnumerator LeavePlayMode()
    {
        if (Application.isPlaying)
        {
            Time.timeScale = 1;
            yield return new ExitPlayMode();
        }
    }

    [UnityTest]
    public IEnumerator RemovingBridgeDetachesOnlyDisconnectedStructuralFragment()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode(); Time.timeScale = 1;
        var vehicle = new GameObject("Structural fixture");
        var blocks = new P12_PipelineSafePlayerBlock[3];
        for (int i = 0; i < blocks.Length; i++)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.transform.SetParent(vehicle.transform);
            part.transform.localPosition = new Vector3(i * 0.55f, 2, 0);
            part.transform.localScale = Vector3.one * 0.5f;
            blocks[i] = part.AddComponent<P12_PipelineSafePlayerBlock>();
            part.AddComponent<P12PipelineSafeDamageable>();
        }
        var structure = vehicle.AddComponent<P12PipelineSafeVehicleStructure>();
        structure.RebuildNow();
        Assert.That(structure.ConnectedBlockCount, Is.EqualTo(3));
        blocks[1].GetComponent<P12PipelineSafeDamageable>().ApplyDamage(1000, blocks[1].transform.position, Vector3.up);
        structure.EvaluateStructure();
        Assert.That(blocks[2].DetachedFromVehicle, Is.True);
        Assert.That(blocks[2].GetComponentInParent<Rigidbody>(), Is.Not.Null);
        Assert.That(blocks[0].transform.IsChildOf(vehicle.transform), Is.True);
        Assert.That(structure.CoreAlive, Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PhysicalProjectileIgnoresOwnerAndDamagesOnlyFirstTarget()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode(); Time.timeScale = 1;
        var owner = new GameObject("Owner");
        var shield = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shield.transform.SetParent(owner.transform); shield.transform.position = Vector3.forward;
        var front = GameObject.CreatePrimitive(PrimitiveType.Cube);
        front.transform.position = new Vector3(0, 0, 3);
        var frontHp = front.AddComponent<P12PipelineSafeDamageable>();
        var rear = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rear.transform.position = new Vector3(0, 0, 5);
        var rearHp = rear.AddComponent<P12PipelineSafeDamageable>();
        var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.transform.localScale = Vector3.one * 0.2f;
        var body = shell.AddComponent<Rigidbody>(); body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = Vector3.forward * 20;
        shell.AddComponent<P17PhysicalShell>().Configure(owner.transform, 40, 3, Vector3.forward, null);
        float deadline = Time.realtimeSinceStartup + 5;
        while (frontHp.CurrentHp == 100 && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(frontHp.CurrentHp, Is.EqualTo(60));
        Assert.That(rearHp.CurrentHp, Is.EqualTo(100));
        yield return null;
        Assert.That(shell == null, Is.True);
    }
}
