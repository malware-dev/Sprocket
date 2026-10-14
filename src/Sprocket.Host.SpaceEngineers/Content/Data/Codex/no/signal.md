# Signal

Et signal er det en blokk sender for å utløse en [utløser](#trigger). Det kommer enten fra en plass på verktøylinja til blokken, eller fra en endring i selve blokken, og når det først er kommet, virker de to likt.

## Fra en verktøylinjeplass

Hver blokk med en verktøylinje kan sende signaler fra den, som et knappepanel når en knapp trykkes, eller en sensor når den oppdager noe. Hver verktøylinjeplass sender sitt eget signal, så én blokk kan utløse flere utløsere.

Signalet når ![](mark:logic-system)&nbsp;[logikksystemet](#logic-system) gjennom en ![](mark:signal)&nbsp;[signalplass](#slot), som holder blokken og verktøylinjeplassen. Å fylle signalplassen setter opp den verktøylinjeplassen for deg. Der verktøylinjeplassen allerede gjorde noe annet og du tar den over, gir angre den tilbake, så lenge ingenting annet er lagt der siden.

Tømmer noen verktøylinjeplassen eller gir den en annen jobb, slutter signalet å komme. Fyll signalplassen på nytt for å sette den opp igjen.

## Fra en endring i blokken

En blokk kan også sende et signal når noe ved den endrer seg, som at en dør blir ferdig med å lukke seg eller at et sete blir opptatt. Disse kommer fra selve blokken, så den fyller en vanlig ![](mark:block)&nbsp;blokkplass, og verktøylinja dens blir som den er.
