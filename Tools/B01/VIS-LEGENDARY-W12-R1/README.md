# B01-VIS-LEGENDARY-W12-R1 — actual reworked asset packet

B01-30 owns this isolated package. Receiver: Codex, after B01-40, B01-60 and B01-00 gates. Unity 6000.3.14f1 URP. W12 contract PR #240 SHA 93f290acca26839792a5bec8ea42415ac83932f3. NEVER merge automatically.

**M01-018 / LEG-WPN-001** — reused actual original PR #195 SHA a8f93e9e1fb9cd31ff9e913f472f989b5d6d4f94. Real edited animated GLB committed at binaries/M01-018_LEG-WPN-001_R1.glb, SHA256 795124613e26cf2f4d7d3d59990542d0d2f4b3a65fef911132318748bae589dd; 53 original meshes, 7 materials, 1 original animation, 3 new preview marker nodes. Original visual rest bounding box 1.172×0.9125×3.672 metres: nominal proposed 3×2×2 grid at 0.55m has depth 1.10m, so **real mounting BLOCKED** until owner approves physical footprint. No authoritatively changed mass or collider.

**M04-024 / LEG-MOV-001** — reused actual original PR #196 SHA eef02864b106546b200011a2f0286fe7d9a83a1f. Actual M04 original GLB from Actions artifact 12586794828, SHA256 280bb77e21995b7440fdda5c7551fc135b2b3b5ce6999322708fb670ff979414. R1 reworked GLB SHA256 ab26d649e31a5ea4d21320480c52b3b9781bd7f3752a687f265c24bbf473df4b produced locally and validated 13/13; mesh 105 / 8 materials / 4 original gimbal animation channels retained, BIN unchanged. Existing Blender source GLB used, **no duplicate new model produced**. Rest world AABB 1.5×0.869×1.5 metres, original triangles 29,472. Source tool and GitHub Actions workflow reproduce output from frozen artifact, then pack full two-asset ZIP.

Tool adds named VIS_LEG_*_SOCKET_*, VIS_LEG_*_WEAK_* and VIS_LEG_*_NOMINAL_AIM_AXIS **visual-only empties** and URP material asset-reference policy. They cannot authorize build and are not rigidbody/collider. MobilityHub socket is not currently authoritative; never substitute Structural fallback.

Mass LEG-WPN R0 650kg vs R1 220kg, LEG-MOV R0 780kg vs R1 150kg: all unresolved; no approved mass inferred. Source geometry/animation binary integrity PASS independently, Blender/Unity actual new runtime tests NOT_RUN, G2 HOLD.

The offline B01-30 package ZIP contains actual edited M04 and test tools; M01 is committed in this exact Git branch (offline ZIP deliberately references it rather than mirroring). The GitHub Actions workflow generates a **complete** two-binary Codex ZIP once its actual run succeeds; do not claim workflow PASS before inspecting the run.
