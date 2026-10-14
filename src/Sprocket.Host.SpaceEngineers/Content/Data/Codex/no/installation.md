# Installasjon

En installasjon er ett sted et ![](mark:logic-system)&nbsp;[logikksystem](#logic-system) kjører, som én luftsluse blant flere. Den fyller [plassene](#slot) til logikksystemet med sine egne blokker, for eksempel sine egne dører, sin egen ventil og sin egen knapp.

## Mange installasjoner, ett logikksystem

Et logikksystem for luftsluser kan ha én installasjon for hver luftsluse på skipet. Alle kjører de samme ![](mark:sequence)&nbsp;[sekvensene](#sequence) og [utløserne](#trigger), hver på sine egne blokker. Sluser du gjennom én luftsluse, blir de andre stående som de er.

En endring i logikksystemet når alle installasjonene med én gang. Rett et steg, og alle luftslusene får rettelsen.

## Legge til en

Hvert logikksystem starter med én installasjon, kalt *Installasjon 1*. For å legge til en til trykker du `+ installasjon` under logikksystemet på [startskjermen](#home). [Installasjonspanelet](#installation-panel) til den nye installasjonen åpnes, der du gir den et navn og fyller plassene.

Trykker du på raden til en installasjon, åpnes installasjonspanelet igjen, så du kan gi den nytt navn eller bytte blokkene.

## Bryteren

Hver installasjon har sin egen bryter, så én luftsluse kan slås av mens de andre fortsetter å virke. En ny installasjon starter slått på.

Bryteren til logikksystemet står over dem. Slår du av logikksystemet, stopper alle installasjonene. Slår du det på igjen, starter de som har sin egen bryter på, og de andre forblir av. Mens logikksystemet er av, kan ikke bryterne til installasjonene trykkes.

## Raden

På startskjermen har hver installasjon en rad under logikksystemet sitt. Raden lyser grønt mens en av sekvensene kjører, og rødt når den må repareres. Overskriften til logikksystemet blir rød hvis noen av installasjonene blir det.

En installasjon der plasser er delt eller slått sammen av en endring i logikksystemet, sier det på raden, for eksempel `1 plass å fylle på nytt`. Installasjonspanelet viser hvilke.

Når en sekvens ikke kan fortsette, slås bare den installasjonen av, og raden sier hvorfor. De andre installasjonene fortsetter å kjøre.

## Fjerne en

Søppelkassen på raden til en installasjon sletter den med én gang, og angre på startskjermen henter den tilbake, slått av. Logikksystemet og de andre installasjonene blir beholdt. Søppelkassen kan trykkes mens logikksystemet er slått av, som for alle endringer. Et logikksystem beholder minst én installasjon, så søppelkassen på den siste kan ikke trykkes.
