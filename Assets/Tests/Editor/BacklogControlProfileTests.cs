using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed class BacklogControlProfileTests
{
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
}
