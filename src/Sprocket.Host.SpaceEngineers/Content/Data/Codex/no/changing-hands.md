# Eierskifte

Automasjonen til en [konstruksjon](#construct) tilhører eierne av konstruksjonen: spillerne som eier flest blokker på den, talt over alle rutenettene. Eier flere spillere like mange, er alle eiere. Når et skip blir tatt eller gitt til en annen spiller, skifter automasjonen eier sammen med det.

## Hvem som kan åpne den

Bare eierne av konstruksjonen kan åpne automasjonen. Alle andre som trykker `Automasjon` i terminalen til en programmerbar blokk, også medlemmer av fraksjonen, får beskjeden `Bare eierne av {construct} kan åpne automasjonen dens.` En eierløs konstruksjon kan alle åpne.

## Blokkene den kan bruke

Automasjonen bruker blokkene eierne kan bruke: blokkene de eier, og dem som er delt med fraksjonen deres eller med alle. Enhver annen blokk, som en dør en annen spiller har bygd og holdt for seg selv, er borte for automasjonen, slik en ødelagt blokk er. Den kommer tilbake av seg selv når den blir delt.

## Når konstruksjonen skifter eier

Går konstruksjonen over til en spiller den var delt med, fortsetter automasjonen å kjøre. Et skip som er delt med fraksjonen til eieren og gis til et annet medlem av fraksjonen, går videre som før.

Går den over utenfor delingen, ved at den blir tatt eller gitt til en spiller den var holdt unna, settes automasjonen på pause. Alt den gjør, stopper: knappene blir døde, og en ![](mark:sequence) [sekvens](#sequence) som er midt i en kjøring, blir stående på steget sitt. Den forblir på pause når verdenen lastes inn igjen, til en av de nye eierne fortsetter den. Spillet ber på samme måte den nye eieren av en programmerbar blokk om å kompilere skriptet på nytt; automasjonen beholder alt som det var og venter på ett trykk.

## Skjermen Satt på pause

Mens automasjonen står på pause, viser den skjermen `Satt på pause` først når den åpnes. Den sier hvem konstruksjonen gikk fra og til, hvor mange ![](mark:logic-system) [logikksystemer](#logic-system) den har, og hvor mange blokker som er utenfor rekkevidde, de nye eierne ikke kan bruke, eller innenfor rekkevidde igjen, de gamle eierne ikke kunne bruke.

Tre knapper fører videre:

- `Fortsett` starter alt igjen som det var, og går videre til [startskjermen](#home).
- `La den stå på pause` holder den stoppet og går videre til startskjermen, der automasjonen kan leses, og endres som vanlig, før den kjører. Et banner over hver skjerm sier `Satt på pause da {construct} skiftet eier.`, og knappen `Se over` på det åpner skjermen `Satt på pause` igjen.
- `Fjern alt` spør først, og fjerner så hvert logikksystem på konstruksjonen. Angre på startskjermen henter hvert av dem tilbake.
