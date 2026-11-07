using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using B01.BUILD001;
using NUnit.Framework;
using UnityEngine;

public sealed class R4BlockCatalogTests
{
    static Type Catalog=>Type.GetType("R4BlockCatalog, Assembly-CSharp");
    [Test] public void AllThirtySourceDefinitionsHaveValidCellAndSocketGeometry()
    {
        Assert.That(Catalog,Is.Not.Null,"R4 block data should be available to the existing BUILD001 rules.");
        var entries=(IEnumerable)Catalog.GetField("All",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        int count=0;var names=new System.Collections.Generic.HashSet<string>();
        foreach(var entry in entries)
        {
            var type=entry.GetType();var id=(string)type.GetField("Id").GetValue(entry);
            var cells=(Vector3Int[])type.GetField("Cells").GetValue(entry);
            var sockets=(BuildSocket[])type.GetField("Sockets").GetValue(entry);
            var partial=(bool[])type.GetField("PartialSocket").GetValue(entry);
            var bonds=(IEnumerable)type.GetField("Bonds").GetValue(entry);
            var joins=new System.Collections.Generic.List<BuildCellBond>();
            foreach(var bond in bonds){var t=bond.GetType();joins.Add(new BuildCellBond((Vector3Int)t.GetField("Item1").GetValue(bond),(Vector3Int)t.GetField("Item2").GetValue(bond)));}
            Assert.That(names.Add(id),Is.True,"Duplicate ID "+id);Assert.That(partial.Length,Is.EqualTo(sockets.Length),id);
            var definition=new BuildDefinition(id,BuildRole.Armor,cells,sockets,true,joins.ToArray());
            Assert.That(BuildSocketGeometry.Valid(definition,out string reason),Is.True,id+": "+reason);
            count++;
        }
        Assert.That(count,Is.EqualTo(30));
    }
    [Test] public void DeviceMountingRejectsPartialPortsAndModelsThatForbidModules()
    {
        Assert.That(Catalog,Is.Not.Null);
        var method=Catalog.GetMethod("CanMountModule",BindingFlags.Public|BindingFlags.Static);Assert.That(method,Is.Not.Null);
        bool Can(string id,int index)=>(bool)method.Invoke(null,new object[]{id,index});
        Assert.That(Can("R4B_Cube",0),Is.True);Assert.That(Can("R4B_Wedge",0),Is.True);
        Assert.That(Can("R4B_Wedge",2),Is.False);Assert.That(Can("R4B_Tetra",0),Is.False);
        Assert.That(Can("missing",0),Is.False);Assert.That(Can("R4B_Cube",99),Is.False);
    }
}
