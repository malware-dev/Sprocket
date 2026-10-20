# Angre

Hver skjerm har sin egen ![](mark:undo)&nbsp;angre og ![](mark:redo)&nbsp;gjør om: hver ![](mark:sequence)&nbsp;[sekvens](sequence), siden til hver [plass](slot), [installasjonspanelet](installation-panel) og ![](mark:logic-system)&nbsp;[logikksystemets](logic-system) egen skjerm. Angre tar tilbake den siste endringen på skjermen du står på.

Noen endringer når flere skjermer. Velger du blokker til et steg, endres sekvensen, og plassene for blokkene lages eller fylles, og en ny [utløser](trigger) kan lage sekvensen den kjører. En slik endring kan angres på hver skjerm den nådde.

Angre virker alltid. Der en senere endring på en annen skjerm bygger på det som tas tilbake, tar angre den endringen tilbake også, i ett steg, og verktøytipset nevner de skjermene. Gjør om henter dem tilbake sammen.

Navnet til en plass hører til hele logikksystemet, så et nytt navn på en plass angres på siden til plassen eller på logikksystemets skjerm. Et steg som hentes tilbake med angre, nevner plassen ved sitt nåværende navn.

## Når du går fra en skjerm

Trykker du `Tilbake`, eller Escape, på en skjerm der du har endret noe, spør den `Beholde endringene?`. `Behold dem` går tilbake med dem, og `Forkast dem` tar dem tilbake først. `Ferdig` beholder dem uten å spørre. Logikksystemets skjerm, skjermen til hver sekvens, siden til en plass og panelet til en installasjon spør alle.

Hver skjerm spør bare om endringene som er gjort på den. Det du endret på en skjerm som ble åpnet fra den, som en sekvens åpnet fra logikksystemets skjerm, ble beholdt eller forkastet da du gikk fra den, og blir stående som det er.

Navnet du gir et nytt logikksystem eller en ny sekvens idet du lager dem, hører til det å lage dem, og blir stående uansett hva du velger når du går fra skjermen.
