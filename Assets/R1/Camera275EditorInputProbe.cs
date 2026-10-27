#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections.Generic;

[DefaultExecutionOrder(-2000)]
public sealed class Camera275EditorInputProbe : MonoBehaviour
{
    public Vector2 Delta = new Vector2(100,0);
    public Vector2 Observed { get; private set; }
    public bool Sent { get; private set; }
    Mouse device;
    Keyboard keys;
    readonly List<InputDevice> muted=new List<InputDevice>();
    InputSettings.BackgroundBehavior background;
    InputSettings.EditorInputBehaviorInPlayMode routing;
    void Awake()
    {
        background=InputSystem.settings.backgroundBehavior;
        routing=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        foreach(var item in InputSystem.devices)if(item.enabled&&(item is Keyboard||item is Mouse||item is Gamepad)){muted.Add(item);InputSystem.DisableDevice(item);}
        keys=InputSystem.AddDevice<Keyboard>();device=InputSystem.AddDevice<Mouse>();
    }
    void Update()
    {
        keys.MakeCurrent();device.MakeCurrent();InputState.Change(keys,new KeyboardState());
        InputState.Change(device,new MouseState{delta=Sent?Vector2.zero:Delta});
        if(!Sent){Observed=device.delta.ReadValue();Sent=true;}
    }
    void OnDestroy()
    {
        if(device!=null&&device.added)InputSystem.RemoveDevice(device);
        if(keys!=null&&keys.added)InputSystem.RemoveDevice(keys);
        foreach(var item in muted)if(item.added)InputSystem.EnableDevice(item);
        InputSystem.settings.backgroundBehavior=background;
        InputSystem.settings.editorInputBehaviorInPlayMode=routing;
    }
}
#endif
