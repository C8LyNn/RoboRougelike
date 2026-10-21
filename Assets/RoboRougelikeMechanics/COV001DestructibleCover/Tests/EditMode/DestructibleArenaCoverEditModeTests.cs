using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.COV001.Tests
{
    public sealed class DestructibleArenaCoverEditModeTests
    {
        private GameObject root;
        private BoxCollider blocker;
        private DestructibleArenaCover cover;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("COV001 test obstacle");
            blocker = root.AddComponent<BoxCollider>();
            cover = root.AddComponent<DestructibleArenaCover>();
            cover.ResetCover();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void StartsIntactWithFullDurabilityAndBlockingCollider()
        {
            Assert.AreEqual(50f, cover.MaxDurability);
            Assert.AreEqual(50f, cover.Durability);
            Assert.AreEqual(CoverDamageStage.Intact, cover.Stage);
            Assert.IsTrue(blocker.enabled);
        }

        [Test]
        public void RejectsInvalidDamageWithoutStateMutation()
        {
            Assert.IsFalse(cover.ApplyDamage(0f));
            Assert.IsFalse(cover.ApplyDamage(-1f));
            Assert.IsFalse(cover.ApplyDamage(float.NaN));
            Assert.IsFalse(cover.ApplyDamage(float.PositiveInfinity));
            Assert.IsFalse(cover.ApplyDamage(float.NegativeInfinity));
            Assert.AreEqual(50f, cover.Durability);
        }

        [Test]
        public void ReportsOrderedDamageStages()
        {
            var stages = new List<CoverDamageStage>();
            cover.StageChanged += stages.Add;
            Assert.IsTrue(cover.ApplyDamage(21f));
            Assert.IsTrue(cover.ApplyDamage(15f));
            Assert.IsTrue(cover.ApplyDamage(14f));
            CollectionAssert.AreEqual(new[] {
                CoverDamageStage.Damaged, CoverDamageStage.Critical,
                CoverDamageStage.Broken }, stages);
        }

        [Test]
        public void BrokenOnlyOnceAndResetAllowsAnotherCycle()
        {
            int brokenCount = 0;
            cover.BrokenOnce += () => brokenCount++;
            Assert.IsTrue(cover.ApplyDamage(100f));
            Assert.IsFalse(cover.ApplyDamage(100f));
            Assert.AreEqual(1, brokenCount);
            Assert.IsFalse(blocker.enabled);
            cover.ResetCover();
            Assert.IsTrue(blocker.enabled);
            Assert.IsTrue(cover.ApplyDamage(50f));
            Assert.AreEqual(2, brokenCount);
        }

        [Test]
        public void ResetPreservesInitiallyDisabledChildBlocker()
        {
            Object.DestroyImmediate(cover);
            var child = new GameObject("disabled blocker");
            child.transform.SetParent(root.transform, false);
            var disabled = child.AddComponent<BoxCollider>();
            disabled.enabled = false;
            cover = root.AddComponent<DestructibleArenaCover>();
            cover.ResetCover();

            Assert.IsTrue(cover.ApplyDamage(50f));
            Assert.IsFalse(blocker.enabled);
            Assert.IsFalse(disabled.enabled);
            cover.ResetCover();
            Assert.IsTrue(blocker.enabled);
            Assert.IsFalse(disabled.enabled);
        }

        [Test]
        public void BreakingOuterCoverDoesNotDisableInactiveNestedCoverCollider()
        {
            // Reinitialize the outer cover only after the nested hierarchy exists.
            Object.DestroyImmediate(cover);
            var nested = new GameObject("inactive nested cover");
            nested.transform.SetParent(root.transform, false);
            var grandchild = new GameObject("nested blocking geometry");
            grandchild.transform.SetParent(nested.transform, false);
            var nestedBlocker = grandchild.AddComponent<BoxCollider>();
            nested.AddComponent<DestructibleArenaCover>();
            nested.SetActive(false);

            cover = root.AddComponent<DestructibleArenaCover>();
            cover.ResetCover();
            Assert.IsTrue(nestedBlocker.enabled);

            Assert.IsTrue(cover.ApplyDamage(50f));
            Assert.IsFalse(blocker.enabled);
            Assert.IsTrue(nestedBlocker.enabled,
                "Outer cover must not disable colliders owned by inactive nested covers.");

            nested.SetActive(true);
            Assert.IsTrue(nestedBlocker.enabled);
            cover.ResetCover();
            Assert.IsTrue(blocker.enabled);
            Assert.IsTrue(nestedBlocker.enabled);
        }

        [Test]
        public void AcceptedDamageEventReportsClampedAmount()
        {
            float total = 0f;
            cover.DamageAccepted += value => total += value;
            Assert.IsTrue(cover.ApplyDamage(1000f));
            Assert.AreEqual(50f, total);
            Assert.AreEqual(0f, cover.Durability);
        }
    }
}
