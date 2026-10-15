# Steg

Et steg er én linje i en ![](mark:sequence)&nbsp;[sekvens](sequence), og sekvensen kjører stegene sine i rekkefølge fra toppen. `+ steg` legger til et av disse:

- `oppgave` gjør noe med blokker: en [handling](action) som `open`, eller å sette en av verdiene deres.
- `wait` [venter](wait) en fastsatt tid.
- `run` og `stop` [starter eller stopper en annen sekvens](run-and-stop).
- `hvis` [sjekker blokker](if), og kjører stegene under seg bare når betingelsen er oppfylt.
- `await all pending tasks` venter på de avventbare stegene over seg.

Dra et steg for å flytte det, inn i et hvis eller ut av ett. For å prøve en sekvens uten noen av stegene setter du dem på pause med ![](mark:pause)&nbsp;på radene deres, og sekvensen hopper over dem til de gjenopptas.
