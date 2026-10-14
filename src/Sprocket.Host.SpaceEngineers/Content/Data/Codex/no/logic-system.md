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

Det et logikksystem gjør, kan bare endres mens det er slått av, så en halvferdig sekvens aldri starter ved et uhell. Et nytt logikksystem starter slått av. Bryteren står på overskriften på startskjermen, og øverst til høyre på logikksystemets egen skjerm.

Slår du det av, stopper alt som kjører, og blokkene blir stående som de er. Når du forlater et endret logikksystem som fortsatt er av, blir du spurt om du vil slå det på.

Å slette er en endring som alle andre. Søppelkassen til en sekvens, søppelkassen til en installasjon og søppelkassen øverst på logikksystemets egen skjerm sletter med én gang. Angre på skjermene til logikksystemet henter tilbake en sekvens, og angre på startskjermen en installasjon eller et logikksystem. Mens logikksystemet kjører, blir søppelkassene stående der de er, men kan ikke trykkes, og de sier hvorfor. Å kopiere en sekvens venter på bryteren på samme måte.

## Virkelige blokker, tatt vare på som plasser

Nedenfor sekvensene viser `Installasjonspanel` det hver installatør leser, satt opp slik [installasjonspanelet](#installation-panel) deres viser det: beskrivelsen, så plassene.

Blokkene du velger, blir [plasser](#slot), som står under `Plasser`. Det finnes to slags:

- En ![](mark:block) **blokkplass**, som *den indre døra*, holder blokker som en sekvens virker på eller følger med på.
- En ![](mark:signal) **signalplass**, som *knappen*, holder en blokk og verktøylinjeplassen på den som sender signalet.

Hver plass viser navnet sitt, hva den er til, og blokkene som fyller den. Trykker du blokkene, velger du andre, og trykker du navnet, åpnes plassens egen side, der du gir den navn og sier hva den er til.

`Installasjon`-listen, øverst på skjermen til logikksystemet, velger hvilken installasjons blokker stegene og plassene viser, og hvilken installasjon en blokk du velger, fyller.

## Beskrivelsen

Beskrivelsen, den første delen av `Installasjonspanel`, sier hva logikksystemet gjør, og hva den som installerer det, trenger å vite. Mens logikksystemet er slått av, er det en boks å skrive i, med overskriften `Beskrivelse`, med overskrifter, lister og fet eller kursiv tekst, og `Markering` åpner siden om å [skrive tekst](#writing).

Den vises ved siden av plantegningen til logikksystemet i biblioteket, og øverst på installasjonspanelet til hver installasjon. Den som får plantegningen din, har aldri sett konstruksjonen din, så beskrivelsen og plassene er den eneste veiledningen.

## Angre og gjør om

Hvert logikksystem har én liste over endringene som er gjort i det, på hvilken som helst av skjermene: sin egen, sekvensenes, sidene til plassene og installasjonspanelene til installasjonene. Pila for å angre på hver av disse skjermene tar tilbake den siste endringen på lista, uansett hvor den ble gjort, og pila for å gjøre om gjør den igjen. Begge går gjennom lista i den rekkefølgen endringene ble gjort, og lista varer hele spilløkten.

På lista står hver endring i stegene, sekvensene og utløserne, plassene, beskrivelsen og navnet, og i navnene til installasjonene og blokkene som fyller plassene deres. Å legge til en utløser er én endring, sammen med sekvensen den laget og knappen den tok over, om den tok over en. Angre hopper over det å slå av og på, å kjøre en sekvens for hånd, hvilken installasjon som vises, og hvordan velgeren viser blokkene.

Angre åpner skjermen der endringen var: sekvensen til et steg, installasjonspanelet til en installasjon for blokkene, og logikksystemets egen skjerm for alt annet.

En endring i det logikksystemet gjør, som et steg, tas tilbake og gjøres igjen bare mens logikksystemet er slått av, slik den ble gjort. Blokkene til en installasjon tas tilbake bare mens den installasjonen, eller logikksystemet, er slått av. Det som kan endres mens det kjører, som et navn, tas tilbake når som helst. Til en endring kan tas tilbake, kan pila ikke trykkes.

## Mer enn én luftsluse

På startskjermen trykker du `+ installasjon` under logikksystemet og fyller plassene med blokkene til den andre luftslusen på installasjonspanelet til den. Da kjører begge luftslusene de samme sekvensene på sine egne blokker, og en endring i en sekvens når begge.

## Når noe går galt

På startskjermen lyser en rad grønt mens den kjører, og rødt når den må repareres. Åpne den for å se hvorfor.

Kan en sekvens ikke fortsette, for eksempel fordi en blokk gikk tapt eller en plass er tom, slås den installasjonen av, og raden sier hvorfor: *Stoppet ved steg 1 i Sluse ut: Luftsluse indre dør gikk tapt.* Rett opp det den nevner, for eksempel ved å fylle plassen på installasjonspanelet, og slå på installasjonen igjen.

## Ta vare på og dele det

`Lag plantegning` lagrer en kopi i [biblioteket](#library) som en ![](mark:blueprint) [plantegning](#blueprint). Fra da av står det `Oppdater plantegning` på den samme knappen, og den oppdaterer kopien i biblioteket. Setter du plantegningen på en annen konstruksjon, får du et logikksystem med én installasjon, der plassene fylles på installasjonspanelet.
