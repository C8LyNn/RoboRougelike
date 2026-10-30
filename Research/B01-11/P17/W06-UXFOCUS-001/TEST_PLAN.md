# Real source test plan — UXFOCUS-001

Execute `dotnet run --project Research/B01-11/P17/W06-UXFOCUS-001/Tests/MenuNavTests.csproj -c Release` against original `Runtime/MenuNavCore.cs`. Dedicated isolated workflow runs original compiled C# on Ubuntu + Windows. **60 test cases per OS**: full 3 vehicle cards x/y and actions, left/right boundary, spatial down to seed/resume/quit, conditional checkpoint, absent optional map/sandbox/quit, seed edit lock, transitioning state, 3/2/1/0 reward card handling, last reward consumed, stale screen or stale slot cannot confirm, results restart, invalid state counts and enums, deterministic navigation, no duplicate focus IDs/actions.

Producer source run: [GitHub Actions 41029585530](https://github.com/C8LyNn/RoboRougelike/actions/runs/41029585530). Record exact final packaging SHA and confirmation run separately before independent acceptance.

**Non-executed checks:** Unity Editor/PlayMode/Windows Player real keyboard/GUI input, 720p/1080p layout, actual R1RunController integration and actual player study NOT_RUN. .NET source tests do not assert these.
