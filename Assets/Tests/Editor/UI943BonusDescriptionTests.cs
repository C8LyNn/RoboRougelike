using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;

public sealed class UI943BonusDescriptionTests
{
    [Test] public void LoadedBonusDescriptionsMatchTheActualSingleCellReward()
    {
        var upgrades=Resources.LoadAll<R1UpgradeDefinition>("R1/Upgrades");
        Assert.That(upgrades.Select(u=>u.Id),Is.EquivalentTo(new[]{"armor","core","damage","mobility","reload","repair"}),"Sprawdź rzeczywiście wczytane zasoby nagród.");
        Assert.That(R3VehicleModel.UsesWideFootprint(R3PartKind.BonusModule),Is.False);
        foreach(var upgrade in upgrades)
        {
            Assert.That(upgrade.Description,Does.Contain("1×1×1"),upgrade.Id);
            Assert.That(upgrade.Description,Does.Not.Contain("1×2×2"),upgrade.Id);
            var reward=R3VehicleModel.CreateBonusReward(upgrade);
            Assert.That(reward,Is.Not.Null,upgrade.Id);
            Assert.That(reward.UpgradeDescription,Is.EqualTo(upgrade.Description),upgrade.Id);
            foreach(var turns in new[]{Vector3Int.zero,Vector3Int.right,Vector3Int.up,Vector3Int.forward})
            {
                var anchor=new Vector3Int(2,3,-1);var cells=R3VehicleModel.OccupiedCells(anchor,reward.Kind,turns).ToArray();
                Assert.That(cells,Is.EqualTo(new[]{anchor}),$"{upgrade.Id}, obrót {turns}");
            }
        }
    }
}
