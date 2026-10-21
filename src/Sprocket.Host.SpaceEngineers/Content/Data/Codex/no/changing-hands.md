# Eierskifte

Automasjonen til en [konstruksjon](construct) tilhører eierne: ingeniørene som eier flest blokker på den, talt over alle rutenettene, og alle sammen når flere står likt. Når et skip kapres eller overleveres, skifter automasjonen eier med det.

Automasjonen åpnes fra en programmable block som en av eierne eier, for alle den blokken er delt med: eierne selv, fraksjonen deres når den er delt med fraksjonen, og alle når den er delt med alle. En konstruksjon uten eier kan åpnes av hvem som helst. En programmable block som eies av noen andre, står utenfor automasjonen, uansett hvordan den er delt.

Automasjonen holdes i en programmable block som en av eierne eier. Går den blokken over til noen andre, tar en annen av eiernes programmable blocks over. Har de ingen igjen, tas automasjonen vare på og venter til en av dem eier en programmable block på konstruksjonen igjen, og en ![](mark:sequence)&nbsp;[sekvens](sequence) som er midt i kjøringen, blir stående ved steget sitt så lenge. Det skjer midt i overleveringen av et skip med flere rutenett, siden ingeniøren som tar imot det, godtar det rutenett for rutenett: automasjonen venter til hele skipet er kommet over. Den går tapt bare når den siste programmable block på konstruksjonen blir ødelagt eller fjernet.

Automasjonen kan bruke blokkene eierne eier og de som er delt med fraksjonen deres eller med alle. Alle andre blokker regnes som tapte, slik en ødelagt blokk gjør, og kommer tilbake av seg selv når de deles.

## Når konstruksjonen skifter eier

En konstruksjon som gis til en den er delt med, fortsetter å kjøre, som et skip som er delt med fraksjonen og overleveres til et fraksjonsmedlem. Gis den utenfor delingen, ved en kapring eller en overlevering, settes automasjonen på pause: knappene slutter å virke, og en sekvens som er midt i kjøringen, blir stående ved steget sitt. Den forblir på pause til en av de nye eierne gjenopptar den, omtrent slik en programmable block ber den nye eieren kompilere skriptet sitt på nytt. En omlasting endrer ikke det.

Et eierskifte vurderes én gang, fra eierne før det til eierne etter det, etter delingen slik den var før, fordi spillet slår av delingen på hver blokk det gir til en annen ingeniør. Når fraksjonsmedlemmet gir skipet tilbake, settes det derfor på pause, med mindre de delte det med fraksjonen igjen først.

Når du åpner en automasjon som står på pause, ser du først hva som er endret, og får se den over før den kjører igjen.
