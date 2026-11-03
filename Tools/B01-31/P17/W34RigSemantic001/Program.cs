// W34 non-Unity source semantics: original B01-31 methods are inserted VERBATIM
// at build time, never hand-implemented or edited in this test file.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    // Deliberately tiny ancestry/Renderer stand-ins used for a managed source
    // test ONLY. NOT UnityEngine, FBX, Transform matrices, PhysX or a Player.
    public sealed class Renderer
    {
        public Transform transform { get; }
        public Renderer(Transform t) { transform = t; }
    }

    public sealed class GameObject
    {
        public string name;
        public readonly Transform transform;
        internal Renderer renderer;
        public GameObject(string name, bool hasRenderer = false)
        {
            this.name = name;
            transform = new Transform(this);
            if (hasRenderer) renderer = new Renderer(transform);
        }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class =>
            transform.GetComponentsInChildren<T>(includeInactive);
        public void RemoveRenderer() => renderer = null;
    }

    public sealed class Transform
    {
        public readonly GameObject gameObject;
        public string name => gameObject.name;
        private Transform currentParent;
        private readonly List<Transform> children = new List<Transform>();
        public Transform(GameObject owner) { gameObject = owner; }
        public void SetParent(Transform next)
        {
            currentParent?.children.Remove(this);
            currentParent = next;
            next?.children.Add(this);
        }
        public bool IsChildOf(Transform ancestor)
        {
            for (var p = this; p != null; p = p.currentParent)
                if (ReferenceEquals(p, ancestor)) return true;
            return false;
        }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class
        {
            var found = new List<T>();
            Collect(this, found);
            return found.ToArray();
        }
        private static void Collect<T>(Transform current, List<T> found) where T : class
        {
            if (current is T t) found.Add(t);
            if (current.gameObject.renderer is T renderer) found.Add(renderer);
            foreach (var child in current.children) Collect(child, found);
        }
    }
}

namespace B0131W34RealSource
{
    using UnityEngine;
    internal static class RealB0131RigMethods
    {
        // __EXTRACT_ORIGINAL_FIND_UNIQUE__
        // __EXTRACT_ORIGINAL_RIG_GATE__
    }

    public static class Program
    {
        private static int count;
        private sealed class Rig
        {
            public GameObject root, yaw, pitch, left, right;
        }
        private static GameObject Node(string name, GameObject parent, bool renderer = false)
        {
            var node = new GameObject(name, renderer);
            if (parent != null) node.transform.SetParent(parent.transform);
            return node;
        }
        private static Rig Valid()
        {
            var r = new Rig();
            r.root = Node("Imported_R41_V5_FixedRoot", null, true);
            r.yaw = Node("R41_Yaw", r.root, true);
            r.pitch = Node("R41_Pitch", r.yaw, true);
            r.left = Node("Muzzle_L", r.pitch);
            r.right = Node("Muzzle_R", r.pitch);
            return r;
        }
        private static void Test(string title, Action action)
        {
            try { action(); count++; System.Console.WriteLine("SOURCE_SEMANTIC_PASS " + title); }
            catch (Exception e)
            { throw new InvalidOperationException("SOURCE_SEMANTIC_FAIL " + title + " :: " + e.Message, e); }
        }
        private static void Expect(GameObject source, bool ok, string expectedReason)
        {
            var got = RealB0131RigMethods.TryValidateSourceRigHierarchy(source, out var refusal);
            if (got != ok) throw new Exception("expected " + ok + " got " + got + " (" + refusal + ")");
            if (!ok && refusal != expectedReason)
                throw new Exception("expected refusal '" + expectedReason + "' got '" + refusal + "'");
            if (ok && refusal.Length != 0) throw new Exception("accepted but refused: " + refusal);
        }
        public static void Main()
        {
            Test("real-original-method_null_refuses", () =>
                Expect(null, false, "MISSING_ORIGINAL_R41_VISUAL"));
            Test("real-original-method_valid_three_segments", () =>
                Expect(Valid().root, true, ""));
            Test("real-original-method_missing_yaw_refuses", () =>
            { var r = Valid(); r.yaw.name = "NO_REAL_YAW"; Expect(r.root, false, "MISSING_FBX_TRANSFORM_R41_Yaw"); });
            Test("real-original-method_missing_pitch_refuses", () =>
            { var r = Valid(); r.pitch.name = "NO_REAL_PITCH"; Expect(r.root, false, "MISSING_FBX_TRANSFORM_R41_Pitch"); });
            Test("real-original-method_duplicate_yaw_refuses", () =>
            { var r = Valid(); Node("R41_Yaw", r.root, true); Expect(r.root, false, "DUPLICATE_FBX_TRANSFORM_R41_Yaw"); });
            Test("real-original-method_duplicate_pitch_refuses", () =>
            { var r = Valid(); Node("R41_Pitch", r.yaw, true); Expect(r.root, false, "DUPLICATE_FBX_TRANSFORM_R41_Pitch"); });
            Test("real-original-method_pitch_detached_from_yaw_refuses", () =>
            { var r = Valid(); r.pitch.transform.SetParent(r.root.transform); Expect(r.root, false, "UNPROVEN_R41_YAW_PITCH_HIERARCHY"); });
            Test("real-original-method_left_muzzle_outside_pitch_refuses", () =>
            { var r = Valid(); r.left.transform.SetParent(r.yaw.transform); Expect(r.root, false, "MUZZLE_MARKERS_OUTSIDE_PITCH_BRANCH"); });
            Test("real-original-method_right_muzzle_outside_pitch_refuses", () =>
            { var r = Valid(); r.right.transform.SetParent(r.root.transform); Expect(r.root, false, "MUZZLE_MARKERS_OUTSIDE_PITCH_BRANCH"); });
            Test("real-original-method_missing_fixed_base_renderer_refuses", () =>
            { var r = Valid(); r.root.RemoveRenderer(); Expect(r.root, false, "UNPROVEN_FIXED_BASE_YAW_AND_PITCH_VISUAL_SEGMENTS"); });
            Test("real-original-method_missing_yaw_renderer_refuses", () =>
            { var r = Valid(); r.yaw.RemoveRenderer(); Expect(r.root, false, "UNPROVEN_FIXED_BASE_YAW_AND_PITCH_VISUAL_SEGMENTS"); });
            Test("real-original-method_missing_pitch_renderer_refuses", () =>
            { var r = Valid(); r.pitch.RemoveRenderer(); Expect(r.root, false, "UNPROVEN_FIXED_BASE_YAW_AND_PITCH_VISUAL_SEGMENTS"); });
            Test("real-original-method_duplicate_left_muzzle_refuses", () =>
            { var r = Valid(); Node("Muzzle_L", r.pitch); Expect(r.root, false, "DUPLICATE_FBX_TRANSFORM_Muzzle_L"); });
            Test("real-original-method_source_yaw_must_not_be_root", () =>
            { var r = Valid(); r.root.name = "R41_Yaw"; r.yaw.name = "UNNAMED_YAW"; Expect(r.root, false, "UNPROVEN_R41_YAW_PITCH_HIERARCHY"); });
            System.Console.WriteLine("ORIGINAL_B0131_W33_CSHARP_SOURCE_SEMANTIC_PASS=" + count);
            System.Console.WriteLine("UNITY_EDITOR_NOT_RUN; TRUE_ORIGINAL_V5_FBX_NOT_AVAILABLE; WINDOWS_PLAYER_NOT_RUN");
        }
    }
}
