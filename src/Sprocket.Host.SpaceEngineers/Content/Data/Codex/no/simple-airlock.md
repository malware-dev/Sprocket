# Enkel luftsluse

Dette bygger en luftsluse som sluser ut når knappen trykkes: den indre døra lukker seg, rommet tømmes, og den ytre døra åpner seg. Den trenger en indre dør, en ytre dør, en luftventil i rommet mellom dem og et knappepanel, alle på én [konstruksjon](#construct).

1. På [startskjermen](#home) trykker du `+ logikksystem` og gir det navnet *Luftsluse*.
2. Under `Sekvenser` trykker du `+ sekvens` og gir den navnet *Sluse ut*.
3. Trykk `+ steg` og så `oppgave`, [velg](#picking) den indre døra, og så `close`. `close` er avventbar, fordi døra bruker tid på å lukke seg, så neste steg venter på den.
4. Legg til `depressurize` for ventilen og `open` for den ytre døra på samme måte.
5. Gå tilbake. Under `Utløsere` trykker du `+ utløser` og så `når`, velger knappen og lar den kjøre *Sluse ut*.
6. Forlat skjermen, og slå på logikksystemet når du blir spurt.

Trykk knappen for å prøve. Mens den kjører, lyser raden til sekvensen grønt, og steget som kjøres, lyser på skjermen til sekvensen.

Tilbake til [eksempler](#examples).
