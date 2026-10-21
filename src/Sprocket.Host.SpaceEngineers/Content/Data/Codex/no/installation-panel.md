# Installasjonspanel

Installasjonspanelet er der du setter opp en ![](mark:installation)&nbsp;[installasjon](installation). Det viser hver [plass](slot) ![](mark:logic-system)&nbsp;[logikksystemet](logic-system) trenger å få fylt, med eventuelle merknader om hva hver er til, og du velger blokkene og ![](mark:signal)&nbsp;[signalene](signal) som skal i dem. Trykk på raden til en installasjon på [startskjermen](home) for å åpne panelet dens.

## Utforme det

Fanen `Installasjonspanel` på logikksystemets skjerm viser det alle som installerer dette logikksystemet, ser på sitt eget panel: en beskrivelse, så plassene.

For å hjelpe folk å forstå hva logikksystemet er til, eller bare for å minne deg selv på det, kan du skrive en beskrivelse. Den som får ![](mark:blueprint)&nbsp;[plantegningen](blueprint) din, har aldri sett konstruksjonen din, så beskrivelsen og plassene er alt de har å gå på. Beskrivelsen skrives mens logikksystemet er av, og kan inneholde [formatert tekst](rich-text).

Hvert sett med blokker du velger for et steg eller en utløser, blir en plass. En plass med én blokk får navnet til blokken, og en med flere blir nummerert. En ![](mark:block)&nbsp;**blokkplass**, som *den indre døra*, holder blokkene en sekvens virker på eller følger med på. En ![](mark:signal)&nbsp;**signalplass**, som *knappen*, holder en blokk og verktøylinjeplassen på den som sender signalet.

På denne fanen er navnet til en plass en knapp som åpner plassen, så du kan gi den navn og skrive hva den er til. På panelet til en installasjon er navnet vanlig tekst.

Plassene står i den rekkefølgen de ble laget. Dra en plass etter grepet for å flytte den til et annet sted.

For å samle plasser i grupper trykker du `+ kategori`, som legger til *Kategori 1*, så *Kategori 2* og så videre. Trykk på navnet til en kategori for å gi den nytt navn. ![](mark:delete)&nbsp;på en kategori fjerner den og lar plassene dens stå blant dem øverst. Plassene utenfor en kategori kommer først, så navnet til hver kategori med plassene sine rykket inn under seg. Dra en plass under navnet til en kategori for å legge den i den kategorien, eller blant plassene øverst for å ta den ut. Grepet til selve kategorien flytter den med alle plassene sine.

Hver av disse endringene er ett steg i ![](mark:undo)&nbsp;[angre](undo), og som alle andre endringer kan de bare gjøres mens logikksystemet er slått av. Panelet til en installasjon viser den samme rekkefølgen som vanlig tekst, med hver kategori som har plasser, med det samme navnet.

## Skjermer

Hver installasjon velger sin egen skjerm. Fyller du en skjermplass, åpner [velgeren](selector) seg med stegets egen handling alene, og nedtrekksboksen for skjerm over den, under overskriften `Skjermer`, når blokker er valgt. Den ber ikke om noen verdier, siden de hører til steget.

Et steg viser et problem når blokken mangler skjermen installasjonen valgte, for eksempel etter at blokken er byttet ut med en som har færre skjermer: *{block} har ikke skjermen dette steget bruker. Velg skjermen på installasjonspanelet.* Steget lar den blokken være.

## Fylle en plass på nytt

En endring i logikksystemet kan dele en plass denne installasjonen hadde fylt, eller slå den sammen med en større, og panelet ber da om at den fylles på nytt. Blokkene plassen hadde, legges til side og tilbys først, og beholdes til hver plass i installasjonen er fylt igjen.
