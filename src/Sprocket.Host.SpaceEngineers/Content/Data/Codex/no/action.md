# Handling

En handling er noe en blokk kan gjøre, som at en dør åpner seg eller et stempel strekker seg ut. Hver slags blokk har sine egne, og et [steg](step) gjør en når du legger det til med `oppgave`.

I [blokkvelgeren](selector) er en handling merket ![](mark:awaited)&nbsp;[avventbar](sequence): den går over mer enn ett [tikk](tick), og sekvensen kan vente på at den blir ferdig. Med en merket ![](mark:action)&nbsp;starter sekvensen handlingen og går videre, uansett hvor lang tid blokken bruker på å fullføre den.

Blokkvelgeren lister handlingene som er kjent for å virke med automasjonen, under enkle navn. `Terminal ›` lister i stedet alt blokkene tilbyr i terminalen, under terminalens egne navn. Disse tilbys uten noen garanti: de er ikke sjekket mot automasjonen, og virker kanskje ikke slik du venter.

## Flere blokker samtidig

Ett steg kan virke på flere blokker, som alle lysene i luftslusen, og blokkvelgeren tilbyr da bare det alle sammen kan gjøre. Et avventbart steg er ferdig når den siste av blokkene er det.

## Å sette en verdi

En verdi er en av egenskapene til en blokk, som fargen på et lys. Å sette en er også en oppgave, oppført sammen med handlingene og merket ![](mark:value). Den virker med én gang, så sekvensen venter aldri på den. En verdi som er en tilstand blokken er i, som en tank som lagrer opp eller en antenne som sender, leses som å slå den på eller av: steget lyder `enable stockpiling on Tank` når det slår tilstanden på og `disable stockpiling on Tank` når det slår den av, og et trykk på det første ordet bytter mellom dem. For å sjekke en verdi i stedet bruker du et [hvis](if); den samme tilstanden lyder `if Tank is stockpiling`.

Eksempelet [borearm](drill-arm) bruker handlinger med valg.
