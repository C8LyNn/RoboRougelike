# Codex recipient checklist — B01 PACKAGE-AUTO-001, not a G2 dispatch

Use **only after B01-00 expressly issues per-package G2 GO** and B01-60 independent Unity evidence is reviewed. Before that, `verify`/`preflight` may be used in new disposable test worktrees, not the active game.

1. ACK actual game `git rev-parse HEAD`, dirty/unpushed status, protected paths, installed UPM packages and Unity version; preserve existing `Assets/B01Packages.meta`, never rename known GUIDs.
2. Obtain full frozen producer ZIP, validate with PACKAGE-AUTO-001 config, reproduce `preflight` in a new `sandbox`, inspect `actions` (owned files), `preserved_parent` GUID mapping and `conflicts`. Do not copy a PR incremental diff instead of complete package.
3. If G2 authorized, use actual staged source from isolated worktree as an *input* to a new Codex-owned integration branch only; leave full source/hashes/asset GUIDs unchanged, author **only** host wiring/adapters the game needs. Review manifest and ensure no unexpected asset paths or metadata collisions.
4. Run native Unity 6000.3.14f1 compilation, Edit/Play XML, minimal and real Windows URP Player (shader pixel verification), R6 mounted Light/Heavy physics, regeneration/lifecycle, camera functional reticle/corridor, garage/activities, 720/1080 render captures; return raw exit codes, logs, XML, PNG and new target SHA. Get B01-60 independent sign-off.
5. No merge, release, or production integration without renewed B01-00/HQ permission. Preserve the transaction journal for rollback and never delete files changed since import.

Unautomated work: host R3/R4/Pack adapter and gameplay integration, actual native Unity licenses/build/playtest, final owner balance decisions, independent B01-60 G1 and HQ G2. LEGENDARY R1 remains an unintegrated .NET prototype, not a Unity-ready installer.
