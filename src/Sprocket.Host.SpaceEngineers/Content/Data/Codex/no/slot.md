# Plass

En plass er et navngitt sted i et ![](mark:logic-system) [logikksystem](#logic-system), som *den indre døra*. [Stegene](#step) og [utløserne](#trigger) i logikksystemet bruker plassen, og hver ![](mark:installation) [installasjon](#installation) fyller den med sine egne blokker. Slik kan ett logikksystem kjøre alle luftslusene på et skip.

## To slags

- En ![](mark:block) **blokkplass** holder blokkene et steg virker på eller en utløser følger med på, for eksempel en dør, eller alle lysene i luftslusen.
- En ![](mark:signal) **signalplass** holder en blokk og verktøylinjeplassen på den som sender ![](mark:signal) [signalet](#signal), for eksempel et knappepanel og en av knappene.

## Hvordan en plass blir til

Blokkene som [velges](#picking) sammen for ett steg, én utløser eller det et [hvis](#if) sjekker, er ett sett, og hvert sett er én plass som holder alle blokkene sine. En plass med én blokk får navn etter blokken, som *Luftsluse indre dør*. En plass med flere blokker heter *Plass 1*, *Plass 2* og så videre.

- Velger du de samme blokkene igjen, i hvilken som helst rekkefølge, brukes den samme plassen.
- Velger du nøyaktig blokkene til to eller flere plasser, brukes de plassene, så få som mulig.
- Alle andre sett blir en egen plass, så en blokk kan være i mer enn én plass.

Plassene regnes ut på nytt etter hver endring. Seks lys som velges for et steg som slår dem av, er *Plass 1*. Velg lys 1 til 3 for et andre steg og lys 4 til 6 for et tredje, så blir de to plasser: *Plass 1* forsvinner, og det første steget nevner de to mindre plassene. Fjern det andre og det tredje steget, så kommer plassen med alle seks tilbake.

En plass som blir stående, beholder navnet sitt, hva den er til, og blokkene til hver installasjon. Angre tar tilbake et valg sammen med alle endringene det gjorde på plassene.

## Bytte blokkene til et steg eller en utløser

Brukes plassen til et steg eller en utløser bare av det, beholdes plassen når du velger andre blokker for det, med navnet, hva den er til, og blokkene til de andre installasjonene. De nye blokkene fyller den for installasjonen som vises, så lenge de er av et slag plassen forventer. Ellers er de nye blokkene et sett som alle andre, og blir til en plass etter reglene over.

Fjerner du det siste steget eller den siste utløseren som bruker en plass, fjernes plassen. Blokker en annen installasjon hadde i den, legges til side for den installasjonen, og tilbys først når den fyller plassene sine på nytt.

## Fylle en plass

Blokkene du velger for et steg eller en utløser, fyller plassen for installasjonen som er valgt under `Installasjon`. Alle andre installasjoner fyller den på sitt eget [installasjonspanel](#installation-panel).

Skjermen til logikksystemet viser plassene under `Plasser`, hver med blokkene installasjonen som vises, fyller den med. Under blokkene står det hva hver av de andre installasjonene fortsatt må gjøre, for eksempel *Installasjon 2: ny, fyll den*, *Installasjon 2: endret, fyll den på nytt* eller *Installasjon 2: mistet en blokk*.

Trykk blokkene til en plass på skjermen til logikksystemet, eller en plass på et installasjonspanel, for å velge hva som fyller den. Bare blokker av et slag plassen forventer, blir tilbudt. Passer ingenting på denne [konstruksjonen](#construct), sier lista det.

- En blokkplass kan fylles med én blokk eller flere.
- En signalplass trenger en blokk og verktøylinjeplassen på den som sender signalet. Den verktøylinjeplassen settes opp for deg, og du blir spurt først hvis den allerede gjør noe annet.

## Navnet og hva den er til

Trykker du navnet til en plass på skjermen til logikksystemet, åpnes siden til plassen. Det som står der, viser installasjonspanelet til hver installasjon. Det endres mens logikksystemet er slått av.

- **Overskriften** er navnet til plassen. Gi den nytt navn med blyanten der. Hver plass har sitt eget navn.
- **`Hva den er til`** er en boks å skrive i, med overskrifter, lister og fet eller kursiv tekst. `Markering` åpner siden om å [skrive tekst](#writing).
- **`Forventer`** viser hvilke slags blokker plassen tar, for eksempel *Door, Sliding Door*, eller `Hvilken som helst blokk`.

Skriv det for noen som aldri har sett konstruksjonen din. En plass som heter *Interior Door 2* eller *Plass 1*, lar den som fyller den, gjette. Gi den navnet *Indre dør* og si at det er døra på siden med trykk, så vet de hvilken dør de skal velge.

## En tom plass

Når en sekvens kommer til en plass som er tom, eller der blokken gikk tapt, slås installasjonen av, og raden sier hvorfor. En utløser med tom signalplass kan ikke starte, og slår av installasjonen sin på samme måte.
