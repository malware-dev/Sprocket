# Logikksystem

Et logikksystem er automasjonen for én maskin, som en luftsluse eller en heis, med alle jobbene som hører til å drive den maskinen. Det virker innenfor [konstruksjonen](construct) det står på, og når ikke utenfor den. En oversikt over alle logikksystemene på konstruksjonen din finner du på [startskjermen](home), som du kommer til ved å finne en programmerbar blokk i terminalen og trykke `Automasjon`-knappen på den.

Et logikksystem består av disse delene:

- ![](mark:sequence)&nbsp;[Sekvenser](sequence) sier hva som skjer, steg for steg.
- ![](mark:trigger)&nbsp;[Utløsere](trigger) sier når, for eksempel når en knapp trykkes eller en dør er ferdig med å lukke seg.
- ![](mark:installation)&nbsp;[Installasjoner](installation) sier hvilke blokker som er med.

Jobben holdes atskilt fra blokkene, så ett logikksystem kan kjøre hver luftsluse på skipet. Den [enkle luftslusen](simple-airlock) bygger ett fra begynnelsen.

## Slå det av før du endrer det

Et logikksystem kan bare endres mens det er slått av, så en halvferdig sekvens aldri starter ved et uhell. Nye logikksystemer lages slått av. Bryteren står på overskriften til logikksystemet på startskjermen, og øverst til høyre på logikksystemets egen skjerm.

Slår du det av, slutter utløserne å utløses, og alle sekvenser som kjører, avbrytes, bortsett fra de som er startet for hånd.

![](mark:delete)&nbsp;sletter med én gang; angre henter tilbake det den tok.

## Installasjonspanelet

Under sekvensene står det alle som installerer dette logikksystemet, ser på sitt eget [panel](installation-panel): en beskrivelse, så plassene.

For å hjelpe folk å forstå hva logikksystemet er til, eller bare for å minne deg selv på det, kan du skrive en beskrivelse. Den som får plantegningen din, har aldri sett konstruksjonen din, så beskrivelsen og plassene er alt de har å gå på. Beskrivelsen skrives mens logikksystemet er av, og kan inneholde [rik tekst](rich-text).

Hvert sett med blokker du velger for et steg eller en utløser, blir en [plass](slot). En plass med én blokk får navnet til blokken, og en med flere blir nummerert. En ![](mark:block)&nbsp;**blokkplass**, som *den indre døra*, holder blokkene en sekvens virker på eller følger med på. En ![](mark:signal)&nbsp;**signalplass**, som *knappen*, holder en blokk og verktøylinjeplassen på den som sender [signalet](signal).

Når du har mer enn én installasjon, velger `Installasjon`-listen øverst på skjermen den du jobber med: blokkene og signalene dens vises i utløserne og sekvensene, og mens du endrer logikksystemet ditt, oppdateres denne valgte installasjonen direkte.

## Mer enn én luftsluse

På startskjermen trykker du `+ installasjon` under logikksystemet og fyller plassene med blokkene til den andre luftslusen. Begge luftslusene kjører da de samme sekvensene på sine egne blokker, og en endring i en sekvens når begge.

## Når noe går galt

Startskjermen forteller deg når noe går galt med et logikksystem. Kan en sekvens ikke fortsette, for eksempel fordi en blokk gikk tapt eller en plass er tom, slås den installasjonen av, og raden sier hvorfor:

*Stoppet ved steg 1 i Sluse ut: Luftsluse indre dør gikk tapt.*

Rett opp det den nevner, og slå så installasjonen på igjen.

## Plantegninger

For å ta vare på et logikksystem til senere, eller bruke det på et annet skip, trykker du `Lag plantegning`. Den lagrer en kopi i [biblioteket](library) som en ![](mark:blueprint)&nbsp;[plantegning](blueprint), og når du endrer logikksystemet etterpå, oppdaterer `Oppdater plantegning` kopien. Sett plantegningen på en annen konstruksjon, så får den det samme logikksystemet, klart for sine egne blokker.
