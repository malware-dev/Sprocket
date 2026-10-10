# Writing text

The description of a ![](mark:logic-system) [logic system](#logic-system), and the line on what each of its [slots](#slot) is for, are written on its [configuration panel](#configuration-panel), in boxes with a `Markup` and a `Preview` button. A few marks typed into the text give it headings, lists, tables, and bold or italic words. `Markup` opens this page, and `Preview` shows the text as others will see it.

Each part below shows what to write, then what it gives.

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

A table is as wide as its cells. A star in a column's dashes, as in `:--*`, gives that column the width to spare, so the table reaches across. With stars in more than one column, each star is an equal share.

`| Room | Vents |`
`|:--*|--:|`
`| Airlock | 1 |`

| Room | Vents |
|:--*|--:|
| Airlock | 1 |

## Writing a mark as itself

`\*Inner door\*`

\*Inner door\*

A backslash before a mark shows the mark as it is typed.
