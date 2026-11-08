using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

namespace RoboRougelike.R1
{
    [Serializable] sealed class R1TechnologyData { public int version=1; public int[] weapons; }

    // Durable metaprogression uses the game's existing atomic checkpoint writer,
    // in a separate file because this data must outlive an individual run.
    public sealed class R1TechnologyProgress
    {
        readonly string path;
        readonly HashSet<R4WeaponKind> unlocked=new HashSet<R4WeaponKind>
            {R4WeaponKind.Basic,R4WeaponKind.Automatic,R4WeaponKind.Heavy};
        bool readFailed,dirty;
        public string Error { get; private set; }
        public static string DefaultPath
        {
            get
            {
#if UNITY_EDITOR || B01_UNIFIED_RC2_ACCEPTANCE
                // Batch Editor and explicit acceptance Players must never write
                // discoveries to the player's retail profile.
                if(Application.isBatchMode)return Path.Combine(Path.GetTempPath(),
                    "RoboRougelike-Technology-QA-"+System.Diagnostics.Process.GetCurrentProcess().Id,"technology.json");
#endif
                return Path.Combine(Application.persistentDataPath,"Progression","technology.json");
            }
        }
        public R1TechnologyProgress(string storagePath)
        {
            if(string.IsNullOrWhiteSpace(storagePath))throw new ArgumentException("Brak ścieżki technologii.");
            path=storagePath;
            if(!File.Exists(path))return;
            try
            {
                var data=JsonUtility.FromJson<R1TechnologyData>(R1RunCheckpointStorage.ReadPayload(path));
                if(data==null||data.version!=1||data.weapons==null||data.weapons.Length>R4WeaponDefinition.Count||
                    data.weapons.Any(i=>!Enum.IsDefined(typeof(R4WeaponKind),i))||data.weapons.Distinct().Count()!=data.weapons.Length)
                    throw new InvalidDataException("Niepoprawne dane technologii.");
                foreach(int id in data.weapons)unlocked.Add((R4WeaponKind)id);
            }
            catch(Exception problem)
            {
                // Never overwrite an unreadable profile or revoke access on error.
                readFailed=true;Error="Nie odczytano technologii; plik pozostawiono bez zmian: "+problem.Message;
            }
        }
        public bool IsUnlocked(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic)
            =>kind!=R3PartKind.Gun||readFailed||unlocked.Contains(weapon);
        public int Discover(IEnumerable<R3PartState> parts)
        {
            if(parts==null)return 0;
            int count=0;
            foreach(var part in parts)
                if(part!=null&&part.Kind==R3PartKind.Gun&&Enum.IsDefined(typeof(R4WeaponKind),part.Weapon)&&unlocked.Add(part.Weapon))count++;
            dirty|=count>0;
            if(dirty&&!readFailed)
            {
                try
                {
                    R1RunCheckpointStorage.WritePayload(path,JsonUtility.ToJson(new R1TechnologyData{
                        weapons=unlocked.OrderBy(x=>x).Select(x=>(int)x).ToArray()}));
                    dirty=false;Error=null;
                }
                catch(Exception problem){Error="Nie zapisano odkrytych technologii: "+problem.Message;}
            }
            return count;
        }
    }
}
