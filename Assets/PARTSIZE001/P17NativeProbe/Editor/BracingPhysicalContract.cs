// B01-23 P17 W03. Source-compiled exact braced physical-layout contract.
// Unity adapter is Editor-only and MUST use actual assigned prefabs.
// A logical validation result NEVER means measured collider, socket or G1 PASS.
using System;
using System.Collections.Generic;
using System.Linq;
using B01.PARTSIZE001;

namespace B01.PARTSIZE001.P17Probe
{
    [Serializable] public sealed class Int3
    {
        public int x,y,z;
        public Cell3 AsCell() => new Cell3(x,y,z);
    }
    [Serializable] public sealed class RigPart
    {
        public string localId;
        public string kind;
        public Int3 cell;
        public Int3 rotationQuarterTurns;
        public int contactMask;
    }
    [Serializable] public sealed class PhysicalRig
    {
        public string schema;
        public string blueprintId;
        public string sourcePartSizeCommit;
        public string nativePhysicalStatus;
        public RigPart[] parts;
    }
    [Serializable] public sealed class Binding
    {
        public string kind;
        public string assetPath;
        public string assetGuid;
    }
    [Serializable] public sealed class Intake
    {
        public string schema;
        public string originalBracingSha;
        public string nativeEvidence;
        public PhysicalRig[] rigs;
        public Binding[] bindings;
    }

    public static class BracingPhysicalContract
    {
        public const string OriginalProducerSha="5666a39eabd4cea91b2b605a9eb99ea2f1e226e8";
        public const string FrozenPartsizeSha="be2be0e4802bc3f450179901f9ae91c6d84bcb5b";
        public const string SourceSchema="B01-PARTSIZE-P17-RIG-BLUEPRINT-v1";
        public const string IntakeSchema="B01-P17-W03-NATIVE-PROBE-INTAKE-v1";
        public const string SourceState="UNMEASURED_COLLIDER_AND_SOCKET";
        public const string ReportState="NOT_RUN";
        public static readonly string[] RigNames={
            "P17-FLANKER-WHEELS-REINFORCED",
            "P17-BULWARK-TRACKS-REINFORCED",
            "P17-SKIRMISHER-HOVER-REINFORCED"
        };
        public static readonly string[] Kinds={
            "Armor","Core","CrawlerTrack","Gun","HoverEngine","Wheel"
        };
        private static readonly int[] Counts={15,16,12};
        private static readonly string[] AddedArmor={
            "P17-BRACE-FLANKER-001","P17-BRACE-BULWARK-L",
            "P17-BRACE-BULWARK-R","P17-BRACE-SKIRMISHER-LOW",
            "P17-BRACE-SKIRMISHER-TOP"
        };
        private static readonly Cell3[] Directions={
            new Cell3(1,0,0),new Cell3(-1,0,0),
            new Cell3(0,1,0),new Cell3(0,-1,0),
            new Cell3(0,0,1),new Cell3(0,0,-1)
        };
        private static bool Guid32(string s)=>
            s!=null&&s.Length==32&&s.All(ch=>
                (ch>='0'&&ch<='9')||(ch>='a'&&ch<='f'));

        public static string InputError(Intake data)
        {
            if(data==null||data.schema!=IntakeSchema||
                data.originalBracingSha!=OriginalProducerSha||
                data.nativeEvidence!=ReportState||
                data.rigs==null||data.rigs.Length!=3)
                return "UNTRUSTED_INTAKE_OR_FALSE_NATIVE_PASS";
            var allOwners=new HashSet<string>(StringComparer.Ordinal);
            var actualKinds=new HashSet<string>(StringComparer.Ordinal);
            int braceOwners=0,allParts=0;
            for(int ri=0;ri<data.rigs.Length;ri++)
            {
                PhysicalRig rig=data.rigs[ri];
                if(rig==null||rig.schema!=SourceSchema||rig.blueprintId!=RigNames[ri]||
                    rig.sourcePartSizeCommit!=FrozenPartsizeSha||
                    rig.nativePhysicalStatus!=SourceState||
                    rig.parts==null||rig.parts.Length!=Counts[ri])
                    return "FROZEN_RIG_PROVENANCE_OR_OWNER_COUNT";
                int cores=0;
                var ownerByCell=new Dictionary<Cell3,int>();
                for(int p=0;p<rig.parts.Length;p++)
                {
                    RigPart part=rig.parts[p];
                    if(part==null||string.IsNullOrEmpty(part.localId)||
                       !allOwners.Add(part.localId)||part.cell==null||
                       part.rotationQuarterTurns==null||
                       part.contactMask!=63||!Kinds.Contains(part.kind))
                        return "UNTRUSTED_OR_DUPLICATE_PHYSICAL_OWNER";
                    actualKinds.Add(part.kind);
                    if(part.kind=="Core")cores++;
                    if(AddedArmor.Contains(part.localId))
                    {
                        if(part.kind!="Armor")return "BRACE_NOT_ARMOR";
                        braceOwners++;
                    }
                    if(part.rotationQuarterTurns.x!=0||
                        part.rotationQuarterTurns.y!=0||
                        part.rotationQuarterTurns.z!=0)
                        return "UNAUTHORISED_UNMEASURED_ROTATION";
                    var definition=PartSizeBaselineCatalog.ForSourceKind(part.kind);
                    foreach(Cell3 c in PartSizeGrid.Occupied(
                        definition,part.cell.AsCell(),part.rotationQuarterTurns.AsCell()))
                        if(!ownerByCell.TryAdd(c,p))
                            return "LOGICAL_GRID_OVERLAP";
                    allParts++;
                }
                if(cores!=1)return "MISSING_OR_DUPLICATE_CORE";
                var coreIndex=Array.FindIndex(rig.parts,p=>p.kind=="Core");
                var visited=new HashSet<int>{coreIndex};
                var queue=new Queue<int>();
                queue.Enqueue(coreIndex);
                while(queue.Count>0)
                {
                    int cur=queue.Dequeue();
                    var part=rig.parts[cur];
                    var def=PartSizeBaselineCatalog.ForSourceKind(part.kind);
                    foreach(Cell3 own in PartSizeGrid.Occupied(
                        def,part.cell.AsCell(),part.rotationQuarterTurns.AsCell()))
                        foreach(Cell3 direction in Directions)
                        {
                            int next;
                            if(ownerByCell.TryGetValue(own+direction,out next)&&
                               visited.Add(next))queue.Enqueue(next);
                        }
                }
                if(visited.Count!=rig.parts.Length)
                    return "DISCONNECTED_UNMEASURED_OWNER";
            }
            if(allParts!=43||braceOwners!=5||
               !actualKinds.SetEquals(Kinds))
                return "WRONG_AUTHORED_BRACE_SOURCES";
            if(data.bindings==null||data.bindings.Length!=Kinds.Length)
                return "MISSING_ACTUAL_PREFAB_BINDINGS";
            var covered=new HashSet<string>(StringComparer.Ordinal);
            var guids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var binding in data.bindings)
            {
                if(binding==null||!Kinds.Contains(binding.kind)||
                   !covered.Add(binding.kind)||
                   string.IsNullOrWhiteSpace(binding.assetPath)||
                   !binding.assetPath.StartsWith("Assets/",StringComparison.Ordinal)||
                   !binding.assetPath.EndsWith(".prefab",StringComparison.OrdinalIgnoreCase)||
                   binding.assetPath.Contains("..")||
                   binding.assetPath.Contains('\\')||
                   !Guid32(binding.assetGuid)||!guids.Add(binding.assetGuid))
                    return "MISSING_OR_UNTRUSTED_UNITY_PREFAB";
            }
            return covered.SetEquals(Kinds)?null:"PREFAB_KIND_MISMATCH";
        }
    }
}
