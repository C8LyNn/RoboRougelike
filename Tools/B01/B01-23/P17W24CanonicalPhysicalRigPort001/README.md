# B01-23 W24 — REAL-510-PHYSICAL-PORT-001 / 1.0.0



NEW standalone Unity6 world implementation, child of solely selected original canonical #729. Actually uses ORIGINAL frozen #510 SecureRigCatalog + AtomicDockLedger (no fork), frozen #556/W06 codec/consumed IDs 2310001 Light, 2310002 Heavy. Actual Unity Rigidbody + BoxCollider/GameObject hierarchy/parent, world position/quaternion, mass, dynamic/kinematic, linear+angular velocity, real ID consumption and physics state Capture; each partially allocated physical object is owned immediately, inactive during stage, published only after quiescent canonical gateway. Deferred Unity Object.Destroy never considered physically retired until real null equality; false/throw must quarantine and retain OS owner.



**Precise old private Rig[] boundary:** original frozen #510 SecureDebrisPlaygroundV2 is sealed and its Rig[] is private, with no public Save export/hydrate. This new CanonicalPhysicalRigPortW24 implements actual replacement ICanonicalMutationGate/ICanonicalRigFactory, NOT a serializer of an existing live old component. No reflection and no old scene/user Save migration. UI/AI/FixedUpdate/physics writer quiescence must be made true by a future authorized owner replacing/deactivating the old host; merely setting the port flag is insufficient. Real #749 B01-62 single `.owner` session lives in Tools and is NOT installed in Unity; Unity-facing session bridge remains HOLD. Do not coinstall competing #753/#741, use only selected #749 by B01-00 decision.



**Real tests:** .NET8 C#9 producer directly compiles frozen original #510 AtomicDockLedger, SecureRigSpec, original #556 W06 and NEW pure ledger hydrator on Windows+Ubuntu; 24 combinations and 5 additional rollback/foreign ID/consumed tests. Original Unity6 world source is **authored but not natively compiled by .NET**. Three original Unity Physics NUnit `[UnityTest]` physical cases authored only; require genuine activated Unity 6000.3.14f1 Windows Editor.log/NUnit XML; G1=NOT_RUN until that happens. Original Git-only 9 distinct .meta GUIDs, two asmdefs and namespaced ZIP strict safe scratch rollback; G0 independent B01-40/62 PENDING. No original #510/#556/#690/#729/#749 source mutation, no merge, no automatic Windows/SSH access, no Codex ACK/G2.



**Receivers:** B01-20 and B01-62 for one real Unity-facing #749 concrete session, strict physical owner / actual OLD route deactivation; B01-61/41 independent ID/rollback/physics and hostile duplicate owner; B01-40 G0 GUID/script closure, migration safe owned scratch and ZIP; B01-60 owner-operated Unity native G1; B01-00 single selection and integration disposition. Crash/partial cleanup fail closed. Hostile external valid backup + trusted root rollback, parent fsync/powercut and real game save integration NOT PROVEN. Approved economics handled separately; this port only preserves part IDs for later save and cannot invent wallet/loaner state beyond frozen #556.