# Plass

For at ett ![](mark:logic-system)&nbsp;[logikksystem](logic-system) skal kunne kjøre hver luftsluse på et skip, kan ikke [stegene](step) og [utløserne](trigger) dets nevne dørene til én luftsluse. De nevner plasser i stedet: navngitte steder som *den indre døra*, som hver ![](mark:installation)&nbsp;[installasjon](installation) fyller med sine egne blokker.

- En ![](mark:block)&nbsp;**blokkplass** holder blokkene et steg virker på eller en utløser følger med på, som en dør, eller alle lysene i luftslusen.
- En ![](mark:signal)&nbsp;**signalplass** holder en blokk og verktøylinjeplassen på den som sender [signalet](signal), som et knappepanel og en av knappene.
- En ![](mark:block)&nbsp;**skjermplass** holder blokker og hvilken av skjermene til hver blokk, for et steg eller et [hvis](if) som virker på en skjerm. Et tekstpanel har én skjerm, en cockpit eller en reklametavle flere.

Blokkene du velger, fyller plassene til installasjonen som er valgt under `Installasjon`. Hver andre installasjon fyller dem på sitt eget [installasjonspanel](installation-panel).

## Hvordan plasser lages

Du lager ikke plasser selv. Hver gang du [velger](selector) blokker for et steg, en utløser eller et hvis, er blokkene du valgte sammen en gruppe, og logikksystemet lager plassene sine av disse gruppene:

- **Den samme gruppen to ganger er én plass.** Et steg som lukker den indre døra og et som åpner den, bruker begge *Luftsluse indre dør*.
- **En gruppe av andre plasser bruker dem.** Der den indre og den ytre døra hver har en plass fra før, bruker et steg som lukker begge dørene, de to plassene, og lager ingen ny.
- **Enhver annen gruppe er en plass for seg.** Et steg som slår av både den indre døra og lyset i luftslusen, der lyset ikke har noen plass ennå, lager *Plass 1* av de to. Den indre døra er da i to plasser, og hver installasjon fyller begge.

En plass med én blokk får navnet til blokken, og en med flere blir nummerert.

En skjermplass er unntaket fra gruppene: hvert steg eller hvis som virker på en skjerm, har en egen, navngitt på samme måte. Velger du andre blokker eller en annen skjerm for steget, beholder det plassen sin, og valgene til de andre installasjonene. En skjermplass som ingen bruker lenger, forsvinner, som andre plasser.

En plass tar én type blokk: den nærmeste typen alle blokkene i den deler, etter spillets egne typer. Ett innelys, eller flere, lager en plass for `Innelys`. Et innelys og en lyskaster lager en plass for `Lys`, som tar begge. Et lys og en dør lager en for `Blokk som slås av og på`, og blokker som ikke deler noe nærmere, lager en plass som tar hvilken som helst blokk. Siden til plassen viser typen under `Forventer`.

Fordi en plass tar en type framfor én bestemt blokk, tar en plass laget av en stor dør også små dører, og en ![](mark:blueprint)&nbsp;[plantegning](blueprint) laget på et stort rutenett kan installeres på et lite.

Logikksystemet regner ut plassene på nytt etter hver endring, så de kan deles og slås sammen mens du bygger. Seks lys som slås av i ett steg, lager *Plass 1*. Legg til et steg som gjør lys 1 til 3 røde og et som gjør lys 4 til 6 blå, så blir de to gruppene plasser. Sammen er de nøyaktig de seks lysene, så det første steget bruker nå begge, og *Plass 1* forsvinner. Slett de røde og blå stegene, så kommer *Plass 1* tilbake.

En plass som blir stående, beholder navnet sitt, hva den er til og blokkene til hver installasjon. Det gjør også en plass som bare ett steg eller én utløser bruker, når du velger andre blokker for den, så lenge de nye er av typen plassen tar.

## Navnet og hva den er til

Navnet til en plass og `Hva den er til` vises på hvert installasjonspanel, så skriv dem for noen som aldri har sett konstruksjonen din. *Plass 1* lar dem gjette. *Indre dør*, med en linje som sier at det er døra på siden med trykk, forteller dem hvilken dør de skal velge.
