# UI #943 — portrait parts catalog

Source commit 7bd40c7b9a19d07a30ea0582d00c565ce0aca6b0, PR #950, based on #949. Unity 6000.3.14f1 targeted EditMode 22/22 PASS, exit 0. Tests include actual inventory/controller purchase, selection, repair and confirmed dismantle through API/reflection, plus layout and garage regression. Logs/XML are sanitized and original SHA256 hashes are recorded.

Windows Player render, real mouse clicks, scrolling, modal blocking and full PlayMode NOT RUN. An actual Player screenshot is being prepared; API tests do not prove usability.

A real Windows Player at 7bd, 768x1024, rendered the actual catalog with three owned armor instances (Player-catalog-768x1024.png). The isolated fixture funded and bought items via API and opened the catalog/selected its group via reflection. It does not verify real clicks, scrolling or modal interaction. Build exit 0; after saving the screenshot the automated Player exited with C000041D (-1073740771), cause unconfirmed.
