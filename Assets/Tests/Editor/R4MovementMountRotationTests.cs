using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4MovementMountRotationTests
{
    [UnityTest] public IEnumerator FourModelsStayOnRotatedAndScaledBlockFaces()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        foreach(var kind in new[]{R3PartKind.Booster,R3PartKind.Gyroscope,R3PartKind.Suspension,R3PartKind.JumpDrive})
        foreach(var rotation in new[]{Quaternion.identity,Quaternion.Euler(90,0,0),Quaternion.Euler(0,90,0),Quaternion.Euler(0,0,90)})
        foreach(float scale in new[]{.52f,1.04f})
        {
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.position=new Vector3(10,5,3);
            block.transform.rotation=rotation;block.transform.localScale=Vector3.one*scale;
            R41PartGeometry.Payload(block.transform,kind,R4WeaponKind.Basic,material,true,out _);
            bool attached=kind==R3PartKind.Booster?R4BoosterArt.TryAttach(block.transform)!=null:
                kind==R3PartKind.Gyroscope?R4GyroArt.TryAttach(block.transform)!=null:
                kind==R3PartKind.Suspension?R4SuspensionArt.TryAttach(block.transform):R4JumpArt.TryAttach(block.transform)!=null;
            Assert.That(attached,Is.True,kind+" model should attach");
            var socket=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Socket_Mount");
            Assert.That(Vector3.Distance(socket.position,block.transform.TransformPoint(Vector3.up*.5f)),Is.LessThan(.001f),kind+" socket location");
            Assert.That(Vector3.Dot(-socket.forward,block.transform.up),Is.GreaterThan(.9f),kind+" socket normal");
            Object.Destroy(block);
        }
        Object.Destroy(material);yield return null;yield return new ExitPlayMode();
    }
}
