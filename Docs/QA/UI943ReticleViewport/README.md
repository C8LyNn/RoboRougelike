# UI #943 — weapon marker projection

Source 6e162b25864870e83980e6f3b23e9d0f1a8e848d, PR #957 based on #956. The RED test modeled the old literal renderer clamps: 1/4 PASS, 3/4 FAIL, exit 2. Unity 6000.3.14f1 integrated targeted EditMode GREEN 27/27 PASS, exit 0; tests cover camera projections, 16:9/4:3/portrait, partial and small viewports, off-screen labels, reticles and existing camera checks. RED is geometric and does not render IMGUI. Actual Player rendering, human aiming/shooting and full PlayMode NOT RUN. Sanitized logs/XML and original hashes included.
