using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace B01.PARTSIZE001.Tests
{
    public sealed class PartSize001EditModeTests
    {
        [Test] public void LegacyCubeSourceDimensionsFitOneGridCell()
        {
            var d=PartSizeReferenceFixtures.LegacyCube();
            Assert.AreEqual(null,PartSizeGrid.EnvelopeError(d,new Cell3(0,0,0),new Cell3(0,0,0)));
            Assert.AreEqual(1,PartSizeGrid.Occupied(d,new Cell3(0,0,0),new Cell3(0,0,0)).Length);
        }
        [Test] public void LegacyWideFourCellsHaveOneOwnerAndReal107CmHull()
        {
            var d=PartSizeReferenceFixtures.LegacyWide();
            Assert.AreEqual(null,PartSizeGrid.EnvelopeError(d,new Cell3(0,0,0),new Cell3(0,0,0)));
            Assert.AreEqual(4,PartSizeGrid.Occupied(d,new Cell3(0,0,0),new Cell3(0,0,0)).Length);
            var shape=PartSizeGrid.PhysicalBounds(d,new Cell3(0,0,0),new Cell3(0,0,0));
            Assert.That(shape.Max.X-shape.Min.X,Is.EqualTo(1.07).Within(.000001));
            Assert.That(shape.Max.Z-shape.Min.Z,Is.EqualTo(1.07).Within(.000001));
        }
        [TestCase(0,0,0)]
        [TestCase(0,1,0)]
        [TestCase(0,2,0)]
        [TestCase(0,3,0)]
        [TestCase(1,0,0)]
        [TestCase(0,0,1)]
        [TestCase(1,2,3)]
        public void AllXYZQuarterTurnFixturesFitTheSamePhysicalEnvelope(int x,int y,int z)
        {
            var d=PartSizeReferenceFixtures.LegacyWide();
            var anchor=new Cell3(3,1,-4);
            var turns=new Cell3(x,y,z);
            Assert.IsNull(PartSizeGrid.EnvelopeError(d,anchor,turns));
            var occupied=PartSizeGrid.Occupied(d,anchor,turns);
            Assert.AreEqual(4,new HashSet<Cell3>(occupied).Count);
        }
        [Test] public void WideInteriorFacesAreNotRealExternalMountPoints()
        {
            var d=PartSizeReferenceFixtures.LegacyWide();
            Assert.IsFalse(PartSizeGrid.IsExposedFace(d,new Cell3(0,0,0),new Cell3(1,0,0)));
            Assert.IsTrue(PartSizeGrid.IsExposedFace(d,new Cell3(0,0,0),new Cell3(-1,0,0)));
            Assert.IsFalse(PartSizeGrid.IsExposedFace(d,new Cell3(4,0,0),new Cell3(1,0,0)));
        }
        [Test] public void RotatedLogicalCellsActuallyBlockAnExistingOwner()
        {
            var d=PartSizeReferenceFixtures.LegacyWide();
            var a=PartSizeGrid.Occupied(d,new Cell3(0,0,0),new Cell3(0,1,0));
            var b=PartSizeGrid.Occupied(PartSizeReferenceFixtures.LegacyCube(),
                new Cell3(0,0,-1),new Cell3(0,0,0));
            Assert.IsTrue(PartSizeGrid.Overlaps(a,b));
            b=PartSizeGrid.Occupied(PartSizeReferenceFixtures.LegacyCube(),
                new Cell3(9,0,0),new Cell3(0,0,0));
            Assert.IsFalse(PartSizeGrid.Overlaps(a,b));
        }
        [Test] public void PhysicalBoxCannotClaimOneCellFor120CmHull()
        {
            var d=new PartSizeDefinition("Oversize",new Cell3(1,1,1),
                new Meters3(0,0,0),new Meters3(1.2,.52,.52),"TEST");
            Assert.AreEqual("PHYSICAL_ENVELOPE_OUTSIDE_OWNED_CELLS",
                PartSizeGrid.EnvelopeError(d,new Cell3(0,0,0),new Cell3(0,1,0)));
        }
        [Test] public void NoRealColliderMeasurementsNeverTurnIntoFakeApproval()
        {
            var d=PartSizeReferenceFixtures.Unmeasured("Wheel",new Cell3(1,1,1));
            Assert.AreEqual("UNMEASURED_PHYSICAL_ENVELOPE",
                PartSizeGrid.EnvelopeError(d,new Cell3(0,0,0),new Cell3(0,0,0)));
            Assert.AreEqual(1,PartSizeGrid.Occupied(d,new Cell3(0,0,0),
                new Cell3(0,0,0)).Length);
        }
        [Test] public void RejectNaNInfinityIncompleteOrNonpositivePhysicalHull()
        {
            var pos=new Meters3(0,0,0);
            foreach(var size in new[]{new Meters3(double.NaN,1,1),
                new Meters3(double.PositiveInfinity,1,1),new Meters3(0,1,1)})
            {
                var d=new PartSizeDefinition("Bad",new Cell3(1,1,1),pos,size,"TEST");
                Assert.AreEqual("INVALID_PHYSICAL_ENVELOPE",
                    PartSizeGrid.EnvelopeError(d,new Cell3(0,0,0),new Cell3(0,0,0)));
            }
            var half=new PartSizeDefinition("Incomplete",new Cell3(1,1,1),
                new Meters3(0,0,0),null,"TEST");
            Assert.AreEqual("INCOMPLETE_PHYSICAL_ENVELOPE",
                PartSizeGrid.EnvelopeError(half,new Cell3(0,0,0),new Cell3(0,0,0)));
        }
        [Test] public void InvalidHugeGridAllocationIsNeverAccepted()
        {
            var d=new PartSizeDefinition("Huge",new Cell3(int.MaxValue,1,int.MaxValue),
                new Meters3(0,0,0),new Meters3(1,1,1),"TEST");
            Assert.AreEqual("INVALID_LOGICAL_FOOTPRINT",
                PartSizeGrid.EnvelopeError(d,new Cell3(0,0,0),new Cell3(0,0,0)));
        }
        [Test] public void CanonicalR3NineteenTypesNeverInferWheelOrGunPhysicalExtent()
        {
            Assert.AreEqual(19,PartSizeBaselineCatalog.CanonicalNames.Length);
            foreach(var id in new[]{"Wheel","Gun","Suspension","CrawlerTrack","HoverEngine"})
            {
                var d=PartSizeBaselineCatalog.ForSourceKind(id);
                Assert.AreEqual("UNMEASURED_PHYSICAL_ENVELOPE",
                    PartSizeGrid.EnvelopeError(d,new Cell3(0,0,0),new Cell3(0,0,0)));
            }
            foreach(var id in new[]{"BonusModule","DirectionalShield","PointDefense",
                "StructuralScanner","ScannerPulse","IndependentSteering"})
            {
                var d=PartSizeBaselineCatalog.ForSourceKind(id);
                Assert.AreEqual(4,PartSizeGrid.Occupied(d,
                    new Cell3(0,0,0),new Cell3(0,0,0)).Length);
                Assert.IsNull(PartSizeGrid.EnvelopeError(d,new Cell3(0,0,0),
                    new Cell3(0,0,0)));
            }
        }
        [Test] public void RotationPeriod4AndNegativeTurnsRemainEquivalent()
        {
            var d=PartSizeReferenceFixtures.LegacyWide();
            var a=PartSizeGrid.Occupied(d,new Cell3(1,2,3),new Cell3(0,-1,0));
            var b=PartSizeGrid.Occupied(d,new Cell3(1,2,3),new Cell3(0,3,0));
            CollectionAssert.AreEquivalent(a,b);
        }
    }
}
