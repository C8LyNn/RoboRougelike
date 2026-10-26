using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.MapSystemR4.Tests
{
    public sealed class MapR6JunctionClearancePlayModeTests
    {
        private static MapR4MapData Map()
        {
            return new MapR4MapData
            {
                mapId="junction_obstacle_demo", version=6,
                nodes=new[]
                {
                    new MapR4NodeData{id="west",position=new Vector3(-60,0,0)},
                    new MapR4NodeData{id="middle",position=Vector3.zero,padWidth=36},
                    new MapR4NodeData{id="north",position=new Vector3(0,0,60)}
                },
                edges=new[]
                {
                    new MapR4EdgeData{id="a",fromNode="west",toNode="middle",width=12,clearHeight=8,roadThickness=.35f,roadSegments=24},
                    new MapR4EdgeData{id="b",fromNode="middle",toNode="north",width=12,clearHeight=8,roadThickness=.35f,roadSegments=24}
                }
            };
        }
        private static MapR4VehicleProfile Vehicle() => new MapR4VehicleProfile
        {
            id="test",width=2.6f,height=2f,length=3.5f,minTurnRadius=.1f,
            maxSlopeDeg=15f,sideMargin=.3f,verticalMargin=.2f
        };

        [UnityTest]
        public IEnumerator ObstacleInsideValidJunction_IsBlocked()
        {
            var vehicle=Vehicle();
            var plan=MapR6JunctionPlanner.Plan(Map(),"a","b","middle",vehicle);
            Assert.IsTrue(plan.Valid,plan.Reason);
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.layer=8;
            block.transform.position=plan.Evaluate(.5f)+Vector3.up*1f;
            block.transform.localScale=new Vector3(2f,2f,2f);
            yield return new WaitForFixedUpdate();
            var result=MapR6JunctionClearanceProbe.Check(plan,vehicle,1<<8);
            Assert.AreEqual(MapR5ProbeStatus.Blocked,result.Status,result.Reason);
            Object.Destroy(block);
            yield return null;
        }

        [Test]
        public void MissingObstacleLayer_IsInvalid_NotPass()
        {
            var vehicle=Vehicle();
            var plan=MapR6JunctionPlanner.Plan(Map(),"a","b","middle",vehicle);
            Assert.IsTrue(plan.Valid,plan.Reason);
            var result=MapR6JunctionClearanceProbe.Check(plan,vehicle,(LayerMask)0);
            Assert.AreEqual(MapR5ProbeStatus.InvalidConfiguration,result.Status);
        }
    }
}
