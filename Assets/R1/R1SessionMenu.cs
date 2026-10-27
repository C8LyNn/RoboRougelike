using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        public bool QuitRequested { get; private set; }
        public int PauseSelection { get; private set; }
        bool returningToMenu;

        // Run before state/transition guards: quitting must also work while loading.
        bool ReadSessionInput()
        {
            if (QuitRequested || returningToMenu) return true;
#if ENABLE_INPUT_SYSTEM
            var keys = Keyboard.current;
            var pad = Gamepad.current;
            if (keys != null && keys.f4Key.wasPressedThisFrame &&
                (keys.leftAltKey.isPressed || keys.rightAltKey.isPressed))
            { RequestQuit(); return true; }
            if ((keys != null && keys.escapeKey.wasPressedThisFrame) ||
                (pad != null && pad.startButton.wasPressedThisFrame))
            { HandleEscape(); return true; }
            if (!Paused) return false;
            // Preserve the range reset shortcut while the session menu owns other input.
            if (!MapEditorTesting && Progress?.State == R1RunState.TestDrive && keys != null && keys.rKey.wasPressedThisFrame)
            { ResetTestRange(); return true; }
            int pauseOptions=MapEditorTesting?4:3;
            if ((keys != null && keys.upArrowKey.wasPressedThisFrame) ||
                (pad != null && pad.dpad.up.wasPressedThisFrame)) PauseSelection = (PauseSelection + pauseOptions-1) % pauseOptions;
            if ((keys != null && keys.downArrowKey.wasPressedThisFrame) ||
                (pad != null && pad.dpad.down.wasPressedThisFrame)) PauseSelection = (PauseSelection + 1) % pauseOptions;
            if (pad != null && pad.buttonEast.wasPressedThisFrame) { TogglePause(); return true; }
            if ((keys != null && keys.enterKey.wasPressedThisFrame) ||
                (pad != null && pad.buttonSouth.wasPressedThisFrame))
            {
                if (PauseSelection == 0) TogglePause();
                else if (PauseSelection == 1 && MapEditorTesting) ReturnFromEditedMapTest();
                else if (PauseSelection == 1 || (PauseSelection == 2 && MapEditorTesting)) RestartRun();
                else RequestQuit();
            }
            return true;
#else
            if (Input.GetKeyDown(KeyCode.F4) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            { RequestQuit(); return true; }
            if (Input.GetKeyDown(KeyCode.Escape)) { HandleEscape(); return true; }
            return Paused;
#endif
        }

        public void RequestQuit()
        {
            if (QuitRequested) return;
            try { PrepareShutdown("MenuOrShortcut"); }
            catch (System.Exception error) { Debug.LogException(error); }
            finally
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit(0);
#endif
            }
        }

        // Native X/Alt+F4 are never vetoed. No waits, process kills or recursive Quit calls.
        bool PrepareShutdown(string source)
        {
            if (QuitRequested) return false;
            QuitRequested = true;
            try { StopAllCoroutines(); SetSimulation(false); }
            finally
            {
                Time.timeScale = 1;
                if(!Application.isBatchMode){Cursor.lockState = CursorLockMode.None;Cursor.visible = true;}
            }
            Debug.Log("[EXIT273] Shutdown prepared: " + source);
            return true;
        }

        void OnApplicationQuit() { PrepareShutdown("NativeClose"); }
    }
}
