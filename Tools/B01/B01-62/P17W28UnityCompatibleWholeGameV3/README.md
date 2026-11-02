# B01-62 | PASS17 / W28 | Unity 6000.3.14f1 API-compatible ONE world + ECON V7 Save

New versioned **sole-replacement candidate** of original [#797](https://github.com/C8LyNn/RoboRougelike/pull/797) @ exact \`2b7983f0f9e09dd1d56906b379a3f7af7b02f21a\`. All original #797 files are IMMUTABLE. The owner must install **either** this V3 implementation **or** the old V2 writer, never both and never raw #787 alongside it. Original selected #787 provides the canonical owner contract **reference only**; original #729 provides the ONLY real OS \`.owner\` lease. No merge/release/Unity Player result is claimed.

## Corrected actual Unity ABI
- Original \`SHA256.HashData\` -> \`SHA256.Create().ComputeHash\` with the same SHA256 content and exactly the same first 16 bytes for the stable part GUID.
- Original \`Convert.ToHexString\` -> byte-by-byte lowercase hexadecimal identical to \`ToHexString(...).ToLowerInvariant()\`.
- Original \`File.Move(source, dest, true)\` -> initial \`File.Move(source,dest)\`; existing canonical destination \`File.Replace(source,dest,null)\`, **no delete gap, no backup file**, same directory validated before publishing. Source flushed with \`FileOptions.WriteThrough\` and \`Flush(true)\` and reread after publish. Lost-ACK quarantines the process owner until a cold reopen.
- Same envelope magic **\`B0162G02\`**, marker **\`slot.canonical-game-v2\`**, original exact B01-22 #780 binary ECON V7 section, original frozen #556 world bytes, same SHA256, same run/slot/generation/IDs. No migration or split writer.

## Executable source evidence
\`WholeGameEconomyUnityV3QA.csproj\`: original 7 OS source scenarios and two new g1->g2 **existing-file** replacement faults (before replace retains g1; after replace lost-ACK reads g2 on a new OS owner). \`WholeGameEconomyUnityV3Abi.csproj\`: exact same two production source files compiled as C#9 / netstandard2.1 along with original #729/#556 original #780 producer sources; this is an API/assembly check, **not** a native Unity Editor import. Windows+Ubuntu original Git source blob guard. Source CI results are only valid for exact final HEAD/Actions logs, not simply because checks are written.

## Gates
Independent B01-40/B01-41/B01-61 G0 must recheck exactly this new candidate and eventual composite (GUID/asmdef, transport, one writer). B01-23 and B01-20 own native game-facing binder and genuine combat/victory authorization. B01-60 must execute real Unity 6000.3.14f1 on the actual candidate with logs, full Save->cold restore physical Rig[]/ledger/wallet/loaner/economy. G1=NOT_RUN until then; G2=HOLD. This package is **Tools-only** and NOT integrated into a playable Unity Player yet.

**Caveat:** \`File.Replace\` supplies atomic same-directory replacement when OS/filesystem supports it; actual power-loss durability of the parent directory is not established (no parent-directory fsync proof). Do not claim powercut resistance.
