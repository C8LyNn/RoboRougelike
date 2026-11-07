# First encounter energy — native verification

A small Player screenshot initially suggested 500/600, but native reproduction showed that reading was incorrect. No production behavior was changed. New tests navigate the real Ready → garage → route → first combat flow for each starter vehicle and compare the live VehicleEnergyBank against its actual maximum.

Unity 6000.3.14f1 EditMode at `f49dd405461a39c833fec958571805d0a138eca9`: 22/22 passed, exit 0. Verified light 500/500, standard 600/600 and heavy 800/800, with existing energy regeneration and spend tests. Local evidence: `Artifacts/Local/RC2-InitialEnergyAll-f49dd405-20261107T232831Z/`.
Windows Player after this test-only commit: not separately rebuilt. This does not prove all later transitions or save/load cases.
