using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.MAPGEN001.Tests
{
    /// <summary>
    /// Unassisted physics driving smoke: AddForce/AddTorque, not a transform
    /// teleport or an API-only traversal assertion. Requires Unity PlayMode.
    /// </summary>
    public sealed class MapGenPhysicalTraversalTests
    {
        [UnityTest] public IEnumerator LightAndHeavyRigidbodyDriveThreeSeedsForBothRecipes()
        {
            var host = new GameObject("MAPGEN001_PhysicsTraversal_Host");
            var builder = host.AddComponent<MapGenBuilder>();
            try
            {
                foreach (MapRecipe recipe in new[] { MapRecipe.CombatArena, MapRecipe.ObstacleCourse })
                foreach (int seed in new[] { 7, 31, 83 })
                foreach (bool heavy in new[] { false, true })
                {
                    var profile = heavy ? MapVehicleProfile.Heavy : MapVehicleProfile.Light;
                    var layout = builder.Generate(recipe, seed, MapGenConfig.Default, profile);
                    var rover = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rover.name = "MAPGEN001_PhysicsTraversal_" + recipe + "_" + seed + "_" + (heavy ? "Heavy" : "Light");
                    rover.transform.localScale = new Vector3(profile.Width, 1f, profile.Length);
                    rover.transform.position = layout.Start;
                    var rb = rover.AddComponent<Rigidbody>();
                    rb.mass = heavy ? 600f : 100f;
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.linearDamping = .35f;
                    rb.angularDamping = 2f;
                    Physics.SyncTransforms();

                    var goals = new List<Vector3>(layout.CheckpointDescriptors);
                    goals.Add(layout.Finish);
                    int current = 0;
                    bool reached = false;
                    for (int step = 0; step < 900; step++)
                    {
                        if (rb.position.y < -2f) break;
                        Vector3 to = goals[current] - rb.position;
                        to.y = 0f;
                        if (to.magnitude < 3.2f)
                        {
                            current++;
                            if (current == goals.Count) { reached = true; break; }
                            to = goals[current] - rb.position;
                            to.y = 0f;
                        }
                        Vector3 desired = to.normalized;
                        float heading = Vector3.SignedAngle(rb.transform.forward, desired, Vector3.up);
                        float steer = Mathf.Clamp(heading * .45f - rb.angularVelocity.y * 3.5f, -30f, 30f);
                        rb.AddTorque(Vector3.up * steer, ForceMode.Acceleration);
                        float forwardSpeed = Vector3.Dot(rb.linearVelocity, rb.transform.forward);
                        float throttle = Mathf.Clamp(to.magnitude * 1.4f - forwardSpeed * 2f, -7f, 7f);
                        rb.AddForce(rb.transform.forward * throttle, ForceMode.Acceleration);
                        yield return new WaitForFixedUpdate();
                    }
                    Assert.IsTrue(reached, "PHYSICS_TRAVERSAL_FAIL recipe=" + recipe +
                        " seed=" + seed + " rig=" + (heavy ? "Heavy" : "Light") +
                        " waypoint=" + current + "/" + goals.Count + " pos=" + rb.position);
                    Object.Destroy(rover);
                    builder.Clear();
                    yield return null;
                }
            }
            finally { Object.Destroy(host); }
        }
    }
}
