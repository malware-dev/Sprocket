# Logikksystem

Et logikksystem er automasjonen for én maskin, som en luftsluse eller en heis, med alle jobbene som hører til å drive den maskinen. Det virker innenfor [konstruksjonen](construct) det står på, og når ikke utenfor den. En oversikt over alle logikksystemene på konstruksjonen din finner du på [startskjermen](home), som du kommer til ved å finne en programmable block i terminalen som eies av en av [eierne](changing-hands) av konstruksjonen, og trykke `Automasjon`-knappen på den.

Et logikksystem består av disse delene:

- ![](mark:sequence)&nbsp;[Sekvenser](sequence) sier hva som skjer, steg for steg.
- ![](mark:trigger)&nbsp;[Utløsere](trigger) sier når, for eksempel når en knapp trykkes eller en dør er ferdig med å lukke seg.
- ![](mark:installation)&nbsp;[Installasjoner](installation) sier hvilke blokker som er med.

Jobben holdes atskilt fra blokkene, så ett logikksystem kan kjøre hver luftsluse på skipet. Den [enkle luftslusen](simple-airlock) bygger ett fra begynnelsen.

## Skjermen

Under overskriften har skjermen til et logikksystem `Valgt installasjon`-listen. Når du har mer enn én installasjon, velger listen den installasjonen du jobber med: blokkene og signalene dens vises i utløserne og sekvensene, og mens du endrer logikksystemet ditt, oppdateres denne valgte installasjonen direkte.

Under listen ligger fire faner: `Utløsere`, `Sekvenser`, `Tilstand` og `Installasjonspanel`, der du utformer [installasjonspanelet](installation-panel) hver installasjon fylles ut på.

En fane der siden har et problem eller en advarsel, bærer et merke i fargen til problemet eller advarselen. Skjermen til hvert logikksystem åpner på fanen du sist forlot den på, til verdenen lastes på nytt.

## Slå det av før du endrer det

Et logikksystem kan bare endres mens det er slått av, så en halvferdig sekvens aldri starter ved et uhell. Nye logikksystemer lages slått av. Bryteren står på overskriften til logikksystemet på startskjermen, og øverst til høyre på logikksystemets egen skjerm.

Slår du det av, slutter utløserne å utløses, og alle sekvenser som kjører, avbrytes, bortsett fra de som er startet manuelt.

![](mark:delete)&nbsp;sletter med én gang; ![](mark:undo)&nbsp;[angre](undo) henter tilbake det den tok.

## Plantegninger

For å ta vare på et logikksystem til senere, eller bruke det på et annet skip, trykker du `Lag plantegning`. Den lagrer en kopi i [biblioteket](library) som en ![](mark:blueprint)&nbsp;[plantegning](blueprint), og når du endrer logikksystemet etterpå, oppdaterer `Oppdater plantegning` kopien. Sett plantegningen på en annen konstruksjon, så får den det samme logikksystemet, klart for sine egne blokker.
