using System.Collections.Generic;
using B01.BUILD001;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    /// <summary>
    /// Opt-in conversion of source block metadata to existing BUILD001 definitions.
    /// Structural connections may use partial patches except quarantined source
    /// discrepancies. Weapon and mobility
    /// support must use the separate full, non-quarantined patch definition.
    /// This adapter is not connected to placement or the runtime graph.
    /// </summary>
    public static class R4MountDefinitionAdapter
    {
        // One mask per occupied cell in the rotated vehicle-local grid. A
        // bit means a catalog socket exists; exact patch overlap is still the
        // responsibility of BUILD001, not this compact graph representation.
        public static bool TryCreateCellPortMasks(string id,bool moduleSupport,Vector3Int turns,
            out Dictionary<Vector3Int,int> masks)
        {
            masks=null;
            if(!TryCreate(id,moduleSupport,out var definition))return false;
            var result=new Dictionary<Vector3Int,int>();
            foreach(var cell in definition.Cells)result.Add(R3MountingGrid.Rotate(cell,turns),0);
            foreach(var socket in definition.Sockets)
            {
                var cell=R3MountingGrid.Rotate(socket.Cell,turns);
                var face=R3MountingGrid.Rotate(socket.Face,turns);
                if(!result.ContainsKey(cell))return false;
                result[cell]|=R3MountingGrid.FaceBit(face);
            }
            masks=result;return true;
        }
        // The pinned #963 source's sampled collider does not cover these
        // patches. Keep the source catalogue intact, but do not authorize
        // structural or device support until its owner resolves the geometry.
        private static bool QuarantinedPatch(string id,int socketIndex) =>
            (id=="R4B_HexColumn"||id=="R4B_OctColumn") && (socketIndex==0||socketIndex==1);

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
                    if (!QuarantinedPatch(id,i) && (!moduleSupport || R4BlockCatalog.CanMountModule(id, i)))
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
