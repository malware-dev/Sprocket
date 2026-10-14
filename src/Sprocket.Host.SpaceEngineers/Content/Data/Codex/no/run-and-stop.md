# Kjøre og stoppe

Et [steg](step) kan starte eller stoppe en annen ![](mark:sequence)&nbsp;[sekvens](sequence) i det samme logikksystemet, med `kjør` eller `stopp`. Begge virker på den ![](mark:installation)&nbsp;[installasjonen](installation) steget kjører på, så sekvensene til babord luftsluse starter eller stopper aldri styrbord luftsluse.

## Kjøre en sekvens

Et kjør-steg lar en sekvens bygge på en annen: *Gå fra borde* kan kjøre *Sluse ut*, og så skyve ut rampen. Sekvensen venter på den den kjører til den er ferdig, som for ethvert avventbart steg, så rampen begynner å skyves ut når den ytre døra er åpen.

En sekvens som allerede kjører, startes ikke en gang til. Kjør-steget venter i stedet på kjøringen som allerede pågår.

## Stoppe en sekvens

Et stopp-steg avslutter den andre sekvensens kjøring ved steget den har nådd, på samme måte som å stoppe den for hånd, og sekvensen med stopp-steget fortsetter med én gang. En *Nedstenging*-sekvens kan stoppe *Sluse inn* og *Sluse ut*, og så lukke begge dørene, uansett hvor luftslusen var i slusingen.
