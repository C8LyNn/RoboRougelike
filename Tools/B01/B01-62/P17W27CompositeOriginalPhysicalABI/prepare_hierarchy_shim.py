#!/usr/bin/env python3
"""W27 QA-only patch of *test-only* #792 UnityEngine shim: Unity Destroy(parent)
also destroys children. NEVER install generated shim in Unity; NOT real PhysX."""
import hashlib
import pathlib
import sys

path=pathlib.Path(sys.argv[1]).resolve()
original=path.read_bytes()
blob=hashlib.sha1(b"blob "+str(len(original)).encode()+b"\\0"+original).hexdigest()
assert blob=="53d16eb828772532f81a7af1ed2ed3a712b0e9e4", "FROZEN_792_SHIM_SHA_MISMATCH"

src=original.decode("utf-8")
before='''        private Transform? current;
        public Transform? parent=>current;
        public Vector3 position,localPosition,localScale;
        public Quaternion rotation;
        public void SetParent(Transform? parent,bool worldPositionStays=false)
        { current=parent; }'''
after='''        private Transform? current;
        private readonly List<Transform> children=new List<Transform>();
        internal Transform[] ChildSnapshot()=>children.ToArray();
        public Transform? parent=>current;
        public Vector3 position,localPosition,localScale;
        public Quaternion rotation;
        public void SetParent(Transform? parent,bool worldPositionStays=false)
        {
            if(ReferenceEquals(current,parent))return;
            current?.children.Remove(this);
            current=parent;
            current?.children.Add(this);
        }'''
assert src.count(before)==1, "UNEXPECTED_ORIGINAL_TRANSFORM_ABI"
src=src.replace(before,after)
before2='''        internal override void MarkDestroyed()
        {
            base.MarkDestroyed();
            foreach(var c in comps)c.MarkDestroyed();
        }'''
after2='''        internal override void MarkDestroyed()
        {
            if(Destroyed)return;
            // Unity Destroy(root) recursively destroys owned chassis children.
            // Original source-only #792 shim lacked this behavior.
            foreach(var child in transform.ChildSnapshot())
                child.gameObject.MarkDestroyed();
            base.MarkDestroyed();
            foreach(var c in comps)c.MarkDestroyed();
        }'''
assert src.count(before2)==1, "UNEXPECTED_ORIGINAL_GAMEOBJECT_ABI"
src=src.replace(before2,after2)
path.write_text(src,encoding="utf-8")
print("W27_QA_ONLY_792_SHIM_HIERARCHICAL_DESTROY_PATCH=APPLIED_FROM_EXACT_ORIGINAL_BLOB")
print("W27_QA_ONLY_REAL_UNITY_PHYSX=NOT_RUN")
