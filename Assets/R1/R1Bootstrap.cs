using UnityEngine;
using UnityEngine.SceneManagement;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R1
{
    public static class R1Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (R1RunController.Instance != null ||
                Object.FindFirstObjectByType<P12PipelineSafeTankController>() == null ||
                Object.FindFirstObjectByType<P12PipelineSafeEnemyTurret>() == null) return;
            new GameObject("Wyprawa R1").AddComponent<R1RunController>();
        }
    }
}
