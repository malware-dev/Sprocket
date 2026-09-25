# The UI framework

The target for `Ui/`: the framework every screen is built with. What is written here is decided and
not yet built, or built in part. Once a piece is built, the code is the record and it leaves this
file. Anything not written here is undecided.

The framework can be copied as-is into another mod and used there, so nothing in it is Sprocket's:
no Sprocket type, content or wording. What an application means by a press, or how it looks, it
supplies through the framework's interfaces and styles.

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

## Styles

Styles belong to the application, never to the framework, which ships no colors or sizes of its own.

A style holds simple visual properties only: colors, fonts, sizes, spacing. Nothing that moves.

A style is found by reference, never looked up. The application keeps its styles as static fields,
each built with an initializer and frozen, after which any setter throws. A style based on another
starts from a copy of it.

Every style has a `Base`. The states beyond it belong to the control the style is for, and its style
type declares them as layers of its own: a switch's style has `On`, a text box's has `Editing`, and a
label's has none. Each style type lays its layers in an order it states, so a later layer wins. A
state names only what it changes, and may change anything `Base` can, sizes included.

```csharp
public static readonly SwitchStyle Toggle = new SwitchStyle
{
    Base = { Background = Palette.Page },
    On = { Background = Palette.Live },
    Hot = { Edge = Palette.Ink }
}.Frozen();
```

Applying a style lays down neutral values the style type holds as constants, then `Base`, then the
states its control is in. An element given no style starts from the same neutral values.

The framework decides only two states about any element: hot, being what a press would reach, and
enabled. A state that only the application knows, such as a button waiting to be ready, is a different
style the view assigns.

A style is for a control's own look and its own states, and each kind of a control, such as a default
and a cancel button, is a style of its own. A value that carries the application's data,
such as a state light's color or the color of a warning, is set on the element directly over a
default style. A separate style earns its place only where it saves setting many properties at once;
for one or two it is overkill.

A control is composed rather than derived: a button has a label, and is not one. A control's style
styles the parts it owns, so a button's style sets its own label's font, size and color in each of
its states. Nothing is inherited from what an element sits in.

Any element can be pressed, by being given a press. `Button` is one control among others, with text
of its own, not the only thing that answers a press.

## Text

A font size is a size in UI units, never a scale of the font's own size. The size is the font's line
height as the game draws it, so a label with a 24 font is one line of 24 units.
The game's scale is worked out from it at the point where text is handed to the game.

## Sizes

Every size the framework and its views use comes from one scale: 4, 8, 16, 24, 32, 48, 64, and on
up. Font sizes, spacing, padding and margins alike.

## Focus

Being focusable is part of the framework, not of each control. Keyboard support built into single
controls in the meantime moves onto it.

## Commands

What pressing an element does is an `ICommand` the application implements, the way WPF's is. The
framework never learns what a command means to the application. `Deed` stays with the old controls
until the last of them goes.

## Later

- `Offset` goes. Dragging a row and rows sliding aside use a render translate, applied at drawing
  and in hit testing but not in layout.
- The glyph system is reworked.
- Brushes: a background or edge painted with any material, not only the flat fill. Animating a color
  then means changing a mutable brush's color.

## Open

- Margin: on every element, or in placement.
- Alignment of a child inside the room it is given, across a stack and inside a grid cell.
- The name of the `As` extension.
- What a property the view sets on an element does when a style's state later changes the same
  property: whether the state wins, or the view's value is kept.
- What `Bar` still does that HStack does not, before the last of them fold in: `Tight`, where the
  middle may shrink but the bar reports only what its parts need, and `Tops`, where the parts sit
  along the top rather than on the middle.
- An items control, with a row template, in place of the six lists of rows built by hand.
- Whether rows and columns inside a grid can share its tracks through other containers.
- The framework's own namespace, in place of `Sprocket.Host.SpaceEngineers.Ui`.
- Where the old controls live until the port ends, since `Ui/Legacy` would be copied with `Ui/`.
