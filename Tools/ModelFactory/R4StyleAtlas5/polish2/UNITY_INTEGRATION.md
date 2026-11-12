# Visual Polish Pass 2 — wymagania integracji z Unity 6 URP

Żaden punkt poniżej nie był testowany w Unity. Status wszystkiego: **UNITY_NOT_RUN** (import, shadery, build, wydajność).
To lista wymagań wynikających z tego, jak zbudowane są assety; weryfikacja należy do integracji (Codex).

| temat | co jest w assetach | czego wymaga Unity 6 URP | status |
|---|---|---|---|
| Format | FBX + GLB na wariant, LOD0 i LOD1, tekstury PNG w `textures/` (GLB ma je osadzone) | FBX: materiały trzeba przypisać ręcznie lub remapem po nazwach stref `R4P_*`; GLB: importer glTF (np. glTFast) | NOT_RUN |
| Metaliczność / chropowatość | glTF ORM: G = roughness, B = metallic, mnożone przez współczynniki strefy | URP Lit oczekuje **smoothness w kanale alfa** mapy metallic: konwersja `smoothness = 1 − roughness` przy imporcie albo własny Shader Graph czytający G/B bezpośrednio | NOT_RUN |
| Normalne | tangent-space, konwencja OpenGL (+Y), kafelkowane trójpłaszczyznowo | zaznaczyć teksturę jako Normal map; brak tangentów w eksporcie — Unity liczy je przy imporcie (Mikktspace) | NOT_RUN |
| Kolory wierzchołków `COLOR_0` | wypalone zacienienie zagłębień i przetarcia krawędzi (linear RGB, mnożnik base colour), w GLB i FBX | **domyślny URP Lit ich nie używa**: Shader Graph z węzłem Vertex Color × Base Color (glTFast robi to własnym shaderem). Bez tego modele są jaśniejsze i bardziej płaskie niż na planszach | NOT_RUN |
| Emission i Bloom | strefa `R4P_Glow`, siła emisji 4,0 / 1,5 / 0 / 1,0 / 2,0 (A–E), w GLB przez `KHR_materials_emissive_strength` | HDR + Bloom w Volume (próg ok. 1,0); plansze **nie mają bloomu** (EEVEE bez kompozytora) — w grze światła Sleek będą mocniejsze niż na renderach, do strojenia | NOT_RUN |
| Odbicia środowiskowe | Brutal i Sleek mają metaliczność 0,75–1,0 i czytają się głównie przez odbicia | Reflection Probe areny / skybox z sensowną luminancją; w ciemnym otoczeniu bez próbki stal będzie czarna | NOT_RUN |
| Stylizowane cieniowanie Toon | PBR + malowana rampa po wysokości (`E_base.png`) + `COLOR_0` z chłodnym cieniem | docelowo Shader Graph: 2–3 pasma światła (ramp), rim light, opcjonalny kontur (inverted hull). Assety są przygotowane tak, by działać też bez niego. **Shadera toon w tej paczce nie ma** | NOT_RUN |
| Scrap: powtarzalność rdzy | tekstury kafelkowane; na planszach AFTER każdy obiekt ma przesunięcie UV zależne od pozycji | to samo w shaderze (offset UV z pozycji obiektu / world-space triplanar); bez tego wzór rdzy powtarza się na identycznych częściach | NOT_RUN |
| Współdzielone materiały | 4–6 materiałów stref na styl, wspólne dla wszystkich części i zestawu kadłuba; malowanie = współczynniki | jeden zestaw materiałów na styl + `MaterialPropertyBlock` lub warianty dla drużyn; SRP Batcher | NOT_RUN |
| Animacje | jedna oś czasu „Timeline” na plik, klipy = zakresy klatek z `*_QA.json` (`animation.clips`, `events`); mechanizmy to dodatkowe węzły z własnymi kluczami | podział na klipy przy imporcie wg zakresów; zdarzenia gameplayowe w tych samych klatkach co w źródłach | NOT_RUN |
| LOD | LOD1 na wariant (scalony, bez węzłów mechanizmów i FX) | LODGroup; mechanizmy animują się tylko w LOD0 | NOT_RUN |
| Wydajność | trójkąty scen w `renders/<Styl>/scene_stats.json`; robot 208 części: 66–320 tys. trójkątów w LOD0 zależnie od stylu | profil 30 / 100 / 200 / 400 części: draw calls, CPU / GPU frametime, pamięć | NOT_RUN |
| Warstwa kadłuba | zestaw kafli + manifest + reguły: `hull_layer/README.md` | prototyp `VisualHullPrototype` za flagą | NOT_RUN |

Nazwy węzłów kontraktu, pivoty, gniazda (`Socket_*`, `Port_*`, `Muzzle`, `Deploy_Out`), obrysy i identyfikatory są takie same jak w
zatwierdzonych źródłach. Węzły mechanizmów są dodatkowe, różne w każdym stylu i wymienione w `*_QA.json` (`mechanisms`).
