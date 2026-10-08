# Sekvens

En sekvens er det et ![](mark:logic-system) [logikksystem](#logic-system) gjør: en liste med [steg](#step) som kjøres ovenfra og ned. Hver ![](mark:installation) [installasjon](#installation) kjører den på sine egne blokker, så én sekvens sluser alle luftslusene på skipet.

Under `Sekvenser` på skjermen til logikksystemet legger `+ sekvens` til en, og trykker du raden til en sekvens, åpnes den.

## Starte den

En [utløser](#trigger) starter den, for eksempel når knappen til luftslusen trykkes. Spill-merket på raden kjører den for hånd, også mens logikksystemet er slått av, på installasjonen som er valgt under `Bruker blokkene til`. Slik prøver du en sekvens mens du bygger den.

En installasjon kjører en sekvens én gang om gangen. En utløser som går av mens den fortsatt kjører, hoppes over, og sekvensen teller hvor ofte det skjer.

Mens den kjører, blir spill-merket til et stopp-merke, som stopper den. En stoppet kjøring starter ikke noe mer. En dør som allerede beveger seg, går ferdig, og en ventil fortsetter å fylle eller tømme rommet, mens et stempel eller en rotor stopper der det er.

## Steg det ventes på

For de fleste handlinger sier sekvensen fra til blokken og går rett videre til neste steg, uten å vente på at blokken blir ferdig, som å slå på et lys eller spille av en lyd. På noen få ventes det, som at en dør lukker seg eller en ventil tømmer et rom: neste steg begynner først når blokken er ferdig. Så ventilen starter når den indre døra er lukket, og den ytre døra åpnes når rommet er tomt.

I [velgeren](#picking) har hver handling et merke:

- ![](mark:action) **Et lyn** merker en handling sekvensen starter uten å vente på at blokken fullfører den.
- ![](mark:awaited) **En klokke** merker en sekvensen venter på at blokken fullfører.

## Når en kjøring mislykkes

En kjøring mislykkes når et av stegene ikke kan gjøres i det hele tatt, for eksempel fordi blokken det virker på, er borte. Installasjonen slås av, de andre installasjonene fortsetter å kjøre, og raden til installasjonen på [startskjermen](#home) nevner steget og hva som gikk galt:

`Stoppet ved steg 1 i Sluse ut: Luftsluse indre dør var borte.`

Rett det den nevner, og slå på installasjonen igjen. En blokk som er borte, eller en [plass](#slot) som aldri ble fylt, fylles på [konfigurasjonspanelet](#configuration-panel) til installasjonen.

## Når et steg venter

Et steg venter når blokken sitter fast, som en rotor som holdes, eller en ventil der lufta ikke har noe sted å gjøre av seg. Steget venter og sier hvorfor, for eksempel `kan ikke tømme - ingen steder å gjøre av luften`.

Rydd veien, så fortsetter det av seg selv: frigjør rotoren, eller gi lufta et sted å gjøre av seg, som en oksygentank med plass i. Vil du gi opp kjøringen, stopper du den for hånd.

## Eksempel: Sluse ut for luftslusen

*Sluse ut* har tre steg, hver av dem en handling med klokke:

1. `close Luftsluse indre dør` varer til den indre døra er lukket.
2. `depressurize Luftsluseventil` begynner når døra er lukket, og varer til rommet er tomt.
3. `open Luftsluse ytre dør` begynner når rommet er tomt, og kjøringen slutter når døra er åpen.

Trykk knappen til luftslusen, og mens den kjører:

- På startskjermen lyser raden til luftslusens installasjon grønt.
- På skjermen til logikksystemet lyser raden til *Sluse ut* grønt. Mens en dør beveger seg eller ventilen tømmer rommet, står det `kjører - venter på dette steget`.
- På skjermen til *Sluse ut* selv lyser steget som kjøres.

Når den ytre døra er åpen, er kjøringen over, og på raden til *Sluse ut* står det `klar`.

Trykker du knappen igjen under kjøringen, hoppes trykket over. Skjermen til *Sluse ut* teller det, for eksempel `hoppet over 1x mens den var opptatt`. Stopper du kjøringen for hånd under steg 2, står det `stoppet ved steg 2 for hånd` på skjermen. Begge dørene forblir lukket.

## Slags steg

Knappene på slutten av en sekvens legger til et steg:

- `+ oppgave` gjør noe med blokker: [en handling](#action) som `open`, eller å sette en av verdiene deres.
- `+ kjør` og `+ stopp` [kjører eller stopper en annen sekvens](#run-and-stop).
- `+ vent` [venter](#wait) en fastsatt tid.
- `+ hvis` [sjekker blokker](#if), og kjører stegene under det der det som sjekkes stemmer.
