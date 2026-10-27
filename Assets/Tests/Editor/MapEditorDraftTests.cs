using System;
using NUnit.Framework;
using UnityEngine;

public sealed class MapEditorDraftTests
{
    [Test] public void RuntimeMapDraftExistsForInGameEditing()
    {
        var type=typeof(RoboRougelike.R3.R3VehicleModel).Assembly.GetType("RoboRougelike.MapEditor.MapDraft");
        Assert.That(type,Is.Not.Null,"Map editing requires a real shared data model");
        Assert.That(Activator.CreateInstance(type),Is.Not.Null);
    }
}
