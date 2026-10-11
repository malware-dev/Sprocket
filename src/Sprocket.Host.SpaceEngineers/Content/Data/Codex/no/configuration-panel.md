# Konfigurasjonspanel

Konfigurasjonspanelet er der en ![](mark:installation) [installasjon](#installation) får blokkene og ![](mark:signal) [signalene](#signal) sine. Hver installasjon har sitt eget. Det viser også det den som laget ![](mark:logic-system) [logikksystemet](#logic-system), skrev om det, så noen som aldri har sett det, kan installere det.

## Åpne det

På [startskjermen](#home):

- trykk `+ installasjon` under et logikksystem for å legge til en installasjon og fylle plassene, eller
- trykk raden til en installasjon for å bytte blokkene.

Et logikksystem som settes på denne [konstruksjonen](#construct) fra [biblioteket](#library), kommer med én installasjon der ingenting er fylt ut ennå. Trykk raden for å fylle den.

## Hva det viser

- **Navnet på installasjonen**, som du kan endre, og logikksystemet den hører til. Trykker du navnet på logikksystemet, åpnes det.
- **Beskrivelsen** av logikksystemet, skrevet på skjermen til logikksystemet.
- **Hva som er galt**, om noe, med en knapp som åpner ![](mark:sequence) [sekvensen](#sequence) der det gikk galt.
- **Plassene**, med hvor mange som fortsatt mangler en blokk.

Hver [plass](#slot) viser navnet sitt, hva den er til, og hva som fyller den nå. En signalplass viser også verktøylinjeplassen som sender signalet, for eksempel `button 2 is pressed`. En plass der blokken gikk tapt, sier det og nevner blokken.

Navnene og hva hver plass er til, skrives på skjermen til logikksystemet og på siden til hver plass, én gang for alle installasjonene.

## Fylle plassene

Trykk en plass for å [velge](#picking) hva som fyller den. Bare blokker av et slag plassen forventer, blir tilbudt. For en signalplass velger du blokken og verktøylinjeplassen, og den verktøylinjeplassen settes opp for deg.

Bryteren til installasjonen er også på panelet, så du kan slå den på så snart alle plassene er fylt.

## Fylle en plass på nytt

Plassene regnes ut på nytt etter hver endring i logikksystemet, så en plass denne installasjonen har fylt, kan deles i to eller slås sammen til en større. Hver plass som er endret, viser `Endret: fyll den på nytt` i stedet for blokkene sine, og overskriften over plassene teller dem, for eksempel `1 plass å fylle på nytt`.

Blokkene installasjonen hadde, legges til side og vises under plassen etter `Lagt til side, tilbys først:`. Trykk plassen, så viser velgeren dem først, hver merket `Lagt til side`. Velg de som hører hjemme i denne plassen.

Blokkene som er lagt til side, tas vare på til alle plassene til installasjonen er fylt på nytt, og slippes så.
