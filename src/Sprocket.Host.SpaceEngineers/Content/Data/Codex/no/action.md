# Handling

En handling er noe en blokk gjør, en av funksjonene dens, som å åpne en dør eller skyve ut et stempel. Hver slags blokk har sine egne: en dør kan åpnes og lukkes, og en lydblokk kan spille og stoppe.

For et [steg](#step) som gjør en handling, velger du `oppgave` under `+ steg`.

## Finne en

[Velg](#picking) blokkene, så viser `Hva som skal gjøres` hva de kan gjøre, samlet under overskrifter som `Kraft`, `Bevegelse` og `Utseende`. For blokkene automasjonen kjenner, har de vanlige handlingene enkle navn, som `Move to` på et stempel.

`Terminal ›`, ytterst i overskriften, viser i stedet alt blokkene tilbyr i terminalen, under terminalens egne navn. Å bruke dem er ditt eget valg. Den ordnede listen viser dem som er kjent for å virke med automasjonen. Noe automasjonen ikke kan sette ennå, står bare her, dempet, og sier hvorfor når pekeren hviler på det. `‹ Ordnet` går tilbake.

## Hvilke handlinger som tar tid

For de fleste handlinger sier ![](mark:sequence) [sekvensen](#sequence) fra til blokken og går rett videre til neste steg, uten å vente på at blokken blir ferdig, som å spille av en lyd. Noen få tar tid å fullføre, som at en dør lukker seg, at en ventil fyller eller tømmer et rom, eller at et stempel eller en rotor beveger seg dit det ble sendt, og et nytt steg som gjør en av dem, venter på den: neste steg begynner først når blokken er ferdig. Siden om sekvenser forklarer hvordan et steg går rett videre mens blokken fortsatt holder på.

I velgeren har hver handling et merke:

- ![](mark:action) **En pil inn i en blokk** merker en handling sekvensen starter uten å vente på at blokken fullfører den.
- ![](mark:awaited) **En pil inn i en klokke** merker en som tar tid å fullføre. Sekvensen kan vente på den, eller gå rett videre mens den pågår.

Noen handlinger med klokke har et valg i kolonnen ved siden av: med `wait` venter `Recharge` på et batteri eller en hoppmotor til blokken er ladet, og med `move on` går sekvensen rett videre til neste steg, mens `Stockpile` på en gasstank har det samme valget som `until full` og `without waiting`. De venter til du velger noe annet.

Når et steg mislykkes eller venter, for eksempel fordi blokken gikk tapt eller sitter fast, sier siden om sekvenser hva som skjer og hvordan du retter det.

## Flere blokker på én gang

Ett steg kan virke på flere blokker, som alle lysene i luftslusen: kryss av alle i velgeren, så viser `Hva som skal gjøres` det alle sammen kan gjøre. Når handlingen tar tid, er steget ferdig når den siste av dem er det.

## Handlinger som tar valg

Noen handlinger trenger mer å gå på, som hvor de skal og hvor fort. De tar valg, som det spørres etter i en kolonne i velgeren med navnet til handlingen øverst, hvert valg med sitt eget navn:

- `Move to` på et stempel tar en posisjon og en fart.
- `Rotate to` på en rotor tar en vinkel, en fart og hvilken vei den skal dreie.

Et tall skrives inn i boksen sin, og opp og ned flytter det ett steg.

Et valg som må gis, sier fra når det står tomt. Et som kan utelates, kan stå tomt, og da gjør handlingen det den gjør uten det.

## Sette en verdi

En verdi er en av egenskapene til en blokk, som fargen eller radiusen til et lys, og å sette en er en egen oppgave, ved siden av handlingene. Verdiene står under `Hva som skal gjøres` sammen med handlingene, hver merket med ![](mark:value) **`=?`**, merket en verdi har overalt der den nevnes. Velg en, og gi den nye verdien i kolonnen med navnet dens øverst. Sekvensen går rett videre, slik den gjør etter en handling med lyn.

For å sjekke en verdi, og gjøre noe bare hvis betingelsen er oppfylt, bruker du et [hvis](#if).

## Eksempler

### Dørene og ventilen i luftslusen

I *Sluse ut* for luftslusen har `Close` for den indre døra, `Depressurize` for ventilen og `Open` for den ytre døra hver sin klokke, og sekvensen venter ved hver av dem: rommet tømmes først når den indre døra er lukket.

### En stempelarm

For å skyve ut en borearm til 5 m med 1 m/s legger du til en oppgave, velger stempelet i armen, velger `Move to` under `Bevegelse`, og gir 5 som posisjon og 1 som fart. Steget varer til stempelet står på 5 m. Med alle stemplene i armen valgt varer det til det siste av dem kommer dit.

### Lyset i luftslusen

For å gjøre lyset i luftslusen rødt når slusingen starter, legger du til en oppgave og drar den øverst i *Sluse ut*. Velg lyset, velg fargen under `Utseende`, og velg rødt i kolonnen ved siden av. Det ventes ikke på at en verdi settes, så den indre døra begynner å lukke seg rett etterpå.
