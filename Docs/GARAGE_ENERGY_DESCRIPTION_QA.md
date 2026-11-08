# Garage energy module descriptions

Base: `c10ebf12` (Draft PR #1110).

Garage descriptions for repair pulses, wheel motors and energy regenerators now read their displayed energy and duration values from the existing gameplay constants. The text for an owned repair pulse also uses the pulse duration constant. Gameplay, prices and energy balance are unchanged.

Verification on 2026-11-08: the runtime C# assembly compiled with Roslyn and the existing local Unity references (exit 0). Unity compilation, EditMode, PlayMode and visual checks were NOT RUN under the current lightweight-work constraint. Local compiler log: `Artifacts/Local/CSharp-GarageEnergyText-20261108/runtime.log` (ignored by Git).

Manual follow-up: in the garage select each module both with and without an owned instance; compare the displayed costs and duration with their actual behavior.
