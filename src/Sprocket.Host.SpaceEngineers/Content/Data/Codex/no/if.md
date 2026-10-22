# Hvis

Noen ganger trenger du at en ![](mark:sequence)&nbsp;[sekvens](sequence) gjør det ene eller det andre, avhengig av tilstanden til blokkene når den kjører. En luftsluse med én enkelt knapp er det vanlige tilfellet: når rommet har luft, skal knappen sluse deg ut, og når det er tomt, sluse deg inn.

Et hvis, også kalt en "betinget setning", er [steget](step) for det. Det sjekker blokker, og kjører stegene under seg bare når betingelsen er oppfylt. Legg til `otherwise`, så kjører stegene der når betingelsen ikke er oppfylt, og én sekvens dekker begge veiene gjennom luftslusen.

Tilstandene en blokk kan være i, kommer først i listen, som at en dør er åpen eller virker, og leses som blokken selv: *if Luftsluse dør is open*. Alle andre verdier nevnes foran blokkene sine, *if power on Luftsluse dør is on*. Hvordan den sammenlignes, her `is`, er en egen del: trykk på den for å velge en annen, som `is not`, eller `is below` for et tall.

## Mer enn to veier

Rommet i en luftsluse er ikke alltid fullt eller tomt. Står det halvveis, er det ingen av delene, og da er det tryggest å lukke begge dørene. For et slikt tilfelle legger du til et `otherwise if` med en egen betingelse:

- `if` rommet har luft, sluse ut.
- `otherwise if` rommet er tomt, sluse inn.
- `otherwise`, lukk begge dørene.

Hvis-steget prøver grenene fra toppen, og kjører den første der betingelsen er oppfylt. Bare én gren kjører: når en har gjort det, hoppes resten over, og sekvensen fortsetter etter hele hvis-steget. `otherwise` kjører bare når ingen av betingelsene var oppfylt.

## Flere blokker

Når du trenger å vite noe om mer enn én blokk, som alle dørene på en hangar, velger du dem alle. Et ord foran verdien sier da hvordan de leses. Trykk på det, eller bruk nedtrekkslisten i blokkolonnen i [verdivelgeren](selector), for å velge:

- `any`: én av dem er nok til at betingelsen er oppfylt.
- `all`: alle må oppfylle den.
- `the sum of`: verdien lagt sammen over blokkene, *if the sum of uranium on Reaktor 1 and Reaktor 2 is below 10*.
- `the average of`: den summen delt på hvor mange blokker som ble lest.

Summen og gjennomsnittet virker på tall, så velger du en av dem, står bare tall igjen i listen over hva som kan sjekkes. Ordet vises bare med mer enn én blokk, der de fire gir ulike svar.

## Logikksystemets tilstander

Et hvis kan sjekke en av logikksystemets egne [tilstander](logic-system-states) i stedet for blokker, som *hvor*, som holder siden av luftslusen ingeniøren gikk til. Velg `Tilstander` i `Kilde`-boksen øverst i den første kolonnen i velgeren, for et `if` eller et `otherwise if`, så leses det *if hvor is inne*. Det virker som ethvert annet hvis, og bytter du boksen tilbake til `Blokker`, beholder det stegene i seg.

## Sjekke én gang, ikke vente

Et hvis ser på blokkene sine én gang, i det øyeblikket sekvensen kommer til det. Er ikke betingelsen oppfylt da, venter ikke hvis-steget på at den blir det. Trenger du at noe skjer så snart en blokk når en tilstand, uansett når det er, bruker du en [utløser](trigger) i stedet.

Drar du den første raden i et hvis, flyttes hele hvis-steget med alt i det.
