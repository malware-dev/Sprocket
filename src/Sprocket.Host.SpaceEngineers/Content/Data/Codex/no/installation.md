# Installasjon

En installasjon er et sett med blokker og signaler et ![](mark:logic-system)&nbsp;[logikksystem](logic-system) virker på, som én luftsluse blant flere. Den fyller [plassene](slot) til logikksystemet med sine egne blokker, så hver luftsluse kjører de samme ![](mark:sequence)&nbsp;[sekvensene](sequence) og [utløserne](trigger).

Hvert logikksystem har minst én installasjon.

## Mer enn én luftsluse

På [startskjermen](home) trykker du `+ installasjon` under logikksystemet og fyller plassene med blokkene til den andre luftslusen på [installasjonspanelet](installation-panel) dens. Begge luftslusene kjører da de samme sekvensene på sine egne blokker, og en endring i logikksystemet når begge, så å rette et steg retter hver luftsluse.

## Bryteren

Hver installasjon har sin egen bryter, så én luftsluse kan slås av mens de andre fortsetter å virke. En ny installasjon starter slått av, siden plassene til en installasjon bare kan fylles mens den er av. Når hver plass den trenger, har fått en blokk, står det `Alle plassene er fylt.` på installasjonspanelet, med `Slå på` ved siden av. En som er slettet og hentet tilbake med ![](mark:undo)&nbsp;[angre](undo), kommer også tilbake slått av.

En installasjon som er slått på og har en tom plass den trenger, starter ingenting, og sier hvilken plass som er tom, på installasjonspanelet og under raden sin på startskjermen. Den forblir slått på, og kjører når plassen er fylt.

Bryteren til logikksystemet går foran deres. Slår du den av, stopper hver installasjon, og slår du den på igjen, starter bare de der bryteren deres er på. Slår du en av dem på, glemmes hvert problem den meldte, og den ser etter dem på nytt.

## Når noe går galt

Startskjermen forteller deg når noe går galt med en installasjon. Kan en sekvens ikke fortsette, for eksempel fordi en blokk gikk tapt, slås den installasjonen av, og raden sier hvorfor:

*Stoppet ved steg 1 i Sluse ut: Luftsluse indre dør gikk tapt.*

Rett opp det den nevner, og slå så installasjonen på igjen.
