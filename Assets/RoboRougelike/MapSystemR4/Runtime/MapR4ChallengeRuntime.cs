using System;

namespace RoboRougelike.MapSystemR4
{
    // Sandbox win/lose rules. Boss/Survival require adapter events in the real game.
    public sealed class MapR4ChallengeRuntime
    {
        public MapR4Mode Mode { get; }
        public MapR4Status Status { get; private set; } = MapR4Status.Ready;
        public float ElapsedSeconds { get; private set; }
        public string Resolution { get; private set; } = "";
        public float TimeLimitSeconds { get; }
        private readonly float _survivalDuration;
        private bool _checkpointReached;
        public bool CheckpointReached => _checkpointReached;

        public MapR4ChallengeRuntime(MapR4Mode mode, float timeLimitSeconds = 180f, float survivalDuration = 30f)
        {
            Mode = mode;
            TimeLimitSeconds = Math.Max(1f, timeLimitSeconds);
            _survivalDuration = Math.Max(0.1f, survivalDuration);
        }

        public void Begin()
        {
            if (Status != MapR4Status.Ready) return;
            Status = MapR4Status.Running;
        }

        public void Tick(float deltaSeconds)
        {
            if (Status != MapR4Status.Running) return;
            ElapsedSeconds += Math.Max(0f, deltaSeconds);
            if (Mode == MapR4Mode.Survival && ElapsedSeconds >= _survivalDuration)
            {
                Finish(true, "Duration survived (demo: no enemy wave spawner)");
                return;
            }
            if (ElapsedSeconds >= TimeLimitSeconds)
                Finish(false, "Time limit expired");
        }

        public void NotifyAnchor(string anchorId)
        {
            if (Status != MapR4Status.Running) return;
            if (Mode == MapR4Mode.TimeAttack)
            {
                if (anchorId == "checkpoint_upper") _checkpointReached = true;
                else if (anchorId == "finish_east" && _checkpointReached) Finish(true, "Finished after checkpoint");
            }
            else if (Mode == MapR4Mode.Escape && anchorId == "exit_east")
                Finish(true, "Reached evacuation exit");
        }

        public void NotifyBossDefeated()
        {
            if (Status == MapR4Status.Running && Mode == MapR4Mode.Boss)
                Finish(true, "Boss defeat event received");
        }

        public void NotifyPlayerDestroyed()
        {
            if (Status == MapR4Status.Running) Finish(false, "Player destroyed");
        }

        private void Finish(bool success, string resolution)
        {
            Status = success ? MapR4Status.Completed : MapR4Status.Failed;
            Resolution = resolution;
        }
    }
}
