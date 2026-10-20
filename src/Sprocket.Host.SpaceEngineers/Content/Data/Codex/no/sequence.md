# Sekvens

En sekvens er en liste med [steg](step) som et ![](mark:logic-system)&nbsp;[logikksystem](logic-system) kjører ovenfra og ned, som *Sluse ut* i en luftsluse: lukk den indre døra, tøm rommet, åpne den ytre døra. Hver ![](mark:installation)&nbsp;[installasjon](installation) kjører den på sine egne blokker og signaler.

## Starte og stoppe

Mens logikksystemet er slått på, starter en [utløser](trigger) en sekvens, for eksempel når knappen til luftslusen trykkes.

For å prøve en sekvens mens du bygger den, kjører du den manuelt med ![](mark:play)&nbsp;på raden. En manuell kjøring virker selv om logikksystemet er slått av, og bruker `Valgt installasjon`.

En installasjon kjører en sekvens bare én gang om gangen, så sekvensen går glipp av en utløser som utløses mens den fortsatt kjører, og skjermen dens teller hvor mange.

Å stoppe en kjøring avslutter den ved steget den har nådd, men en blokk som allerede beveger seg, kan likevel gå ferdig: en dør som lukker seg, lukker seg ferdig, og en ventil fortsetter å tømme rommet, mens et stempel eller en rotor stopper der de er. Å slå av logikksystemet eller installasjonen stopper kjøringene på samme måte, bortsett fra de som er startet manuelt.

## Steg som går over mer enn ett [tikk](tick)

De fleste handlinger skjer innen ett tikk, som å slå på et lys. Andre tar lengre tid, som at en dør lukker seg eller en ventil tømmer et rom. De som er merket ![](mark:awaited), er **avventbare**: sekvensen venter på at blokken blir ferdig før neste steg, så ventilen starter når den indre døra er lukket. Et steg som [kjører en annen sekvens](run-and-stop), er også avventbart, og venter til den sekvensen er ferdig.

En sekvens kan bare vente på avventbare handlinger, altså de der blokken kan følges til slutten. En handling merket ![](mark:action)&nbsp;startes, og sekvensen går videre selv om blokken fortsetter: en lyd spilles til den er slutt, og en timerblokk teller ned, mens de neste stegene kjører. For å gi en slik blokk tid legger du til en [forsinkelse](delay) på en fastsatt tid etter den, eller lar en utløser starte den neste sekvensen når blokken når tilstanden du trenger.

## Kjøre steg sammen

Når du legger til et avventbart steg, kommer også et `wait for pending tasks`-steg etter det, som venter på det steget og hvert avventbare steg før det, tilbake til forrige `wait for pending tasks`. Flytt det lenger ned, så kjører stegene imellom mens blokken fortsatt holder på, som når begge luftslusedørene lukkes samtidig. Slett det, så går sekvensen rett videre. Går sekvensen rett videre fra sitt siste avventbare steg, slutter kjøringen der, og blokkene gjør seg ferdige på egen hånd.

Hold pekeren over et `wait for pending tasks` for å se hvilke steg det venter på, eller over et avventbart steg for å se hvilken `wait for pending tasks` som fanger det opp. En pulserende linje ved siden av linjenumrene binder sammen linjene som lyser opp.

## Når en kjøring går galt

En kjøring **mislykkes** når et steg ikke kan gjøres i det hele tatt, for eksempel fordi blokken gikk tapt. Den installasjonen slås av, de andre fortsetter å kjøre, og [startskjermen](home) sier hvilket steg som mislyktes og hvorfor. Rett opp det den nevner, og slå installasjonen på igjen. En blokk som gikk tapt, eller en tom [plass](slot), fylles på [installasjonspanelet](installation-panel).

Et steg **venter** når blokken sitter fast, som en rotor som holdes, eller en ventil uten sted å gjøre av lufta. Det venter og sier hvorfor, og fortsetter av seg selv når veien er ryddet. For å gi opp i stedet stopper du kjøringen.

En kjøring **slutter** når noe annet tar over en blokk et steg venter på, som at noen åpner den indre døra fra terminalen mens *Sluse ut* venter på at den skal lukke seg. Steget viser en advarsel om hva som skjedde, installasjonen forblir på, og den neste kjøringen begynner på nytt.

Den [enkle luftslusen](simple-airlock) bygger en sekvens fra begynnelsen.
