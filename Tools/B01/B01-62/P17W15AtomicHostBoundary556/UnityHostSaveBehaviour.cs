// B01-62 P17/W15 -- UNITY-FACING adapter candidate, NOT installed in Assets.
// Only compile in Unity 6000.3.14f1 after an authorized owner provides IAtomicSaveHost.
// No reflection, GetPrivateField, or #510 private Rig access. No native run claimed.
using System;
using System.IO;
using B01.DetachedSaveContract001;
using UnityEngine;

namespace B01.SaveHostBoundary556
{
    [DisallowMultipleComponent]
    public sealed class UnityHostSaveBehaviour : MonoBehaviour
    {
        // Must be a separate NEW owner-authorized component implementing IAtomicSaveHost.
        // Frozen #510 SecureDebrisPlaygroundV2 DOES NOT implement this interface!
        [SerializeField] private MonoBehaviour hostComponent;
        private AtomicHostSaveBridge bridge;

        private bool Resolve()
        {
            if(bridge!=null)return true;
            if(!(hostComponent is IAtomicSaveHost host))return false;
            bridge=new AtomicHostSaveBridge(host);
            return true;
        }
        private static bool ValidSlot(string slot)
        {
            if(string.IsNullOrEmpty(slot) || slot.Length>48)return false;
            foreach(char c in slot)
                if(!((c>='a'&&c<='z')||(c>='0'&&c<='9')||c=='-'||c=='_'))
                    return false;
            return true;
        }
        // Intentionally new-slot-only. Never silently replace old run/checkpoint files.
        public bool SaveNewSlot(string slot)
        {
            if(!ValidSlot(slot)||!Resolve())return false;
            string folder=Path.Combine(Application.persistentDataPath,"B01-62-SecureV2-R2");
            try
            {
                Directory.CreateDirectory(folder);
                return bridge.TrySaveNewFile(Path.Combine(folder,slot+".b01sv1"));
            }
            catch(Exception){return false;}
        }
        public RestoreVerdict ColdLoadSlot(string slot)
        {
            if(!ValidSlot(slot)||!Resolve())return RestoreVerdict.Quarantine;
            string folder=Path.Combine(Application.persistentDataPath,"B01-62-SecureV2-R2");
            return bridge.TryLoadFile(Path.Combine(folder,slot+".b01sv1"));
        }
        public bool IsQuarantined=>bridge!=null&&bridge.Quarantined;
    }
}
