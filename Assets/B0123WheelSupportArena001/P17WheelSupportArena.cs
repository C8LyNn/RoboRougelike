// B01-23 W09 physically instantiated Unity WheelCollider demo; NO canonical vehicle mutations.
// Needs frozen real #451 MeasuredSupportCapture and #417 MassBalanceSolver in authorized scratch receiver.
using System;
using System.Collections;
using System.Collections.Generic;
using B01.MeasuredSupport001;
using B01.PARTSIZE001.P17Balance;
using UnityEngine;

namespace B01.P17WheelSupportArena001
{
    public sealed class P17WheelSupportArena : MonoBehaviour
    {
        [SerializeField] private bool buildOnPlay=true;
        [SerializeField] private bool runNativeProbeAfterBuild=false;
        private readonly List<PhysicalRig> rigs=new List<PhysicalRig>();
        private GameObject ownedArena;
        private Coroutine running;
        private string status="Not built";

        private sealed class PhysicalRig
        {
            public WheelRigRecipe Spec;
            public Rigidbody Body;
            public WheelCollider[] Wheels;
        }

        private void Start()
        {
            if(buildOnPlay) BuildArena();
            if(runNativeProbeAfterBuild) BeginNativeProbe();
        }

        public void BuildArena()
        {
            if(ownedArena!=null) Destroy(ownedArena);
            rigs.Clear();

            ownedArena=new GameObject("B01-23 W09 P17 MEASURED SUPPORT - OWNED DEMO ONLY");
            ownedArena.transform.SetParent(transform,false);
            var ground=new GameObject("Real solid BoxCollider terrain - zero Y surface");
            ground.transform.SetParent(ownedArena.transform,false);
            ground.transform.localPosition=new Vector3(0,-.5f,0);
            var groundBox=ground.AddComponent<BoxCollider>();
            groundBox.size=new Vector3(35f,1f,24f);
            groundBox.isTrigger=false;

            rigs.Add(BuildRig(WheelRigRecipe.Light(),new Vector3(-4f,.92f,0)));
            rigs.Add(BuildRig(WheelRigRecipe.Heavy(),new Vector3(4f,1.18f,0)));
            status="Rigs created: LIGHT 110kg / HEAVY 620kg, 8 REAL WheelColliders; native contact NOT_YET_MEASURED";
        }

        private PhysicalRig BuildRig(WheelRigRecipe spec, Vector3 spawn)
        {
            string error=spec.InvalidReason();
            if(error!=null)throw new InvalidOperationException(error);
            var root=new GameObject(spec.Id);
            root.transform.SetParent(ownedArena.transform,false);
            root.transform.localPosition=spawn;

            var body=root.AddComponent<Rigidbody>();
            body.mass=(float)spec.Kilograms;
            body.useGravity=true;
            body.isKinematic=false;
            body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;

            var chassis=root.AddComponent<BoxCollider>();
            chassis.size=new Vector3((float)spec.WidthMeters,.32f,(float)spec.LengthMeters);
            chassis.center=new Vector3(0,.20f,0);
            chassis.isTrigger=false;
            // Actual Unity sets aggregate worldCenterOfMass from this author-chosen local center.
            // No fake per-part mass or contact values are fed to the stability solver.
            body.centerOfMass=new Vector3(0,.19f,0);

            var wheels=new WheelCollider[4];
            var anchors=spec.Anchors();
            for(int i=0;i<anchors.Length;i++)
            {
                var wheelObject=new GameObject("REAL WheelCollider "+anchors[i].Id);
                wheelObject.transform.SetParent(root.transform,false);
                wheelObject.transform.localPosition=new Vector3(
                    (float)anchors[i].X,-.26f,(float)anchors[i].Z);
                var wheel=wheelObject.AddComponent<WheelCollider>();
                wheel.radius=(float)spec.WheelRadiusMeters;
                wheel.suspensionDistance=(float)spec.SuspensionTravelMeters;
                var spring=wheel.suspensionSpring;
                spring.spring=(float)spec.Spring;
                spring.damper=(float)spec.Damper;
                spring.targetPosition=.5f;
                wheel.suspensionSpring=spring;
                // Real hit results are queried only after actual FixedUpdate physics.
                wheels[i]=wheel;
            }
            return new PhysicalRig{Spec=spec,Body=body,Wheels=wheels};
        }

        public void BeginNativeProbe()
        {
            if(running!=null)StopCoroutine(running);
            if(rigs.Count!=2)BuildArena();
            running=StartCoroutine(NativePhysicsProbe());
        }

        private IEnumerator NativePhysicsProbe()
        {
            status="Running real PhysX: 240 FixedUpdate warmup frames";
            for(int i=0;i<240;i++) yield return new WaitForFixedUpdate();

            string error;
            if(!TryNativeAssertion(out error))
            {
                status="NATIVE_PROBE_FAIL "+error;
                Debug.LogError("B01-23 W09 "+status,this);
                running=null;yield break;
            }

            // Destructively disable only one owned fixture wheel; no game state.
            var front=rigs[0].Wheels[0];
            front.enabled=false;
            for(int i=0;i<8;i++)yield return new WaitForFixedUpdate();
            StabilityResult tri;
            string failure;
            bool result=MeasuredSupportCapture.TryMeasure(
                new[]{rigs[0].Body},rigs[0].Wheels,out tri,out failure);
            // Disabling one wheel must never create more than 3 physical contacts.
            int grounded=GroundedCount(rigs[0].Wheels);
            if(result && grounded!=3)
            {
                status="NATIVE_PROBE_FAIL disabled wheel counted as support";
                Debug.LogError("B01-23 W09 "+status,this);
                front.enabled=true;running=null;yield break;
            }

            front.enabled=true;
            rigs[0].Wheels[1].enabled=false;
            rigs[0].Wheels[2].enabled=false;
            for(int i=0;i<8;i++)yield return new WaitForFixedUpdate();
            StabilityResult shouldFail;
            string reason;
            bool falseGreen=MeasuredSupportCapture.TryMeasure(
                new[]{rigs[0].Body},rigs[0].Wheels,out shouldFail,out reason);
            rigs[0].Wheels[1].enabled=true;
            rigs[0].Wheels[2].enabled=true;
            if(falseGreen || reason!="INSUFFICIENT_ACTUALLY_GROUNDED_WHEELS")
            {
                status="NATIVE_PROBE_FAIL two-wheel lift accepted or wrong failure "+reason;
                Debug.LogError("B01-23 W09 "+status,this);
                running=null;yield break;
            }
            status="NATIVE_PROBE_PASS real Rigidbody+WheelCollider ground hits on 2 distinct rigs, 1/2-wheel lifts; SOURCE NOT G2";
            Debug.Log("B01-23 W09 "+status,this);
            running=null;
        }

        private bool TryNativeAssertion(out string failure)
        {
            failure=null;
            if(rigs.Count!=2)
            { failure="MISSING_REAL_RIGS";return false; }
            for(int i=0;i<rigs.Count;i++)
            {
                var rig=rigs[i];
                if(rig.Body==null || rig.Wheels.Length!=4 || GroundedCount(rig.Wheels)<3)
                { failure=rig.Spec.Id+" GROUND_CONTACTS_NOT_ESTABLISHED";return false; }
                StabilityResult result;
                string reason;
                if(!MeasuredSupportCapture.TryMeasure(
                    new[]{rig.Body},rig.Wheels,out result,out reason))
                { failure=rig.Spec.Id+" "+reason;return false; }
                if(Math.Abs(result.TotalKilograms-rig.Spec.Kilograms)>.01 ||
                   !result.ProjectionWithinSupport || result.SignedEdgeMarginMeters<=0)
                { failure=rig.Spec.Id+" MASS_OR_SUPPORT_INVALID";return false; }
                Debug.Log("B01-23 W09 REAL_GROUND "+rig.Spec.Id+
                          " massKg="+result.TotalKilograms+
                          " marginMeters="+result.SignedEdgeMarginMeters+
                          " contacts="+GroundedCount(rig.Wheels),this);
            }
            if(Math.Abs(rigs[0].Body.mass-rigs[1].Body.mass)<1)
            { failure="PHYSICALLY_IDENTICAL_MASSES";return false; }
            return true;
        }

        private static int GroundedCount(WheelCollider[] wheels)
        {
            int n=0;
            foreach(var wheel in wheels)
            {
                WheelHit hit;
                if(wheel!=null && wheel.enabled && wheel.gameObject.activeInHierarchy &&
                   wheel.GetGroundHit(out hit))n++;
            }
            return n;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12,12,550,180),GUI.skin.box);
            GUILayout.Label("P17 Wheel Support Arena | isolated 110kg / 620kg real physics");
            GUILayout.Label(status);
            if(GUILayout.Button("Build / reset BOTH original fixture rigs"))BuildArena();
            if(GUILayout.Button("Run native 2-rig WheelCollider source-to-PhysX checks"))BeginNativeProbe();
            GUILayout.EndArea();
        }
    }
}
