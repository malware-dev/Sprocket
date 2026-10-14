# Skrive formatert tekst

Sprocket støtter formatert tekst flere steder. Formateringen bygger på en del av Markdown-formatet. Her er det som støttes, og hvordan du skriver det.

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

En tabell er så bred som cellene sine. Legg en stjerne i strekene til en kolonne, som i `:--*`, for å strekke tabellen over hele siden, der den kolonnen får en del av bredden. Stjernene bestemmer delene: `|:--*|:--*|` gir to kolonner halvparten hver, mens `|:--*|:--***|` gir den ene kolonnen en fjerdedel og den andre tre fjerdedeler. En kolonne uten stjerne beholder bredden til cellene sine, og de med stjerne deler det som er igjen.

`| Rom | Ventiler |`
`|:--*|--:|`
`| Luftsluse | 1 |`

| Rom | Ventiler |
|:--*|--:|
| Luftsluse | 1 |

## Skrive et merke som seg selv (escape-tegn)

`\*Indre dør\*`

\*Indre dør\*

En omvendt skråstrek foran et merke viser merket slik det er skrevet.

## Holde ord sammen

`10&nbsp;SC`

10&nbsp;SC

`&nbsp;` er et mellomrom en linje aldri brytes ved, så ordene på hver side av det blir stående på samme linje. Det holder også et merke sammen med lenken etter det: `![](mark:trigger)&nbsp;[utløsere](#trigger)`.

## Skrive et tegn med navnet eller nummeret

`&lt;Indre dør&gt; &amp; &#169;`

&lt;Indre dør&gt; &amp; &#169;

`&amp;`, `&lt;`, `&gt;`, `&quot;` og `&apos;` skriver &amp;, &lt;, &gt;, &quot; og &apos;. Hvilket som helst tegn kan skrives med nummeret sitt, som `&#169;` eller `&#xA9;` for &#169;. En `&` som ikke starter noen av disse, skrives slik den er.
