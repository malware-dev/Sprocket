# Utløser

En utløser er en linje i et ![](mark:logic-system)&nbsp;[logikksystem](logic-system) som kjører eller stopper en ![](mark:sequence)&nbsp;[sekvens](sequence) når noe skjer, som å kjøre *Sluse ut* når knappen til luftslusen trykkes. Utløsere utløses bare mens logikksystemet og installasjonen er slått på.

En utløser kan utløses av hvilken som helst av disse:

- Et ![](mark:signal)&nbsp;[signal](signal), som at en knapp trykkes eller en dør blir ferdig med å lukke seg.
- Blokker som når en tilstand, som at en dør er lukket. Med flere blokker starter `any` den når den første er framme, og `all` når den siste er det.
- Et tall på blokker som når en sammenligning, som *When stored power on Basebatteri is below 1*. Sammenligningen er `is`, `is not`, `is above`, `is at least`, `is below` eller `is at most`, og utløseren utløses én gang idet den blir oppfylt, og igjen først etter at den har sluttet å være oppfylt. Med flere blokker leser `any` og `all` dem som for en tilstand, mens `the sum of` og `the average of` gjør dem til ett tall, som i et [hvis](if), så velger du en av dem, står bare tall igjen å velge.
- At en av logikksystemets egne [tilstander](logic-system-states) får en verdi, i det øyeblikket et steg setter den, som *When hvor is ute*. Legg til en slik med `when`, og velg `Tilstander` i `Kilde`-boksen i velgeren.
- At en fastsatt tid går, om og om igjen, som hvert minutt. Legg til en slik med `every`.

## Hver installasjon for seg

Hver ![](mark:installation)&nbsp;[installasjon](installation) har utløserne til logikksystemet for seg selv. Et signal starter en utløser bare for installasjonen der [plassen](slot) holder blokken som sendte det, så knappen til luftslusen på babord side sluser den luftslusen og lar den på styrbord side være.

## Kopiere en utløser

En verktøylinjeplass kan bare starte én utløser, så en kopi av en utløser som startes fra en verktøylinjeplass, får en egen tom plass, som hver installasjon fyller på sitt [installasjonspanel](installation-panel). Enhver annen utløser kopieres i sin helhet.
