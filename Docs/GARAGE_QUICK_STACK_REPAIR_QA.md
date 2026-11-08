# Zbiorcza naprawa na szybkiej liście garażu

Baza `010d2c13` (Draft PR #1138). Dla wybranej grupy magazynu przycisk naprawy wraz z liczbą i kosztem jest dostępny bezpośrednio w garażu, w obu orientacjach. Korzysta z tego samego planu i istniejących stawek co katalog; wynik podaje komunikat garażu. Bez wybranej grupy przycisk pozostaje nieaktywny. Kontrolka zachowuje stan zablokowania, gdy otwarto ustawienia lub wybór aktywności.

- Runtime i testy Editor: kompilacja Roslyn **PASS** 0/0 z istniejącymi referencjami; logi `Artifacts/Local/CSharp-QuickStackRepair-20261108/`.
- Kontrola logicznych prostokątów na skompilowanym C#: **2/2**, przycisk mieści się w płótnie i nie zachodzi na listę. Pierwsze wywołanie narzędzia sprawdzającego miało niejednoznaczne przeciążenie PowerShell; po poprawieniu samej kontroli wynik 2/2.
- Faktyczne renderowanie, długość tekstów, otwarty modal, interakcje w obu orientacjach, test Unity oraz Player: **NOT RUN**. Test ceny i zasad naprawy na modelu opisano w `Docs/CATALOG_BATCH_REPAIR_QA.md`; nie jest testem nowego przycisku.
- Brak nowego buildu. W przyszłym QA na odizolowanym profilu sprawdź widoczność, zakup/naprawę, mały budżet oraz czy podczas wyświetlania ustawień kliknięcia nie trafiają do przycisku pod spodem.
