# Hangardør

Dette bygger en hangar der lysene kommer på mens dørene åpner eller lukker seg, med én knapp for å åpne den og en annen for å lukke den. Den trenger hangardører, lys og et knappepanel med to knapper, alle på én [konstruksjon](#construct).

1. På [startskjermen](#home) trykker du `+ logikksystem` og gir det navnet *Hangar*.
2. Under `Sekvenser` trykker du `+ sekvens` og gir den navnet *Åpne*.
3. Trykk `+ steg` og så `oppgave`, [velg](#picking) lysene, og så `power`, og sett den til på.
4. Legg til en oppgave for hangardørene med `open`. Den er avventbar, så neste steg venter til den siste døra er åpen.
5. Legg til en oppgave som setter `power` på lysene til av igjen.
6. Gå tilbake og lag en andre sekvens, *Lukk*, på samme måte, med `close` for dørene.
7. Under `Utløsere` trykker du `+ utløser` og så `når`, velger den første knappen på knappepanelet og lar den kjøre *Åpne*. Legg til en andre utløser for den andre knappen som kjører *Lukk*.

Trykk den første knappen for å prøve.

Tilbake til [eksempler](#examples).
