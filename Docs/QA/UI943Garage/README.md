# UI #943 â€” first garage fixes

Game baseline PR #937 `2d67ef3ca4d5e5d094e7d7c588425cc86e703185`; code `9b08683254a297ebe5a08170f0e3cba8264bfde9`. No gameplay/economy changes, no merge.

Player audit showed crowded/tiny text in the actual garage, menu and combat at its initial window size; the original GUI screenshots in this folder are real windows (856Ă—511 logical screenshot size; exact player framebuffer resolution not logged). In a separate Windows Player at command-line 768Ă—1024, the original garage camera visibly rendered above/below the letterboxed GUI. The same Player test after the fix showed the camera confined to the GUI and the catalog opened. These two portrait screenshots were observed in computer-use but NOT saved to files; do not treat them as downloadable evidence. The portrait UI text remains too small and requires further design work.

The original hit area included the legend, weapon groups and activity controls. The fix aligns camera coordinates to the same 1280Ă—720 letterbox as the GUI, updates the viewport after a resize, covers outside bars and excludes those overlays and any area outside the camera from placement. Existing input/vehicle systems remain in place.

Unity 6000.3.14f1: layout tests before 0/10 (expected RED), after 10/10 PASS for 1920Ă—1080, 1366Ă—768, 1024Ă—768, 768Ă—1024 and 3840Ă—2160 mathematical cases; full targeted garage/catalog suite 17/17 PASS exit 0. Windows x64 retail and QA builds succeeded with 0 errors, 6 warnings. Manual Windows UI observation is limited to the initial unspecified-size window and 768Ă—1024. Actual Player screenshots and manual validation at the other four requested sizes, reward/store audits, portrait readability and full PlayMode on this candidate are NOT RUN.

An attempted hidden diagnostic 1366Ă—768 produced all-black screenshots and exit C0000005 despite an internal JSON 'Passed'; neither render nor clean process exit can be claimed as PASS. Preserve this separately from visible Player observations; the earlier native issue remains open.

Published text redacts local user paths and Unity session metadata; original hashes are in `original-hashes.txt`.