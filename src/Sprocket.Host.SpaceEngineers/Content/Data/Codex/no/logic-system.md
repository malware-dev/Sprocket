# Logikksystem

Et logikksystem er automasjonen for én maskin, som en luftsluse eller en heis, med alle jobbene som hører til å drive den maskinen. Det virker innenfor [konstruksjonen](construct) det står på, og når ikke utenfor den. En oversikt over alle logikksystemene på konstruksjonen din finner du på [startskjermen](home), som du kommer til ved å finne en programmable block i terminalen og trykke `Automasjon`-knappen på den.

Et logikksystem består av disse delene:

- ![](mark:sequence)&nbsp;[Sekvenser](sequence) sier hva som skjer, steg for steg.
- ![](mark:trigger)&nbsp;[Utløsere](trigger) sier når, for eksempel når en knapp trykkes eller en dør er ferdig med å lukke seg.
- ![](mark:installation)&nbsp;[Installasjoner](installation) sier hvilke blokker som er med.

Jobben holdes atskilt fra blokkene, så ett logikksystem kan kjøre hver luftsluse på skipet. Den [enkle luftslusen](simple-airlock) bygger ett fra begynnelsen.

## Slå det av før du endrer det

Et logikksystem kan bare endres mens det er slått av, så en halvferdig sekvens aldri starter ved et uhell. Nye logikksystemer lages slått av. Bryteren står på overskriften til logikksystemet på startskjermen, og øverst til høyre på logikksystemets egen skjerm.

Slår du det av, slutter utløserne å utløses, og alle sekvenser som kjører, avbrytes, bortsett fra de som er startet manuelt.

![](mark:delete)&nbsp;sletter med én gang; angre henter tilbake det den tok.

## Angre

Hver skjerm har sin egen ![](mark:undo)&nbsp;angre og ![](mark:redo)&nbsp;gjør om: hver sekvens, siden til hver plass, installasjonspanelet og logikksystemets egen skjerm. Angre tar tilbake den siste endringen på skjermen du står på.

Noen endringer når flere skjermer. Velger du blokker til et steg, endres sekvensen, og plassene for blokkene lages eller fylles, og en ny utløser kan lage sekvensen den kjører. En slik endring kan angres på hver skjerm den nådde.

Angre virker alltid. Der en senere endring på en annen skjerm bygger på det som tas tilbake, tar angre den endringen tilbake også, i ett steg, og verktøytipset nevner de skjermene. Gjør om henter dem tilbake sammen.

Navnet til en plass hører til hele logikksystemet, så et nytt navn på en plass angres på siden til plassen eller på logikksystemets skjerm. Et steg som hentes tilbake med angre, nevner plassen ved sitt nåværende navn.

## Installasjonspanelet

Fanen `Installasjonspanel` på logikksystemets skjerm viser det alle som installerer dette logikksystemet, ser på sitt eget [panel](installation-panel): en beskrivelse, så plassene.

For å hjelpe folk å forstå hva logikksystemet er til, eller bare for å minne deg selv på det, kan du skrive en beskrivelse. Den som får plantegningen din, har aldri sett konstruksjonen din, så beskrivelsen og plassene er alt de har å gå på. Beskrivelsen skrives mens logikksystemet er av, og kan inneholde [formatert tekst](rich-text).

Hvert sett med blokker du velger for et steg eller en utløser, blir en [plass](slot). En plass med én blokk får navnet til blokken, og en med flere blir nummerert. En ![](mark:block)&nbsp;**blokkplass**, som *den indre døra*, holder blokkene en sekvens virker på eller følger med på. En ![](mark:signal)&nbsp;**signalplass**, som *knappen*, holder en blokk og verktøylinjeplassen på den som sender [signalet](signal).

Når du har mer enn én installasjon, velger `Valgt installasjon`-listen øverst på skjermen den du jobber med: blokkene og signalene dens vises i utløserne og sekvensene, og mens du endrer logikksystemet ditt, oppdateres denne valgte installasjonen direkte.

## Mer enn én luftsluse

På startskjermen trykker du `+ installasjon` under logikksystemet og fyller plassene med blokkene til den andre luftslusen. Begge luftslusene kjører da de samme sekvensene på sine egne blokker, og en endring i en sekvens når begge.

## Når noe går galt

Startskjermen forteller deg når noe går galt med et logikksystem. Kan en sekvens ikke fortsette, for eksempel fordi en blokk gikk tapt eller en plass er tom, slås den installasjonen av, og raden sier hvorfor:

*Stoppet ved steg 1 i Sluse ut: Luftsluse indre dør gikk tapt.*

Rett opp det den nevner, og slå så installasjonen på igjen.

## Plantegninger

For å ta vare på et logikksystem til senere, eller bruke det på et annet skip, trykker du `Lag plantegning`. Den lagrer en kopi i [biblioteket](library) som en ![](mark:blueprint)&nbsp;[plantegning](blueprint), og når du endrer logikksystemet etterpå, oppdaterer `Oppdater plantegning` kopien. Sett plantegningen på en annen konstruksjon, så får den det samme logikksystemet, klart for sine egne blokker.
