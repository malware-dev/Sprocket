# Plass

En plass er et navngitt sted i et [logikksystem](#logic-system), som *den indre døra*. [Stegene](#step) og [utløserne](#trigger) i logikksystemet bruker plassen, og hver [installasjon](#installation) fyller den med sine egne blokker. Slik kan ett logikksystem kjøre alle luftslusene på et skip.

## To slags

- En **blokkplass** holder blokkene et steg virker på eller følger med på, for eksempel en dør, eller alle lysene i luftslusen.
- En **signalplass** holder en blokk og verktøylinjeplassen på den som sender [signalet](#signal), for eksempel et knappepanel og en av knappene.

## Fylle en plass

Plassene fylles på [konfigurasjonspanelet](#configuration-panel) til installasjonen. Hver plass viser navnet sitt og hva den er til, slik den som laget logikksystemet, skrev dem.

Trykk en plass for å [velge](#picking) hva som fyller den. Bare blokker av riktig slag blir tilbudt. En plass laget av en dør tar dører, og en laget av en ventil tar ventiler. Passer ingenting på denne [konstruksjonen](#construct), sier lista det.

- En blokkplass kan fylles med én blokk eller flere.
- En signalplass trenger en blokk og verktøylinjeplassen på den som sender signalet. Den verktøylinjeplassen settes opp for deg, og du blir spurt først hvis den allerede gjør noe annet.

Overskriften over plassene teller hvor mange som fortsatt mangler en blokk. En plass der blokken gikk tapt, sier det og nevner blokken.

## Hvordan en plass blir til

Når du velger en blokk for et steg eller en utløser, lages en plass med navn etter blokken, som *Luftsluse indre dør*. Velger du den samme blokken igjen, brukes den samme plassen, så en blokk som mange steg bruker, er fortsatt én plass, fylt én gang.

Blokken du valgte, fyller plassen for installasjonen som er valgt under `Bruker blokkene til`. Alle andre installasjoner fyller den på sitt eget konfigurasjonspanel.

::: warning
Når du fjerner det siste steget eller den siste utløseren som bruker en bestemt blokk eller et bestemt signal, forsvinner plassen fra konfigurasjonspanelet, med navnet og hva den er til. Velger du en annen blokk i stedet, lages en ny plass, som alle installasjonene må fylle på nytt. Vil du bytte hvilken blokk en plass holder, fyller du den heller på nytt på konfigurasjonspanelet.
:::

## Navn og beskrivelse

Navnet på hver plass, og en linje om hva den er til, settes én gang, for alle installasjonene, med `Rediger konfigurasjonspanelet`.

## En tom plass

Når en sekvens kommer til en plass som er tom, eller der blokken er borte, slås installasjonen av, og raden sier hvorfor.
