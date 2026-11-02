# B01-31 W27 — real P12/W24/W25 armor live demo and native Unity PlayMode acceptance

**Package:** B01-31-W27-REAL-P12-ARMOR-PLAYMODE-DEMO-001/P17-W27-R1. HQ #800. Stacked ONLY on W25 #788 @ dfff0aeaa7098a16621387557a04836bf873fd73; W24 #770 required. No merge or Codex worktree change.

## Actual Unity functionality

- B0131W27ArmorLiveDemoDriver.cs uses ORIGINAL P12 ApplyDamage/RestoreHealthRatio/Die events, real W25 binder and W24 three original 3D models, with on-screen Hit40, Hit20, Repair and Lethal buttons. This is a sandbox test driver only, not canonical gameplay authority.
- Editor/B0131W27CreateArmorLiveDemoScene.cs manually creates one NEW isolated additive Unity .unity scene, by explicit Editor dialog, from existing W24 visual prefab, original P12 component and W25 binder; floor, light and camera. It refuses overwrite, preserves foreign scenes and does not edit Assets/Pack or Save.
- Five new real original NUnit PlayMode [UnityTest] fixtures with isolated .asmdef are prepared. They use actual Unity GameObjects and REFLECTION into original Assembly-CSharp P12/W24/W25 (Unity asmdefs cannot normally reference predefined Assembly-CSharp directly). Editor/PhysX tests HAVE NOT EXECUTED; G1=NOT_RUN.

## Reproduce original source tests and physical bundle

Run .NET8 project Tools/B01-31/P17/W27ArmorLiveDemo001/Harness/W27OriginalRealSourceHarness.csproj. This compiles the FOUR REAL original P12/W24/W25/W27 C# files; ONLY UnityEngine and unrelated R3/R6 are synthetic. Also rerun original W25 source harness. Source tests do not prove Unity native behavior.

Run Python Tools/B01-31/P17/W27ArmorLiveDemo001/build_bundle.py --repo . --out build/B0131W27 and verify_bundle.py with same arguments. They create/read a REAL 8-GUID/20-member .unitypackage. Independent read-only validator rejects eight tampered/foreign GUID/asset cases. Github Actions runs two full identical exports and independent Windows read-only archive receiver. Actual binary SHA256 and original file inventory are in produced receipt.json.

## Real Unity Windows G1 (B01-60; NOT_RUN)

1. Obtain owner permission for a disposable Unity 6000.3.14f1 project; verify exact PR source SHA and all GUIDs.
2. First import W24 three original OBJ model package, manually create W24 visual prefab. Next import W25 additive P12 adapter and W27 original test bundle, no cherry-pick missing dependencies.
3. Choose Tools/B01-31/W27/Create Isolated P12 Armor Live Demo Scene... in Unity; consent and inspect generated new .unity scene under W27/Generated.
4. In PlayMode press Hit40, Hit20, Repair, Lethal; check original P12 HP, three geometry states, separate visual persists after original P12 source destruction. Real URP materials and R6 callbacks can FAIL and must be reported.
5. Run five W27 real NUnit Test Runner PlayMode fixtures, capture Editor.log/NUnit XML + original commit and artifact SHA. Without such evidence G1 stays NOT_RUN and G2 HOLD.

## Boundaries / rollback

Codex currently owns W26 model/weapon/Garage/Save intake, and R41 AutomaticGun original Claude source. This art test package contains none of those. Original P12 and W24/W25 code frozen and unmodified. Only B01-30/40 can independently certify G0; B01-60 native G1; B01-00 later controls offer to Codex #79. No premature inbox notice, merge, user PC access or fake playtest.

Risk MEDIUM: actual source P12 Die may destroy its source and R6 callbacks may require Unity dependencies, so native tests are essential. No automatic install. Remove ONLY new W27 own files/scene after inspecting foreign even-empty directories in approved isolated scratch.
