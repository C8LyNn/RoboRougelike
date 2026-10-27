#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[DefaultExecutionOrder(-2000)]
public sealed class Camera275EditorInputProbe : MonoBehaviour
{
    public Vector2 Delta = new Vector2(100,0);
    public Vector2 Observed { get; private set; }
    public bool Sent { get; private set; }
    Mouse device;
    InputSettings.BackgroundBehavior background;
    InputSettings.EditorInputBehaviorInPlayMode routing;
    void Awake()
    {
        background=InputSystem.settings.backgroundBehavior;
        routing=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        device=InputSystem.AddDevice<Mouse>();
    }
    void Update()
    {
        InputState.Change(device,new MouseState{delta=Sent?Vector2.zero:Delta});
        if(!Sent){Observed=device.delta.ReadValue();Sent=true;}
    }
    void OnDestroy()
    {
        if(device!=null&&device.added)InputSystem.RemoveDevice(device);
        InputSystem.settings.backgroundBehavior=background;
        InputSystem.settings.editorInputBehaviorInPlayMode=routing;
    }
}
#endif
