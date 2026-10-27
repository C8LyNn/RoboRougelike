using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoboRougelike.MapEditor
{
    // Existing numeric IDs are stable for R1 JSON files.
    public enum MapItemKind { Ground, Wall, Ramp, Obstacle, Spawn, Finish, Platform, Pillar, LowBarrier, Steps }
    [Serializable] public sealed class MapItem
    {
        public MapItemKind kind;
        public int x,z,rotation;
        public int scaleX=1,scaleY=1,scaleZ=1;
        public MapItem Copy()=>new MapItem{kind=kind,x=x,z=z,rotation=rotation,scaleX=scaleX,scaleY=scaleY,scaleZ=scaleZ};
    }
    [Serializable] public sealed class MapDocument
    {
        public int version=2;
        public bool globalFloor;
        public List<MapItem> items=new List<MapItem>();
    }

    public sealed class MapDraft
    {
        public const int MaxItems=2000,MaxCoordinate=50,MaxScale=4;
        sealed class Snapshot {public bool floor;public List<MapItem> items;}
        readonly List<MapItem> items=new List<MapItem>();
        readonly Stack<Snapshot> undo=new Stack<Snapshot>(),redo=new Stack<Snapshot>();
        public IReadOnlyList<MapItem> Items=>items;
        public bool GlobalFloor {get;private set;}
        public bool CanUndo=>undo.Count>0;
        public bool CanRedo=>redo.Count>0;
        public MapItem Spawn=>items.FirstOrDefault(p=>p.kind==MapItemKind.Spawn);
        public MapItem Finish=>items.FirstOrDefault(p=>p.kind==MapItemKind.Finish);
        static bool Ground(MapItemKind kind)=>kind==MapItemKind.Ground;
        static List<MapItem> Copy(IEnumerable<MapItem> source)=>source.Select(x=>x.Copy()).ToList();
        Snapshot Capture()=>new Snapshot{floor=GlobalFloor,items=Copy(items)};
        void Checkpoint(){undo.Push(Capture());redo.Clear();}
        void Restore(Snapshot snapshot){GlobalFloor=snapshot.floor;items.Clear();items.AddRange(Copy(snapshot.items));}
        public bool SetGlobalFloor(bool enabled)
        {if(GlobalFloor==enabled)return false;Checkpoint();GlobalFloor=enabled;return true;}
        public static IEnumerable<Vector2Int> Footprint(MapItem item)
        {
            for(int x=0;x<item.scaleX;x++)for(int z=0;z<item.scaleZ;z++)
            {
                int rx=x,rz=z;
                switch(item.rotation){case 1:rx=z;rz=-x;break;case 2:rx=-x;rz=-z;break;case 3:rx=-z;rz=x;break;}
                yield return new Vector2Int(item.x+rx,item.z+rz);
            }
        }
        static bool Valid(MapItem p)=>p!=null&&Enum.IsDefined(typeof(MapItemKind),p.kind)&&p.rotation>=0&&p.rotation<4&&
            p.scaleX>=1&&p.scaleX<=MaxScale&&p.scaleY>=1&&p.scaleY<=MaxScale&&p.scaleZ>=1&&p.scaleZ<=MaxScale&&
            Math.Abs((long)p.x)<=MaxCoordinate&&Math.Abs((long)p.z)<=MaxCoordinate&&
            Footprint(p).All(c=>Math.Abs((long)c.x)<=MaxCoordinate&&Math.Abs((long)c.y)<=MaxCoordinate);
        public bool CanPlace(MapItemKind kind,int x,int z,int rotation,int sx,int sy,int sz)
        {
            var p=new MapItem{kind=kind,x=x,z=z,rotation=((rotation%4)+4)%4,scaleX=sx,scaleY=sy,scaleZ=sz};
            if(!Valid(p)||(Ground(kind)&&GlobalFloor))return false;
            var unique=(kind==MapItemKind.Spawn||kind==MapItemKind.Finish)?items.FirstOrDefault(a=>a.kind==kind):null;
            if(unique==null&&items.Count>=MaxItems)return false;
            var cells=new HashSet<Vector2Int>(Footprint(p));
            return !items.Any(a=>a!=unique&&Ground(a.kind)==Ground(kind)&&Footprint(a).Any(cells.Contains));
        }
        public bool Place(MapItemKind kind,int x,int z,int rotation)=>Place(kind,x,z,rotation,1,1,1);
        public bool Place(MapItemKind kind,int x,int z,int rotation,int sx,int sy,int sz)
        {
            if(!CanPlace(kind,x,z,rotation,sx,sy,sz))return false;
            rotation=((rotation%4)+4)%4;
            var unique=(kind==MapItemKind.Spawn||kind==MapItemKind.Finish)?items.FirstOrDefault(a=>a.kind==kind):null;
            if(unique!=null&&unique.x==x&&unique.z==z&&unique.rotation==rotation&&unique.scaleX==sx&&unique.scaleY==sy&&unique.scaleZ==sz)return false;
            Checkpoint();if(unique!=null)items.Remove(unique);
            items.Add(new MapItem{kind=kind,x=x,z=z,rotation=rotation,scaleX=sx,scaleY=sy,scaleZ=sz});return true;
        }
        public bool Remove(int x,int z)
        {
            var cell=new Vector2Int(x,z);
            var found=items.LastOrDefault(p=>!Ground(p.kind)&&Footprint(p).Contains(cell))??items.LastOrDefault(p=>Footprint(p).Contains(cell));
            if(found==null)return false;Checkpoint();items.Remove(found);return true;
        }
        public bool Undo(){if(!CanUndo)return false;redo.Push(Capture());Restore(undo.Pop());return true;}
        public bool Redo(){if(!CanRedo)return false;undo.Push(Capture());Restore(redo.Pop());return true;}
        public string ToJson()=>JsonUtility.ToJson(new MapDocument{globalFloor=GlobalFloor,items=Copy(items)},true);
        public bool TryLoad(string json,out string error)
        {
            error=null;MapDocument doc;
            try{doc=JsonUtility.FromJson<MapDocument>(json);}catch(Exception e){error="Niepoprawny JSON: "+e.Message;return false;}
            if(doc==null||(doc.version!=1&&doc.version!=2)||doc.items==null||doc.items.Count>MaxItems){error="Niepoprawny format lub wersja mapy.";return false;}
            var floor=new HashSet<Vector2Int>();var objects=new HashSet<Vector2Int>();int spawns=0,finishes=0;
            foreach(var p in doc.items)
            {
                if(p!=null&&doc.version==1)p.scaleX=p.scaleY=p.scaleZ=1;
                if(!Valid(p)){error="Niepoprawny element.";return false;}
                var cells=Ground(p.kind)?floor:objects;
                foreach(var cell in Footprint(p))if(!cells.Add(cell)){error="Nakładające się elementy.";return false;}
                if(p.kind==MapItemKind.Spawn)spawns++;if(p.kind==MapItemKind.Finish)finishes++;
            }
            if(spawns>1||finishes>1){error="Mapa może mieć tylko jeden start i jedną metę.";return false;}
            Checkpoint();Restore(new Snapshot{floor=doc.version>=2&&doc.globalFloor,items=doc.items});return true;
        }
        public string TestError()
        {
            if(Spawn==null)return "Dodaj punkt startowy.";
            if(Finish==null)return "Dodaj metę.";
            if(!GlobalFloor)
            {
                var floor=new HashSet<Vector2Int>(items.Where(p=>Ground(p.kind)).SelectMany(Footprint));
                if(Footprint(Spawn).Any(c=>!floor.Contains(c)))return "Dodaj podłoże pod punktem startowym.";
            }
            return null;
        }
    }
}
