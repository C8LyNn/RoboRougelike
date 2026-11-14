using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine;
using RogueMachine.P12PipelineSafe;

public sealed class BacklogControlProfileTests
{
    [Serializable] sealed class LegacyControlData { public int version=1; public string[] actions,bindings; }
    [Test] public void LegacyProfilesAndBlueprintBindingsGainConfigurableSteeringMode()
    {
        string path=Path.Combine(Path.GetTempPath(),"RRL-controls-legacy-"+Guid.NewGuid()+".json");
        try
        {
            var profile=new R1ControlProfile(path);var actions=R1ControlProfile.Actions.Take(20).ToArray();
            var bindings=actions.Select(profile.Binding).ToArray();bindings[0]="K";
            var data=new LegacyControlData{actions=actions,bindings=bindings};
            typeof(R1RunCheckpointStorage).GetMethod("WritePayload",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{path,JsonUtility.ToJson(data)});
            var loaded=new R1ControlProfile(path);Assert.That(loaded.Error,Is.Null);
            Assert.That(loaded.Binding("MoveForward"),Is.EqualTo("K"));Assert.That(loaded.Binding("SteeringMode"),Is.EqualTo("M"));
            Assert.That(loaded.Binding("CombatDetails"),Is.EqualTo("I"));
            Assert.That(R1ControlProfile.ValidSnapshot(bindings),Is.True,"Existing blueprints keep valid bindings.");
            Assert.That(loaded.TryBind("SteeringMode","P"),Is.True);loaded.Save();
            Assert.That(new R1ControlProfile(path).Binding("SteeringMode"),Is.EqualTo("P"));
            Assert.That(loaded.TryRestoreSnapshot(bindings,out _),Is.True);
            Assert.That(loaded.Binding("SteeringMode"),Is.EqualTo("M"));
            Assert.That(loaded.TryBind("Weapon1","M"),Is.False,"Mode binding participates in conflict detection.");
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
    [Test] public void ControlProfilePersistsBindingsAndRejectsConflicts()
    {
        var type=Type.GetType("RoboRougelike.R1.R1ControlProfile, Assembly-CSharp");
        Assert.That(type,Is.Not.Null,"A persistent input profile is needed.");
        string path=Path.Combine(Path.GetTempPath(),"RRL-controls-"+Guid.NewGuid()+".json");
        try
        {
            var profile=Activator.CreateInstance(type,new object[]{path});
            var bind=type.GetMethod("TryBind");var key=type.GetMethod("Binding");
            Assert.That((bool)bind.Invoke(profile,new object[]{"MoveForward","K"}),Is.True);
            Assert.That((bool)bind.Invoke(profile,new object[]{"MoveBackward","K"}),Is.False,"Conflicting inputs must not silently overwrite another action.");
            type.GetMethod("Save").Invoke(profile,null);
            var loaded=Activator.CreateInstance(type,new object[]{path});
            Assert.That(key.Invoke(loaded,new object[]{"MoveForward"}),Is.EqualTo("K"));
            Assert.That(key.Invoke(loaded,new object[]{"MoveBackward"}),Is.EqualTo("S"));
            type.GetMethod("ResetDefaults").Invoke(loaded,null);
            Assert.That(key.Invoke(loaded,new object[]{"MoveForward"}),Is.EqualTo("W"));
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
    [Test] public void IntermediateSnapshotPreservesSteeringAndAddsDefaultDetails()
    {
        string path=Path.Combine(Path.GetTempPath(),"RRL-controls-v21-"+Guid.NewGuid()+".json");
        try
        {
            var profile=new R1ControlProfile(path);Assert.That(profile.TryBind("SteeringMode","P"),Is.True);
            var old=profile.Snapshot().Take(R1ControlProfile.Actions.Length-1).ToArray();
            var earlier=profile.Snapshot().Take(21).ToArray();
            profile.ResetDefaults();Assert.That(profile.TryRestoreSnapshot(old,out _),Is.True);
            Assert.That(profile.Binding("SteeringMode"),Is.EqualTo("P"));Assert.That(profile.Binding("CombatDetails"),Is.EqualTo("I"));
            Assert.That(new R1ControlProfile(path).Binding("SteeringMode"),Is.EqualTo("P"));
            profile.ResetDefaults();Assert.That(profile.TryRestoreSnapshot(earlier,out _),Is.True);
            Assert.That(profile.Binding("SteeringMode"),Is.EqualTo("P"));Assert.That(profile.Binding("CombatDetails"),Is.EqualTo("I"));Assert.That(profile.Binding("ResetRange"),Is.EqualTo("R"));
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }

    [Test] public void UnreadableProfileIsNeverOverwritten()
    {
        string path=Path.Combine(Path.GetTempPath(),"RRL-controls-bad-"+Guid.NewGuid()+".json");
        try
        {
            File.WriteAllText(path,"corrupt");var profile=new R1ControlProfile(path);
            Assert.That(profile.Error,Is.Not.Null);Assert.Throws<InvalidOperationException>(()=>profile.Save());
            Assert.That(File.ReadAllText(path),Is.EqualTo("corrupt"));
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }

    [Test] public void ProfileReadsChangedKeyboardAndMouseBindings()
    {
        string path=Path.Combine(Path.GetTempPath(),"RRL-controls-input-"+Guid.NewGuid()+".json");
        var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
        try
        {
            var profile=new R1ControlProfile(path);
            Assert.That(profile.TryBind("MoveForward","K"),Is.True);
            Assert.That(profile.TryBind("Weapon1","MouseMiddle"),Is.True);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.K));
            InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Middle));InputSystem.Update();
            Assert.That(profile.Pressed("MoveForward"),Is.True);Assert.That(profile.Pressed("Weapon1"),Is.True);
            Assert.That(profile.Pressed("MoveBackward"),Is.False);
        }
        finally{InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);if(File.Exists(path))File.Delete(path);}
    }

    [Test] public void ActualPlayerControllerUsesReassignedMovementAndWeapon()
    {
        var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
        var vehicle=new GameObject("Input profile test");var profile=R1ControlProfile.Current;
        try
        {
            profile.ResetDefaults();Assert.That(profile.TryBind("MoveForward","K"),Is.True);
            Assert.That(profile.TryBind("Weapon1","MouseMiddle"),Is.True);
            var controller=vehicle.AddComponent<P12PipelineSafeTankController>();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.K));
            InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Middle));InputSystem.Update();
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            typeof(P12PipelineSafeTankController).GetMethod("Update",flags).Invoke(controller,null);
            Assert.That((float)typeof(P12PipelineSafeTankController).GetField("moveInput",flags).GetValue(controller),Is.EqualTo(1));
            Assert.That((bool)typeof(P12PipelineSafeTankController).GetField("fireHeld",flags).GetValue(controller),Is.True);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
            InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));InputSystem.Update();
            typeof(P12PipelineSafeTankController).GetMethod("Update",flags).Invoke(controller,null);
            Assert.That((float)typeof(P12PipelineSafeTankController).GetField("moveInput",flags).GetValue(controller),Is.Zero);
            Assert.That((bool)typeof(P12PipelineSafeTankController).GetField("fireHeld",flags).GetValue(controller),Is.False);
        }
        finally{profile.ResetDefaults();UnityEngine.Object.DestroyImmediate(vehicle);InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);}
    }

    [Test] public void SnapshotRestoreRejectsConflictsAndPersistsValidValues()
    {
        string path=Path.Combine(Path.GetTempPath(),"RRL-controls-snapshot-"+Guid.NewGuid()+".json");
        try
        {
            var profile=new R1ControlProfile(path);var snapshot=profile.Snapshot();
            Assert.That(profile.TryBind("MoveForward","K"),Is.True);
            var bad=(string[])snapshot.Clone();bad[1]=bad[0];
            Assert.That(profile.TryRestoreSnapshot(bad,out _),Is.False);
            Assert.That(profile.Binding("MoveForward"),Is.EqualTo("K"));
            Assert.That(profile.TryRestoreSnapshot(snapshot,out _),Is.True);
            Assert.That(new R1ControlProfile(path).Binding("MoveForward"),Is.EqualTo("W"));
            Assert.That(profile.TryBind("MoveForward","R"),Is.False,"Range reset is reserved until remapped.");
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
}
