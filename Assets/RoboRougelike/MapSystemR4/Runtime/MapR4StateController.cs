using System;
using System.Collections.Generic;

namespace RoboRougelike.MapSystemR4
{
    public sealed class MapR4StateController
    {
        private readonly MapR4MapData _map;
        private readonly MapR4BuildResult _build;
        public string StateId { get; private set; }
        public event Action<string> StateChanged;

        public MapR4StateController(MapR4MapData map, MapR4BuildResult build)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _build = build ?? throw new ArgumentNullException(nameof(build));
        }

        public bool TrySetState(string newStateId)
        {
            MapR4StateData state = null;
            if (_map.states != null)
                foreach (var candidate in _map.states)
                    if (candidate != null && candidate.id == newStateId) { state = candidate; break; }
            if (state == null) return false;
            var disabled = new HashSet<string>(state.disabledEdgeIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            foreach (var item in _build.Edges)
                if (item.Value != null) item.Value.SetActive(!disabled.Contains(item.Key));
            if (StateId == newStateId) return true;
            StateId = newStateId;
            StateChanged?.Invoke(newStateId);
            return true;
        }
    }
}
