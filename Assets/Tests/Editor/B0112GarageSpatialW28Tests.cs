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
    [Test] public void BonusPartOccupiesOneLogicalCell()
    {
        Assert.That(R3VehicleModel.OccupiedCells(Vector3Int.zero,R3PartKind.BonusModule,new Vector3Int(0,1,0)).Count(),Is.EqualTo(1));
    }
    [Test] public void InvalidMountRemainsRedIndependentOfPurpleOrOrangeWarnings()
    {
        Assert.That(B0112GarageSpatialOverlay.CandidateTint(true,"Fizyczny obrys części przenika istniejący collider."),
            Is.EqualTo(B0112ZoneKind.OccupiedRed));
    }
    [Test] public void SixZoneKindsKeepStableReceiverColors()
    {
        Assert.That(System.Enum.GetValues(typeof(B0112ZoneKind)).Length,Is.EqualTo(6));
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
