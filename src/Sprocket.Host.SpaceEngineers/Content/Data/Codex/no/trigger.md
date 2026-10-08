# Utløser

En utløser sier når en ![](mark:sequence) [sekvens](#sequence) kjører. Den er én linje: når noe skjer, gjør én ting. For eksempel: når knappen til luftslusen trykkes, kjør *Sluse ut*.

For å legge til en trykker du `+ når` under `Utløsere` på skjermen til ![](mark:logic-system) [logikksystemet](#logic-system), eller `+ hver` for en som starter på en fast tid. Trykker du en del av en utløser, åpnes [velgeren](#picking) på den delen, så du kan endre den.

## Hva som starter den

- Et ![](mark:signal) [signal](#signal) som kommer, som at en knapp trykkes.
- Blokker som kommer i en tilstand, som at en dør er ferdig med å lukke seg. Med flere blokker starter `hvilken som helst` den når den første kommer dit, og `alle sammen` når den siste gjør det.
- En fastsatt tid som går, om og om igjen, som hvert minutt.

Noen tilstander, som lufta i et rom eller ladningen i et batteri, finnes ved å se på blokken. Hvor ofte utløseren ser, velges under `Hvor ofte`, og de raske valgene sier hva de koster.

## Hva den gjør

Den kjører en sekvens, stopper en, eller gjør `En oppgave på blokker`, som å slå på lysene i luftslusen.

En utløser gjør den ene tingen sin med én gang. Alt som venter, som å lukke en dør før en annen åpnes, hører hjemme i en sekvens.

## Hvilken installasjon den starter

En utløser som startes av et signal, kjører bare for den ![](mark:installation) [installasjonen](#installation) der [signalplassen](#slot) holder det signalet. Så knappen til babord luftsluse sluser babord luftsluse, og styrbord luftsluse blir stående som den er.
