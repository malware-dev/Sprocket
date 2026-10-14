# Borearm

Dette bygger en sekvens som skyver en borearm sakte ut med boreren i gang, og henter den inn igjen når den er ferdig. Den trenger stempler i armen og boremaskiner ytterst, alle på én [konstruksjon](#construct).

1. På [startskjermen](#home) trykker du `+ logikksystem` og gir det navnet *Borearm*.
2. Under `Sekvenser` trykker du `+ sekvens` og gir den navnet *Grav*.
3. Trykk `+ steg` og så `oppgave`, [velg](#picking) boremaskinene, og så `power`, og sett den til på.
4. Legg til en oppgave for stemplene i armen med `move to`, og oppgi 5 som posisjon og 0,5 som fart. Hvert stempel flytter seg ut til 5 m, og steget varer til det siste av dem er framme.
5. Legg til en `move to` til for stemplene, med 0 som posisjon og 2 som fart, for å hente armen inn igjen.
6. Legg til en oppgave som setter `power` på boremaskinene til av.
7. Forlat skjermen, og slå på logikksystemet når du blir spurt.

Kjør *Grav* for hånd med ![](mark:play) for å prøve den, eller gi den en [utløser](#trigger), som en knapp på kontrollpanelet til armen.

Tilbake til [eksempler](#examples).
