# Signal

Et signal er det en blokk sender fra verktøylinja si for å starte en [utløser](#trigger). Alle blokker med verktøylinje kan sende et, for eksempel:

- et knappepanel, når en av knappene trykkes,
- en sensor, når den oppdager noe eller slutter å oppdage det,
- en timerblokk, når den går av,
- en hendelseskontroller, når betingelsen er oppfylt eller ikke lenger er det,
- en cockpit, når en plass på verktøylinja trykkes.

Hver knapp eller verktøylinjeplass sender sitt eget signal, så én blokk kan starte flere utløsere.

## Sette opp et

Et signal når et ![](mark:logic-system) [logikksystem](#logic-system) gjennom en [signalplass](#slot), som holder blokken og verktøylinjeplassen som sender det. Når signalplassen fylles, settes den verktøylinjeplassen opp for deg og viser `Automasjonssignal`. Gjør den allerede noe annet, blir du spurt om du vil ta den over og erstatte det den gjorde, eller la den være.

## Når et signal kommer

Et signal starter utløseren bare for den ![](mark:installation) [installasjonen](#installation) der signalplassen holder det, så knappen til babord luftsluse sluser bare babord luftsluse. Et signal som kommer mens installasjonen er slått av, eller mens ![](mark:sequence) [sekvensen](#sequence) det starter allerede kjører, hoppes over. Sekvensen teller hvor ofte det skjer.

## Når verktøylinjeplassen endres

Tømmes verktøylinjeplassen, eller får den noe annet å gjøre, slutter signalet å komme, og utløseren sier det: *Knappen på Luftsluseknapp ble tømt, så den kan ikke starte dette.* Fyll signalplassen på nytt for å sette opp verktøylinjeplassen igjen.
