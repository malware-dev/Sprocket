# Utløser

En utløser er en linje i et ![](mark:logic-system)&nbsp;[logikksystem](logic-system) som kjører eller stopper en ![](mark:sequence)&nbsp;[sekvens](sequence) når noe skjer, som å kjøre *Sluse ut* når knappen til luftslusen trykkes. Utløsere utløses bare mens logikksystemet og installasjonen er slått på.

En utløser kan utløses av hvilken som helst av disse:

- Et ![](mark:signal)&nbsp;[signal](signal), som at en knapp trykkes eller en dør blir ferdig med å lukke seg.
- Blokker som når en tilstand, som at en dør er lukket. Med flere blokker starter `hvilken som helst` den når den første er framme, og `alle sammen` når den siste er det.
- At en fastsatt tid går, om og om igjen, som hvert minutt. Legg til en slik med `hver`.

## Hver installasjon for seg

Hver ![](mark:installation)&nbsp;[installasjon](installation) har utløserne til logikksystemet for seg selv. Et signal starter en utløser bare for installasjonen der [plassen](slot) holder blokken som sendte det, så knappen til babord luftsluse sluser babord luftsluse og lar styrbord være.

## Kopiere en utløser

En verktøylinjeplass kan bare starte én utløser, så en kopi av en utløser som startes fra en, kommer med en tom plass for seg, som hver installasjon fyller på sitt [installasjonspanel](installation-panel). Enhver annen utløser kopieres hel.
