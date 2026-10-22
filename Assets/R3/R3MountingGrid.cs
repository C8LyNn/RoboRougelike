using System;
using System.Collections.Generic;
using UnityEngine;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R3
{
    /// <summary>
    /// Six face-centre mounting ports per occupied grid cell. A connection exists
    /// only when two outward, opposing sockets touch on adjacent grid cells.
    /// ContactMask is defined in the item's LOCAL space, and follows 90-degree
    /// rotations around all three axes.
    /// </summary>
    public static class R3MountingGrid
    {
        public const int AllFaces = 0x3F;
        public static readonly Vector3Int[] FaceDirections = {
            Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down,
            Vector3Int.forward, Vector3Int.back
        };

        public static Vector3Int Normalize(Vector3Int turns) =>
            new Vector3Int(Mod4(turns.x),Mod4(turns.y),Mod4(turns.z));

        private static int Mod4(int turns) => ((turns%4)+4)%4;

        public static Quaternion Rotation(Vector3Int quarterTurns)
        {
            var n=Normalize(quarterTurns);
            return Quaternion.Euler(n.x*90f,n.y*90f,n.z*90f);
        }
        public static Vector3Int Rotate(Vector3Int offset,Vector3Int turns)
        {
            var v=Rotation(turns)*(Vector3)offset;
            return new Vector3Int(Mathf.RoundToInt(v.x),Mathf.RoundToInt(v.y),Mathf.RoundToInt(v.z));
        }
        public static Vector3Int InverseRotate(Vector3Int direction,Vector3Int turns)
        {
            var v=Quaternion.Inverse(Rotation(turns))*(Vector3)direction;
            return new Vector3Int(Mathf.RoundToInt(v.x),Mathf.RoundToInt(v.y),Mathf.RoundToInt(v.z));
        }
        public static int FaceBit(Vector3Int direction)
        {
            for(int i=0;i<FaceDirections.Length;i++)
                if(FaceDirections[i]==direction)return 1<<i;
            return 0;
        }
        public static int WorldPortMask(Vector3Int rotation,int localMask)
        {
            int result=0;
            foreach(var direction in FaceDirections)
                if((localMask&FaceBit(direction))!=0)
                    result|=FaceBit(Rotate(direction,rotation));
            return result;
        }
        public static bool PortOnFace(R3PartState part,Vector3Int worldDirection) =>
            part!=null&&(WorldPortMask(part.RotationQuarterTurns,part.ContactMask)&FaceBit(worldDirection))!=0;

        public static bool Compatible(R3PartState a,R3PartState b,Vector3Int direction) =>
            a!=null&&b!=null&&(ReferenceEquals(a,b)||
            (PortOnFace(a,direction)&&PortOnFace(b,-direction)));

        /// <summary>
        /// The existing P12 vehicle graph uses this adapter to mirror exactly
        /// the placement rules, including the one-item 1x2x2 structural sockets.
        /// Blocks without the component (legacy enemy/scene prefabs) have six
        /// ports; this preserves pre-existing vehicles.
        /// </summary>
        public static bool Compatible(P12_PipelineSafePlayerBlock a,
            P12_PipelineSafePlayerBlock b,Vector3Int direction)
        {
            if(a==null||b==null)return false;
            var left=a.GetComponent<R3BlockMountPoints>();
            var right=b.GetComponent<R3BlockMountPoints>();
            if(left!=null&&right!=null&&!string.IsNullOrEmpty(left.GroupId)&&left.GroupId==right.GroupId)
                return true;
            int lm=left!=null?left.WorldMask:AllFaces;
            int rm=right!=null?right.WorldMask:AllFaces;
            return (lm&FaceBit(direction))!=0 && (rm&FaceBit(-direction))!=0;
        }

        public static IEnumerable<Vector3Int> FaceCenters(Vector3Int cell,Vector3Int direction)
        {
            // One port is centred on every exterior voxel face.
            yield return cell;
        }
    }

    /// <summary>Port data on a real physical block or a non-colliding footprint socket.</summary>
    [DisallowMultipleComponent]
    public sealed class R3BlockMountPoints : MonoBehaviour
    {
        [SerializeField] private string groupId;
        [SerializeField,Range(0,63)] private int worldMask=R3MountingGrid.AllFaces;
        public string GroupId=>groupId;
        public int WorldMask=>worldMask;
        public void Configure(string physicalPartId,int mask)
        {
            groupId=physicalPartId;
            worldMask=mask&R3MountingGrid.AllFaces;
        }
    }
}
