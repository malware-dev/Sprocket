# Hvis

Noen ganger trenger du at en ![](mark:sequence)&nbsp;[sekvens](sequence) gjør det ene eller det andre, avhengig av tilstanden til blokkene når den kjører. En luftsluse med én enkelt knapp er det vanlige tilfellet: når rommet har luft, skal knappen sluse deg ut, og når det er tomt, sluse deg inn.

Et hvis, også kalt en "betingelse", er [steget](step) for det. Det sjekker blokker, og kjører stegene under seg bare når betingelsen er oppfylt. Legg til `ellers`, så kjører stegene der når betingelsen ikke er oppfylt, og én sekvens dekker begge veiene gjennom luftslusen.

Tilstandene en blokk kan være i, kommer først i listen, som at en dør er åpen eller virker, og leses som blokken selv: *hvis Luftsluse dør er åpen*. Alle andre verdier nevnes i sjekken, *hvis Luftsluse dør power er på*, med hvordan den sammenlignes valgt ved siden av.

## Mer enn to veier

Rommet i en luftsluse er ikke alltid fullt eller tomt. Står det halvveis, er det ingen av delene, og da er det tryggest å lukke begge dørene. For et slikt tilfelle legger du til et `ellers hvis` med en egen betingelse:

- `hvis` rommet har luft, sluse ut.
- `ellers hvis` rommet er tomt, sluse inn.
- `ellers`, lukk begge dørene.

Hvis-steget prøver grenene fra toppen, og kjører den første der betingelsen er oppfylt. Bare én gren kjører: når en har gjort det, hoppes resten over, og sekvensen fortsetter etter hele hvis-steget. `ellers` kjører bare når ingen av betingelsene var oppfylt.

## Flere blokker

Når du trenger å vite noe om mer enn én blokk, som alle dørene på en hangar, velger du dem alle. Så velger du: `hvilken som helst` av dem er nok til at betingelsen er oppfylt, eller det kreves `alle sammen`.

## Sjekke én gang, ikke vente

Et hvis ser på blokkene sine én gang, i det øyeblikket sekvensen kommer til det. Er ikke betingelsen oppfylt da, venter ikke hvis-steget på at den blir det. Trenger du at noe skjer så snart en blokk når en tilstand, uansett når det er, bruker du en [utløser](trigger) i stedet.

Drar du den første raden i et hvis, flyttes hele hvis-steget med alt i det.
