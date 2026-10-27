using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoboRougelike.MapEditor
{
    public enum MapItemKind { Ground, Wall, Ramp, Obstacle, Spawn, Finish }
    [Serializable] public sealed class MapItem
    {
        public MapItemKind kind;
        public int x,z,rotation;
        public MapItem Copy()=>new MapItem{kind=kind,x=x,z=z,rotation=rotation};
    }
    [Serializable] public sealed class MapDocument
    {
        public int version=1;
        public List<MapItem> items=new List<MapItem>();
    }

    // The editable data is independent of Unity scene objects and test-drive copies.
    public sealed class MapDraft
    {
        public const int MaxItems=2000,MaxCoordinate=50;
        readonly List<MapItem> items=new List<MapItem>();
        readonly Stack<List<MapItem>> undo=new Stack<List<MapItem>>(),redo=new Stack<List<MapItem>>();
        public IReadOnlyList<MapItem> Items=>items;
        public bool CanUndo=>undo.Count>0;
        public bool CanRedo=>redo.Count>0;
        public MapItem Spawn=>items.FirstOrDefault(p=>p.kind==MapItemKind.Spawn);
        public MapItem Finish=>items.FirstOrDefault(p=>p.kind==MapItemKind.Finish);
        static bool Ground(MapItemKind kind)=>kind==MapItemKind.Ground;
        static List<MapItem> Copy(IEnumerable<MapItem> source)=>source.Select(x=>x.Copy()).ToList();
        void Checkpoint(){undo.Push(Copy(items));redo.Clear();}
        void Restore(List<MapItem> snapshot){items.Clear();items.AddRange(Copy(snapshot));}
        public bool Place(MapItemKind kind,int x,int z,int rotation)
        {
            if(!Enum.IsDefined(typeof(MapItemKind),kind)||Math.Abs((long)x)>MaxCoordinate||Math.Abs((long)z)>MaxCoordinate)return false;
            rotation=((rotation%4)+4)%4;
            var old=items.FirstOrDefault(p=>p.x==x&&p.z==z&&Ground(p.kind)==Ground(kind));
            var unique=(kind==MapItemKind.Spawn||kind==MapItemKind.Finish)?items.FirstOrDefault(p=>p.kind==kind):null;
            if(old!=null&&old.kind==kind&&old.rotation==rotation&&ReferenceEquals(old,unique??old))return false;
            if(old==null&&unique==null&&items.Count>=MaxItems)return false;
            Checkpoint();if(old!=null)items.Remove(old);if(unique!=null)items.Remove(unique);
            items.Add(new MapItem{kind=kind,x=x,z=z,rotation=rotation});return true;
        }
        public bool Remove(int x,int z)
        {
            var found=items.LastOrDefault(p=>p.x==x&&p.z==z&&!Ground(p.kind))??items.LastOrDefault(p=>p.x==x&&p.z==z);
            if(found==null)return false;Checkpoint();items.Remove(found);return true;
        }
        public bool Undo(){if(!CanUndo)return false;redo.Push(Copy(items));Restore(undo.Pop());return true;}
        public bool Redo(){if(!CanRedo)return false;undo.Push(Copy(items));Restore(redo.Pop());return true;}
        public string ToJson()=>JsonUtility.ToJson(new MapDocument{items=Copy(items)},true);
        public bool TryLoad(string json,out string error)
        {
            error=null;MapDocument doc;
            try{doc=JsonUtility.FromJson<MapDocument>(json);}catch(Exception e){error="Niepoprawny JSON: "+e.Message;return false;}
            if(doc==null||doc.version!=1||doc.items==null||doc.items.Count>MaxItems){error="Niepoprawny format lub wersja mapy.";return false;}
            var cells=new HashSet<string>();int spawns=0,finishes=0;
            foreach(var p in doc.items)
            {
                if(p==null||!Enum.IsDefined(typeof(MapItemKind),p.kind)||Math.Abs((long)p.x)>MaxCoordinate||Math.Abs((long)p.z)>MaxCoordinate||p.rotation<0||p.rotation>3||
                    !cells.Add(p.x+":"+p.z+":"+(Ground(p.kind)?0:1))){error="Niepoprawny lub nakładający się element.";return false;}
                if(p.kind==MapItemKind.Spawn)spawns++;if(p.kind==MapItemKind.Finish)finishes++;
            }
            if(spawns>1||finishes>1){error="Mapa może mieć tylko jeden start i jedną metę.";return false;}
            Checkpoint();Restore(doc.items);return true;
        }
        public string TestError()
        {
            if(Spawn==null)return "Dodaj punkt startowy.";
            if(Finish==null)return "Dodaj metę.";
            if(!items.Any(p=>p.kind==MapItemKind.Ground&&p.x==Spawn.x&&p.z==Spawn.z))return "Dodaj podłoże pod punktem startowym.";
            return null;
        }
    }
}
