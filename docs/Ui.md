# The UI framework

The target for `Ui/`: the framework every screen is built with. What is written here is decided and
not yet built, or built in part. Once a piece is built, the code is the record and it leaves this
file. Anything not written here is undecided.

## Views are one initializer

A view is written as one object initializer, the way a XAML file is one tree. Containers take their
children in the initializer, and nothing about the tree is wired by statements afterwards.

An element the view changes later is named where it sits, with an extension that hands it back
through an `out` field:

```csharp
Children =
{
    new Label { Text = "Speed" }.As(out _speed),
}
```

## Placement

`Element` has no width and no height. What a container needs to know about how a child is placed
lives in the child's `Placement`, and each container reads the placement interface it understands.

- **Containers expect interfaces**, so any type satisfying one can be placed in that container.
- **A container that cannot honour a child's placement refuses the child when it is added.**
- **Placement interfaces are read-only.** Shared placements are immutable constants, such as
  `Flex.Grow`.
- **A placement announces its changes** through a `Changed` event on the base interface, which the
  element listens to. An immutable placement implements it with empty `add` and `remove`, so a shared
  constant holds no handlers.
- **A container keeps what it works out in entries of its own**, never in the placement, since a
  placement may be shared by many elements.

## HStack and VStack

Children in a line, across or down. A child whose placement asks to grow is given a share of the
room the others leave, by weight. `Bar` and `Dock` fold into these.

## Grid

`Columns` and `Rows` are tracks of `Length`: a fixed size, auto, or flex with a weight. A track
nobody defines is auto. Cells are placed with an indexer, and a span with a four-argument one:

```csharp
new Grid
{
    Columns = { 26, Length.Auto, Length.Flex(1) },
    Cells =
    {
        [0, 0] = grip,
        [0, 1] = number,
        [0, 2] = sentence,
        [1, 0, 1, 3] = adders
    }
}
```

- An auto track is as large as the largest cell in it.
- A cell's position and span are the grid's own entries, not part of the cell's placement.
- One element per cell. A cell that would overlap one already placed is refused when added.

`Length` exists for a grid's tracks and nowhere else.

## Text

A font size is a size in UI units, never a scale of the font's own size. The size is the font's line
height, as the game's `GetLineHeight` reports it, so a label with a 24 font is one line of 24 units.
The game's scale is worked out from it at the point where text is handed to the game.

## Sizes

Every size the framework and its views use comes from one scale: 4, 8, 16, 24, 32, 48, 64, and on
up. Font sizes, spacing, padding and margins alike.

## Focus

Being focusable is part of the framework, not of each control. Keyboard support built into single
controls in the meantime moves onto it.

## Later

- `Offset` goes. Dragging a row and rows sliding aside use a render translate, applied at drawing
  and in hit testing but not in layout.
- The glyph system is reworked.

## Open

- Margin: on every element, or in placement.
- Alignment of a child inside the room it is given, across a stack and inside a grid cell.
- The name of the `As` extension.
- Styles as values that an initializer assigns, in place of the static helpers in `Widgets`. A
  `Widgets` factory hands back a finished element, and a method's result cannot take properties in
  an initializer, so a factory-made child cannot be given a `Placement` where it sits. Rows whose
  growing middle comes from a factory, such as the choice list's typed row, wait on this.
- What `Bar` still does that HStack does not, before the last of them fold in: `Tight`, where the
  middle may shrink but the bar reports only what its parts need, and `Tops`, where the parts sit
  along the top rather than on the middle.
- An items control, with a row template, in place of the six lists of rows built by hand.
- Whether rows and columns inside a grid can share its tracks through other containers.
- How focus works: what owns it, how the focused element shows it, and where Tab order comes from.
