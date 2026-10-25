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

        // Control test: verify the very same friction-independent cube actuator
        // can accelerate on a simple flat collider. This discriminates motor or
        // test-scene problems from actual MAPGEN route geometry.
        [UnityTest] public IEnumerator CubeForceActuatorMovesOnFlatControlSurface()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "MAPGEN001_Flat_Control_Floor";
            floor.transform.position = new Vector3(0f, -.5f, 0f);
            floor.transform.localScale = new Vector3(24f, 1f, 40f);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(0f, 1.25f, -14f);
            var mat = CreateLowFrictionContact();
            cube.GetComponent<Collider>().sharedMaterial = mat;
            var rb = cube.AddComponent<Rigidbody>();
            rb.mass = 100f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.linearDamping = .35f;
            rb.angularDamping = 2f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Physics.SyncTransforms();
            float maxZ = cube.transform.position.z;
            try
            {
                for (int i = 0; i < 150; i++)
                {
                    rb.AddForce(Vector3.forward * 7f, ForceMode.Acceleration);
                    yield return new WaitForFixedUpdate();
                    maxZ = Mathf.Max(maxZ, rb.position.z);
                }
                Assert.Greater(maxZ, -10f,
                    "CONTROL_MOTOR_FAIL: cube with explicit low-friction contact must move >4m on flat ground");
            }
            finally { Object.Destroy(cube); Object.Destroy(floor); Object.Destroy(mat); }
        }

        private static PhysicsMaterial CreateLowFrictionContact()
        {
            // The cube is a DRIVE TEST FIXTURE, not a wheeled R6 vehicle.
            // Default cube/floor PhysX static friction can pin the fixture.
            // Eliminate that uncontrolled source of traction while still using
            // real gravity, contacts, mass, forces, torque and Rigidbody.
            return new PhysicsMaterial("MAPGEN001_CubeControl_LowFriction") {
                staticFriction = 0f, dynamicFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f
            };
        }

        private static IEnumerator DriveOneCase(MapRecipe recipe, int seed, bool heavy)
        {
            var profile = heavy ? MapVehicleProfile.Heavy : MapVehicleProfile.Light;
            string caseId = recipe + "/seed=" + seed + "/rig=" + (heavy ? "Heavy" : "Light");
            var host = new GameObject("MAPGEN001_PhysicsTraversal_Host_" + seed);
            var builder = host.AddComponent<MapGenBuilder>();
            GameObject rover = null;
            PhysicsMaterial contactMaterial = null;
            bool reached = false;
            int waypoint = 0;
            int waypointCount = 0;
            int physicsSteps = 0;
            float maxDisplacement = 0f;
            float finalVelocity = 0f;
            float finalHeading = 0f;
            float maxPlanarSpeed = 0f;
            float bestRemainingDistance = float.PositiveInfinity;
            int groundedSamples = 0;
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
                contactMaterial = CreateLowFrictionContact();
                rover.GetComponent<Collider>().sharedMaterial = contactMaterial;
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
                    maxPlanarSpeed = Mathf.Max(maxPlanarSpeed,
                        new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude);
                    RaycastHit floorHit;
                    if (Physics.Raycast(rb.position + Vector3.up * .1f, Vector3.down,
                        out floorHit, 1.5f, ~0, QueryTriggerInteraction.Ignore)
                        && floorHit.collider != rover.GetComponent<Collider>())
                        groundedSamples++;
                    if (rb.position.y < -2f)
                    {
                        failure = "FELL_THROUGH_FLOOR";
                        break;
                    }
                    Vector3 to = goals[waypoint] - rb.position;
                    to.y = 0f;
                    bestRemainingDistance = Mathf.Min(bestRemainingDistance, to.magnitude);
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
                if (contactMaterial != null) Object.Destroy(contactMaterial);
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
                " heading=" + finalHeading.ToString("F2") +
                " maxPlanarSpeed=" + maxPlanarSpeed.ToString("F3") +
                " bestGoalDistance=" + bestRemainingDistance.ToString("F3") +
                " floorRaySamples=" + groundedSamples + " reason=" + failure +
                " (CUBE ONLY: PR125/OBJ210 real R6 wheels still require separate QA)");
        }
    }
}
