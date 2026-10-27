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
            foreach(char c in name)if(!(char.IsLetterOrDigit(c)||c=='_'||c=='-'||c==' '))return false;
            path=Path.Combine(directory,name.Trim()+".json");return true;
        }
        public static void Save(string path,MapDraft draft)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllText(temp,draft.ToJson());if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
        public static bool Load(string path,MapDraft draft,out string error)
        {
            error=null;
            try{var file=new FileInfo(path);if(!file.Exists||file.Length>1024*1024){error="Plik nie istnieje lub jest zbyt duży.";return false;}return draft.TryLoad(File.ReadAllText(path),out error);}
            catch(Exception e){error="Nie można wczytać mapy: "+e.Message;return false;}
        }
    }
}
