# Steg

Et steg er én linje i en ![](mark:sequence) [sekvens](#sequence): en oppgave, en venting eller et hvis. Knappene på slutten av en sekvens, `+ oppgave`, `+ vent`, `+ kjør`, `+ stopp` og `+ hvis`, legger til et.

## Oppgave

En oppgave er én operasjon:

- en handling på blokker, som `open` eller `depressurize`,
- å sette en av verdiene deres, som `Radius` eller `Color` på et lys,
- å kjøre eller stoppe en annen sekvens.

En handling er noe en blokk gjør, og en verdi er en av egenskapene dens.

## Venting

En venting bruker en fastsatt tid før neste steg. En handling som tar tid, som at en dør lukker seg, ventes det allerede på, så en venting er for selve tiden, for eksempel for å gi mannskapet et øyeblikk til å gå gjennom.

## Hvis

Et hvis sjekker blokker én gang, når sekvensen kommer til det, og kjører stegene under det der det som sjekkes stemmer. Radene lyder `if`, `otherwise if` og `otherwise`:

- `otherwise if` sjekker noe annet, der ingenting over det stemte,
- `otherwise` kjører der ingenting over det stemte.

Hvis-et slutter med en egen ordløs rad, der `+ ellers hvis` og `+ ellers` legger til en gren.

## Flytte steg

Dra et steg for å flytte det. Drar du den første raden i et hvis, flyttes hele hvis-et, med alt i det. For å flytte et steg inn i en gren eller ut av en, drar du selve steget.

Hver linje er nummerert i én telling, radene til hvis-et også. Et problem nevner et steg med det nummeret, for eksempel *Stoppet ved steg 4 i Sluse ut*.

## Endre et steg

Trykker du en del av et steg, som blokkene eller handlingen, åpnes [velgeren](#picking) som laget det, på den delen.

Et steg kan settes på pause med pausemerket på raden: sekvensen hopper over det til det gjenopptas.

Steg kan bare endres mens ![](mark:logic-system) [logikksystemet](#logic-system) er slått av.
