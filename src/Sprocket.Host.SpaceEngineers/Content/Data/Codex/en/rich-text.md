# Rich text

Rich text is plain text with a few marks typed into it, taken from Markdown, that turn it into headings, lists, tables, and bold or italic words. This page shows each mark, and what it gives.

## Bold, italic and code

`` **bold**  *italic*  ***both***  `code` ``

**bold**  *italic*  ***both***  `code`

## Headings

`# Large`
`## Middle`
`### Small`

# Large

## Middle

### Small

## Lines and paragraphs

Enter starts a new line, and a blank line between two lines starts a new paragraph.

## Lists

`- Inner door`
`- Outer door`
`1. Close the inner door`
`2. Depressurize the airlock`

- Inner door
- Outer door

1. Close the inner door
2. Depressurize the airlock

## Quotes

`> Wear a helmet before cycling out.`

> Wear a helmet before cycling out.

## Dividing lines

`---`

---

## Centered and right

`::: center`
`Hangar 2`
`:::`

::: center
Hangar 2
:::

`::: right` puts the lines between on the right the same way.

## Tables

`| Room | Vents |`
`|:--|--:|`
`| Airlock | 1 |`
`| Hangar | 4 |`

| Room | Vents |
|:--|--:|
| Airlock | 1 |
| Hangar | 4 |

The second line says how each column lines up: `:--` left, `:-:` centered, `--:` right.

To leave out the heading, start the table with a row of empty cells:

`| | |`
`|:--|--:|`
`| Airlock | 1 |`
`| Hangar | 4 |`

| | |
|:--|--:|
| Airlock | 1 |
| Hangar | 4 |

A table is as wide as its cells. Add a star into a column's dashes, as in `:--*`, to stretch the table across the page, that column taking a share of it. Stars set the shares: `|:--*|:--*|` gives two columns half each, while `|:--*|:--***|` gives one column a quarter and the other three quarters. A column without a star keeps the width of its cells, and the starred ones share what is left.

`| Room | Vents |`
`|:--*|--:|`
`| Airlock | 1 |`

| Room | Vents |
|:--*|--:|
| Airlock | 1 |

## Writing a mark as itself (escape character)

`\*Inner door\*`

\*Inner door\*

A backslash before a mark shows the mark as it is typed.

## Keeping words together

`10&nbsp;SC`

10&nbsp;SC

`&nbsp;` is a space a line never breaks at, so the words either side of it stay on one line. It keeps a mark with the link after it too: `![](mark:trigger)&nbsp;[triggers](trigger)`.

## Writing a character by its name or number

`&lt;Inner door&gt; &amp; &#169;`

&lt;Inner door&gt; &amp; &#169;

`&amp;`, `&lt;`, `&gt;`, `&quot;` and `&apos;` write &amp;, &lt;, &gt;, &quot; and &apos;. Any character can be written by its number, as `&#169;` or `&#xA9;` for &#169;. An `&` that starts none of these is written as it is typed.
