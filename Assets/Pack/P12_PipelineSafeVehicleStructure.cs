using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RogueMachine.P12PipelineSafe
{
    [DefaultExecutionOrder(350)]
    [DisallowMultipleComponent]
    public class P12PipelineSafeVehicleStructure : MonoBehaviour
    {
        [Header("Structural grid")]
        [SerializeField] private float gridSize = 0.55f;
        [SerializeField] private float checkInterval = 0.06f;

        [Header("Detached chunks")]
        [SerializeField] private float massPerBlock = 8f;
        [SerializeField] private float detachUpImpulse = 1.15f;
        [SerializeField] private float detachOutImpulse = 0.85f;
        [SerializeField] private float detachedChunkLifetime = 14f;

        [Header("Pack 18 scope")]
        [SerializeField] private bool includeTurretShellInGraph = false;

        private P12_PipelineSafePlayerBlock coreBlock;
        [SerializeField] private P12_PipelineSafePlayerBlock designatedCore;

        // Optional explicit core lets the enemy reuse this graph without changing player core selection.
        public void SetDesignatedCore(P12_PipelineSafePlayerBlock block) { designatedCore = block; }

        private float nextCheckTime;
        private bool initialized;
        private bool coreFailureHandled;

        private int initialStructuralBlockCount;
        private int connectedBlockCount;
        private int detachedBlockCountTotal;

        private readonly Dictionary<
            Vector3Int,
            P12_PipelineSafePlayerBlock
        > grid =
            new Dictionary<
                Vector3Int,
                P12_PipelineSafePlayerBlock
            >();

        private readonly HashSet<Vector3Int>
            connected =
                new HashSet<Vector3Int>();

        private static readonly Vector3Int[] Neighbours =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1)
        };

        public bool CoreAlive =>
            coreBlock != null &&
            coreBlock.transform != null &&
            coreBlock.transform.IsChildOf(
                transform
            ) &&
            !coreFailureHandled;

        public int InitialStructuralBlockCount =>
            initialStructuralBlockCount;

        public int ConnectedBlockCount =>
            connectedBlockCount;

        public int DetachedBlockCountTotal =>
            detachedBlockCountTotal;

        public Vector3Int CoreGridPosition =>
            coreBlock != null
                ? coreBlock.LogicalGridPosition
                : Vector3Int.zero;

        public bool IsBlockConnected(
            P12_PipelineSafePlayerBlock block)
        {
            if (block == null ||
                block.DetachedFromVehicle ||
                !block.LogicalGridAssigned ||
                !block.transform.IsChildOf(transform))
            {
                return false;
            }

            if (!initialized)
                RebuildNow();

            return connected.Contains(
                block.LogicalGridPosition
            );
        }

        public P12_PipelineSafePlayerBlock FindNearestStructuralBlock(
            Vector3 worldPosition)
        {
            P12_PipelineSafePlayerBlock[] all =
                GetComponentsInChildren<
                    P12_PipelineSafePlayerBlock
                >(true);

            P12_PipelineSafePlayerBlock best = null;
            float bestDistance = float.PositiveInfinity;

            foreach (P12_PipelineSafePlayerBlock block in all)
            {
                if (block == null || block.DetachedFromVehicle)
                    continue;

                if (!includeTurretShellInGraph &&
                    IsUnderNamedAncestor(
                        block.transform,
                        "Turret_Yaw"
                    ))
                {
                    continue;
                }

                float distance =
                    (block.transform.position - worldPosition)
                    .sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = block;
                }
            }

            return best;
        }

        private void Start()
        {
            RebuildNow();
        }

        private void Update()
        {
            if (Time.time <
                nextCheckTime)
                return;

            nextCheckTime =
                Time.time +
                checkInterval;

            EvaluateStructure();
        }

        public void RebuildNow()
        {
            InitializeLogicalGrid(
                true
            );

            EvaluateStructure(
                true
            );
        }

        public void EvaluateStructure()
        {
            EvaluateStructure(
                false
            );
        }

        private void EvaluateStructure(
            bool forceInitialize)
        {
            if (!initialized ||
                forceInitialize)
            {
                InitializeLogicalGrid(
                    forceInitialize
                );
            }

            if (!initialized)
                return;

            BuildCurrentGrid();

            if (coreBlock == null ||
                !coreBlock.transform.IsChildOf(
                    transform
                ))
            {
                HandleCoreDestroyed();
                return;
            }

            connected.Clear();

            Vector3Int corePosition =
                coreBlock.LogicalGridPosition;

            if (!grid.ContainsKey(
                corePosition
            ))
            {
                HandleCoreDestroyed();
                return;
            }

            FloodFillFromCore(
                corePosition
            );

            connectedBlockCount = connected.Select(cell=>grid[cell]).Distinct().Count();

            List<
                P12_PipelineSafePlayerBlock
            > disconnectedBlocks = grid.Values.Distinct().Where(block=>
                !OccupiedGridCells(block).Any(connected.Contains)).ToList();

            if (disconnectedBlocks.Count == 0)
                return;

            DetachDisconnectedComponents(
                disconnectedBlocks
            );

            BuildCurrentGrid();

            connectedBlockCount = grid.Values.Distinct().Count();
        }

        private void InitializeLogicalGrid(
            bool force)
        {
            P12_PipelineSafePlayerBlock[] all =
                GetComponentsInChildren<
                    P12_PipelineSafePlayerBlock
                >(true);

            List<
                P12_PipelineSafePlayerBlock
            > structural =
                new List<
                    P12_PipelineSafePlayerBlock
                >();

            foreach (
                P12_PipelineSafePlayerBlock block
                in all)
            {
                if (block == null ||
                    block.DetachedFromVehicle)
                    continue;

                if (!includeTurretShellInGraph &&
                    IsUnderNamedAncestor(
                        block.transform,
                        "Turret_Yaw"
                    ))
                {
                    continue;
                }

                Vector3 local =
                    transform.InverseTransformPoint(
                        block.transform.position
                    );
                if (designatedCore != null)
                    local -= transform.InverseTransformPoint(designatedCore.transform.position);

                Vector3Int logical =
                    new Vector3Int(
                        Mathf.RoundToInt(
                            local.x / gridSize
                        ),
                        Mathf.RoundToInt(
                            local.y / gridSize
                        ),
                        Mathf.RoundToInt(
                            local.z / gridSize
                        )
                    );

                if (force ||
                    !block.LogicalGridAssigned)
                {
                    block.ForceLogicalGrid(
                        logical
                    );
                }

                block.SetCore(
                    false
                );

                structural.Add(
                    block
                );
            }

            coreBlock = designatedCore != null && structural.Contains(designatedCore)
                ? designatedCore : FindBestCore(structural);

            if (coreBlock == null)
            {
                initialized =
                    false;

                Debug.LogError(
                    "[Pack 18] No structural blocks found for the player vehicle."
                );

                return;
            }

            coreBlock.SetCore(
                true
            );

            initialStructuralBlockCount =
                Mathf.Max(
                    initialStructuralBlockCount,
                    structural.Count
                );

            connectedBlockCount =
                structural.Count;

            initialized =
                true;

            coreFailureHandled =
                false;

            Debug.Log(
                "[Pack 18] Structural Core initialized at grid " +
                coreBlock.LogicalGridPosition +
                " with " +
                structural.Count +
                " structural block(s)."
            );
        }

        private P12_PipelineSafePlayerBlock
            FindBestCore(
                List<
                    P12_PipelineSafePlayerBlock
                > blocks)
        {
            P12_PipelineSafePlayerBlock exact =
                null;

            P12_PipelineSafePlayerBlock best =
                null;

            int bestScore =
                int.MaxValue;

            foreach (
                P12_PipelineSafePlayerBlock block
                in blocks)
            {
                if (block == null)
                    continue;

                Vector3Int p =
                    block.LogicalGridPosition;

                if (p ==
                    Vector3Int.zero)
                {
                    exact =
                        block;

                    break;
                }

                int score =
                    Mathf.Abs(p.x) * 4 +
                    Mathf.Abs(p.z) * 4 +
                    Mathf.Abs(p.y) * 7;

                if (score <
                    bestScore)
                {
                    bestScore =
                        score;

                    best =
                        block;
                }
            }

            return exact != null
                ? exact
                : best;
        }

        private void BuildCurrentGrid()
        {
            grid.Clear();

            P12_PipelineSafePlayerBlock[] all =
                GetComponentsInChildren<
                    P12_PipelineSafePlayerBlock
                >(true);

            foreach (
                P12_PipelineSafePlayerBlock block
                in all)
            {
                if (block == null ||
                    block.DetachedFromVehicle)
                    continue;

                if (!includeTurretShellInGraph &&
                    IsUnderNamedAncestor(
                        block.transform,
                        "Turret_Yaw"
                    ))
                {
                    continue;
                }

                if (!block.LogicalGridAssigned)
                {
                    Vector3 local =
                        transform.InverseTransformPoint(
                            block.transform.position
                        );

                    block.AssignLogicalGrid(
                        new Vector3Int(
                            Mathf.RoundToInt(
                                local.x / gridSize
                            ),
                            Mathf.RoundToInt(
                                local.y / gridSize
                            ),
                            Mathf.RoundToInt(
                                local.z / gridSize
                            )
                        )
                    );
                }

                foreach(var p in OccupiedGridCells(block))
                {
                    if(grid.ContainsKey(p))
                    {
                        Debug.LogWarning("[Pack 18] Duplicate structural grid coordinate "+p+". Keeping the first block.");
                        continue;
                    }
                    grid.Add(p,block);
                }
            }
        }

        private static IEnumerable<Vector3Int> OccupiedGridCells(P12_PipelineSafePlayerBlock block)
        {
            var ports=block.GetComponent<RoboRougelike.R3.R3BlockMountPoints>();
            if(ports==null){yield return block.LogicalGridPosition;yield break;}
            foreach(var offset in ports.GridOffsets)yield return block.LogicalGridPosition+offset;
        }

        private void FloodFillFromCore(
            Vector3Int start)
        {
            Queue<Vector3Int> queue =
                new Queue<Vector3Int>();

            connected.Add(
                start
            );

            queue.Enqueue(
                start
            );

            while (queue.Count > 0)
            {
                Vector3Int current =
                    queue.Dequeue();

                foreach (
                    Vector3Int offset
                    in Neighbours)
                {
                    Vector3Int next =
                        current +
                        offset;

                    if (connected.Contains(
                        next
                    ))
                    {
                        continue;
                    }

                    if(!grid.TryGetValue(next,out var nextBlock)||
                       !RoboRougelike.R3.R3MountingGrid.Compatible(grid[current],nextBlock,offset))
                        continue;

                    connected.Add(
                        next
                    );

                    queue.Enqueue(
                        next
                    );
                }
            }
        }

        private void DetachDisconnectedComponents(
            List<
                P12_PipelineSafePlayerBlock
            > disconnectedBlocks)
        {
            Dictionary<
                Vector3Int,
                P12_PipelineSafePlayerBlock
            > disconnectedGrid =
                new Dictionary<
                    Vector3Int,
                    P12_PipelineSafePlayerBlock
                >();

            foreach (
                P12_PipelineSafePlayerBlock block
                in disconnectedBlocks)
            {
                if (block == null)
                    continue;

                foreach(var cell in OccupiedGridCells(block))disconnectedGrid[cell]=block;
            }

            HashSet<Vector3Int> handled =
                new HashSet<Vector3Int>();

            foreach (
                KeyValuePair<
                    Vector3Int,
                    P12_PipelineSafePlayerBlock
                > entry
                in disconnectedGrid)
            {
                if (handled.Contains(
                    entry.Key
                ))
                {
                    continue;
                }

                List<
                    P12_PipelineSafePlayerBlock
                > component =
                    new List<
                        P12_PipelineSafePlayerBlock
                    >();

                Queue<Vector3Int> queue =
                    new Queue<Vector3Int>();

                handled.Add(
                    entry.Key
                );

                queue.Enqueue(
                    entry.Key
                );

                while (queue.Count > 0)
                {
                    Vector3Int current =
                        queue.Dequeue();

                    if (disconnectedGrid.TryGetValue(
                        current,
                        out P12_PipelineSafePlayerBlock block))
                    {
                        if(!component.Contains(block))component.Add(block);
                    }

                    foreach (
                        Vector3Int offset
                        in Neighbours)
                    {
                        Vector3Int next =
                            current +
                            offset;

                        if(handled.Contains(next)||!disconnectedGrid.TryGetValue(next,out var nextBlock)||
                           !RoboRougelike.R3.R3MountingGrid.Compatible(disconnectedGrid[current],nextBlock,offset))
                            continue;

                        handled.Add(
                            next
                        );

                        queue.Enqueue(
                            next
                        );
                    }
                }

                if (component.Count > 0)
                {
                    DetachAsChunk(
                        component,
                        false
                    );
                }
            }
        }

        private void DetachAsChunk(
            List<
                P12_PipelineSafePlayerBlock
            > blocks,
            bool coreFailure)
        {
            if (blocks == null ||
                blocks.Count == 0)
                return;

            Vector3 centroid =
                Vector3.zero;

            int validCount =
                0;

            foreach (
                P12_PipelineSafePlayerBlock block
                in blocks)
            {
                if (block == null)
                    continue;

                centroid +=
                    block.transform.position;

                validCount++;
            }

            if (validCount == 0)
                return;

            centroid /=
                validCount;

            var ownerBody=GetComponent<Rigidbody>();
            var inheritedVelocity=ownerBody!=null?ownerBody.GetPointVelocity(centroid):Vector3.zero;
            var inheritedSpin=ownerBody!=null?ownerBody.angularVelocity:Vector3.zero;
            float releasedMass=blocks.Where(b=>b!=null).Sum(b=>{
                var hp=b.GetComponent<P12PipelineSafeDamageable>();
                return hp!=null?RoboRougelike.R6.R6VehicleDrive.PartMass(hp):0f;
            });
            GameObject chunk =
                new GameObject(
                    coreFailure
                        ? "P18_CoreFailureChunk"
                        : "P18_DisconnectedChunk"
                );

            chunk.transform.position =
                centroid;

            chunk.transform.rotation =
                Quaternion.identity;

            foreach (
                P12_PipelineSafePlayerBlock block
                in blocks)
            {
                if (block == null)
                    continue;

                var damageable=block.GetComponent<P12PipelineSafeDamageable>();
                if(damageable!=null)RoboRougelike.R6.R6CombatFeedback.ModuleLost(damageable);
                block.MarkDetached();

                block.transform.SetParent(
                    chunk.transform,
                    true
                );
            }

            Rigidbody body =
                chunk.AddComponent<Rigidbody>();

            body.mass =
                Mathf.Max(
                    1f,
                    validCount *
                    massPerBlock
                );

            RoboRougelike.R6.R6DebrisRelease.Release(body,ownerBody,inheritedVelocity,inheritedSpin,releasedMass);

            body.interpolation =
                RigidbodyInterpolation.Interpolate;

            body.collisionDetectionMode =
                CollisionDetectionMode.Continuous;

            Vector3 outward =
                centroid -
                transform.position;

            outward.y =
                0f;

            if (outward.sqrMagnitude <
                0.001f)
            {
                outward =
                    transform.right;
            }

            outward.Normalize();

            // Heavy detached chunks receive mass-independent separation velocity.
            RoboRougelike.R6.R6DebrisRelease.Eject(body,ownerBody,outward,
                Mathf.Max(5f,detachOutImpulse*7f),Mathf.Max(2.5f,detachUpImpulse*2.5f));

            Vector3 localCentroid =
                transform.InverseTransformPoint(
                    centroid
                );

            Vector3 torque =
                new Vector3(
                    -localCentroid.z,
                    0.35f,
                    localCentroid.x
                );

            if (torque.sqrMagnitude >
                0.001f)
            {
                body.AddTorque(
                    torque.normalized *
                    0.85f,
                    ForceMode.Impulse
                );
            }

            detachedBlockCountTotal +=
                validCount;

            Destroy(
                chunk,
                detachedChunkLifetime
            );

            Debug.Log(
                "[Pack 18] Detached structural chunk: " +
                validCount +
                " block(s)."
            );
        }

        private void HandleCoreDestroyed()
        {
            if (coreFailureHandled)
                return;

            coreFailureHandled =
                true;

            List<
                P12_PipelineSafePlayerBlock
            > remaining =
                new List<
                    P12_PipelineSafePlayerBlock
                >();

            foreach (
                KeyValuePair<
                    Vector3Int,
                    P12_PipelineSafePlayerBlock
                > entry
                in grid)
            {
                if (entry.Value != null)
                    remaining.Add(
                        entry.Value
                    );
            }

            if (remaining.Count > 0)
            {
                DetachAsChunk(
                    remaining,
                    true
                );
            }

            P12PipelineSafeTankController controller =
                GetComponent<
                    P12PipelineSafeTankController
                >();

            if (controller != null)
                controller.enabled =
                    false;

            Collider rootCollider =
                GetComponent<Collider>();

            if (rootCollider != null)
                rootCollider.enabled =
                    false;

            Rigidbody rootBody =
                GetComponent<Rigidbody>();

            if (rootBody != null && !rootBody.isKinematic)
            {
#if UNITY_6000_0_OR_NEWER
                rootBody.linearVelocity =
                    Vector3.zero;
#else
                rootBody.velocity =
                    Vector3.zero;
#endif
                rootBody.angularVelocity =
                    Vector3.zero;

                rootBody.isKinematic =
                    true;
            }

            connectedBlockCount =
                0;

            Debug.LogWarning(
                "[Pack 18] CORE DESTROYED. Vehicle control disabled."
            );
        }

        private static bool IsUnderNamedAncestor(
            Transform child,
            string ancestorName)
        {
            Transform current =
                child != null
                    ? child.parent
                    : null;

            while (current != null)
            {
                if (current.name ==
                    ancestorName)
                {
                    return true;
                }

                current =
                    current.parent;
            }

            return false;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem(
            "Tools/Rogue Machine/[18] Structural Core/INSTALL OR REBUILD ON CURRENT TANK"
        )]
        private static void InstallOrRebuild()
        {
            P12PipelineSafeTankController controller =
                Object.FindFirstObjectByType<
                    P12PipelineSafeTankController
                >();

            if (controller == null)
            {
                Debug.LogError(
                    "[Pack 18] No player tank found in the current scene."
                );
                return;
            }

            P12PipelineSafeVehicleStructure structure =
                controller.GetComponent<
                    P12PipelineSafeVehicleStructure
                >();

            if (structure == null)
            {
                structure =
                    UnityEditor.Undo.AddComponent<
                        P12PipelineSafeVehicleStructure
                    >(
                        controller.gameObject
                    );
            }

            structure.RebuildNow();

            UnityEditor.EditorUtility.SetDirty(
                structure
            );

            UnityEditor.SceneManagement
                .EditorSceneManager
                .MarkSceneDirty(
                    UnityEngine.SceneManagement
                    .SceneManager
                    .GetActiveScene()
                );

            Debug.Log(
                "[Pack 18] Structural Core installed/rebuilt on current tank."
            );
        }

        [UnityEditor.MenuItem(
            "Tools/Rogue Machine/[18] Structural Core/DUMP STRUCTURE STATUS"
        )]
        private static void DumpStatus()
        {
            P12PipelineSafeVehicleStructure structure =
                Object.FindFirstObjectByType<
                    P12PipelineSafeVehicleStructure
                >();

            if (structure == null)
            {
                Debug.LogWarning(
                    "[Pack 18] Structural Core component is not present."
                );
                return;
            }

            Debug.Log(
                "[Pack 18 Status] CoreAlive=" +
                structure.CoreAlive +
                " | CoreGrid=" +
                structure.CoreGridPosition +
                " | Connected=" +
                structure.ConnectedBlockCount +
                " | Initial=" +
                structure.InitialStructuralBlockCount +
                " | DetachedTotal=" +
                structure.DetachedBlockCountTotal
            );
        }
#endif
    }
}
