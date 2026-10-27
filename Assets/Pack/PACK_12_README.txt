ROGUE MACHINE — PACK 12 — PIPELINE SAFE
=======================================

CO NAPRAWIA PACK 12
-------------------
Magenta / pink w Unity oznacza niekompatybilny albo brakujacy shader.

Poprzedni builder probowal wybierac shader po nazwie:
- Standard
- Universal Render Pipeline/Lit
- HDRP/Lit

To jest zbyt kruche.

NOWY SYSTEM
-----------
Pack 12 NIE zgaduje shadera po nazwie.

Kolejnosc:
1. sprawdza QualitySettings.renderPipeline,
2. sprawdza GraphicsSettings.defaultRenderPipeline,
3. sprawdza GraphicsSettings.currentRenderPipeline,
4. pobiera DEFAULT MATERIAL bezposrednio z aktywnego RenderPipelineAsset,
5. klonuje ten material,
6. dopiero na kopii zmienia kolor.

Dla Built-in:
- korzysta z Unity Default-Material.mat.

Awaryjnie:
- tworzy probe Cube
- bierze material, ktory samo Unity przypisalo primitive'owi.

Dzieki temu shader pochodzi z samego aktywnego projektu,
a nie z zalozenia buildera.

MATERIALY SA TWORZONE OD NOWA
-----------------------------
Przy RESET SCENE stare P12 .mat sa kasowane i ponownie tworzone
z aktualnego pipeline template.

Nie moze wiec zostac stary shader po zmianie pipeline.

DIAGNOSTYKA
-----------
Tools
> Rogue Machine
> [12] Pipeline Safe
> DIAGNOSE MATERIAL PIPELINE

Console wypisze:
- Quality RP
- Default RP
- Current RP
- nazwe template material
- nazwe shadera
- czy shader jest supported

HIERARCHIA
----------
Pack 12 zachowuje poprawke Pack 11:
- Hull local origin = 0
- Wheels local origin = 0
- poprawny parenting enemy base
- validator hierarchy

INSTALACJA
----------
1. Zamknij Unity.
2. Wypakuj ZIP.
3. Uruchom:
   INSTALL_PACK_12_PIPELINE_SAFE.bat
4. Otworz Unity.
5. Poczekaj na kompilacje.
6. Uzyj:

Tools
> Rogue Machine
> [12] Pipeline Safe
> RESET SCENE

Jesli nadal zobaczysz magenta:
najpierw uruchom:
Tools > Rogue Machine > [12] Pipeline Safe > DIAGNOSE MATERIAL PIPELINE

i podeslij Console.

ZIP
---
Plaski ZIP, bez folderow.
