#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Test-only input-state injection inside a real player Update. Excluded from retail.
[DefaultExecutionOrder(-2000)]
public sealed class RC2EditorInputProbe : MonoBehaviour
{
    Keyboard keyboard;
    InputSettings.BackgroundBehavior oldBackground;
    InputSettings.EditorInputBehaviorInPlayMode oldRouting;
    public bool Sent {get;private set;}
    public bool PressObserved {get;private set;}
    void Awake()
    {
        oldBackground=InputSystem.settings.backgroundBehavior;oldRouting=InputSystem.settings.editorInputBehaviorInPlayMode;
        // Supported Editor setting routes the headless GameView's input to the
        // player buffers. Restore both settings on teardown; no asset is saved.
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    }
    void Update()
    {
        if(Sent)return;
        keyboard=InputSystem.AddDevice<Keyboard>();
        InputState.Change(keyboard,new KeyboardState(Key.Enter));
        PressObserved=keyboard.enterKey.wasPressedThisFrame;Sent=true;
    }
    void OnDestroy()
    {
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldRouting;
    }
}
#endif
