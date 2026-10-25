using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace B01.MAPGEN001.Tests
{
    public sealed class MapGenEditModeTests
    {
        [TestCase(MapRecipe.CombatArena)]
        [TestCase(MapRecipe.ObstacleCourse)]
        public void HundredSeedsGenerateDeterministicallyAndKeepClearance(MapRecipe recipe)
        {
            var config = MapGenConfig.Default;
            var unique = new HashSet<string>();
            for (int seed = 0; seed < 100; seed++)
            {
                var a = MapGenPlanner.Plan(recipe, seed, config, MapVehicleProfile.Heavy);
                var b = MapGenPlanner.Plan(recipe, seed, config, MapVehicleProfile.Heavy);
                Assert.AreEqual(a.GeometryHash, b.GeometryHash, "Seed " + seed);
                Assert.AreEqual(a.Start, b.Start);
                Assert.AreEqual(a.Finish, b.Finish);
                Assert.GreaterOrEqual(a.GuaranteedClearanceWidth, MapVehicleProfile.Heavy.Width);
                Assert.AreEqual(recipe == MapRecipe.ObstacleCourse ? config.Segments : 0, a.Checkpoints.Count);
                Assert.Greater(a.Elements.Count, 0);
                Assert.IsFalse(string.IsNullOrEmpty(a.Diagnostics));
                unique.Add(a.GeometryHash);
            }
            Assert.GreaterOrEqual(unique.Count, 6, "Not enough genuine geometry variation");
        }

        [Test] public void DifferentRecipesDoNotAliasEachOther()
        {
            var c = MapGenConfig.Default;
            var arena = MapGenPlanner.Plan(MapRecipe.CombatArena, 9, c, MapVehicleProfile.Light);
            var course = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 9, c, MapVehicleProfile.Light);
            Assert.AreNotEqual(arena.GeometryHash, course.GeometryHash);
            Assert.AreEqual(3, arena.EnemySpawns.Count);
            Assert.AreEqual(0, course.EnemySpawns.Count);
            Assert.AreEqual(0, arena.Checkpoints.Count);
            Assert.AreEqual(c.Segments, course.Checkpoints.Count);
        }

        [Test] public void OutOfRangeConfigAndImpossibleVehicleFailClosed()
        {
            var c = MapGenConfig.Default;
            c.LaneWidth = 10f;
            var tooWide = new MapVehicleProfile { Width = 8f, Length = 8f, MinTurnRadius = 5f };
            Assert.Throws<ArgumentException>(() => MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 1, c, tooWide));
            c = MapGenConfig.Default; c.Difficulty = float.NaN;
            Assert.Throws<ArgumentException>(() => MapGenPlanner.Plan(MapRecipe.CombatArena, 1, c, MapVehicleProfile.Light));
            c = MapGenConfig.Default; c.Segments = 6;
            Assert.Throws<ArgumentException>(() => MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 1, c, MapVehicleProfile.Light));
        }

        [Test] public void ObstacleCourseHasContinuousFloorOverlapAndCentreClearOfSolidObstacles()
        {
            for (int seed = 0; seed < 60; seed++)
            {
                var p = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, seed, MapGenConfig.Default, MapVehicleProfile.Heavy);
                var floors = p.Elements.FindAll(e => e.Kind == MapElementKind.Floor);
                Assert.AreEqual(MapGenConfig.Default.Segments, floors.Count);
                for (int i = 1; i < floors.Count; i++)
                {
                    float gap = floors[i].Position.z - floors[i - 1].Position.z -
                        (floors[i].Size.z + floors[i - 1].Size.z) / 2f;
                    Assert.LessOrEqual(gap, 0f, "Gap on seed " + seed);
                    Assert.Less(Mathf.Abs(floors[i].Position.x - floors[i - 1].Position.x),
                        MapGenConfig.Default.LaneWidth - MapVehicleProfile.Heavy.Width);
                }
                foreach (var e in p.Elements)
                {
                    if (e.Kind != MapElementKind.Obstacle && e.Kind != MapElementKind.Ramp) continue;
                    var floor = floors.Find(f => Mathf.Abs(f.Position.z - e.Position.z) < 8f);
                    Assert.Greater(Mathf.Abs(e.Position.x - floor.Position.x) - e.Size.x / 2f,
                        MapVehicleProfile.Heavy.Width / 2f,
                        "Central clearance blocked at seed " + seed);
                }
            }
        }
    }
}
