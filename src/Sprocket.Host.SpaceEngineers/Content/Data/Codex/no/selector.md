# Velger

En velger er der du velger blokker, og hva som skal gjøres med dem eller sjekkes på dem. Det finnes én for hver jobb:

- **Blokkvelgeren** velger blokkene til et [steg](step) og hva det gjør med dem, og fyller en [plass](slot).
- **Signalvelgeren** velger blokken og [signalet](signal) som får en [utløser](trigger) til å utløses.
- **Verdivelgeren** velger blokkene og verdien et [hvis](if) sjekker.

`Søk` leter etter ordene du skriver i den rekkefølgen du skriver dem, hvert av dem hvor som helst i navnet til en blokk: *luft 1* finner *Luftsluse 1*, *Luftsluse 10* og *Luftsluse dør 1*, og utelater *1 Luftsluse*, og hvert søkefelt søker på samme måte. Det du skrev, blir stående til du forlater skjermen.

Blokker med `Show block in toolbar config` slått av i terminalen, er utelatt. `Vis blokker som er skjult fra verktøylinjer`, ytterst på overskriften `Blokker`, viser dem også, og det blir stående slik du forlot det, i hver velger og hvor du enn er.

Blokkvelgeren og verdivelgeren lister bare det som er kjent for å virke med automasjonen. `Terminal ›` bytter hvilken som helst av dem til alt blokkene tilbyr i terminalen, under terminalens egne navn, uten det løftet.

Alt blokkene kan gjøre, har et merke for slaget sitt:

- ![](mark:action)&nbsp;En [handling](action) sekvensen starter og går videre fra, uansett hvor lang tid blokken bruker på å fullføre den.
- ![](mark:awaited)&nbsp;En [avventbar](sequence) handling, en som går over mer enn ett [tikk](tick) og som sekvensen kan vente på.
- ![](mark:value)&nbsp;En **verdi**, en av egenskapene til blokken, som kan settes eller sjekkes.
- ![](mark:signal)&nbsp;Et **signal** blokken sender, tilbudt i signalvelgeren.
