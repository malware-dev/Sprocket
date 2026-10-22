# Logikksystemets tilstander

Noen ganger må et ![](mark:logic-system)&nbsp;[logikksystem](logic-system) huske noe fra én kjøring til den neste, som hvilken side av luftslusen ingeniøren gikk til. Med begge dørene lukket og rommet tomt ser blokkene like ut uansett. Logikksystemets tilstander er til det: hver tilstand holder én verdi, og hver ![](mark:installation)&nbsp;[installasjon](installation) har sin egen verdi i den så lenge installasjonen finnes, gjennom lagring og innlasting. At luftslusen på babord side husker at ingeniøren er ute, lar verdien til luftslusen på styrbord side være som den var.

En tilstand har enten på eller av, som om en hangar er låst ned, eller ett alternativ av en **type**. En type er et navngitt sett med minst to alternativer, som *side*, med alternativene *inne* og *ute*. Luftslusen holder ingeniørens side i en tilstand som heter *hvor*, som har typen *side*.

## Fanen Tilstand

Logikksystemets tilstander ligger på fanen `Tilstand` på skjermen dets, med typene over seg.

Under `Typer` viser hver type navnet sitt og så alternativene sine. `+ type` legger til en som heter *type 1*, med alternativene *alternativ 1* og *alternativ 2*, og `+ alternativ` etter alternativene til en type legger til ett til. Trykk på navnet til en type for å gi den nytt navn; det nye navnet når fram overalt typen brukes. Trykk på et alternativ for en meny med `Gi nytt navn`, og med `Fjern` så lenge ingenting bruker alternativet og typen beholder to andre.

Under `Tilstander` viser hver tilstand navnet sitt, hva den har (navnet på en type, eller `på eller av`), `starter som` med verdien hver installasjon starter med, og `nå` med hva `Valgt installasjon` har i øyeblikket. `+ tilstand` legger til *tilstand 1*, som har på eller av og starter som av.

Trykk på verdien etter `starter som` for å velge en annen. Listen tilbyr `on`, `off` og hvert alternativ av hver type, så det du velger, bestemmer også hva tilstanden har. Så snart et [steg](step) eller en [utløser](trigger) bruker tilstanden, tilbys den bare verdiene av det den allerede har.

Dra en type eller en tilstand for å flytte den. En som noe bruker, blir stående: ![](mark:delete)&nbsp;på den er nedtonet, og tipset sier hva som bruker den.

## Sette en tilstand

I en ![](mark:sequence)&nbsp;[sekvens](sequence) gir `set state` i menyen til `+ steg` en tilstand en ny verdi. Steget setter bare tilstander, så det åpner **tilstandsvelgeren**, [velgeren](selector) for logikksystemets tilstander, med en gang: velg tilstanden til venstre, og så verdien til høyre: et av alternativene, eller på eller av. Under dem kommer de andre tilstandene som har samme type; velger du en av dem, kopierer steget det den tilstanden har når steget kjører. Steget leses *set hvor to ute*.

## Sjekke en tilstand

Et [hvis](if) kan sjekke en tilstand der det ellers sjekker blokker. Den første kolonnen i velgeren har overskriften `Kilde`, med en boks som velger `Blokker` eller `Tilstander`. Velg `Tilstander`, så viser tilstandsvelgeren logikksystemets tilstander: velg en, så `is` eller `is not`, og verdien, og hvis-steget leses *if hvor is inne*. Det virker som ethvert hvis: stegene under kjører bare hvis det stemmer når sekvensen kommer til det. Trykk på `is` på raden for å bytte mellom `is` og `is not`.

Et `otherwise if` har den samme boksen, og det har også `if` i plussen på en `otherwise`-rad, så enhver gren kan sjekke en tilstand.

Med disse kan en luftsluse med én knapp gjøre det riktige hver gang. Sekvensen sjekker *if hvor is inne*, sluser ut og setter *hvor* til *ute*; `otherwise` sluser inn og setter *hvor* tilbake til *inne*.

## En utløser på en tilstand

På fanen `Utløsere` åpner `when` i menyen til `+ utløser` en velger med den samme `Kilde`-boksen. Med `Tilstander` lager den en utløser som utløses idet en tilstand får en verdi: velg tilstanden, `is` eller `is not` og verdien, og så hva den gjør, som for enhver annen utløser: *When hvor is ute*. Den utløses i det øyeblikket et steg setter tilstanden slik at det stemmer, for installasjonen steget kjørte på. Startverdien til en tilstand utløser den aldri.

## Lage en tilstand underveis

Tilstandsvelgeren avslutter listen over tilstander med `En ny tilstand`, som lager en på stedet. Velg den, så spør den andre kolonnen etter tilstandens `navn`, fylt ut med et nummerert navn som *tilstand 4* som du skriver over, og verdien den `starter som`, som også bestemmer hva den har. Velg så verdien som vanlig.

For å gi den nye tilstanden en egen type velger du `+ En ny type` nederst i listen til `starter som`. Typen vises under listen slik fanen `Tilstand` viser en type: navnet, nummerert som *type 3*, så *alternativ 1*, *alternativ 2* og `+ alternativ`. Trykk på navnet eller et alternativ for å gi det nytt navn; et alternativ kan også fjernes så lenge typen har mer enn to. Tilstanden starter som det første alternativet til typen, og verdien steget setter, eller hvis-steget eller utløseren sammenligner med, er et av alternativene.

Navnet på tilstanden, og på den nye typen, må være et som ingen plass, type eller tilstand i logikksystemet har, og kolonnen sier fra til det er det. `Ferdig` legger til alt som er laget her, sammen med steget eller utløseren, og ett ![](mark:undo)&nbsp;[angre](undo) tar alt tilbake.

Trykk på en hvilken som helst del av raden til en set state for å åpne tilstandsvelgeren igjen. Trykk på en del av et `if`, et `otherwise if` eller en `when`, så åpner velgeren på kilden den bruker; bytter du `Kilde`-boksen der, går den fra blokker til en tilstand eller tilbake, og et hvis beholder stegene i seg.
