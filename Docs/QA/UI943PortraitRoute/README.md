# UI #943 — portrait route

Source 2af5ba7a3c3476fd48dff8d391e6103c752f7847, PR #951 based on #950. Unity 6000.3.14f1 EditMode: RED 2/2 expected missing methods, exit 2; GREEN 22/22 PASS, exit 0, covering route layout, portrait geometry, menu and garage regression. Sanitized logs/XML and original hashes provided. Windows Player image and actual clicking are in progress; full PlayMode and five-resolution interactive QA NOT RUN.

Actual Windows Player at 2af5, seed 2026, 768x1024: stage 2 displays two readable offers in Player-stage2-768x1024.png. The fixture advanced stage 1 via API damage and confirmed rewards; no player clicks were tested. The first fixture attempt failed because stage 1 legitimately provides only one offer; its failure log is retained. The stage-2 automated Player exited C000041D after saving the screenshot, cause unconfirmed. Manual choice and full interactive QA remain NOT RUN.
