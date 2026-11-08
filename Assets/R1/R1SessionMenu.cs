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
        int mapQuitWarningFrame=-1;

        void OnEnable()
        {
#if !UNITY_EDITOR
            Application.wantsToQuit += HandleNativeQuit;
#endif
        }
        void OnDisable()
        {
#if !UNITY_EDITOR
            Application.wantsToQuit -= HandleNativeQuit;
#endif
        }

        // A native close must not silently discard the current map. Keep the
        // existing shutdown path unchanged when there is no unsaved draft.
        bool HandleNativeQuit()
        {
            if(QuitRequested)return true;
            if(MapQuitPending&&mapQuitWarningFrame==Time.frameCount)return false;
            if(ConfirmMapEditorExit(true))return true;
            mapQuitWarningFrame=Time.frameCount;
            EditorMessage="Niezapisane zmiany mapy zostaną utracone. Zamknij okno ponownie lub potwierdź wyjście w menu.";
            if(!Paused)TogglePause();
            return false;
        }

        void RequestQuitByShortcut()
        {
            if(HandleNativeQuit())RequestQuit();
        }

        // Run before state/transition guards: quitting must also work while loading.
        bool ReadSessionInput()
        {
            if (QuitRequested || returningToMenu) return true;
#if ENABLE_INPUT_SYSTEM
            var keys = Keyboard.current;
            var pad = Gamepad.current;
            if (keys != null && keys.f4Key.wasPressedThisFrame &&
                (keys.leftAltKey.isPressed || keys.rightAltKey.isPressed))
            { RequestQuitByShortcut(); return true; }
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
                else RequestQuitFromMenu();
            }
            return true;
#else
            if (Input.GetKeyDown(KeyCode.F4) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            { RequestQuitByShortcut(); return true; }
            if (Input.GetKeyDown(KeyCode.Escape)) { HandleEscape(); return true; }
            return Paused;
#endif
        }

        public void RequestQuitFromMenu()
        {
            if(QuitRequested||!ConfirmMapEditorExit(true))return;
            RequestQuit();
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

        // After any map confirmation, shutdown never waits or kills the process.
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
