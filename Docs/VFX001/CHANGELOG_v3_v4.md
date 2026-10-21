# Zmiany VFX-001 v3 → v4

1. Nowy `ArmorScarController`: wielokrotnie używane nakładki na uszkodzony pancerz; stały limit do 16 na część; `VfxScarResolver` dobiera kolor, rozmiar i gęstość pęknięć.
2. Nowy proceduralny Shader URP `VFX001ArmorScarURP.shader`, z parametrem ziarna / pęknięć; dodatkowy materiał w menu generatora.
3. Nowy `BurningModuleFxController`: ciągłe emitery ognia i dymu bez logiki obrażeń i fizyki, opcjonalny LOD emisji.
4. `VfxCoreDestructionTimeline` i `VfxCoreDestructionDirector`: warstwy o przesuniętych czasach i deterministycznych pozycjach; lokalny PRNG bez zakłócania `UnityEngine.Random`.
5. `DetachedModuleVfxController`: cząsteczki odłączonej i poruszającej się części, bez oddziaływania na Rigidbody.
6. `VfxDebrisMeshLibrary`: tetraedryczne fragmenty metalowe i gumowe. Zmieniono renderowanie `fragments` w `VfxBurstActor` na mesh particles.
7. Rozbudowano interaktywny demonstrator i testy.

## Ograniczenia
- Brak własnej integracji z produkcyjnym HP / physics; adaptery należy wykonać z aktualnego projektu R6/MOV.
- Ślady z `ArmorScarController` to płaskie quady, nie deformacje geometrii i nie wolumenowe decale; na zakrzywionych powierzchniach potrzebna adaptacja.
- VFX mogą korzystać z `MaterialPropertyBlock` na małych overlay rendererach; przetestuj SRP Batcher i GPU przy bardzo wielu częściach.
- Ciągłe emitery nie korzystają jeszcze ze wspólnego globalnego menedżera limitującego je między pojazdami; nie dołączać ognia/dymu do każdego bloku bez pomiarów.
- Materiały w buildzie Windows: najpierw utwórz assety menu, dołącz je do builda i sprawdź `Resources`.
