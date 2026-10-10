# Skrive tekst

Beskrivelsen av et ![](mark:logic-system) [logikksystem](#logic-system), og linja om hva hver [plass](#slot) i det er til, skrives på [konfigurasjonspanelet](#configuration-panel), i bokser med knappene `Markering` og `Forhåndsvis`. Noen få merker skrevet inn i teksten gir den overskrifter, lister, tabeller og fet eller kursiv tekst. `Markering` åpner denne siden, og `Forhåndsvis` viser teksten slik andre vil se den.

Hver del nedenfor viser hva du skriver, og så hva det gir.

## Fet, kursiv og kode

`` **fet**  *kursiv*  ***begge***  `kode` ``

**fet**  *kursiv*  ***begge***  `kode`

## Overskrifter

`# Stor`
`## Middels`
`### Liten`

# Stor

## Middels

### Liten

## Linjer og avsnitt

Enter starter en ny linje, og en tom linje mellom to linjer starter et nytt avsnitt.

## Lister

`- Indre dør`
`- Ytre dør`
`1. Lukk den indre døra`
`2. Tøm luftslusen for luft`

- Indre dør
- Ytre dør

1. Lukk den indre døra
2. Tøm luftslusen for luft

## Sitater

`> Ta på hjelmen før du sluser ut.`

> Ta på hjelmen før du sluser ut.

## Skillelinjer

`---`

---

## Midtstilt og til høyre

`::: center`
`Hangar 2`
`:::`

::: center
Hangar 2
:::

`::: right` legger linjene imellom til høyre på samme måte.

## Tabeller

`| Rom | Ventiler |`
`|:--|--:|`
`| Luftsluse | 1 |`
`| Hangar | 4 |`

| Rom | Ventiler |
|:--|--:|
| Luftsluse | 1 |
| Hangar | 4 |

Den andre linja sier hvordan hver kolonne står: `:--` til venstre, `:-:` midtstilt, `--:` til høyre.

For å utelate overskriften starter du tabellen med en rad tomme celler:

`| | |`
`|:--|--:|`
`| Luftsluse | 1 |`
`| Hangar | 4 |`

| | |
|:--|--:|
| Luftsluse | 1 |
| Hangar | 4 |

En tabell er så bred som cellene sine. En stjerne i strekene til en kolonne, som i `:--*`, gir den kolonnen bredden som er til overs, så tabellen strekker seg helt over. Med stjerner i flere kolonner er hver stjerne en like stor del.

`| Rom | Ventiler |`
`|:--*|--:|`
`| Luftsluse | 1 |`

| Rom | Ventiler |
|:--*|--:|
| Luftsluse | 1 |

## Skrive et merke som seg selv

`\*Indre dør\*`

\*Indre dør\*

En omvendt skråstrek foran et merke viser merket slik det er skrevet.
