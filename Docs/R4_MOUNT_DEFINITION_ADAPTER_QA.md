# R4 staged BUILD001 definition adapter

The opt-in adapter converts an existing source-catalog entry to a fresh BUILD001 armor definition, preserving its cells, sockets and authored cell bonds. The structural definition retains partial patches. The separate module-support definition filters to full, allowed, non-quarantined patches; it fails closed if no eligible patches exist. It copies arrays so a caller cannot accidentally mutate the source catalog through the returned definition.

This is an isolated building block, **not integrated with garage, preview, placement, restoration, save/load, P12 connectivity or gameplay**. A caller must use the module-support definition specifically for weapons and mobility and the structural definition for armor, taking all neighboring supports into account. The adapter does not solve identity, per-cell runtime links, physical verification, serialization or the source socket discrepancy. It does not validate collider geometry or prove that the chosen ports are physically sound. Unity geometry validation remains required.

Offline .NET 8 compiled the actual adapter and catalog with minimal BUILD001/Unity stand-ins: PASS for full and partial port filtering, octagonal quarantine, multi-cell bonds, unknown IDs and defensive copies. A missing-file build failed before the implementation. These tests do not execute Unity's actual `BuildSocketGeometry`.

**NOT RUN:** Unity compilation/EditMode (targeted tests were added), PlayMode, collider physics, placement, restoration, P12 and Windows Player. Do not enable the new shapes based on this adapter alone.
