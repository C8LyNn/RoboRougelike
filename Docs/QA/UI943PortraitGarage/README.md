# UI #943 — portrait garage

Source commit: 4a217ea4db6a8fc5614453ee8e66a5b4dca0388b (PR #949, based on #948).
Unity 6000.3.14f1 targeted EditMode: 30/30 PASS, exit 0, including garage pointer, landscape and portrait camera geometry, portrait menu and map-editor layout guard. XML and Editor.log are sanitized; hashes record the original local files.
Windows Player visual, live construction interaction, orientation switching, full PlayMode and five-resolution interactive QA remain NOT RUN. The portrait catalog is a separate follow-up. Automated screenshot work is in progress. No merge.

Real instrumented Windows Player at 4a217ea4, 768x1024, entered the real garage using PrepareUnifiedRun and captured Player-before-label-fix-768x1024.png. It exposed clipping of the B weapon group label; c809bfb31479ba27ed3e369034c2b05d8bef58f3 adjusts it, and a new Player image is pending. The automated fixture then exited with C0000005 (-1073741819); cause unconfirmed, not a manual shutdown test. Final focused EditMode at c809: 19/19 PASS, exit 0. Actual button and mouse interactions remain NOT RUN.

A new actual 768x1024 Player capture at c809 confirms both A/B weapon labels visible (Player-after-label-fix-768x1024.png). The isolated fixture entered the real garage programmatically; mouse/keyboard interaction remains NOT RUN. It exited C0000005 again after capture; cause remains unconfirmed. Build exit 0; runtime process exit is separately recorded.
