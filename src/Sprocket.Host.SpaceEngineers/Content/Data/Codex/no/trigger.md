# Utløser

En utløser sier når en ![](mark:sequence)&nbsp;[sekvens](#sequence) kjører. Den er én linje: når noe skjer, gjør én ting. For eksempel: når knappen til luftslusen trykkes, kjør *Sluse ut*.

For å legge til en trykker du `+ utløser` under `Utløsere` på skjermen til ![](mark:logic-system)&nbsp;[logikksystemet](#logic-system), og velger `når`, eller `hver` for en som starter på en fast tid. Trykker du en del av en utløser, kan du endre den: tiden åpnes ved siden av den, og enhver annen del åpner [velgeren](#picking) på den delen.

## Hva som starter den

- Et ![](mark:signal)&nbsp;[signal](#signal) som kommer, som at en knapp trykkes eller en dør er ferdig med å lukke seg.
- Blokker som kommer i en tilstand, som at en dør er lukket. Med flere blokker starter `hvilken som helst` den når den første kommer dit, og `alle sammen` når den siste gjør det.
- En fastsatt tid som går, om og om igjen, som hvert minutt.

Der spillet melder en endring, som at en dør lukker seg eller en blokk slås på, starter utløseren med én gang. For andre endringer, som at lufta i et rom fylles eller tømmes, sjekker utløseren selve blokken, ofte nok til å fange den opp. En utløser som starter på en fast tid, velger den tiden under `Hvor ofte`, i sekunder eller i tikk, seksti på et sekund. En ny starter på ett sekund, i den enheten som ble valgt sist. Trykker du tiden på raden, åpnes det samme valget ved siden av den: `Ferdig` beholder tiden, og Escape eller et trykk et annet sted lar den være som den var.

## Hva den gjør

Den kjører en sekvens eller stopper en.

En utløser gjør den ene tingen sin med én gang. Alt som venter, som å lukke en dør før en annen åpnes, hører hjemme i en sekvens.

## Duplisere en utløser

Kopimerket på raden til en utløser legger til en utløser som gjør det samme. En knapp starter bare én utløser, så en kopi av en knappeutløser starter fra en tom [plass](#slot) for seg selv, som hver installasjon fyller på [installasjonspanelet](#installation-panel) sitt. Alle andre utløsere kopieres hele.

## Hvilken installasjon den starter

En utløser som startes av et signal, kjører bare for den ![](mark:installation)&nbsp;[installasjonen](#installation) der [plassen](#slot) holder blokken som sendte det. Så knappen til babord luftsluse sluser babord luftsluse, og styrbord luftsluse blir stående som den er.
