// Authored real Unity EditMode tests for B01-12 spatial readout.
// G1 status remains NOT_RUN until an activated Unity Editor actually executes them.
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoboRougelike.B01.GarageSpatialW28;
using RoboRougelike.R3;

public sealed class B0112GarageSpatialW28Tests
{
    [Test] public void CanonicalGridIs055Meters()
    {
        Assert.That(B0112GarageSpatialOverlay.CellMeters,Is.EqualTo(.55f));
    }
    [Test] public void PreviewGreenOnlyFromExistingValidator()
    {
        Assert.That(B0112GarageSpatialOverlay.CandidateTint(true,null),Is.EqualTo(B0112ZoneKind.ValidGreen));
    }
    [Test] public void PreviewRedForActualHostRejection()
    {
        Assert.That(B0112GarageSpatialOverlay.CandidateTint(true,"occupied"),Is.EqualTo(B0112ZoneKind.OccupiedRed));
    }
    [Test] public void PreviewRedWithoutTarget()
    {
        Assert.That(B0112GarageSpatialOverlay.CandidateTint(false,null),Is.EqualTo(B0112ZoneKind.OccupiedRed));
    }
    [Test] public void WidePartStillOneItemFourLogicalCells()
    {
        Assert.That(R3VehicleModel.OccupiedCells(Vector3Int.zero,R3PartKind.BonusModule,new Vector3Int(0,1,0)).Count(),Is.EqualTo(4));
    }
    [Test] public void InvalidMountRemainsRedIndependentOfPurpleOrOrangeWarnings()
    {
        Assert.That(B0112GarageSpatialOverlay.CandidateTint(true,"Fizyczny obrys części przenika istniejący collider."),
            Is.EqualTo(B0112ZoneKind.OccupiedRed));
    }
    [Test] public void FiveZoneKindsKeepStableReceiverColors()
    {
        Assert.That(System.Enum.GetValues(typeof(B0112ZoneKind)).Length,Is.EqualTo(5));
    }
    [Test] public void RotatingSawGhostUsesSelectedWeaponNotMountedPartComponent()
    {
        Assert.That(B0112GarageSpatialOverlay.NeedsSawDiscZone(R3PartKind.Gun,RoboRougelike.R4.R4WeaponKind.RotarySaw),Is.True);
        Assert.That(B0112GarageSpatialOverlay.NeedsSawDiscZone(R3PartKind.Gun,RoboRougelike.R4.R4WeaponKind.Basic),Is.False);
        Assert.That(B0112GarageSpatialOverlay.NeedsSawDiscZone(R3PartKind.Armor,RoboRougelike.R4.R4WeaponKind.RotarySaw),Is.False);
    }
    [Test] public void RotatingSawZoneUsesDefaultRealDiscDimensionsAndMuzzlePose()
    {
        var go=new GameObject("saw-preview-ghost-no-mounted-part");
        try
        {
            go.transform.position=new Vector3(2,3,4);
            go.transform.rotation=Quaternion.Euler(0,30,0);
            var zone=B0112GarageSpatialOverlay.SawDiscHint(go.transform);
            Assert.That(go.GetComponent<R3MountedPart>(),Is.Null);
            Assert.That(zone.Kind,Is.EqualTo(B0112ZoneKind.MechanismOrange));
            Assert.That(zone.Position,Is.EqualTo(go.transform.position));
            Assert.That(zone.Rotation,Is.EqualTo(go.transform.rotation));
            Assert.That(zone.Size.x,Is.EqualTo(.28f).Within(.0001f));
            Assert.That(zone.Size.y,Is.EqualTo(1.8f).Within(.0001f));
            Assert.That(zone.Size.z,Is.EqualTo(1.8f).Within(.0001f));
        }
        finally {Object.DestroyImmediate(go);}
    }
    [Test] public void EditorAwakeLeavesNoTransientPrimitiveBehind()
    {
        int before=Object.FindObjectsOfType<MeshFilter>(true).Length;
        var go=new GameObject("W30-overlay-editor-cleanup");
        try
        {
            go.AddComponent<B0112GarageSpatialOverlay>();
            Assert.That(Object.FindObjectsOfType<MeshFilter>(true).Length,Is.EqualTo(before));
        }
        finally {Object.DestroyImmediate(go);}
    }
    [Test] public void ActualFunctionalEnvelopeCreatesOrangeZoneForUninstalledSawGhost()
    {
        var holder=new GameObject("B0112-real-envelope-test");
        var ghost=new GameObject("uninstalled-saw-preview");
        var muzzle=new GameObject("Środek piły");
        try
        {
            muzzle.transform.SetParent(ghost.transform,false);
            var overlay=holder.AddComponent<B0112GarageSpatialOverlay>();
            var type=typeof(B0112GarageSpatialOverlay);
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var method=type.GetMethod("AddFunctionalEnvelope",flags);
            var field=type.GetField("boxes",flags);
            Assert.That(method,Is.Not.Null);
            Assert.That(field,Is.Not.Null);
            Assert.That(ghost.GetComponent<R3MountedPart>(),Is.Null);
            method.Invoke(overlay,new object[]{ghost.transform,R3PartKind.Gun,
                RoboRougelike.R4.R4WeaponKind.RotarySaw});
            var boxes=(System.Collections.Generic.List<B0112ZoneBox>)field.GetValue(overlay);
            Assert.That(boxes.Any(z=>z.Kind==B0112ZoneKind.MechanismOrange),Is.True,
                "A preview ghost must show the saw disc even before it is mounted.");
            boxes.Clear();
            method.Invoke(overlay,new object[]{ghost.transform,R3PartKind.Gun,
                RoboRougelike.R4.R4WeaponKind.Basic});
            Assert.That(boxes.Any(z=>z.Kind==B0112ZoneKind.MechanismOrange),Is.False,
                "Basic projectile gun must not inherit the saw motor zone.");
        }
        finally {Object.DestroyImmediate(muzzle);Object.DestroyImmediate(ghost);Object.DestroyImmediate(holder);}
    }
    [Test] public void ProjectileGunOnlyOwnsDirectionalMuzzleVisual()
    {
        Assert.That(B0112GarageSpatialOverlay.NeedsDirectionalMuzzleZone(
            R3PartKind.Gun,RoboRougelike.R4.R4WeaponKind.Basic),Is.True);
        Assert.That(B0112GarageSpatialOverlay.NeedsDirectionalMuzzleZone(
            R3PartKind.Gun,RoboRougelike.R4.R4WeaponKind.RotarySaw),Is.False);
        Assert.That(B0112GarageSpatialOverlay.NeedsDirectionalMuzzleZone(
            R3PartKind.Gun,RoboRougelike.R4.R4WeaponKind.EmpEmitter),Is.False);
        Assert.That(B0112GarageSpatialOverlay.NeedsDirectionalMuzzleZone(
            R3PartKind.Armor,RoboRougelike.R4.R4WeaponKind.Basic),Is.False);
    }
    [Test] public void ActualEmpGhostShowsSourceMarkerButNeverDirectionalShotLine()
    {
        var holder=new GameObject("emp-overlay-test");
        var ghost=new GameObject("emp-unmounted-ghost");
        var muzzle=new GameObject("Wylot");
        try
        {
            muzzle.transform.SetParent(ghost.transform,false);
            var overlay=holder.AddComponent<B0112GarageSpatialOverlay>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var method=typeof(B0112GarageSpatialOverlay).GetMethod("AddFunctionalEnvelope",flags);
            var field=typeof(B0112GarageSpatialOverlay).GetField("boxes",flags);
            Assert.That(ghost.GetComponent<R3MountedPart>(),Is.Null);
            method.Invoke(overlay,new object[]{ghost.transform,R3PartKind.Gun,
                RoboRougelike.R4.R4WeaponKind.EmpEmitter});
            var volumes=(System.Collections.Generic.List<B0112ZoneBox>)field.GetValue(overlay);
            Assert.That(volumes.Count,Is.EqualTo(1),"Radial EMP must not inherit projectile corridor.");
            Assert.That(volumes[0].Kind,Is.EqualTo(B0112ZoneKind.WeaponPurple));
            Assert.That(volumes[0].Position,Is.EqualTo(ghost.transform.position));
            Assert.That(volumes[0].Size,Is.EqualTo(Vector3.one*.55f));
            Assert.That(volumes.Any(z=>z.Kind==B0112ZoneKind.MechanismOrange),Is.False);
        }
        finally {Object.DestroyImmediate(muzzle);Object.DestroyImmediate(ghost);Object.DestroyImmediate(holder);}
    }
    [Test] public void ActualSawGhostDrawsOnlyOrangeDiscWhileBasicGunKeepsTwoPurpleHints()
    {
        var holder=new GameObject("saw-direction-test");
        var ghost=new GameObject("weapon-unmounted-ghost");
        var sawMuzzle=new GameObject("Środek piły");
        var gunMuzzle=new GameObject("Wylot");
        try
        {
            sawMuzzle.transform.SetParent(ghost.transform,false);
            gunMuzzle.transform.SetParent(ghost.transform,false);
            var overlay=holder.AddComponent<B0112GarageSpatialOverlay>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var method=typeof(B0112GarageSpatialOverlay).GetMethod("AddFunctionalEnvelope",flags);
            var field=typeof(B0112GarageSpatialOverlay).GetField("boxes",flags);
            method.Invoke(overlay,new object[]{ghost.transform,R3PartKind.Gun,
                RoboRougelike.R4.R4WeaponKind.RotarySaw});
            var volumes=(System.Collections.Generic.List<B0112ZoneBox>)field.GetValue(overlay);
            Assert.That(volumes.Count(z=>z.Kind==B0112ZoneKind.MechanismOrange),Is.EqualTo(1));
            Assert.That(volumes.Any(z=>z.Kind==B0112ZoneKind.WeaponPurple),Is.False);
            volumes.Clear();
            method.Invoke(overlay,new object[]{ghost.transform,R3PartKind.Gun,
                RoboRougelike.R4.R4WeaponKind.Basic});
            Assert.That(volumes.Count(z=>z.Kind==B0112ZoneKind.WeaponPurple),Is.EqualTo(2));
            Assert.That(volumes.Any(z=>z.Size.z>3f),Is.True);
            Assert.That(volumes.Any(z=>z.Kind==B0112ZoneKind.MechanismOrange),Is.False);
        }
        finally {Object.DestroyImmediate(sawMuzzle);Object.DestroyImmediate(gunMuzzle);
            Object.DestroyImmediate(ghost);Object.DestroyImmediate(holder);}
    }
    [Test] public void ReadoutHasNoPhysicsColliderAndStartsHidden()
    {
        var go=new GameObject("B0112-readout-test");
        try
        {
            var overlay=go.AddComponent<B0112GarageSpatialOverlay>();
            overlay.RebuildReadout(null);
            Assert.That(overlay.VisibleBoxCount,Is.Zero);
            Assert.That(overlay.SourceValidated,Is.False);
            Assert.That(overlay.LastReason,Is.Null);
            Assert.That(go.GetComponentsInChildren<Collider>(true),Is.Empty);
        }
        finally {Object.DestroyImmediate(go);}
    }
}
