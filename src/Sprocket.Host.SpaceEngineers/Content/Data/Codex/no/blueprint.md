# Plantegning

En plantegning er en kopi av et ![](mark:logic-system)&nbsp;[logikksystem](#logic-system) som tas vare på i [biblioteket](#library). Den holder ![](mark:sequence)&nbsp;[sekvensene](#sequence), [utløserne](#trigger) og [plassene](#slot) til logikksystemet, beskrivelsen, og hva hver plass er til. Den holder ingen blokker, siden hver ![](mark:installation)&nbsp;[installasjon](#installation) tar med sine egne.

## Lage en

På [startskjermen](#home) lagrer `Lag plantegning` i overskriften til et logikksystem en kopi i biblioteket. Fra da av står det `Oppdater plantegning` på knappen, og den erstatter kopien i biblioteket med logikksystemet slik det er nå. Til du trykker den, blir en endring værende på denne [konstruksjonen](#construct).

## Sette den på en konstruksjon

I biblioteket velger du en plantegning og trykker `Legg til på denne konstruksjonen`. Den blir et logikksystem på denne konstruksjonen, med én installasjon der plassene fortsatt er tomme. Fyll dem på [installasjonspanelet](#installation-panel).

En konstruksjon kan bare ha en plantegning én gang. Vil du ha en til, lager du en kopi.

## Lage en kopi

`Lag en kopi` i biblioteket setter et nytt logikksystem på denne konstruksjonen som starter som en kopi av det valgte. Bruk det for å bygge noe som virker som et logikksystem du allerede har, for eksempel en hangardør laget av en luftsluse, uten å endre originalen.

- Det kopieres fra raden du valgte: logikksystemet på denne konstruksjonen, sikkerhetskopien eller plantegningen i lageret.
- Det får navn med et tall, for eksempel *Luftsluse 2*, og du står som den som laget det.
- Det har én installasjon der plassene fortsatt er tomme.
- Det har ingen plantegning før du trykker `Lag plantegning`. Det og originalen er atskilt fra da av, og en endring i det ene lar det andre være som det er.

Kopien åpnes med én gang, klar til å endres.

## Ta den av

Minustegnet ved et logikksystem på startskjermen tar det og installasjonene av denne konstruksjonen, etter at du er spurt. Ble logikksystemet endret etter plantegningen, blir du også spurt om plantegningen skal oppdateres. Et logikksystem uten plantegning har en søppelkasse der i stedet, siden det er den eneste kopien. Søppelkassen sletter det med én gang, og angre på startskjermen henter det tilbake, slått av.

## Hver konstruksjon beholder sitt eget

Å oppdatere en plantegning endrer bare kopien i biblioteket. Et logikksystem som allerede er på en annen konstruksjon, fortsetter å kjøre som det var.

Der plantegningen i biblioteket ble endret etter et logikksystem, står det `Oppdater fra biblioteket` på knappen til logikksystemet, på overskriften på startskjermen og i biblioteket. Den setter plantegningen inn i stedet for logikksystemet, etter å ha spurt, og installasjonene beholder blokkene sine i alle plassene plantegningen fortsatt har.

::: player
Plantegningene dine lagres på datamaskinen din, så alle verdener du spiller på den, kan bruke dem. De kommer ikke til en annen datamaskin eller til spillets skylagring av seg selv. Spillet lar en mod lagre filer enten inne i én verden eller på selve datamaskinen, og gir den ingen måte å nå skyen på.

For å ta en plantegning med til en annen datamaskin, eller gi den til en venn, kopierer du fila:

1. Lim inn `%AppData%\SpaceEngineers\Storage` i adresselinja i Filutforsker for å åpne mappa.
2. Kopier fila til plantegningen. Navnet begynner med `sprocket-bp-`, for eksempel `sprocket-bp-Airlock.cog`.
3. Legg fila i den samme mappa på den andre datamaskinen. Åpne så `sprocket-bp.index` der, og legg til navnet på fila på en egen linje.

Plantegningen dukker opp i biblioteket neste gang en verden lastes.
:::
