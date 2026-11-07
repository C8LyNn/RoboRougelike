using System.Collections.Generic;
using B01.BUILD001;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    /// <summary>
    /// Opt-in conversion of source block metadata to existing BUILD001 definitions.
    /// Structural connections may use partial patches. Weapon and mobility
    /// support must use the separate full, non-quarantined patch definition.
    /// This adapter is not connected to placement or the runtime graph.
    /// </summary>
    public static class R4MountDefinitionAdapter
    {
        public static bool TryCreate(string id, bool moduleSupport, out BuildDefinition definition)
        {
            definition = null;
            foreach (var entry in R4BlockCatalog.All)
            {
                if (entry.Id != id) continue;
                if (entry.Cells == null || entry.Sockets == null || entry.PartialSocket == null ||
                    entry.Bonds == null || entry.Sockets.Length != entry.PartialSocket.Length)
                    return false;

                var sockets = new List<BuildSocket>();
                for (int i = 0; i < entry.Sockets.Length; i++)
                    if (!moduleSupport || R4BlockCatalog.CanMountModule(id, i))
                        sockets.Add(entry.Sockets[i]);
                if (sockets.Count == 0) return false;

                var cells = (Vector3Int[])entry.Cells.Clone();
                var bonds = new BuildCellBond[entry.Bonds.Length];
                for (int i = 0; i < bonds.Length; i++)
                    bonds[i] = new BuildCellBond(entry.Bonds[i].Item1, entry.Bonds[i].Item2);
                definition = new BuildDefinition(entry.Id, BuildRole.Armor, cells,
                    sockets.ToArray(), true, bonds);
                return true;
            }
            return false;
        }
    }
}
