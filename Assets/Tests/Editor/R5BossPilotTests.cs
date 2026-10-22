using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;
public class R5BossPilotTests
{
    private R1RunController Run=>R1RunController.Instance;
    private Mouse mouse;private Keyboard keyboard;private InputSettings original,settings;
    private static IEnumerator Until(Func<bool> condition,string message,float timeout=15)
    {float end=Time.realtimeSinceStartup+timeout;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(condition(),Is.True,message);}
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();yield return Until(()=>Run!=null&&Run.Progress!=null,"Bootstrap");
        original=InputSystem.settings;settings=UnityEngine.Object.Instantiate(original);InputSystem.settings=settings;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();
    }
    [UnityTest,Timeout(240000)] public IEnumerator LightDefeatsBossWithPhysicalGunAndActiveAi(){yield return Fight(R3VehicleKind.Light);}
    [UnityTest,Timeout(240000)] public IEnumerator HeavyDefeatsBossWithPhysicalGunAndActiveAi(){yield return Fight(R3VehicleKind.Heavy);}
    private IEnumerator Fight(R3VehicleKind kind)
    {
        Assert.That(Run.SelectVehicle(kind),Is.True);Run.StartRun();
        // Prefix is explicitly an API-damage fixture, NOT evidence of a physical full run.
        for(int stage=0;stage<6;stage++)
        {
            Run.ChooseRoute(0);yield return Until(()=>!Run.Transitioning,"Stage");if(stage==5)break;
            Run.Encounter.SetCombatActive(false);foreach(var foe in Run.Encounter.Enemies)foe.Core.ApplyDamage(100000,foe.Core.transform.position,Vector3.up);
            yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Fixture loot");RRLW2TestFlow.Claim(Run,Array.Empty<string>());Run.StartNextEncounter();
        }
        var boss=Run.Encounter.Enemies.First();var body=boss.GetComponent<Rigidbody>();var camera=Camera.main;
        UnityEngine.Object.FindFirstObjectByType<P12PipelineSafeCamera>().enabled=false;
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));
        Vector3 previous=body.position,velocity=Vector3.zero;float start=Time.realtimeSinceStartup,deadline=start+150;
        int peakShells=0;long peakMemory=0;
        while(Run.Progress.State==R1RunState.Combat&&Time.realtimeSinceStartup<deadline)
        {
            var current=body.position;if(Time.deltaTime>0)velocity=Vector3.Lerp(velocity,(current-previous)/Time.deltaTime,.25f);previous=current;
            Vector3 muzzle=Run.Player.transform.Find("Turret_Yaw/Cannon_Pitch/Muzzle").position;
            Vector3 aim=boss.Core.transform.position+velocity*(Vector3.Distance(muzzle,boss.Core.transform.position)/34);
            camera.transform.position=Run.Player.transform.position+new Vector3(0,3,-6);camera.transform.rotation=Quaternion.LookRotation(aim-camera.transform.position);
            Vector3 away=Vector3.ProjectOnPlane(Run.Player.transform.position-body.position,Vector3.up);float distance=away.magnitude;
            Vector3 waypoint=body.position;if(distance<16)waypoint=body.position+away.normalized*11+Vector3.Cross(Vector3.up,away.normalized)*7;
            Vector3 direction=waypoint-Run.Player.transform.position;
            float angle=Vector3.SignedAngle(Run.Player.transform.forward,Vector3.ProjectOnPlane(direction,Vector3.up),Vector3.up);
            var keys=new List<Key>();if(angle>4)keys.Add(Key.D);else if(angle< -4)keys.Add(Key.A);
            if(distance<6)keys.Add(Key.S);else if(Mathf.Abs(angle)<65)keys.Add(Key.W);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));
            peakShells=Math.Max(peakShells,UnityEngine.Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Length);
            peakMemory=Math.Max(peakMemory,UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());yield return null;
        }
        InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        Debug.Log($"[R5 BOSS PILOT] {kind} result={Run.Progress.State} seconds={Time.realtimeSinceStartup-start:0.00} peakShells={peakShells} editorAllocatedBytes={peakMemory} core={(boss!=null&&boss.Core!=null?boss.Core.CurrentHp:0)}");
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Salvage),"Boss combat uses only normal player input/physical rounds, active AI; API prefix excluded.");
        RRLW2TestFlow.Claim(Run,Array.Empty<string>());Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Won));
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
        if(original!=null)InputSystem.settings=original;if(settings!=null)UnityEngine.Object.Destroy(settings);
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }
}
