# Velger

En velger er der du velger blokker, og hva som skal gjøres med dem eller sjekkes på dem. Det finnes én for hver jobb:

- **Blokkvelgeren** velger blokkene til et [steg](step) og hva det gjør med dem, og fyller en [plass](slot).
- **Signalvelgeren** velger blokken og [signalet](signal) som får en [utløser](trigger) til å utløses.
- **Verdivelgeren** velger blokkene og verdien et [hvis](if) sjekker.

`Søk` leter etter ordene du skriver i den rekkefølgen du skriver dem, hvert av dem hvor som helst i navnet til en blokk: *luft 1* finner *Luftsluse 1*, *Luftsluse 10* og *Luftsluse dør 1*, og utelater *1 Luftsluse*, og hvert søkefelt søker på samme måte. Det du skrev, blir stående til du forlater skjermen.

Blokker med `Show block in toolbar config` slått av i terminalen, er utelatt. `Vis blokker som er skjult fra verktøylinjer`, ytterst på overskriften `Blokker`, viser dem også, og det blir stående slik du forlot det, i hver velger og hvor du enn er.

Hver velger viser bare det som er kjent for å virke med automasjonen. `Terminal ›` bytter den til alt blokkene tilbyr i terminalen, under terminalens egne navn, uten det løftet.

I signalvelgeren er en verktøylinje noen trykker på, én linje: `hotbar slot is pressed` for en cockpit eller et annet sete, `button is pressed` for et button panel. Velger du den, åpner plassene på verktøylinja seg under den som nummererte ruter, med sidetall der verktøylinja har mer enn én side, slik verktøylinja i en cockpit har ni. En rute er tom der plassen er ledig, fylt med bildet av blokken der den holder en av spillets egne handlinger, og grønn der den allerede svarer på en utløser i automasjonen. Verktøytipset på den sier hva den holder, og eventuell etikett plassen eller knappen har fått. Den første ledige plassen er valgt for deg, og linja under rutene nevner den, som `Side 1, plass 3: ledig`. Trykk på en annen rute for å velge den, og bla først om den er på en annen side. Utløseren nevner da plassen: *When Cockpit hotbar page 1, slot 3 is pressed*.

Et steg som gjør noe med en skjerm, eller et hvis som sjekker en innstilling på en skjerm, virker på én skjerm på hver blokk. Velger du en slik handling eller innstilling på blokker med mer enn én skjerm, åpner en nedtrekksboks seg under linja, satt inn slik plassene på verktøylinja er. Den viser skjermene de valgte blokkene har felles, med navnene spillet kaller dem, som `Front` og `Back` på en reklametavle, og den første er valgt til å begynne med. En linje under nedtrekksboksen sier hva den valgte skjermen viser nå: `text and images`, `nothing`, eller `app` og navnet på appen, som `app TSS_ClockDigital`. Blokker med én skjerm, som et LCD panel, viser ingen nedtrekksboks; den skjermen er den som brukes.

En verktøylinje der plassene står for noe som skjer, beholder en linje for hver, som `starts detecting` og `stops detecting` på en sensor, eller `goes off` på en timerblokk. Alle andre verktøylinjer, som den på en luftventil, er én linje i terminalvisningen til signalvelgeren, `runs a toolbar slot`, og plassen velges i rutene på samme måte.

Alt blokkene kan gjøre, har et merke for slaget sitt:

- ![](mark:action)&nbsp;En [handling](action) sekvensen starter og går videre fra, uansett hvor lang tid blokken bruker på å fullføre den.
- ![](mark:awaited)&nbsp;En [avventbar](sequence) handling, en som går over mer enn ett [tikk](tick) og som sekvensen kan vente på.
- ![](mark:value)&nbsp;En **verdi**, en av egenskapene til blokken, som kan settes eller sjekkes.
- ![](mark:signal)&nbsp;Et **signal** blokken sender, tilbudt i signalvelgeren.
