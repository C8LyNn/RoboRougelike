using System;
using System.IO;
using UnityEngine;

namespace RoboRougelike.MapEditor
{
    public static class MapStorage
    {
        public static string DirectoryPath=>Path.Combine(Application.persistentDataPath,"Maps");
        public static bool TryPath(string directory,string name,out string path)
        {
            path=null;if(string.IsNullOrWhiteSpace(name)||name.Length>64||name=="."||name=="..")return false;
            if(name!=name.Trim())return false;
            foreach(char c in name)if(!(char.IsLetterOrDigit(c)||c=='_'||c=='-'||c==' '))return false;
            string upper=name.ToUpperInvariant();
            if(upper=="CON"||upper=="PRN"||upper=="AUX"||upper=="NUL"||
                upper=="CONIN$"||upper=="CONOUT$"||IsDeviceNumber(upper,"COM")||IsDeviceNumber(upper,"LPT"))return false;
            if(string.IsNullOrWhiteSpace(directory))return false;
            if(Directory.Exists(directory))
                foreach(var file in Directory.EnumerateFiles(directory))
                {
                    string existing=Path.GetFileName(file),candidate=name+".json";
                    if(string.Equals(existing,candidate,StringComparison.OrdinalIgnoreCase)&&
                        !string.Equals(existing,candidate,StringComparison.Ordinal))return false;
                }
            path=Path.Combine(directory,name+".json");return true;
        }
        static bool IsDeviceNumber(string name,string prefix) => name.Length==4&&name.StartsWith(prefix,StringComparison.Ordinal)&&
            ((name[3]>='1'&&name[3]<='9')||name[3]=='¹'||name[3]=='²'||name[3]=='³');
        static bool ValidPath(string path,out string directory)
        {
            directory=null;if(string.IsNullOrWhiteSpace(path))return false;
            directory=Path.GetDirectoryName(path);string filename=Path.GetFileName(path);
            return filename.EndsWith(".json",StringComparison.Ordinal)&&
                TryPath(directory,Path.GetFileNameWithoutExtension(filename),out var checkedPath)&&
                string.Equals(Path.GetFullPath(path),Path.GetFullPath(checkedPath),StringComparison.Ordinal);
        }
        public static void Save(string path,MapDraft draft,bool overwrite=false)
        {
            if(string.IsNullOrWhiteSpace(path)||draft==null)throw new ArgumentException("Niepoprawna mapa lub ścieżka.");
            if(!ValidPath(path,out var directory))
                throw new IOException("Niepoprawna lub kolidująca nazwa mapy.");
            Directory.CreateDirectory(directory);
            if(File.Exists(path)&&!overwrite)throw new IOException("Mapa już istnieje; wymagane potwierdzenie nadpisania.");
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllText(temp,draft.ToJson());if(overwrite&&File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
        public static bool Load(string path,MapDraft draft,out string error)
        {
            error=null;
            try{if(draft==null||!ValidPath(path,out _)){error="Niepoprawna lub kolidująca nazwa mapy.";return false;}var file=new FileInfo(path);if(!file.Exists||file.Length>1024*1024){error="Plik nie istnieje lub jest zbyt duży.";return false;}return draft.TryLoad(File.ReadAllText(path),out error);}
            catch(Exception e){error="Nie można wczytać mapy: "+e.Message;return false;}
        }
    }
}
