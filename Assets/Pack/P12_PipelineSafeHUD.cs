using UnityEngine;

namespace RogueMachine.P12PipelineSafe
{
    public class P12PipelineSafeHUD : MonoBehaviour
    {
        private GUIStyle leftStyle;
        private GUIStyle rightStyle;
        private GUIStyle structureStyle;
        private GUIStyle crosshairStyle;

        private P12PipelineSafeVehicleStructure
            cachedStructure;

        private P12PipelineSafeVehicleModuleSystem
            cachedModules;

        private void Setup()
        {
            if (leftStyle != null)
                return;

            leftStyle =
                new GUIStyle(
                    GUI.skin.label
                );

            leftStyle.fontSize = 18;
            leftStyle.normal.textColor =
                Color.white;

            rightStyle =
                new GUIStyle(
                    leftStyle
                );

            rightStyle.alignment =
                TextAnchor.UpperRight;

            structureStyle =
                new GUIStyle(
                    leftStyle
                );

            structureStyle.fontSize =
                17;

            structureStyle.normal.textColor =
                new Color(
                    0.55f,
                    0.88f,
                    1f,
                    1f
                );

            crosshairStyle =
                new GUIStyle(
                    GUI.skin.label
                );

            crosshairStyle.fontSize = 28;
            crosshairStyle.fontStyle =
                FontStyle.Bold;

            crosshairStyle.normal.textColor =
                new Color(
                    1f,
                    0.76f,
                    0.18f,
                    1f
                );

            crosshairStyle.alignment =
                TextAnchor.MiddleCenter;
        }

        private void OnGUI()
        {
            if (RoboRougelike.R1.R1RunController.Instance != null) return;
            Setup();

            if (cachedStructure == null)
            {
                cachedStructure =
                    FindFirstObjectByType<
                        P12PipelineSafeVehicleStructure
                    >();
            }

            int playerBlocks;

            string structuralStatus;

            if (cachedStructure != null)
            {
                playerBlocks =
                    cachedStructure
                        .ConnectedBlockCount;

                structuralStatus =
                    cachedStructure.CoreAlive
                        ? "CORE: ONLINE"
                        : "CORE: DESTROYED";
            }
            else
            {
                playerBlocks =
                    FindObjectsByType<
                        P12_PipelineSafePlayerBlock
                    >(
                        FindObjectsSortMode.None
                    ).Length;

                structuralStatus =
                    "CORE: INITIALIZING";
            }

            int targetBlocks =
                FindObjectsByType<
                    P12_PipelineSafeTargetBlock
                >(
                    FindObjectsSortMode.None
                ).Length;

            GUI.Label(
                new Rect(
                    18f,
                    16f,
                    620f,
                    30f
                ),
                $"CONNECTED BLOCKS: {playerBlocks}    |    LMB FIRE",
                leftStyle
            );

            GUI.Label(
                new Rect(
                    18f,
                    44f,
                    620f,
                    30f
                ),
                cachedStructure != null
                    ? structuralStatus +
                      "    |    DETACHED: " +
                      cachedStructure
                          .DetachedBlockCountTotal
                    : structuralStatus,
                structureStyle
            );

            if (cachedModules == null)
            {
                cachedModules =
                    FindFirstObjectByType<
                        P12PipelineSafeVehicleModuleSystem
                    >();
            }

            if (cachedModules != null)
            {
                GUI.Label(
                    new Rect(
                        18f,
                        72f,
                        860f,
                        30f
                    ),
                    "WHEELS: " +
                    cachedModules.ActiveWheelCount +
                    "/" +
                    cachedModules.InitialWheelCount +
                    "  (L " +
                    cachedModules.ActiveLeftCount +
                    " / R " +
                    cachedModules.ActiveRightCount +
                    ")" +
                    "    |    DRIVE " +
                    Mathf.RoundToInt(
                        cachedModules.DriveFactor * 100f
                    ) +
                    "%" +
                    "    |    WEAPON: " +
                    (
                        cachedModules.WeaponOnline
                            ? "ONLINE"
                            : "OFFLINE"
                    ),
                    structureStyle
                );
            }

            GUI.Label(
                new Rect(
                    Screen.width - 340f,
                    16f,
                    320f,
                    30f
                ),
                $"ENEMY / TARGET BLOCKS: {targetBlocks}",
                rightStyle
            );

            GUI.Label(
                new Rect(
                    Screen.width * 0.5f - 20f,
                    Screen.height * 0.5f - 20f,
                    40f,
                    40f
                ),
                "+",
                crosshairStyle
            );
        }
    }
}
