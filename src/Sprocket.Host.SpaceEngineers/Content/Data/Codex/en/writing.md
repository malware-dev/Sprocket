# Writing rich text

Sprocket supports rich formatted text in several locations. This formatting is based on a subset of the Markdown format. Here is what is supported, and how to type it.

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

A table is usually as wide as its cells. Add a star into a column's dashes, as in `:--*`, to stretch that column to fill any free space. Use multiple stars to indicate proportion: '|:--*|:--*' gives two columns with 50% each, while '|:--*|:--****' will give one column at 25%, and one column at 75%.

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
