# Velge blokker

Velgeren er der blokker, og hva som skal gjøres med dem, velges for et [steg](#step), en [utløser](#trigger) eller en [plass](#slot). Kolonnene går fra venstre mot høyre: `Blokker`, så hva som skal gjøres med dem, så, der det som er valgt tar valg, en kolonne med navnet dets øverst som spør etter dem. Kolonnen lengst til høyre beskriver det som er valgt. `Ferdig` bruker det og går tilbake.

## Blokkene

Trykk en blokk for å velge den, eller kryss av flere for å velge alle sammen.

Skriv i `Søk` for å vise bare blokkene der navnet inneholder det du skriver, og velg en type for å vise bare blokker av den typen, eller en familie, som `Dør`, for å vise alle typene i den. Søket står slik du skrev det for skjermen velgeren ble åpnet fra, til du forlater den skjermen, så et søk etter *Luftsluse* fortsatt gjelder når du velger for neste steg.

Blokker som har `Show block in toolbar config` slått av i terminalen sin, er utelatt. Øyet ytterst på overskriften `Blokker` viser dem også, og trykt igjen skjuler det dem. Det blir stående slik du forlot det, i alle velgere og alle verdener.

Når en plass fylles, blir bare blokker av riktig slag tilbudt.

## Hva som skal gjøres

`Hva som skal gjøres` viser hva de valgte blokkene kan gjøre, samlet under overskrifter som `Kraft` og `Åpning`. `Terminal ›`, ytterst i overskriften, viser i stedet alt blokkene tilbyr i terminalen, under terminalens egne navn, og `‹ Ordnet` går tilbake. Se [Handling](#action).

Hvert valg har et merke for slaget sitt:

- ![](mark:action) **En handling**, merket med et lyn, startes uten å vente på at blokken fullfører den, som å slå på et lys.
- ![](mark:awaited) **En handling som tar tid**, merket med en klokke, som å lukke en dør, kan ventes på før neste steg begynner, eller få holde på mens sekvensen går rett videre.
- ![](mark:value) **En verdi**, merket `=?`, er en av egenskapene til blokken, som skal settes eller sjekkes, som `Radius` på et lys.
- ![](mark:signal) **Et signal**, merket med en firkantpuls, er noe en blokk sender, som at en knapp trykkes eller en dør er ferdig med å lukke seg. Signaler tilbys når du velger for en utløser.

## Endre et valg

Trykker du en del av et steg eller en utløser, som blokkene eller handlingen, åpnes den samme velgeren, på kolonnen for den delen.
