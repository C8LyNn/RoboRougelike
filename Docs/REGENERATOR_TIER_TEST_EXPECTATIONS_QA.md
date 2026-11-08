# Regenerator tier reservation test correction

Base: `11d4ba96`. The production rule agreed with the user reserves 25 energy for each passive module regardless of tier; the T2/T3 energy-regenerator tests still asserted the former 30/35 reservation. Updated only those expected reservations and their test names. Their checks for increased regeneration (20 and 30 energy per second respectively) remain in place.

- Editor test assembly compilation with Roslyn and existing Unity references: PASS, exit 0. Log: `Artifacts/Local/CSharp-RegeneratorTierAssertions-20261108/editor.log`.
- Unity execution of the two corrected tests: NOT RUN. A separate earlier model check exercised reservations and regeneration without native Unity; it is not a substitute for these actual tests.
- Windows Player and GUI: NOT RUN. No build produced.
