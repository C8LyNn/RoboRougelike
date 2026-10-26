using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Mechanics.RAC001.Tests
{
    public class RAC001PhysicalDemoTests
    {
        [UnityTest] public IEnumerator RigidbodyDemoUsesRealForcesToReachCheckpoint()
        {
            var host = new GameObject("RAC001_PHYSICS_TEST_HOST");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "RAC fixture physical floor";
            cube.name = "RAC fixture driven cube";
            var checkpoint = new GameObject("CheckpointPhysical");
            try
            {
                floor.transform.position = new Vector3(3f, -.5f, 0f);
                floor.transform.localScale = new Vector3(24f, 1f, 24f);
                cube.transform.position = new Vector3(0f, .65f, 0f);
                var body = cube.AddComponent<Rigidbody>();
                var contacts = cube.AddComponent<RACFixtureContactTrace>();
                body.mass = 150f;
                body.useGravity = true;
                body.linearDamping = .8f;
                body.angularDamping = 2f;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.constraints = RigidbodyConstraints.FreezeRotationX |
                                   RigidbodyConstraints.FreezeRotationZ;
                checkpoint.transform.position = new Vector3(6f, .5f, 0f);
                var race = host.AddComponent<CheckpointRaceChallenge>();
                race.Configure(cube.transform, new[] { checkpoint.transform }, 1.3f, 30f);
                host.AddComponent<CheckpointRaceDemoController>().Configure(race, cube.transform,
                    new[] { checkpoint.transform });
                Physics.SyncTransforms();
                Assert.IsTrue(race.Begin());
                float maxX = body.position.x;
                for (int step = 0; step < 240 && !race.Won; step++)
                {
                    yield return new WaitForFixedUpdate();
                    if(step%60==0) Debug.Log("[RAC native diagnostic] step="+step+" dt="+Time.fixedDeltaTime+" renderDt="+Time.deltaTime+" racePhase="+race.Phase+" raceElapsed="+race.ElapsedSeconds+" time="+Time.time+" gravity="+Physics.gravity+" pos="+body.position+" velocity="+body.linearVelocity+" contacts="+string.Join(";",contacts.Contacts));
                    maxX = Mathf.Max(maxX, body.position.x);
                    Assert.Greater(body.position.y, -2f, "Fell off static physical floor");
                }
                Assert.Greater(maxX, 4f, "Dynamic body did not cross the real floor");
                Assert.IsTrue(race.Won, "Force-driven checkpoint was not reached");
                Assert.Less(race.ElapsedSeconds, 30f);
            }
            finally
            {
                Object.Destroy(host);
                Object.Destroy(cube);
                Object.Destroy(floor);
                Object.Destroy(checkpoint);
            }
            yield return null;
        }
    }

    public sealed class RACFixtureContactTrace : MonoBehaviour
    {
        public readonly System.Collections.Generic.HashSet<string> Contacts = new System.Collections.Generic.HashSet<string>();
        void OnCollisionStay(Collision collision)
        {
            var other=collision.collider;
            Contacts.Add(other.name+" parent="+other.transform.parent?.name+" scene="+other.gameObject.scene.name+" bounds="+other.bounds);
        }
    }
}
