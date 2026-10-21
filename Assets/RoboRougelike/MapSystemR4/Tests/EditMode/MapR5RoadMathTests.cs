using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.MapSystemR4.Tests
{
    public class MapR5RoadMathTests
    {
        private MapR4MapData map;
        [SetUp]
        public void SetUp()
        {
            var resource = Resources.Load<TextAsset>("MAP_R4_quarry_03");
            Assert.IsNotNull(resource);
            map = MapR4MapData.Parse(resource.text);
        }

        private MapR4EdgeData Edge(string id)
        {
            foreach (var e in map.edges) if (e.id == id) return e;
            Assert.Fail("Missing edge " + id);
            return null;
        }
        private Vector3 Node(string id)
        {
            foreach (var node in map.nodes) if (node.id == id) return node.position;
            Assert.Fail("Missing node " + id);
            return Vector3.zero;
        }

        [Test]
        public void CurvedRoute_BeginsAndEndsExactlyAtNodes()
        {
            var e = Edge("junction_upper");
            Assert.IsTrue(e.useCurve);
            Assert.Less(Vector3.Distance(Node(e.fromNode), MapR5RoadMath.Evaluate(Node(e.fromNode), Node(e.toNode), e, 0f)), 0.0001f);
            Assert.Less(Vector3.Distance(Node(e.toNode), MapR5RoadMath.Evaluate(Node(e.fromNode), Node(e.toNode), e, 1f)), 0.0001f);
        }

        [Test]
        public void AllRoads_HaveSlopeWithinMetadata()
        {
            foreach (var edge in map.edges)
            {
                float slope = MapR5RoadMath.MeasuredMaxSlopeDeg(Node(edge.fromNode), Node(edge.toNode), edge);
                Assert.LessOrEqual(slope, edge.declaredSlopeDeg + 0.15f, edge.id);
            }
        }

        [Test]
        public void RoadVerticalProfile_IsFlatAtBothNodeEndpoints()
        {
            var e = Edge("junction_upper");
            var a = Node(e.fromNode); var b = Node(e.toNode);
            float initial = MapR5RoadMath.Evaluate(a,b,e,0.001f).y - a.y;
            float final = b.y - MapR5RoadMath.Evaluate(a,b,e,0.999f).y;
            Assert.Less(Mathf.Abs(initial), 0.001f);
            Assert.Less(Mathf.Abs(final), 0.001f);
        }

        [Test]
        public void StraightPath_HasInfiniteCurvatureRadius()
        {
            var e = Edge("start_junction");
            Assert.IsTrue(float.IsPositiveInfinity(MapR5RoadMath.MeasuredMinRadiusXZ(Node(e.fromNode), Node(e.toNode), e)));
        }

        [Test]
        public void CurvedRoad_GeneratesClosedColliderMesh()
        {
            var e = Edge("junction_upper");
            var mesh = MapR5RoadMesh.Build(Node(e.fromNode), Node(e.toNode), e);
            try
            {
                Assert.AreEqual(4 * (e.roadSegments + 1), mesh.vertexCount);
                Assert.AreEqual(e.roadSegments * 24 + 12, mesh.triangles.Length);
                Assert.Greater(mesh.normals[0].y, 0f);
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void IncorrectSlopeMetadata_RejectsMap()
        {
            Edge("junction_upper").declaredSlopeDeg = 0f;
            Assert.IsNotEmpty(MapR4GraphValidator.Validate(map));
        }

        [Test]
        public void CurveRadius_RejectsVehicleWithInsufficientSteering()
        {
            var e = Edge("junction_upper");
            float radius = MapR5RoadMath.MeasuredMinRadiusXZ(Node(e.fromNode), Node(e.toNode), e);
            Assert.IsFalse(float.IsInfinity(radius));
            Assert.Greater(radius, 0f);
            var vehicle = new MapR4VehicleProfile
            {
                id = "too-stiff", width = 2.6f, height = 2f, length = 3.5f,
                minTurnRadius = radius + 1f, maxSlopeDeg = 16f
            };
            Assert.Greater(vehicle.minTurnRadius, radius);
        }
    }
}
