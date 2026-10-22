# Windows player — diagnostyka zakończenia

Próby retail Direct3D12 (`77f22e2c6c794c15adbd2963179642e1`) i Direct3D11 (`0fa76d75a0a44d8caea927bee1b5a2b0`) po natywnej obsłudze menu, garażu, poligonu i powrotu kończą się po Alt+F4 kodem **-1073741819 / 0xC0000005**. Ten sam kod wystąpił w QA `703ed911019742708a44638d4629820f`. Zakończenie PowerShell z kodem 0 nie jest wynikiem procesu gry. Nie odnotowano odpowiadającego wyjątku C# w obserwatorze.

Windows Application Error 1000 dla własnego procesu wskazuje `UnityPlayer.dll` 6000.3.14.35903, RVA `0xb1b031`. SHA256 biblioteki jest zgodny z lokalnym silnikiem: `BBDF3E73522223EA45C3A54049B716F9DA3E632994636734C5D53326AA2C36D0`.

Odczyt publicznych symboli pasującego PDB (RSDS GUID `223bd7a7-393f-40cf-9b0c-20abedf13a2e`, age 1) umieszcza adres pomiędzy:

- `RuntimeStatic<PlatformAccessibilityManager,0>::StaticDestroy(void*)`, start `0xb1afe0`, przesunięcie awarii **+0x51**;
- następnym publicznym symbolem `StaticInitialize`, start `0xb1b0d0`.

To dopasowanie publicznego symbolu, nie pełny stos prywatnych symboli ani potwierdzenie przyczyny przez Unity. Zdarzenie odpowiada opisowi **UUM-146676**: awaria zamykania Windows Player po interakcji klienta UI Automation/czytnika ekranu. [Oficjalne wydanie Unity 6000.6.0b5](https://unity.com/releases/editor/beta/6000.6.0b5) wymienia jego poprawkę. Nie potwierdzono backportu do wymaganej wersji 6000.3.14f1.

Kontrola D3D11 odrzuca prostą hipotezę specyficzną dla D3D12. Nie zmieniano grafiki, dostępności systemu, pakietów ani wymaganej wersji Unity. Wcześniejsze potwierdzenie użytkownika, że zwykłe zamykanie R6 działa, dotyczy starszego wydania. **Zwykłe ręczne zamknięcie obecnego W2: NOT RUN. Zamykanie po tej automatyzacji: FAIL.** Osobna kontrola człowieka i ewentualna zatwierdzona aktualizacja silnika są kolejnym krokiem; nie wolno zastępować nimi nieuzyskanego PASS.

Surowe zdarzenie Windows, metadane własnych procesów, pasujące symbole i logi pozostają lokalnie w ignorowanym `Artifacts/Local/`; dokument nie publikuje licencji ani danych konta.
