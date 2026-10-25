using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.MAPGEN001.Tests
{
    /// <summary>
    /// W10: 12 independently reportable Rigidbody CUBE test cases, retaining
    /// physical AddForce/torque without Transform-driven teleport acceptance.
    /// This source NEVER substitutes for 12 real PR125/OBJ210 R6-wheel trials.
    /// </summary>
    public sealed class MapGenPhysicalTraversalTests
    {
        [UnityTest] public IEnumerator Arena_Seed7_Light_CubePhysics()
            => DriveOneCase(MapRecipe.CombatArena, 7, false);
        [UnityTest] public IEnumerator Arena_Seed7_Heavy_CubePhysics()
            => DriveOneCase(MapRecipe.CombatArena, 7, true);
        [UnityTest] public IEnumerator Arena_Seed31_Light_CubePhysics()
            => DriveOneCase(MapRecipe.CombatArena, 31, false);
        [UnityTest] public IEnumerator Arena_Seed31_Heavy_CubePhysics()
            => DriveOneCase(MapRecipe.CombatArena, 31, true);
        [UnityTest] public IEnumerator Arena_Seed83_Light_CubePhysics()
            => DriveOneCase(MapRecipe.CombatArena, 83, false);
        [UnityTest] public IEnumerator Arena_Seed83_Heavy_CubePhysics()
            => DriveOneCase(MapRecipe.CombatArena, 83, true);
        [UnityTest] public IEnumerator Course_Seed7_Light_CubePhysics()
            => DriveOneCase(MapRecipe.ObstacleCourse, 7, false);
        [UnityTest] public IEnumerator Course_Seed7_Heavy_CubePhysics()
            => DriveOneCase(MapRecipe.ObstacleCourse, 7, true);
        [UnityTest] public IEnumerator Course_Seed31_Light_CubePhysics()
            => DriveOneCase(MapRecipe.ObstacleCourse, 31, false);
        [UnityTest] public IEnumerator Course_Seed31_Heavy_CubePhysics()
            => DriveOneCase(MapRecipe.ObstacleCourse, 31, true);
        [UnityTest] public IEnumerator Course_Seed83_Light_CubePhysics()
            => DriveOneCase(MapRecipe.ObstacleCourse, 83, false);
        [UnityTest] public IEnumerator Course_Seed83_Heavy_CubePhysics()
            => DriveOneCase(MapRecipe.ObstacleCourse, 83, true);

        private static IEnumerator DriveOneCase(MapRecipe recipe, int seed, bool heavy)
        {
            var profile = heavy ? MapVehicleProfile.Heavy : MapVehicleProfile.Light;
            string caseId = recipe + "/seed=" + seed + "/rig=" + (heavy ? "Heavy" : "Light");
            var host = new GameObject("MAPGEN001_PhysicsTraversal_Host_" + seed);
            var builder = host.AddComponent<MapGenBuilder>();
            GameObject rover = null;
            bool reached = false;
            int waypoint = 0;
            int waypointCount = 0;
            int physicsSteps = 0;
            float maxDisplacement = 0f;
            float finalVelocity = 0f;
            float finalHeading = 0f;
            Vector3 start = Vector3.zero, finalPosition = Vector3.zero;
            string failure = "";
            try
            {
                MapResult layout = builder.Generate(recipe, seed, MapGenConfig.Default, profile);
                start = layout.Start;
                rover = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rover.name = "MAPGEN001_Cube_" + caseId.Replace("/", "_");
                rover.transform.localScale = new Vector3(profile.Width, 1f, profile.Length);
                rover.transform.position = start;
                var rb = rover.AddComponent<Rigidbody>();
                rb.mass = heavy ? 600f : 100f;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.linearDamping = .35f;
                rb.angularDamping = 2f;
                Physics.SyncTransforms();

                var goals = new List<Vector3>(layout.CheckpointDescriptors);
                goals.Add(layout.Finish);
                waypointCount = goals.Count;

                for (int step = 0; step < 900; step++)
                {
                    physicsSteps = step + 1;
                    finalPosition = rb.position;
                    finalVelocity = rb.linearVelocity.magnitude;
                    Vector3 delta = rb.position - start;
                    delta.y = 0f;
                    maxDisplacement = Mathf.Max(maxDisplacement, delta.magnitude);
                    if (rb.position.y < -2f)
                    {
                        failure = "FELL_THROUGH_FLOOR";
                        break;
                    }
                    Vector3 to = goals[waypoint] - rb.position;
                    to.y = 0f;
                    if (to.magnitude < 3.2f)
                    {
                        waypoint++;
                        if (waypoint == goals.Count)
                        {
                            reached = true;
                            break;
                        }
                        to = goals[waypoint] - rb.position;
                        to.y = 0f;
                    }
                    Vector3 desired = to.normalized;
                    float heading = Vector3.SignedAngle(rb.transform.forward, desired, Vector3.up);
                    finalHeading = heading;
                    float steer = Mathf.Clamp(heading * .45f - rb.angularVelocity.y * 3.5f, -30f, 30f);
                    rb.AddTorque(Vector3.up * steer, ForceMode.Acceleration);
                    float forwardSpeed = Vector3.Dot(rb.linearVelocity, rb.transform.forward);
                    float throttle = Mathf.Clamp(to.magnitude * 1.4f - forwardSpeed * 2f, -7f, 7f);
                    rb.AddForce(rb.transform.forward * throttle, ForceMode.Acceleration);
                    yield return new WaitForFixedUpdate();
                }
                finalPosition = rb.position;
                finalVelocity = rb.linearVelocity.magnitude;
            }
            finally
            {
                if (rover != null) Object.Destroy(rover);
                builder.Clear();
                Object.Destroy(host);
                // Every test owns exactly one cube and one map; cleanup also
                // executes if the coroutine fails before reaching its finish.
            }

            // Prevent one failing seed from suppressing the 11 other cases.
            // Each [UnityTest] has a distinct NUnit ID and independent result.
            Assert.IsTrue(reached, "CUBE_PHYSICS_TRAVERSAL_FAIL " + caseId +
                " waypoint=" + waypoint + "/" + waypointCount +
                " steps=" + physicsSteps +
                " start=" + start + " end=" + finalPosition +
                " maxHorizontalDisplacement=" + maxDisplacement.ToString("F3") +
                " endSpeed=" + finalVelocity.ToString("F3") +
                " heading=" + finalHeading.ToString("F2") + " reason=" + failure +
                " (CUBE ONLY: PR125/OBJ210 real R6 wheels still require separate QA)");
        }
    }
}
