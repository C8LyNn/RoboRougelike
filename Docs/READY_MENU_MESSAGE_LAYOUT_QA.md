# Rozmieszczenie komunikatów w menu

Kod `38e7fe6e0144750b65b644ec1d69f35628acd85b` przenosi opis wyprawy oraz komunikat zapisu/wczytywania do oddzielnych wierszy w poziomym menu. Komunikat ma szerszy obszar; ostrzeżenie o nieprawidłowym seedzie pozostaje obok. Mechanizmy zapisu, wczytywania i seeda nie zostały zmienione.

Kompilacja C# gry i kodu testów na lokalnych zależnościach: **exit 0/0**. Bez uruchamiania Unity wykonano 36 porównań rzeczywistych prostokątów zwróconych przez skompilowane funkcje HUD, używając `UnityEngine.Rect`: położenie na ekranie i brak przecięć pomiędzy komunikatami, kartami pojazdów oraz przyciskami. **36/36 PASS**. Wykorzystano skompilowany DLL SHA-256 `A06BBE135D46121C6CA81F091452537C456A92CFD90E4EE11EE8A0B5709C571D`, z wcześniejszymi lokalnymi bibliotekami projektu.

[Wyniki JSON](QA/OfflineReadyGeometry-20261108/results.json) i [skrypt](QA/OfflineReadyGeometry-20261108/Run.ps1) są dołączone. Skrypt wymaga lokalnych artefaktów kompilacji i bibliotek Unity/`Library/ScriptAssemblies`; sam raport nie zawiera wszystkich zależności.

**Unity EditMode/PlayMode, renderowanie tekstu, pełne wyświetlanie długich błędów oraz Windows Player: NOT RUN.** Sprawdzenie geometrii nie mierzy szerokości tekstu ani nie dowodzi jego czytelności na rzeczywistym ekranie.
