using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class WeaponModelFootprintInventoryTests
{
    [UnityTest] public IEnumerator ImportedWeaponModelsHaveMeasurableMountsAndAimEnvelopes()
    {
        yield return new EnterPlayMode();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        try
        {
            foreach(R4WeaponKind weapon in Enum.GetValues(typeof(R4WeaponKind)))
            {
                var block=GameObject.CreatePrimitive(PrimitiveType.Cube);
                try
                {
                    block.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
                    var pivot=R41PartGeometry.Payload(block.transform,R3PartKind.Gun,weapon,material,true,out var muzzle);
                    var art=R4WeaponArt.TryAttach(block.transform,pivot,muzzle,weapon);
                    Assert.That(art,Is.Not.Null,weapon+" must have an attachable imported model");
                    var axes=block.GetComponent<R4ArtWeaponAxisAdapter>();
                    Assert.That(axes,Is.Not.Null);Assert.That(axes.Muzzle,Is.Not.Null);
                    var source=axes.transform.GetComponentsInChildren<Renderer>(true)
                        .Where(r=>r.enabled&&r.transform!=block.transform&&!r.transform.IsChildOf(pivot)).ToArray();
                    Assert.That(source,Is.Not.Empty,weapon+" must have visible imported geometry");
                    var yawNode=block.GetComponentsInChildren<Transform>(true)
                        .First(t=>t.name==(weapon==R4WeaponKind.Automatic?"R41":"R4W_"+weapon)+"_Yaw");
                    if(weapon==R4WeaponKind.Automatic)
                    {
                        var pitchNode=block.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R41_Pitch");
                        Debug.Log($"AUTOMATIC_AXES yaw={yawNode.position.ToString("F4")} pitch={pitchNode.position.ToString("F4")} physical={pivot.position.ToString("F4")}");
                    }
                    var fixedRenderers=source.Where(r=>!r.transform.IsChildOf(yawNode)).ToArray();
                    if(fixedRenderers.Length>0)
                    {
                        var fixedBounds=fixedRenderers[0].bounds;
                        foreach(var renderer in fixedRenderers.Skip(1))fixedBounds.Encapsulate(renderer.bounds);
                        Debug.Log($"WEAPON_FIXED {weapon} min={fixedBounds.min.ToString("F4")} max={fixedBounds.max.ToString("F4")}");
                    }
                    else Debug.Log($"WEAPON_FIXED {weapon} no stationary renderer outside yaw hierarchy");
                    foreach(float yaw in new[]{0f,90f,180f,270f})
                    {
                        axes.SetAim(yaw,0);
                        var bounds=source[0].bounds;
                        foreach(var renderer in source.Skip(1))bounds.Encapsulate(renderer.bounds);
                        Assert.That(float.IsNaN(bounds.size.x)||float.IsNaN(bounds.size.y)||float.IsNaN(bounds.size.z),Is.False);
                        Assert.That(bounds.size.sqrMagnitude,Is.GreaterThan(.0001f));
                        Debug.Log($"WEAPON_BOUNDS {weapon} yaw={yaw:0} min={bounds.min.ToString("F4")} max={bounds.max.ToString("F4")} muzzle={axes.Muzzle.position.ToString("F4")}");
                    }
                }
                finally{UnityEngine.Object.Destroy(block);}
                yield return null;
            }
        }
        finally{UnityEngine.Object.Destroy(material);}
        yield return new ExitPlayMode();
    }
}
