# Signal

Et signal er det en blokk sender for å få en [utløser](trigger) til å utløses. Det kommer enten fra en plass på verktøylinja til blokken, eller fra en endring i selve blokken, og når det først er kommet, virker de to likt.

## Fra en verktøylinjeplass

Hver blokk med en verktøylinje kan sende signaler fra den, som et button panel når en knapp trykkes, eller en sensor når den oppdager noe. Hver verktøylinjeplass sender sitt eget signal, så én blokk kan få flere utløsere til å utløses. Du velger plassen i [signalvelgeren](selector).

Signalet når ![](mark:logic-system)&nbsp;[logikksystemet](logic-system) gjennom en ![](mark:signal)&nbsp;[signalplass](slot), som holder blokken og verktøylinjeplassen. Å fylle signalplassen setter opp den verktøylinjeplassen for deg. Der verktøylinjeplassen allerede gjorde noe annet og du tar den over, gir angre den tilbake, så lenge ingenting annet er lagt der siden.

Hver verktøylinjeplass som settes opp slik, viser det korte navnet til signalplassen, de få ordene du skriver under `Kort navn` på siden til plassen. En knapp på et button panel får det som navn, og en verktøylinjeplass viser så mye av det som får plass under ikonet. Står det tomt, viser de navnet til plassen. Som resten av plassen kan det korte navnet bare endres mens logikksystemet er slått av.

Tømmer noen verktøylinjeplassen eller gir den en annen jobb, slutter signalet å komme. Fyll signalplassen på nytt for å sette den opp igjen.

## Fra en endring i blokken

En blokk kan også sende et signal når noe ved den endrer seg, som at en dør blir ferdig med å lukke seg eller at et sete blir opptatt. Disse kommer fra selve blokken, så den fyller en vanlig ![](mark:block)&nbsp;blokkplass, og verktøylinja dens blir stående som den er.
