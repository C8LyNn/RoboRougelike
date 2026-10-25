# B01-22 / LEGENDARY-UNITY-001 W12 — actual standalone Unity source package

**AUTH:** #224 comment 6401227838. **Contract B01-10:** PR #240 SHA `93f290acca26839792a5bec8ea42415ac83932f3`. **Prior core:** PR #230 SHA `7e9d9ca0a0ef6d4c9bf1ce39fdb187b0d960958d`. **R3 reference:** OBJ #210 SHA `4c9236a4cdc27814ea2e7679872ec4a524150cc8`.

Runtime `Assets/B01Packages/LEGENDARY001/Runtime/LegendaryUnityCore.cs` is an actual Unity6 C# asmdef. No change to shared R3/Pack, no production duplicate inventory. `IAuthoritativeR3Port` must read existing R3 Parts/Inventory and delegate all mutations. Metadata stores only instance-id → legendary-type and reward event identifiers, never copies of physical items. Mount one of each definition per robot incl 0 HP; extra owned copies and different legendaries allowed. Physical GUID unique across live host snapshots. Two weapon families max; ordinary Railgun shares legendary Railgun. Crash-consistent full host save coordination remains integration responsibility.

Strict typed socket proof: WeaponBase / MobilityHub ONLY, missing typed BUILD adapter = REJECT. R3 base-kind geometry is single-cell for Gun/HoverEngine, so real 3×2×2 / 3×3×2 footprint and physics are NOT verified. No Structure wildcard fallback. Missing safe 0HP unmount (R3 Remove loses destroyed part) is rejected until paid R3 repair. Unsupported cross-robot transfer rejected unchanged until an atomic R3 API is provided.

**Hypotheses, not tuned values:** Przebijacz Gun/Railgun WeaponBase size 3×2×2 mass grid [220,435,650] kg; Serce Zawisu HoverEngine MobilityHub size 3×3×2 mass grid [150,465,780] kg. Neither mass finalized; no damage/hover/energy/heat effects implemented.

**Tests:** Compile real source through .NET8 C# harness with explicitly isolated Unity JsonUtility surrogate; 11 pinned B01-10 LEG fixtures + independent negatives/seeded 2500 transitions. Unity NUnit EditMode sources included, native test **NOT_RUN** until licensed environment. Run:
```
dotnet build Tools/B01/LEGENDARY-UNITY-001/tests/LegendaryUnity.SurrogateTests.csproj -c Release
dotnet run --project Tools/B01/LEGENDARY-UNITY-001/tests/LegendaryUnity.SurrogateTests.csproj -c Release --no-build -- artifacts/legendary-unity/test-results.json
python3 Tools/B01/LEGENDARY-UNITY-001/build_package.py artifacts/legendary-unity
```
The Actions artifact contains deterministic full Unity ZIP (including metas), source-manifest.json, source-sha256.txt, zip-sha256.txt, package receipt and test evidence with exact head SHA. B01-40 G0 and B01-60 QA PENDING, native G1 NOT_RUN, G2 HOLD.
