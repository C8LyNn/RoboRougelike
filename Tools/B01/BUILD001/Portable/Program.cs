// Portable compile + executable CONTRACT smoke tests on actual production C#.
// Uses explicitly fake UnityEngine math/physics, NOT Unity Editor or PhysX.
using System;
using System.Linq;
using B01.BUILD001;
using UnityEngine;

static class Program
{
    private static BuildDefinition Patch(string id,BuildRole role,
        Vector3Int normal,float half,float offsetX=0)
    {
        return new BuildDefinition(id,role,new[]{Vector3Int.zero},
            new[]{new BuildSocket(Vector3Int.zero,normal,
                BuildSocketKind.Structural,new Vector2(half,half),
                new Vector2(offsetX,0))},true);
    }

    private static int completed;
    private static BuildInstalledPart Part(string id,BuildDefinition def,int x) =>
        new BuildInstalledPart(id,def,new BuildPose(new Vector3Int(x,0,0),Vector3Int.zero));

    private static void Expect(bool ok,string text)
    { if(!ok)throw new Exception(text); }

    private static void Run(string name,Action run)
    {
        run();
        completed++;
        Console.WriteLine("SURROGATE_PASS " + name);
    }

    public static void Main()
    {
        Run("01_core_hub_positive",() => {
            Expect(BuildPlacementRules.Check(new[]{Part("core",BuildFixtureCatalog.Core(),0)},
                Part("wheel",BuildFixtureCatalog.Wheel(),1),out var p) == BuildPlacementResult.Valid &&
                p=="core","hub allowed");
        });

        Run("02_tread_negative",() => {
            Expect(BuildPlacementRules.Check(new[]{Part("core",BuildFixtureCatalog.Core(),0)},
                Part("wheel",BuildFixtureCatalog.Wheel(),2),out _) != BuildPlacementResult.Valid,
                "tread cannot be support");
        });

        Run("03_loaded_second_core_fail_closed",() => {
            var list=new[]{Part("coreA",BuildFixtureCatalog.Core(),0),
                Part("coreB",BuildFixtureCatalog.Core(),4)};
            Expect(BuildPlacementRules.Reachable(list).Count==0,"both roots must be quarantined");
            Expect(BuildPlacementRules.Check(list,Part("armor",BuildFixtureCatalog.ArmorCube(),5),
                out _)==BuildPlacementResult.MultipleCoreRoots,"must reject ambiguous graph");
        });

        Run("04_disconnected_island_fail_closed",() => {
            var list=new[]{Part("core",BuildFixtureCatalog.Core(),0),
                Part("orphan",BuildFixtureCatalog.ArmorCube(),4)};
            Expect(BuildPlacementRules.CheckForInstallation(list,
                Part("new",BuildFixtureCatalog.ArmorCube(),5),null,out _) ==
                BuildPlacementResult.CoreDisconnected,"orphan support not authoritative");
        });

        Run("05_disjoint_footprint_no_owner_bridge",() => {
            var def=new BuildDefinition("gap",BuildRole.Device,
                new[]{Vector3Int.zero,Vector3Int.right*2},
                BuildFixtureCatalog.ArmorCube().Sockets,true,
                new[]{new BuildCellBond(Vector3Int.zero,Vector3Int.right*2)});
            Expect(!BuildSocketGeometry.Valid(def,out _),"gap invalid");
            Expect(BuildPlacementRules.Check(new[]{Part("core",BuildFixtureCatalog.Core(),0)},
                Part("device",def,1),out _)==BuildPlacementResult.DisconnectedFootprint,
                "gap must be detected as footprint error");
        });

        Run("06_missing_joint_no_implicit_physics",() => {
            var def=new BuildDefinition("open",BuildRole.Device,
                new[]{Vector3Int.zero,Vector3Int.right,Vector3Int.right*2},
                BuildFixtureCatalog.ArmorCube().Sockets,true,
                new[]{new BuildCellBond(Vector3Int.zero,Vector3Int.right)});
            Expect(!BuildSocketGeometry.Valid(def,out _),"no implicit joint authority");
        });

        Run("07_legal_multicell_bonds_pass",() => {
            var wide=BuildFixtureCatalog.Wide2x2();
            Expect(BuildSocketGeometry.Valid(wide,out _),"three joins connect four voxels");
            Expect(BuildSocketGeometry.Occupied(wide,
                new BuildPose(Vector3Int.zero,new Vector3Int(0,1,0)))
                .Distinct().Count()==4,"rotated ownership must be unique");
        });

        Run("08_unverified_physics_refused",() => {
            Expect(BuildPlacementRules.CheckForInstallation(
                new[]{Part("core",BuildFixtureCatalog.Core(),0)},
                Part("armor",BuildFixtureCatalog.ArmorCube(),1),
                null,out _) == BuildPlacementResult.PhysicsUnverified,"missing evidence");
        });

        Run("09_synthetic_depth_031_gate",() => {
            var old=new GameObject();
            var candidate=new GameObject();
            old.transform.position=Vector3.zero;
            candidate.transform.position=new Vector3(.55f,0,0);
            var a=old.AddComponent<BoxCollider>();
            a.Half=new Vector3(.26f,.26f,.26f);
            var b=candidate.AddComponent<BoxCollider>();
            b.Half=new Vector3(.6f,.6f,.6f);
            var evidence=new BuildPhysicsEvidence(new[]{(Collider)b},new[]{(Collider)a},true);
            Expect(BuildVolumeValidator.Check(evidence)==BuildPlacementResult.ColliderPenetration,
                "synthetic 0.31 overlap must reject");
            Expect(BuildPlacementRules.CheckForInstallation(
                new[]{Part("core",BuildFixtureCatalog.Core(),0)},
                Part("device",BuildFixtureCatalog.ArmorCube(),1),evidence,out _) ==
                BuildPlacementResult.ColliderPenetration,"physical veto overrides grid");
            b.Half=new Vector3(.26f,.26f,.26f);
            Expect(BuildVolumeValidator.Check(evidence)==BuildPlacementResult.Valid,
                "synthetic zero overlap is accepted");
        });

        Run("10_strict_colliders_need_exact_socket_marker",() => {
            var go=new GameObject();
            var def=BuildFixtureCatalog.Core();
            var collider=go.AddComponent<BoxCollider>();
            var registry=new BuildSocketRegistry();
            Expect(!registry.Register(collider,go.transform,"core",def,0),
                "unmarked root must be refused");
            var marker=go.AddComponent<BuildMountSurface>();
            marker.PartInstanceId="core";marker.SocketIndex=0;
            Expect(registry.Register(collider,go.transform,"core",def,0),
                "trusted explicit marker accepted");
            var decorated=new GameObject();
            decorated.transform.parent=go.transform;
            var visual=decorated.AddComponent<BoxCollider>();
            Expect(!registry.Register(visual,go.transform,"core",def,0),
                "decorative descendant must be refused");
            var visualHit=new RaycastHit { collider=visual,point=new Vector3(.31f,0,0),
                normal=Vector3.right };
            Expect(BuildSocketRaycast.ValidateHit(visualHit,go.transform,"core",def,
                new BuildPose(Vector3Int.zero,Vector3Int.zero),Vector3.zero,registry,out _) ==
                BuildHitResult.DecorativeSurface,"visual ray rejected");
            var structuralHit=new RaycastHit { collider=collider,point=new Vector3(.26f,0,0),
                normal=Vector3.right };
            Expect(BuildSocketRaycast.ValidateHit(structuralHit,go.transform,"core",def,
                new BuildPose(Vector3Int.zero,Vector3Int.zero),Vector3.zero,registry,out var face) ==
                BuildHitResult.Valid && face==Vector3Int.right,"exact registered port accepted");
        });

        Run("11_typed_mobility_does_not_wildcard",() => {
            var def=new BuildDefinition("typed",BuildRole.Device,
                new[]{Vector3Int.zero},new[]{new BuildSocket(Vector3Int.zero,
                Vector3Int.left,BuildSocketKind.MobilityHub,new Vector2(.4f,.4f))},false);
            Expect(BuildPlacementRules.Check(new[]{Part("core",BuildFixtureCatalog.Core(),0)},
                Part("typed",def,1),out _)==BuildPlacementResult.NoCompatibleSocket,
                "mobility typed socket may not silently become structural");
        });

        Run("12_rc2_nested_tiny_denied",() => {
            var support=Patch("s",BuildRole.Core,Vector3Int.up,.48f);
            var candidate=Patch("c",BuildRole.Armor,Vector3Int.down,.02f);
            Expect(!BuildSocketGeometry.Touches(support,
                new BuildPose(Vector3Int.zero,Vector3Int.zero),candidate,
                new BuildPose(Vector3Int.up,Vector3Int.zero),out var area) &&
                area==0f, "tiny nested contact cannot borrow outer area");
        });

        Run("13_rc2_nested_medium_exact_area",() => {
            var support=Patch("s",BuildRole.Core,Vector3Int.up,.48f);
            var candidate=Patch("c",BuildRole.Armor,Vector3Int.down,.2f);
            Expect(BuildSocketGeometry.Touches(support,
                new BuildPose(Vector3Int.zero,Vector3Int.zero),candidate,
                new BuildPose(Vector3Int.up,Vector3Int.zero),out var area) &&
                MathF.Abs(area-.0484f)<.000001f,"nested physical area must be exact");
        });

        Run("14_rc2_nonoverlap_edge_denied",() => {
            var support=Patch("s",BuildRole.Core,Vector3Int.up,.2f,-.2f);
            var candidate=Patch("c",BuildRole.Armor,Vector3Int.down,.2f,.2f);
            Expect(!BuildSocketGeometry.Touches(support,
                new BuildPose(Vector3Int.zero,Vector3Int.zero),candidate,
                new BuildPose(Vector3Int.up,Vector3Int.zero),out _),
                "edge-only patch cannot attach");
        });

        Run("15_rc2_unequal_partial_area",() => {
            var support=new BuildDefinition("s",BuildRole.Core,new[]{Vector3Int.zero},
                new[]{new BuildSocket(Vector3Int.zero,Vector3Int.up,
                BuildSocketKind.Structural,new Vector2(.3f,.2f))},true);
            var candidate=new BuildDefinition("c",BuildRole.Armor,new[]{Vector3Int.zero},
                new[]{new BuildSocket(Vector3Int.zero,Vector3Int.down,
                BuildSocketKind.Structural,new Vector2(.25f,.15f),
                new Vector2(.2f,0f))},true);
            Expect(BuildSocketGeometry.Touches(support,
                new BuildPose(Vector3Int.zero,Vector3Int.zero),candidate,
                new BuildPose(Vector3Int.up,Vector3Int.zero),out var area) &&
                MathF.Abs(area-(.35f*.3f*.55f*.55f))<.000001f,
                "partial interval must intersect correctly");
        });

        Run("16_rc2_rotated_nesting",() => {
            var support=Patch("s",BuildRole.Core,Vector3Int.up,.48f);
            var candidate=Patch("c",BuildRole.Armor,Vector3Int.down,.2f);
            var turns=new Vector3Int(1,2,3);
            var anchor=BuildSocketGeometry.Rotate(Vector3Int.up,turns);
            Expect(BuildSocketGeometry.Touches(support,
                new BuildPose(Vector3Int.zero,turns),candidate,
                new BuildPose(anchor,turns),out var area) &&
                MathF.Abs(area-.0484f)<.000002f,
                "XYZ rotation should conserve true area");
        });

        Console.WriteLine("SURROGATE_CSHARP_PASS " + completed +
            "/16; Unity native=NOT_RUN; synthetic Physics is NOT PhysX");
    }
}
