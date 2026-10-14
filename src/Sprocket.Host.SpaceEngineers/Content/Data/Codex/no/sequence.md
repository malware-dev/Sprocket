# Sekvens

En sekvens er det et ![](mark:logic-system) [logikksystem](#logic-system) gjør: en liste med [steg](#step) som kjøres ovenfra og ned. Hver ![](mark:installation) [installasjon](#installation) kjører den på sine egne blokker, så én sekvens sluser alle luftslusene på skipet.

Under `Sekvenser` på skjermen til logikksystemet legger `+ sekvens` til en, og trykker du raden til en sekvens, åpnes den. Søppelkassen på raden sletter den med én gang, og angre henter den tilbake.

## Starte den

En [utløser](#trigger) starter den, for eksempel når knappen til luftslusen trykkes. Spill-merket på raden kjører den for hånd, også mens logikksystemet er slått av, på installasjonen som er valgt under `Installasjon`. Slik prøver du en sekvens mens du bygger den.

En installasjon kjører en sekvens én gang om gangen. En utløser som går av mens den fortsatt kjører, hoppes over, og sekvensen teller hvor ofte det skjer.

Mens den kjører, blir spill-merket til et stopp-merke, som stopper den. En stoppet kjøring starter ikke noe mer. En dør som allerede beveger seg, går ferdig, og en ventil fortsetter å fylle eller tømme rommet, mens et stempel eller en rotor stopper der det er.

Slår du av logikksystemet eller installasjonen, stopper kjøringene ved steget de var kommet til, og når det er slått på igjen, står det `stoppet ved steg 2 da den ble slått av` på sekvensen. En kjøring som er startet for hånd, bryr seg ikke om bryteren og fortsetter.

## Steg som tar tid

For de fleste handlinger sier sekvensen fra til blokken og går rett videre til neste steg, uten å vente på at blokken blir ferdig, som å slå på et lys eller spille av en lyd. Noen få tar tid å fullføre, som at en dør lukker seg eller en ventil tømmer et rom, og et nytt steg som gjør en av dem, venter på den: neste steg begynner først når blokken er ferdig. Så ventilen starter når den indre døra er lukket, og den ytre døra åpnes når rommet er tomt.

I [velgeren](#picking) har hver handling et merke:

- ![](mark:action) **Et lyn** merker en handling sekvensen starter uten å vente på at blokken fullfører den.
- ![](mark:awaited) **En klokke** merker en som tar tid å fullføre. Sekvensen kan vente på den, eller gå rett videre mens den pågår.

## Gå rett videre

Et steg som tar tid, kan la sekvensen gå rett videre, så stegene etter det kjører mens det fortsatt pågår, som når begge dørene i luftslusen lukkes samtidig. Én venting dekker hvert steg som tar tid siden ventingen før, så flere steg kan kjøre samtidig og én venting fanger dem alle. Går sekvensen rett videre fra det siste steget som tar tid, er kjøringen over ved det siste steget, og det den startet, gjør seg ferdig av seg selv.

En verden viser ventingene sine på én av to måter:

- **`Bryter på hvert steg`**: under hvert steg som tar tid henger en linje, koblet til steget slik stegene i et hvis er. `Vent til det er ferdig` venter der på dette steget og alle stegene over det siden forrige venting. Trykker du den, blir den til `Ikke vent`, og sekvensen går rett videre; trykker du igjen, venter den.
- **`Venting som eget steg`**: hver venting er en egen linje som lyder `await all pending tasks`, etter stegene den venter på. Den flyttes, kopieres, settes på pause og slettes som et steg, og `+ steg` tilbyr `avvent alle ventende oppgaver` for å legge til en. Et nytt steg som tar tid, kommer med en etter seg.

Uansett lyser klokka til en venting og klokkene til stegene den venter på når du holder pekeren over den, og klokka til et steg og ventingen som dekker det lyser når du holder pekeren over steget. En pulserende linje ved linjenumrene kobler sammen de opplyste linjene og fortsetter til linjer som er rullet ut av syne; en verden kan slå den av. En venting uten noe steg over seg som tar tid, får en advarsel, siden den ikke har noe å vente på.

## Når en kjøring mislykkes

En kjøring mislykkes når et av stegene ikke kan gjøres i det hele tatt, for eksempel fordi blokken det virker på, gikk tapt. Installasjonen slås av, de andre installasjonene fortsetter å kjøre, og raden til installasjonen på [startskjermen](#home) nevner steget og hva som gikk galt:

`Stoppet ved steg 1 i Sluse ut: Luftsluse indre dør gikk tapt.`

Rett det den nevner, og slå på installasjonen igjen. En blokk som gikk tapt, eller en [plass](#slot) som aldri ble fylt, fylles på [installasjonspanelet](#installation-panel) til installasjonen.

## Når et steg venter

Et steg venter når blokken sitter fast, som en rotor som holdes, eller en ventil der lufta ikke har noe sted å gjøre av seg. Steget venter og sier hvorfor, for eksempel `Luftsluseventil kan ikke tømme fordi det ikke er noe sted å gjøre av luften.`

Rydd veien, så fortsetter det av seg selv: frigjør rotoren, eller gi lufta et sted å gjøre av seg, som en oksygentank med plass i. Vil du gi opp kjøringen, stopper du den for hånd.

## Når noe annet tar over

En kjøring slutter ved steget sitt når noe annet tar over en blokk mens steget venter på den, for eksempel at noen åpner den indre døra fra terminalen mens *Sluse ut* venter på at den skal lukke seg. Installasjonen forblir på. Linjen til steget viser en gul advarsel som nevner blokken og hva som skjedde, til sekvensen kjører neste gang:

`Luftsluse indre dør sluttet å lukke seg fordi noe annet åpnet den.`

Kjør sekvensen igjen, så begynner den på nytt.

## Eksempel: Sluse ut for luftslusen

*Sluse ut* har tre steg, hver av dem en handling med klokke som sekvensen venter på:

1. `close Luftsluse indre dør` varer til den indre døra er lukket.
2. `depressurize Luftsluseventil` begynner når døra er lukket, og varer til rommet er tomt.
3. `open Luftsluse ytre dør` begynner når rommet er tomt, og kjøringen slutter når døra er åpen.

Trykk knappen til luftslusen, og mens den kjører:

- På startskjermen lyser raden til luftslusens installasjon grønt.
- På skjermen til logikksystemet lyser raden til *Sluse ut* grønt. Mens en dør beveger seg eller ventilen tømmer rommet, står det `kjører - venter på dette steget`.
- På skjermen til *Sluse ut* selv lyser steget som kjøres.

Når den ytre døra er åpen, er kjøringen over, og på raden til *Sluse ut* står det `klar`.

Trykker du knappen igjen under kjøringen, hoppes trykket over. Skjermen til *Sluse ut* teller det, for eksempel `hoppet over 1x mens den var opptatt`. Stopper du kjøringen for hånd under steg 2, står det `stoppet ved steg 2 for hånd` på skjermen. Begge dørene forblir lukket.
