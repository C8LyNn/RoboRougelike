using System;
using NUnit.Framework;
using UnityEngine;

namespace B01.MAPGEN001.Tests
{
    public sealed class MapGenRouteValidationTests
    {
        [TestCase(MapRecipe.CombatArena)]
        [TestCase(MapRecipe.ObstacleCourse)]
        public void HundredSeedsPassReferenceVehicleSweepsAndConnectivity(MapRecipe recipe)
        {
            var cfg = MapGenConfig.Default;
            for (int seed = 0; seed < 100; seed++)
            {
                var light = MapGenPlanner.Plan(recipe, seed, cfg, MapVehicleProfile.Light);
                var heavy = MapGenPlanner.Plan(recipe, seed, cfg, MapVehicleProfile.Heavy);
                var checkLight = MapGenRouteValidator.Validate(light, MapVehicleProfile.Light);
                var checkHeavy = MapGenRouteValidator.Validate(heavy, MapVehicleProfile.Heavy);
                Assert.IsTrue(checkLight.Passed, "Light seed=" + seed + ": " + checkLight);
                Assert.IsTrue(checkHeavy.Passed, "Heavy seed=" + seed + ": " + checkHeavy);
                Assert.Greater(checkHeavy.ExploredCells, 0);
                Assert.IsTrue(heavy.Diagnostics.Contains("BFS_SPINE_CLEARANCE_OK"));
            }
        }

        [Test] public void AnArenaWallThroughTheWholeRouteFailsClosed()
        {
            var p = MapGenPlanner.Plan(MapRecipe.CombatArena, 7, MapGenConfig.Default, MapVehicleProfile.Heavy);
            p.Elements.Add(new MapElement(MapElementKind.Obstacle,
                new Vector3(0, 1, 0), new Vector3(63, 2, 4), Quaternion.identity));
            var r = MapGenRouteValidator.Validate(p, MapVehicleProfile.Heavy);
            Assert.IsFalse(r.Passed, r.ToString());
            Assert.AreEqual("SPINE_FOOTPRINT_BLOCKED", r.Code);
        }

        [Test] public void HeavyRoverCannotStartWithRearHangingPastFloor()
        {
            var p = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 11, MapGenConfig.Default, MapVehicleProfile.Heavy);
            p.Start = new Vector3(p.Start.x, p.Start.y, 1f);
            var r = MapGenRouteValidator.Validate(p, MapVehicleProfile.Heavy);
            Assert.IsFalse(r.Passed, r.ToString());
            Assert.AreEqual("SPINE_FOOTPRINT_BLOCKED", r.Code);
        }

        [Test] public void RampBeyondVehicleSlopeCapabilityIsRejected()
        {
            var p = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 2, MapGenConfig.Default, MapVehicleProfile.Heavy);
            int i = p.Elements.FindIndex(e => e.Kind == MapElementKind.Ramp);
            Assert.GreaterOrEqual(i, 0);
            var e = p.Elements[i];
            e.Rotation = Quaternion.Euler(25f, 0, 0);
            p.Elements[i] = e;
            var r = MapGenRouteValidator.Validate(p, MapVehicleProfile.Heavy);
            Assert.IsFalse(r.Passed);
            Assert.AreEqual("RAMP_EXCEEDS_PROFILE_SLOPE", r.Code);
        }

        [Test] public void ArtificiallySharpWaypointFailsSweptRouteOrTurnRadius()
        {
            var p = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 2, MapGenConfig.Default, MapVehicleProfile.Heavy);
            var point = p.Checkpoints[1];
            p.Checkpoints[1] = new Vector3(point.x + 18f, point.y, point.z);
            var r = MapGenRouteValidator.Validate(p, MapVehicleProfile.Heavy);
            Assert.IsFalse(r.Passed);
            Assert.IsTrue(r.Code == "TURN_RADIUS_TOO_SMALL" || r.Code == "SPINE_FOOTPRINT_BLOCKED",
                r.ToString());
        }


        [Test] public void CentralPitCannotPassOnlyFourCornerFootprintProbe()
        {
            // W10: original Codex native RED was START_BLOCKED, because Start==Finish
            // were placed directly over the unsupported central pit.
            // This regression instead supports BOTH endpoints, leaving a true
            // 2m unsupported transverse gap between them to test spine probing.
            var p = new MapBlueprint {
                Recipe = MapRecipe.CombatArena,
                Start = new Vector3(0f, 1.25f, -10f),
                Finish = new Vector3(0f, 1.25f, 10f),
                Bounds = new Bounds(Vector3.zero, new Vector3(40f, 8f, 40f))
            };
            p.Elements.Add(new MapElement(MapElementKind.Floor,
                new Vector3(0f, -.45f, -8.5f),
                new Vector3(20f, .9f, 15f), Quaternion.identity));
            p.Elements.Add(new MapElement(MapElementKind.Floor,
                new Vector3(0f, -.45f, 8.5f),
                new Vector3(20f, .9f, 15f), Quaternion.identity));
            var r = MapGenRouteValidator.Validate(p, MapVehicleProfile.Light);
            Assert.IsFalse(r.Passed, r.ToString());
            Assert.AreEqual("SPINE_FOOTPRINT_BLOCKED", r.Code);
        }

        [Test] public void UnsupportedStartReportsStartBlockedBeforeRouteSampling()
        {
            // Explicit diagnostic priority to preserve true W10 RED evidence.
            // No sampled spine is necessary when the initial footprint has no floor.
            var p = new MapBlueprint {
                Recipe = MapRecipe.CombatArena,
                Start = new Vector3(0f, 1.25f, 0f),
                Finish = new Vector3(0f, 1.25f, 0f),
                Bounds = new Bounds(Vector3.zero, new Vector3(40f, 8f, 40f))
            };
            var profile = MapVehicleProfile.Light;
            float hw = profile.Width * .5f + .25f;
            float hl = profile.Length * .5f + .25f;
            foreach (int x in new[] { -1, 1 })
                foreach (int z in new[] { -1, 1 })
                    p.Elements.Add(new MapElement(MapElementKind.Floor,
                        new Vector3(x * hw, -.45f, z * hl),
                        new Vector3(.5f, .9f, .5f), Quaternion.identity));
            var r = MapGenRouteValidator.Validate(p, profile);
            Assert.IsFalse(r.Passed, r.ToString());
            Assert.AreEqual("START_BLOCKED", r.Code);
        }

        [Test] public void LongNinetyDegreePolylineNeedsRealisticFilletRoom()
        {
            var p = new MapBlueprint {
                Recipe = MapRecipe.ObstacleCourse, Start = new Vector3(0f, 1.25f, 0f),
                Finish = new Vector3(25f, 1.25f, 25f),
                Bounds = new Bounds(new Vector3(12f, 0f, 12f), new Vector3(90f, 8f, 90f))
            };
            p.Elements.Add(new MapElement(MapElementKind.Floor, new Vector3(12f, -.45f, 12f),
                new Vector3(90f, .9f, 90f), Quaternion.identity));
            p.Checkpoints.Add(new Vector3(0f, 1.25f, 25f));
            var profile = new MapVehicleProfile {
                Width = 2.1f, Length = 3.2f, MinTurnRadius = 15f, MaxSlopeDegrees = 12f
            };
            var r = MapGenRouteValidator.Validate(p, profile);
            Assert.IsFalse(r.Passed, r.ToString());
            Assert.AreEqual("TURN_RADIUS_TOO_SMALL", r.Code);
        }

        [Test] public void BlockedAndOverlappingEnemySpawnsAreRejected()
        {
            var p = MapGenPlanner.Plan(MapRecipe.CombatArena, 23, MapGenConfig.Default, MapVehicleProfile.Heavy);
            p.EnemySpawns[1] = p.EnemySpawns[0];
            var overlap = MapGenRouteValidator.Validate(p, MapVehicleProfile.Heavy);
            Assert.IsFalse(overlap.Passed, overlap.ToString());
            Assert.AreEqual("ENEMY_SPAWNS_OVERLAP", overlap.Code);
            p.EnemySpawns[1] = new Vector3(0f, 1.25f, 25f);
            p.Elements.Add(new MapElement(MapElementKind.Obstacle, p.EnemySpawns[0],
                new Vector3(2f, 2f, 2f), Quaternion.identity));
            var blocked = MapGenRouteValidator.Validate(p, MapVehicleProfile.Heavy);
            Assert.IsFalse(blocked.Passed, blocked.ToString());
            Assert.AreEqual("ENEMY_SPAWN_BLOCKED", blocked.Code);
        }

        [Test] public void ArenaCoverPositionsVaryAndStayPlayableForBothProfiles()
        {
            var cfg = MapGenConfig.Default;
            cfg.Difficulty = 1f;
            cfg.ObstacleDensity = 100;
            var positions = new System.Collections.Generic.HashSet<string>();
            for (int seed = 0; seed < 100; seed++)
            {
                var p = MapGenPlanner.Plan(MapRecipe.CombatArena, seed, cfg, MapVehicleProfile.Heavy);
                Assert.IsTrue(MapGenRouteValidator.Validate(p, MapVehicleProfile.Heavy).Passed, "Heavy " + seed);
                Assert.IsTrue(MapGenRouteValidator.Validate(p, MapVehicleProfile.Light).Passed, "Light " + seed);
                foreach (var e in p.Elements)
                    if (e.Kind == MapElementKind.Cover)
                        positions.Add(e.Position.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                            ":" + e.Position.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
            }
            Assert.Greater(positions.Count, 30, "Cover grid should no longer be fixed at all seeds");
        }

        [Test] public void CourseDifficultyChangesOptionalRampPitchWithoutBreakingHeavyRoute()
        {
            var easy = MapGenConfig.Default; easy.Difficulty = 0f;
            var hard = MapGenConfig.Default; hard.Difficulty = 1f;
            var low = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 19, easy, MapVehicleProfile.Heavy);
            var high = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 19, hard, MapVehicleProfile.Heavy);
            var a = low.Elements.Find(e => e.Kind == MapElementKind.Ramp);
            var b = high.Elements.Find(e => e.Kind == MapElementKind.Ramp);
            Assert.Less(a.Rotation.eulerAngles.x, b.Rotation.eulerAngles.x);
            Assert.IsTrue(MapGenRouteValidator.Validate(high, MapVehicleProfile.Heavy).Passed);
        }

        [Test] public void ExtremeValidCourseConfigsStayTraversableAcrossHundredSeeds()
        {
            foreach (int segments in new[] { 3, 4, 5 })
            {
                foreach (bool heavy in new[] { false, true })
                {
                    var cfg = MapGenConfig.Default;
                    cfg.Segments = segments;
                    cfg.Difficulty = 1f;
                    cfg.ObstacleDensity = 100;
                    cfg.LaneWidth = heavy ? 16f : 10f;
                    var profile = heavy ? MapVehicleProfile.Heavy : MapVehicleProfile.Light;
                    for (int seed = 0; seed < 100; seed++)
                    {
                        var map = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, seed, cfg, profile);
                        var route = MapGenRouteValidator.Validate(map, profile);
                        Assert.IsTrue(route.Passed,
                            "segments=" + segments + " heavy=" + heavy + " seed=" + seed + " " + route);
                    }
                }
            }
        }
        [TestCase(2)]
        [TestCase(12)]
        [TestCase(14)]
        [TestCase(22)]
        [TestCase(28)]
        public void R32NarrowLightSideRampsNeverBlockReferenceSpine(int seed)
        {
            // Recorded RED against R3.1 via actual compiled planner/validator
            // in GitHub Actions 40180566091, 95/1013 failures overall.
            // This EditMode case is SOURCE ONLY until native Unity actually runs.
            var cfg = MapGenConfig.Default;
            cfg.Segments = 3;
            cfg.Difficulty = 1f;
            cfg.ObstacleDensity = 100;
            cfg.LaneWidth = 10f;
            var blueprint = MapGenPlanner.Plan(MapRecipe.ObstacleCourse,
                seed, cfg, MapVehicleProfile.Light);
            var route = MapGenRouteValidator.Validate(blueprint, MapVehicleProfile.Light);
            Assert.IsTrue(route.Passed, "narrow-light seed=" + seed + " " + route);
            foreach (var e in blueprint.Elements)
            {
                if (e.Kind != MapElementKind.Ramp) continue;
                Assert.LessOrEqual(e.Size.x, 1.8f + .001f,
                    "10m side ramp must retain the Light centre lane");
            }
        }

        [Test] public void PlannerRejectsNonFiniteSlopeConfiguration()
        {
            var p = MapVehicleProfile.Light;
            p.MaxSlopeDegrees = float.NaN;
            // NaN cannot bypass validation through the optional default-value rule.
            Assert.Throws<ArgumentException>(() =>
                MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 9, MapGenConfig.Default, p));
        }
    }
}
