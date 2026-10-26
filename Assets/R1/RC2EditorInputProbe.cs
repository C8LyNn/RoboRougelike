#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Test-only input-state injection inside a real player Update. Excluded from retail.
[DefaultExecutionOrder(-2000)]
public sealed class RC2EditorInputProbe : MonoBehaviour
{
    Keyboard keyboard;
    public bool Sent {get;private set;}
    public bool PressObserved {get;private set;}
    void Update()
    {
        if(Sent)return;
        keyboard=InputSystem.AddDevice<Keyboard>();
        InputState.Change(keyboard,new KeyboardState(Key.Enter));
        PressObserved=keyboard.enterKey.wasPressedThisFrame;Sent=true;
    }
    void OnDestroy(){if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);}
}
#endif
