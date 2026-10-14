# Steg

Et steg er én linje i en ![](mark:sequence)&nbsp;[sekvens](#sequence). `+ steg`, på slutten av en sekvens og av hver gren i et hvis, legger til et av disse slagene:

- `oppgave` gjør noe med blokker: [en handling](#action) som `open`, eller å sette en av verdiene deres.
- `vent` [venter](#wait) en fastsatt tid.
- `kjør` og `stopp` [kjører eller stopper en annen sekvens](#run-and-stop).
- `avvent alle ventende oppgaver`, som tilbys der hver venting er en egen linje, venter på stegene som tar tid over den, slik siden om sekvenser forklarer.
- `hvis` [sjekker blokker](#if), og kjører stegene under det bare hvis betingelsen er oppfylt.

## Flytte og kopiere steg

Dra et steg for å flytte det, også inn i en gren av et hvis eller ut av en.

Kopimerket på raden til et steg legger en kopi rett etter det. Et hvis kopieres med alle stegene i det.

Hver linje er nummerert i én telling, radene til et hvis også. Et problem nevner et steg med det nummeret, for eksempel *Stoppet ved steg 4 i Sluse ut*.

## Endre et steg

Trykker du en del av et steg, som blokkene eller handlingen, åpnes [velgeren](#picking) som laget det, på den delen.

Et steg kan settes på pause med pausemerket på raden: sekvensen hopper over det til det gjenopptas.

Steg kan bare endres mens ![](mark:logic-system)&nbsp;[logikksystemet](#logic-system) er slått av.
