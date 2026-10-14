# Bibliotek

Biblioteket er der du tar vare på ![](mark:blueprint)&nbsp;[plantegningene](blueprint) dine og styrer ![](mark:logic-system)&nbsp;[logikksystemene](logic-system) som er laget av dem. Åpne det med `Bibliotek` på [startskjermen](home). Det lister tre grupper:

- `På konstruksjonene dine`: denne [konstruksjonen](construct), med hvert logikksystem på den.
- `Sikkerhetskopier`: logikksystemer som er tatt vare på fra konstruksjoner som mistet automasjonen sin.
- `I lageret`: plantegningene dine, som du kan bruke overalt.

::: player
Plantegninger i lageret lagres på datamaskinen din, så alle verdener du spiller på den, kan bruke dem. Å kopiere filen til en plantegning tar den med til en annen datamaskin.
:::

Velg en rad for å se hvem som lagde den, når den sist ble endret og hva den er til.

## Lage en plantegning

`Lag plantegning` legger en kopi av et logikksystem i lageret. Fra da av er de to koblet: en endring i logikksystemet blir stående på konstruksjonen til du trykker `Oppdater plantegning`, og en endring i plantegningen når en konstruksjon bare der noen trykker `Oppdater fra biblioteket`. Begge oppdateringene spør først, siden de erstatter det som var der.

`Tilbakestill til plantegning` setter et logikksystem tilbake til plantegningen, og fjerner hver endring som er gjort siden plantegningen sist ble oppdatert. Både denne og `Oppdater fra biblioteket` lar installasjonene beholde blokkene sine, i hver plass plantegningen fortsatt har.

## Sette en på en konstruksjon

`Legg til på denne konstruksjonen` setter logikksystemet til plantegningen her, med én ![](mark:installation)&nbsp;[installasjon](installation) å fylle. Det tilbys bare mens ingen logikksystem fra den plantegningen står på konstruksjonen, for en andre luftsluse er en andre installasjon av det ene logikksystemet, aldri en andre kopi av det.

`Lag en kopi` setter et nytt logikksystem på denne konstruksjonen, kopiert fra raden du valgte, og åpner det så du kan endre det. Kopien har ingen tilknytning til det den ble kopiert fra, og får en egen plantegning først når du trykker `Lag plantegning`. Slik bygger du noe nytt av noe som virker, som en hangardør av en luftsluse.

## Slette

Å slette en plantegning fra lageret er for godt, men logikksystemene som allerede er laget av den, fortsetter å kjøre, og `Lag plantegning` lager en ny plantegning av hvilket som helst av dem.

## Sikkerhetskopier

Når en konstruksjon mister den siste programmerbare blokken sin, tas logikksystemene dens vare på under `Sikkerhetskopier`, oppført under navnet til konstruksjonen med den nyeste først. Et logikksystem biblioteket allerede har, uendret siden plantegningen sist ble oppdatert, overlates til den plantegningen.

En sikkerhetskopi virker som en plantegning: `Legg til på denne konstruksjonen` setter den på denne konstruksjonen slik den er, og `Lag plantegning` gjør den om til en plantegning, enten som en oppdatering av den den kom fra, hvis den fortsatt ligger i lageret, eller som en ny. Installasjonene dens ble igjen hos konstruksjonen den kom fra, så den kommer med én ny installasjon å fylle.

::: player
Sikkerhetskopier lagres sammen med verdenen de ble tatt i, så biblioteket til en annen verden viser dem ikke.
:::

## Deling

Du kan dele en plantegning fra biblioteket med alle, med fraksjonen din eller med ingeniører du velger ved navn, og den dukker opp i biblioteket deres. Du kan trekke tilbake en deling når som helst, men den som allerede har satt plantegningen din på en konstruksjon, beholder det logikksystemet, og kan lage egne plantegninger av det.
