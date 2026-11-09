using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R5;

namespace RoboRougelike.R1
{
    [Serializable] public sealed class R1RunCheckpoint
    {
        public int version=1,seed,vehicle,variant,defeated,earned,recovered,lastReward;
        public string runId;
        public R1ProgressCheckpoint progress;
        public R3ModelCheckpoint model;
    }
    public static class R1RunCheckpointStorage
    {
        [Serializable] sealed class Envelope {public string payload,sha256;}
        static string Hash(string payload)
        {
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","").ToLowerInvariant();
        }
#if UNITY_EDITOR
        public static string TestPath;
#endif
        public static string PathName {
            get {
#if UNITY_EDITOR
                if(!string.IsNullOrEmpty(TestPath))return TestPath;
#endif
#if B01_UNIFIED_RC2_ACCEPTANCE
                if(Application.isBatchMode)
                {
                    var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rrl-run-save-qa-path");
                    if(i>=0&&i+1<args.Length)return args[i+1];
                }
#endif
                return Path.Combine(Application.persistentDataPath,"Run","checkpoint.json");
            }
        }
        public static void Write(string path,R1RunCheckpoint document)
        {
            if(string.IsNullOrWhiteSpace(path)||document==null)throw new ArgumentException("Niepoprawny zapis.");
            WritePayload(path,JsonUtility.ToJson(document));
        }
        // Meta progression shares this same atomic, checksummed disk writer.
        internal static void WritePayload(string path,string payload)
        {
            if(string.IsNullOrWhiteSpace(path)||string.IsNullOrEmpty(payload))throw new ArgumentException("Niepoprawny zapis.");
            string directory=Path.GetDirectoryName(path);Directory.CreateDirectory(directory);
            string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                File.WriteAllText(temporary,JsonUtility.ToJson(new Envelope{payload=payload,sha256=Hash(payload)}));
                if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
        public static R1RunCheckpoint Read(string path)
        {
            var document=JsonUtility.FromJson<R1RunCheckpoint>(ReadPayload(path));
            if(document==null||(document.version!=1&&document.version!=2))throw new InvalidDataException("Nieobsługiwana wersja zapisu.");
            return document;
        }
        internal static string ReadPayload(string path)
        {
            var file=new FileInfo(path);
            if(!file.Exists||file.Length<=0||file.Length>4*1024*1024)throw new InvalidDataException("Brak zapisu lub niepoprawny rozmiar.");
            var envelope=JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
            if(envelope==null||string.IsNullOrEmpty(envelope.payload)||string.IsNullOrEmpty(envelope.sha256)||
                !string.Equals(Hash(envelope.payload),envelope.sha256,StringComparison.Ordinal))
                throw new InvalidDataException("Uszkodzony zapis lub niezgodna suma kontrolna.");
            return envelope.payload;
        }
    }
    public sealed partial class R1RunController
    {
        public string RunSaveMessage {get;private set;}
        public bool SaveRunCheckpoint()
        {
            // A rejected attempt must not expose a previous save's result.
            RunSaveMessage=null;
            if(Progress?.State!=R1RunState.Garage||Transitioning||SandboxWorkshop||MapEditorSession||ReactorPrepared||VehicleModel==null)return false;
            try
            {
                Player?.GetComponent<R3VehicleAssembly>()?.Capture();
                var document=new R1RunCheckpoint{version=VehicleModel.Parts.Concat(VehicleModel.Inventory).Any(p=>p.Tier>1)?2:1,seed=Seed,vehicle=(int)SelectedVehicle,variant=SelectedVehicleVariant,runId=runId,
                    defeated=DefeatedEnemies,earned=EarnedScrap,recovered=RecoveredModules,lastReward=LastScrapReward,
                    progress=Progress.MakeGarageCheckpoint(),model=VehicleModel.MakeCheckpoint(Time.time)};
                if(!R3VehicleModel.TryRestoreCheckpoint(document.model,SelectedVehicle,Definition.GaragePrices,Time.time,out _,out var error,SelectedVehicleVariant))
                    throw new InvalidDataException(error);
                R1RunCheckpointStorage.Write(R1RunCheckpointStorage.PathName,document);
                RunSaveMessage="Zapisano wyprawę w garażu.";Garage.Message=RunSaveMessage;return true;
            }
            catch(Exception e){RunSaveMessage="Nie zapisano wyprawy: "+e.Message;Garage.Message=RunSaveMessage;return false;}
        }
        public bool LoadRunCheckpoint()
        {
            if(Progress?.State!=R1RunState.Ready||Transitioning||Definition==null)return false;
            try
            {
                var data=R1RunCheckpointStorage.Read(R1RunCheckpointStorage.PathName);
                if(!Enum.IsDefined(typeof(R3VehicleKind),data.vehicle)||data.variant<0||data.variant>=4||!Guid.TryParseExact(data.runId,"N",out _)||
                    data.defeated<0||data.earned<0||data.recovered<0||data.lastReward<0||
                    !R1RunProgress.TryRestoreGarage(data.progress,Definition.Encounters.Length,out var restoredProgress))
                    throw new InvalidDataException("Niepoprawny postęp lub dane wyprawy.");
                var kind=(R3VehicleKind)data.vehicle;
                if(!R3VehicleModel.TryRestoreCheckpoint(data.model,kind,Definition.GaragePrices,Time.time,out var restoredModel,out var error,data.variant))
                    throw new InvalidDataException(error);
                // Legacy owned weapons remain purchasable after loading old saves.
                Technology?.Discover(restoredModel.Parts.Concat(restoredModel.Inventory));
                if(Technology?.Error!=null)TechnologyMessage=Technology.Error;
                var restoredPlan=new R5RunPlan(Definition.RouteStages,data.seed);
                var player=R3VehicleAssembly.Build(restoredModel,restoredProgress.Stats,palette,new Vector3(0,.7f,-22),Quaternion.identity);
                Progress=restoredProgress;VehicleModel=restoredModel;Plan=restoredPlan;SelectedVehicle=kind;SelectedVehicleVariant=data.variant;RequestedSeed=data.seed;runId=data.runId;
                DefeatedEnemies=data.defeated;EarnedScrap=data.earned;RecoveredModules=data.recovered;LastScrapReward=data.lastReward;
                Player=player;followCamera.Configure(Player.transform,Player.CameraPivot);SetSimulation(false);Garage.Open();
                RunSaveMessage=restoredModel.LegacyGunScalePending>0?
                    "Wczytano starszy zapis bez zmian broni: zwolnij miejsce w magazynie, aby bezpiecznie dostosować jej rozmiar.":
                    restoredModel.LegacyPartsMovedToInventory>0||restoredModel.LegacyPortPartsMovedToInventory>0?
                    $"Wczytano starszy zapis. Przeniesiono do magazynu {restoredModel.LegacyPartsMovedToInventory+restoredModel.LegacyPortPartsMovedToInventory} części po zmianie rozmiarów lub punktów mocowania. Sprawdź konstrukcję.":
                    restoredModel.LegacyPortMasksNormalized>0?"Wczytano wyprawę. Dopasowano punkty mocowania kół i dział.":"Wczytano wyprawę.";
                Garage.Message=RunSaveMessage;return true;
            }
            catch(Exception e){RunSaveMessage="Nie wczytano wyprawy: "+e.Message;return false;}
        }
    }
}
