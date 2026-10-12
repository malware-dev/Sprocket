# Kjøre og stoppe

## Kjøre en sekvens

Velger du `kjør` under `+ steg`, får du et [steg](#step) som starter en annen ![](mark:sequence) [sekvens](#sequence) i det samme logikksystemet, valgt fra en liste over sekvensene i det. Et kjør-steg tar tid, slik en handling med klokke gjør: sekvensen kan vente på at kjøringen blir ferdig, eller gå rett videre, og et nytt kjør-steg venter, så neste steg begynner når den sekvensen er ferdig.

En ![](mark:installation) [installasjon](#installation) kjører en sekvens én gang om gangen. Kjører sekvensen allerede, betyr det å vente på kjør-steget å vente på at den kjøringen blir ferdig, og sekvensen teller starten som hoppet over, slik den gjør for en [utløser](#trigger): `hoppet over 1x mens den var opptatt`.

En sekvens som kjør-steg starter, trenger ingen egen utløser.

En sekvens som ville startet seg selv igjen gjennom sekvensene den kjører, er et problem: for eksempel kjører *Gå fra borde* *Sluse ut*, og *Sluse ut* kjører *Gå fra borde*. Kjør-steget viser `Dette kjører en sekvens som kjører denne igjen. Fjern en av de to.`, og logikksystemet kjører først når ett av de to kjør-stegene er fjernet.

## Stoppe en sekvens

Velger du `stopp` under `+ steg`, får du et steg som stopper en annen sekvens i det samme logikksystemet. Det virker med en gang, og neste steg begynner straks. Skjermen til sekvensen som ble stoppet, sier hva som stoppet den, for eksempel `stoppet ved steg 2 av Nedstenging`. Et stopp som finner sekvensen ledig, lar den være som den er.

Et stopp lar blokkene være slik et stopp for hånd gjør, som siden om sekvenser forklarer under *Starte den*.

## Eksempler

### Nedstenging

En *Nedstenging*-sekvens stopper luftslusen uansett hvor i slusingen den er, og stenger den:

1. Stopp *Sluse inn*.
2. Stopp *Sluse ut*.
3. Lukk den indre døra, med klokka.
4. Lukk den ytre døra, med klokka.

De to stoppene virker med en gang, så den indre døra begynner å lukke seg straks. Holdt *Sluse ut* på å tømme rommet, står det `stoppet ved steg 2 av Nedstenging` på skjermen dens.

### Gå fra borde

En *Gå fra borde*-sekvens kjører *Sluse ut* og skyver deretter ut rampen. Kjør-steget venter, så rampen venter på hele *Sluse ut*: den begynner å skyves ut når den ytre døra er åpen.
