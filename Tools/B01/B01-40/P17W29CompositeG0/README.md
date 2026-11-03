# B01-40 P17/W29 — wyłączny #826 V3, formalna kontynuacja G0 #807

**Zadanie:** integralność jednej ścieżki Save/Load dla aktualnych dokładnych producentów, bez ingerencji w Codex i bez dodatkowego writer-a. Ten PR jest następcą B01-40 [#807](https://github.com/C8LyNn/RoboRougelike/pull/807) i nowym fizycznym G0 źródeł, NIE produkcyjnym Unity Save.

## Wybrana zależność

| Właściciel | PR@SHA | Warunek |
|---|---|---|
| Save/Load nowy | #826 @ `99fb08619c78b3834bb4c0450499babf991f218a` | **jedyny pełny writer** V3, Tools-only |
| Authority | #787 @ `f5db40bf293fe9582ec7c6fcb3515aeff6fcf937` | **reference-only** oryginalny .owner; NIE instalować raw Store obok #826 |
| Fizyczny świat | #792 @ `16cb2a7b5158605b15191b6074f9e392e0d1ebd3` | zastępuje starsze #772; native binder brak |
| Ekonomia | #780 @ `b4df10a7ad7e6c758f8331013f6b216b3a3689a3` | oryginalna jedna V7 binary SECTION, NIE osobny writer |
| ColdRestart | #750 @ `60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb` | poprawia TYLKO fixture oryginalnego #690 R5 |
| Frozen | #729, #556, #510 | niezmienione oryginalne blob SHA |

**Zabronione równoległe instalacje:** old #797 writer, raw #787/#749/#773, V6 #793/#764, #753/#741/#794, old #772 i duplicate #732. Nie używać `git merge` całości starych gałęzi i nie zakładać, że oryginalna prywatna Rig[] #510 jest podłączona.

## Co wykonuje niezależny kod G0

`intake.py` to **wersjonowane rozwinięcie oryginalnego wykonawczego B01-40 #807**, nie skopiowany nowy produkt. Kontroluje dokładne relacje przodków i zmiany producentów, wskazane frozen blob SHA, 5-producer fizyczne `.meta` GUID/asmdef/m_Script z łączonym source closure, oryginalny #780 floor65+PAID0, oryginalny #750 generation4→5, ścieżkę właściciela R3 oraz oznaki poprawnego nowego #826 SHA256/Create/ComputeHash, File.Replace/Move(2args), Flush(true), brak trzech starych API #797. Wykonuje na rzeczywistym systemie plików staging wyłącznie NOWEGO scratch i testy odmowy rollbacku pliku zmodyfikowanego oraz pustego folderu obcego. Wytwarza JSON per-file SHA256+GUID bez używania live Codex worktree.

`probe_original826.py` osobno pobiera **oryginalne bajty Git** #826 V3 dwóch produkcyjnych C# i producenta 9 testów, prawdziwe #780 ekonomii, oryginalne #729 gateway/owner oraz frozen #556/#510. Kompiluje **netstandard2.1/C#9** i **.NET8/C#9**, uruchamia oryginalne dziewięć testów rzeczywistego lokalnego filesystem (wyłącznie scratch): 1/2 generation, File.Replace before/after lost ACK cold restart, checksum/IDs/genuine V7 and loaner. Nie używa syntetycznego Unity ani nie uruchamia Unity Editor. CI Windows2025+Ubuntu24 i trzeci job **fizycznie pobierający dwa artefakty** kontroluje identyczny SHA manifestu/Git źródeł.

## Bramki odbioru

`SOURCE_TRANSPORT_G0=PASS` dopiero po wykonanym CI na finalnym QA SHA. `SOURCE_NETSTANDARD21_ABI=PASS` dopiero po kompilacji oryginalnych bajtów. Te wyniki **nie** są `Unity G1`. Actual Tools-only #826 nie ma MonoBehaviour single-session, aktualnej prywatnej #510 Rig[]/AtomicDockLedger, legalnych gameplay WIN receipt, sprawdzonej migracji Save i realnego Windows Player. B01-23/20/62 i Codex potrzebują jednej autorytatywnej implementacji binder-a; B01-60 wykonuje natywne Unity 6000.3.14f1 Edit/Play, cold shutdown, corrupt, lost ACK i rzeczywiste ruchy Rigidbody, log Editor/NUnit XML/Player.log.

Zgodnie z #812 i #837 `ANTI_SOFTLOCK=RED` dopóki B01-00 nie wybierze zgodnej ekonomicznie ścieżki kontynuacji/końca runu i nie zostanie przetestowana w grze.

**Bezpieczny odbiór:** tylko czysty, NOWY katalog scratch. Porównać per-file SHA/Unity GUID/asmdef; zabronić istniejących plików, symlink, nieznanych katalogów, a przed jakimkolwiek późniejszym merge mieć jawny zakres Codex HEAD/worktree. Nie ruszać bieżącego Unity projektu, player saves, main/develop ani aktywnego worktree. Brak automatycznego rollbacku obcych danych. B01-00 decyduje o ewentualnym nowym #79 handoff, a nie B01-40.

**Receivers:** B01-41/B01-61 independent security/ID/concurrency, B01-23/B01-20 gameplay binder, B01-60 native G1, B01-62 source owner, B01-00 governance.
