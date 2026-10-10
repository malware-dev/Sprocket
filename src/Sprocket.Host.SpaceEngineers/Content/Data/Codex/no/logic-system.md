# Logikksystem

Et logikksystem automatiserer én jobb, for eksempel en luftsluse, en hangardør eller en borerigg. Hvert av dem på denne [konstruksjonen](#construct) har sin egen overskrift på [startskjermen](#home).

## Hva det består av

- ![](mark:sequence) [Sekvenser](#sequence) sier hva som skjer, steg for steg.
- [Utløsere](#trigger) sier når, for eksempel når et ![](mark:signal) [signal](#signal) kommer, en dør er ferdig med å lukke seg eller en fastsatt tid har gått.
- ![](mark:installation) [Installasjoner](#installation) sier hvilke blokker som er med, for eksempel dørene og ventilen, og blokkene som sender signalene.

Fordi blokkene holdes atskilt fra jobben, kan ett logikksystem kjøre alle luftslusene på skipet ditt.

## En første luftsluse

1. På startskjermen trykker du `+ logikksystem` og gir det navnet *Luftsluse* i overskriften.
2. Under `Sekvenser` trykker du `+ sekvens` og gir den navnet *Sluse ut*.
3. Trykk `+ steg` og så `oppgave`, [velg](#picking) den indre døra, og velg `close` med klokka ved siden av. Siden om sekvenser forklarer klokka.
4. Legg til `depressurize` for ventilen og `open` for den ytre døra på samme måte.
5. Gå tilbake. Under `Utløsere` trykker du `+ utløser` og så `når`, velger knappen til luftslusen og lar den kjøre *Sluse ut*.
6. Forlat skjermen, og slå på logikksystemet når du blir spurt.

## Slå det av for å endre det

Et logikksystem kan bare endres mens det er slått av, så en halvferdig sekvens aldri starter ved et uhell. Et nytt logikksystem starter slått av. Bryteren står på overskriften på startskjermen, og øverst til høyre på logikksystemets egen skjerm.

Slår du det av, stopper alt som kjører, og blokkene blir stående som de er. Når du forlater et endret logikksystem som fortsatt er av, blir du spurt om du vil slå det på.

## Virkelige blokker, tatt vare på som plasser

Blokkene du velger, blir [plasser](#slot). Det finnes to slags:

- En ![](mark:block) **blokkplass**, som *den indre døra*, holder blokker som en sekvens virker på eller følger med på.
- En ![](mark:signal) **signalplass**, som *knappen*, holder en blokk og verktøylinjeplassen på den som sender signalet.

`Viser plassene fylt av installasjonen`, øverst på skjermen til logikksystemet, velger hvilken installasjons blokker stegene viser, og hvilken installasjon en blokk du velger, fyller.

## Mer enn én luftsluse

På startskjermen trykker du `+ installasjon` under logikksystemet og fyller plassene med blokkene til den andre luftslusen på [konfigurasjonspanelet](#configuration-panel). Nå kjører begge luftslusene de samme sekvensene på sine egne blokker, og en endring i en sekvens når begge.

## Konfigurasjonspanelet

Konfigurasjonspanelet er der en installasjon får blokkene og signalene sine. Det åpnes når du trykker `+ installasjon`, eller en installasjon på startskjermen. Det viser alle plassene, blokkene som fyller dem, og hvor mange plasser som fortsatt mangler en blokk.

Fyller du en signalplass, settes verktøylinjeplassen på blokken opp for deg, og du blir spurt først hvis den allerede gjør noe annet.

Den som får plantegningen din, har aldri sett konstruksjonen din, så panelet er den eneste veiledningen. `Rediger konfigurasjonspanelet`, på skjermen til logikksystemet, bestemmer hva det forteller:

- en beskrivelse av logikksystemet, med formatering som overskrifter og lister,
- navnet på hver plass, og hva den er til.

En plass får først navnet på blokken du valgte, som *Interior Door 2*. Gi den navnet *Indre dør* og beskriv den som døra på siden med trykk, så velger den som fyller den, riktig første gang.

## Når noe går galt

På startskjermen lyser en rad grønt mens den kjører, og rødt når den må repareres. Åpne den for å se hvorfor.

Kan en sekvens ikke fortsette, for eksempel fordi en blokk er borte eller en plass er tom, slås den installasjonen av, og raden sier hvorfor: *Stoppet ved steg 1 i Sluse ut: Luftsluse indre dør var borte.* Rett opp det den nevner, for eksempel ved å fylle plassen på konfigurasjonspanelet, og slå på installasjonen igjen.

## Ta vare på og dele det

`Lag plantegning` lagrer en kopi i [biblioteket](#library) som en ![](mark:blueprint) [plantegning](#blueprint). Fra da av står det `Oppdater plantegning` på den samme knappen, og den oppdaterer kopien i biblioteket. Setter du plantegningen på en annen konstruksjon, får du et logikksystem med én installasjon, der plassene fylles på konfigurasjonspanelet.
