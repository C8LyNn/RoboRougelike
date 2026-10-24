# DEPLOY-001 — moduły rozstawiania na mapie / projekt R1→R2
Issue #144. **DESIGN ONLY**: kodowana paczka później, po scope ACK/slot B01-20 i rozmiarach #141. Nie duplikować MAPGEN #93 ani systemu zaopatrzenia #139. Inspiracje Tyr/Portal są mechaniczne, nie wolno kopiować chronionych assetów.

## Decyzja o pierwszej paczce
**R1 = portable ramp**, jeden działający oryginalny moduł + rozstawiany na podłożu wedge/compound Collider. Powód: pierwsze wykrywanie fizycznego, trwałego obiektu dla ciężkich robotów bez nowego algorytmu lotu; test przejazdu pod kontrolą PR#125. **R2 = jump pad** o skończonym impulsie Rigidbody, bez transform-teleport. Turret, trap i para anchorów dopiero potem; prawdziwe portale render-through osobny research.

## Kontrakt API (wstępny)
`TryDeploy(partId, Ray placementRay, IWorldQuery world, ResourceSnapshot resources, out DeploymentResult result)`.
Wyniki: READY / MODULE_DESTROYED / NOT_CONNECTED / EMP_DISABLED / COOLDOWN / NO_CHARGE / TOO_FAR / INVALID_TERRAIN / TOO_STEEP / OCCUPIED / BLOCKED_LINE_OF_SIGHT / OUT_OF_ARENA / ACTIVE_LIMIT / OK. Atomowe zachowanie: **najpierw** pełny preflight i rezerwacja jednej opłaty **po** prawidłowym wybraniu miejsca; na niepowodzeniu żadna opłata nie znika. Rejestr właściciela i czas życia world actor niezależnie od visual modelu.

R1 ramp config proponowany, do walidacji liczbami w playteście: footprint 2×3 logicznych komórek świata (1,10 m szerokości, 1,65 m długości) jako **minimalny prototyp**, skala/wymiary rzeczywistej szerokiej rampy Heavy muszą pochodzić z PARTSIZE+PHY; brak zatwierdzonego uniwersalnego wymiaru. Na podłożu prefab wspiera Rigidbody i prawidłowe `Collider`, pochylenie zgodne z realną trakcją, test wjazdu/zejścia i zderzeń. Rampa nie otwiera automatycznie MAPGEN ścieżki, walidator mapy musi jawnie określić, czy obiekt dynamiczny jest dozwolony w danej aktywności.
R2 jump: ground-aligned trigger volume fizyczny; tylko wejście faktycznego Rigidbody z odpowiednim kontaktem, impuls ograniczony przez parametr konfiguracji i masę, po kierunku `surfaceNormal` + `forward`. `AddForce(impulse,ForceMode.Impulse)` z limitami pędu/prędkości; brak teleportowania pose, brak nieskończonego odbijania przez ten sam collider. Test dla Light/Heavy, 0/1/2/4 kół, wzniesień i EMP/lost module.

## Ownership / zniszczenie modułu
Proponowana polityka R1: już rozstawiony obiekt działa do końca swojego ograniczonego czasu życia, jeśli pierwotna część zostanie zniszczona; nie można stawiać kolejnego, dopóki moduł nie zostanie odbudowany. Musi to zatwierdzić B01-10 i użytkownik (aktualnie **PROPOSED**). Stan świata jest sprzątany na końcu aktywności, także przy fail/restart; bez przecieku obiektów lub kolizji.

## Testy wymagane
- Brak miejsca; ściana w linii strzału; daleki cel; zajęta bryła; brak gruntu; granica areny; nachylenie; moduł 0HP/rozłączony/EMP; brak zasobów; cooldown; max aktywnych; brak podwójnej opłaty przy konkurencyjnym wywołaniu.
- Prawdziwe fizyczne przejazdy przez rampę Light/Heavy z PR125, koło bez trakcji w powietrzu, nie teleport; reset 3× usuwa wszystkie collidery.
- Jump R2: impuls w jednostkach N·s, mass-/traction-świadomość, lądowanie z prawidłowym kontaktem; zero wielokrotnego trigger spam.
- `ReadOnly` demo + źródła testów najpierw; Unity Edit/Play z logami, EXIT i zrzutami potem; bez native wyników NOT_RUN.

## Interfejsy i konflikty
- BUILD #140 gniazda modułu; PARTSIZE #141 rozmiar i footprint, FIRE #142 rezerwacja pola przed uzbrojeniem; RES #139 / ECON #136 koszty i charge, nie nowy lokalny system amunicji.
- MAPGEN #93 jest właścicielem geometrii procedur, Deploy R1 właścicielem **wyłącznie obiektów rozstawionych po starcie mapy**, nie edytuje generatora.
- Codex jest domyślnym integratorem fizyki/spawn/damage, GPT OSS tylko po specjalnym lokalnym checkout ACK.
- Po napisaniu prawdziwego kodu każdy osobny R1/R2 ma mieć `INSTALL_FOR_CODEX.md`, `PROMPT_FOR_CODEX.md`, manifest, testy i Draft PR. **Ten dokument nie jest paczką gotową do integracji.**
