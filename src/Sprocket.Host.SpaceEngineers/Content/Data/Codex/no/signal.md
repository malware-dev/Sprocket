# Signal

Et signal er det en blokk sender for å starte en [utløser](#trigger). Det kommer fra ett av to steder: en plass på verktøylinja til blokken, eller en endring i selve blokken. Når det først er kommet, virker begge slagene likt.

For å starte en utløser med et, legger du til utløseren med `når`, velger blokken og velger signalet under `Hva som skjer`. Begge slagene har ![](mark:signal) **signalmerket** der de står oppført.

## Fra en verktøylinjeplass

Alle blokker med verktøylinje kan sende et signal fra den, for eksempel:

- et knappepanel, når en av knappene trykkes,
- en sensor, når den oppdager noe eller slutter å oppdage det,
- en timerblokk, når den går av,
- en cockpit, når en plass på verktøylinja trykkes.

Hver knapp eller verktøylinjeplass sender sitt eget signal, så én blokk kan starte flere utløsere.

### Sette opp et

Signalet når et ![](mark:logic-system) [logikksystem](#logic-system) gjennom en ![](mark:signal) [signalplass](#slot), som holder blokken og verktøylinjeplassen som sender det. Når signalplassen fylles, settes den verktøylinjeplassen opp for deg og viser `Automasjonssignal`. Gjør den allerede noe annet, blir du spurt om du vil ta den over og erstatte det den gjorde, eller la den være. Angrer du overtakelsen, får verktøylinjeplassen tilbake det den gjorde, så lenge ingenting annet er lagt der siden.

### Når verktøylinjeplassen endres

Tømmes verktøylinjeplassen, eller får den noe annet å gjøre, slutter signalet å komme, og utløseren sier det: *Knappen på Luftsluseknapp ble tømt, så den kan ikke starte dette.* Fyll signalplassen på nytt for å sette opp verktøylinjeplassen igjen.

## Fra en endring i blokken

En blokk sender også et signal når noe ved den endrer seg, og automasjonen hører det fra selve blokken. For eksempel:

- en dør, når den er ferdig med å lukke seg eller ferdig med å åpne seg,
- et sete, når noen setter seg i det eller det blir tomt,
- en blokk, når den begynner eller slutter å virke, eller slås på eller av.

Utløseren leser da for eksempel `When Luftsluse indre dør finishes closing`. Blokken fyller en ![](mark:block) **blokkplass**, samme slag som blokkene til et steg fyller, og verktøylinja dens blir stående som den er.

## Når et signal kommer

Et signal starter utløseren bare for den ![](mark:installation) [installasjonen](#installation) der plassen holder blokken, så knappen til babord luftsluse sluser bare babord luftsluse.

Et signal som kommer mens ![](mark:sequence) [sekvensen](#sequence) det starter allerede kjører, hoppes over, og sekvensen teller det: `hoppet over 1x mens den var opptatt`. Et som kommer mens installasjonen er slått av, hoppes også over, og bare de som kommer fra en verktøylinjeplass, telles: `hoppet over 1x mens den var av`.
