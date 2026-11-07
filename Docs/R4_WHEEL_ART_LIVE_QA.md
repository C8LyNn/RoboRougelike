# R4 wheel visual integration

The supplied R4 wheel is attached to the player's existing R3 vehicle assembly. Its rotor turns from Rigidbody point velocity. The original tire collider, R6 grounded-contact solver, damage handling and wheel stats remain authoritative; the imported model adds no colliders. The old tire renderer remains available as a fallback when the catalog or required model nodes are unavailable. The supplied rotor radius matches the existing physical tire radius of 0.39 m. The catalog contains references to eleven supplied movement models, but **only the wheel is connected to the vehicle in this change**.

Source assets: movement PR #941 `c6b655d67c90ef91d7799460a0c0101756c4b975`; integration report PR #963 `03af5207143da76faeeb8004543a51f26c7f9c0d`. Base: R4 saw integration PR #984. Previous intake found four failing raw FBX axis checks; these remain open and are not claimed fixed here.

## Native Unity 6000.3.14f1 results

- **PASS 2/2:** movement and weapon Resources catalog tests (`r4-movement-catalog-green.xml`).
- **PASS 5/5:** physical wheel art, grounded driving, destroyed/detached contact loss, tier-two wheel physical stats, and all three vehicle drive/fire/damage/reset regression (`r4-wheel-art-final.xml`, process exit 0).
- The wheel test uses the actual R3 light vehicle and R6 drive, retains and checks its original tire colliders, verifies four grounded wheels, real movement and visible rotor motion. Like the existing B01 ground-contact test, it disables the normal input controller and recovery only inside the controlled fixture to avoid competing with the explicitly driven input.
- Earlier versions of that new test failed while using a conflicting input fixture and then an unsuitable fixed-step wait. These failures are retained in the local diagnostic XML files; the final test uses the established physics fixture and unchanged movement assertion.

## Limitations

- **NOT RUN:** Windows Player and human visual review at this revision; full EditMode and PlayMode suites; other movement model integrations; enemy wheel art; garage placement preview and animation clips.
- Imported art visual proportions and rotation direction need review during actual gameplay. No independent art approval is implied.
- The historical automated Windows close produced C0000005 on the earlier #982 build; its cause is unresolved. No shutdown fix is claimed.
