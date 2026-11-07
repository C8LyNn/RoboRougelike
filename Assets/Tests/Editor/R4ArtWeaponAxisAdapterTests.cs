using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class R4ArtWeaponAxisAdapterTests
{
    [Test] public void InvalidNonuniformMountIsRejectedWithoutChangingTheVisual()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/R4Intake/Weapons/R4W_Basic/R4W_Basic.fbx");
        var type=Type.GetType("RoboRougelike.R4_1.R4ArtWeaponAxisAdapter, Assembly-CSharp");
        Assert.That(prefab,Is.Not.Null);Assert.That(type,Is.Not.Null);
        var carrier=new GameObject("QA invalid carrier");carrier.transform.localScale=new Vector3(1,2,1);
        var visual=UnityEngine.Object.Instantiate(prefab,carrier.transform,false);
        try
        {
            var adapter=carrier.AddComponent(type);var oldScale=visual.transform.localScale;var oldRotation=visual.transform.localRotation;
            Assert.That((bool)type.GetMethod("Bind").Invoke(adapter,new object[]{visual.transform,"R4W_Basic"}),Is.False);
            Assert.That(visual.transform.localScale,Is.EqualTo(oldScale));Assert.That(visual.transform.localRotation,Is.EqualTo(oldRotation));
            Assert.That(type.GetProperty("Muzzle").GetValue(adapter),Is.Null);
        }
        finally{UnityEngine.Object.DestroyImmediate(carrier);}
    }
    static System.Collections.Generic.IEnumerable<TestCaseData> Weapons()
    {
        foreach(var name in new[]{"Basic","Heavy","Shotgun","Railgun","BeamCutter","RocketLauncher","RotarySaw","EmpEmitter"})
        {yield return new TestCaseData(name,false);yield return new TestCaseData(name,true);}
    }
    [TestCaseSource(nameof(Weapons))] public void ImportedWeaponAimsInUnityAxesAndKeepsItsMount(string name,bool rotatedScaledCarrier)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/R4Intake/Weapons/R4W_{name}/R4W_{name}.fbx");
        Assert.That(prefab,Is.Not.Null);
        var type=Type.GetType("RoboRougelike.R4_1.R4ArtWeaponAxisAdapter, Assembly-CSharp");
        Assert.That(type,Is.Not.Null,"A space adapter is required before connecting the model to the game.");
        var carrier=new GameObject("QA carrier");
        if(rotatedScaledCarrier){carrier.transform.SetPositionAndRotation(new Vector3(2,3,4),Quaternion.Euler(20,120,-15));carrier.transform.localScale=Vector3.one*.52f;}
        var visual=UnityEngine.Object.Instantiate(prefab,carrier.transform,false);
        try
        {
            var adapter=carrier.AddComponent(type);
            var bind=type.GetMethod("Bind",new[]{typeof(Transform),typeof(string)});
            Assert.That((bool)bind.Invoke(adapter,new object[]{visual.transform,"R4W_"+name}),Is.True);
            var aim=type.GetMethod("SetAim",new[]{typeof(float),typeof(float)});
            var muzzle=(Transform)type.GetProperty("Muzzle").GetValue(adapter);
            Assert.That(muzzle,Is.Not.Null);
            var socket=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Socket_Mount_Bottom");
            var socketMeters=Quaternion.Inverse(carrier.transform.rotation)*(socket.position-carrier.transform.position);
            Assert.That(socketMeters.y,Is.EqualTo(-.26f).Within(.001),"Socket coordinates are metres even inside a scaled vehicle block.");
            Assert.That(Vector3.Dot(carrier.transform.forward,muzzle.forward),Is.GreaterThan(.999f),"Forward ray follows the neutral barrel.");
            if(name!="EmpEmitter")Assert.That(carrier.transform.InverseTransformPoint(muzzle.position).z,Is.GreaterThan(0));
            aim.Invoke(adapter,new object[]{90f,0f});
            Assert.That(Vector3.Dot(carrier.transform.right,muzzle.forward),Is.GreaterThan(.999f),"Yaw +90 is Unity right.");
            if(name!="EmpEmitter")
            {
                aim.Invoke(adapter,new object[]{0f,25f});
                Assert.That(Vector3.Dot(carrier.transform.up,muzzle.forward),Is.EqualTo(Mathf.Sin(25*Mathf.Deg2Rad)).Within(.01),"Pitch +25 aims upward.");
                aim.Invoke(adapter,new object[]{-90f,25f});
                Assert.That(Vector3.Dot(-carrier.transform.right,muzzle.forward),Is.EqualTo(Mathf.Cos(25*Mathf.Deg2Rad)).Within(.01),"Combined yaw and pitch stay aligned.");
                Assert.That(Vector3.Dot(carrier.transform.up,muzzle.forward),Is.EqualTo(Mathf.Sin(25*Mathf.Deg2Rad)).Within(.01));
            }
            Assert.That(visual.GetComponentsInChildren<Collider>(true).Length,Is.Zero,"Visual must not introduce physics.");
        }
        finally{UnityEngine.Object.DestroyImmediate(carrier);}
    }
}
