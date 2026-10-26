# MAP-SYSTEM R5 — changelog i decyzje

## Rozwiązane problemy R4

1. Proste płaskie quady dróg zastąpione przez próbkujący generator pełnych siatek o skończonej grubości (nawierzchnia, spód, boki, końce).
2. Wygładzone przejścia wysokościowe z zerowym nachyleniem na końcach odcinka (easing smoothstep w osi Y).
3. Trzy rozbieżności nachyleń R4 w przykładowym Kamieniołomie poprawione przez przeliczenie deklaracji na **maksymalny** kąt wygładzonej nawierzchni.
4. Trzy ścieżki mają kontrolne punkty Béziera (`junction_upper`, `junction_lower`, `bridge_exit`).
5. Zbyt ostry wstępny łuk `junction_lower` został skorygowany do minimalnego promienia około 27,9 m (zależnie od metody próbkowania), dzięki czemu standardowy profil 4,5 m nie jest wykluczony.
6. G1a weryfikuje deklarowany limit nachylenia oraz zmierzoną krzywiznę odcinka.
7. Nowy G1b odpytuje PhysX tylko na jawnych warstwach przeszkód. Brak skonfigurowanej maski jest wynikiem InvalidConfiguration, NIE PASS.

## Czego R5 jeszcze nie dowodzi

- Łączności geometrycznej w skrzyżowaniach i płynności ruchu podczas przejazdu przez wszystkie węzły.
- Dynamicznego przejazdu prawdziwego pojazdu z Rigidbody, kołami, aktywnym zawieszeniem albo gąsienicami.
- Poprawności interakcji AI z zakrzywionymi trasami.
- Balansu mapy podczas walki i dostępu po zniszczeniu wielu elementów jednocześnie.

## R6 — backlog

1. Skrzyżowania modelowane jako lokalne powierzchnie i automatyczne przejścia styczne między drogami.
2. Symulowane przejazdy dla klas pojazdów: kołowy, gąsienicowy, Hover, uszkodzony.
3. Kalibracja łuków, ramp i narożników do realnych gabarytów blokowego pojazdu.
4. Wizualizacja problematycznych odcinków i tras AI w Unity Editor.
5. Bramka G1c: sterowany kontroler fizyczny wykonuje próbę przejazdu i raportuje utknięcia.
