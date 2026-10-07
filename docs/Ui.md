# The UI framework

The target for Lumel (`src/Lumel`): the framework every screen is built with. What is written here is decided and
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

A view lives in its owner's `<Owner>.View.cs`, a second half of the same partial class, and holds
what a designer would write: the tree, its styles and sizes, and the named fields. Words, presses,
kept lists and everything that changes are in the other half, which wires the named elements once
the tree is made. There is no binding layer between the two.

## Controls

No control is sealed. A control made of others, a user control, derives from the layout it needs,
such as `HStack`, rather than from a base of its own: a `UserControl` comes only once one is proven
necessary, since it costs an element per instance that does nothing but pass its room through.

No control has a property that does not apply to it. What only some elements need lives on those
elements, and what an element needs in order to work, such as clipping what it draws, is its own
business rather than a switch somebody else can turn off.

`Element` holds only what every element has: its placement and the layout passes, whether it is
visible, its render transform, hit testing, the box it paints across its bounds (background, edge and
their colors under the pointer), and what pressing, hovering over and styling it means.

- **`IsVisible`.** An element that is not visible is out of the layout as well. There is no state that
  is hidden but holds its place.
- **`Tip`** is a thing of its own: a document, and the arguments for its slots where it has them, read
  once when it is made and kept like a style. The screen draws every tip with its one rich text.
- **`RenderTransform`**, a translate and a uniform scale, moves how an element is drawn and where it is
  pressed, never where layout put it. Text scaled by it is drawn at sizes off the scale and may
  shimmer while it moves, so scale is for animation rather than for anything that rests scaled. There
  is no rotation: the game cannot draw rotated text.
- **`Container`** is an element that holds content given to it from outside, and lays it out. Every
  container has `Padding`, the room between its edge and what it holds, which is why it sits on the
  base: whatever every container must support belongs there. A control builds its own parts instead,
  so a button or a switch is not a container.
  - **`Panel`** holds any number of `Children`. The stacks are panels.
  - **`ContentControl`** holds one `Content`, laid out across the room inside its padding unless the kind
    of control says otherwise. `ScrollView`, `Reveal` and `Window` are content controls.
  - **`Grid`** holds its own `Cells`, and is a container and nothing more.
- A container does not clip: what it holds may draw outside it. An element that has to clip to work,
  such as a scroll view, a reveal or a text box, clips itself, and nothing outside it can switch that off.
- **Padding on a leaf** belongs to the leaf that has a use for it, such as a label or a rule, and is set
  by its style like any other property.
- A user control is only a subtree: it derives from the layout it needs, and is what that layout is.

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

## Alignment

`Align` says where an element sits in the room its holder gives it. It is one flags value, read one axis
at a time:

- **An axis whose edges are named goes by them.** One edge pins the element to it; both stretch it.
  `Wide` is `Left | Right`, `Tall` is `Top | Bottom`, `Fill` is all four.
- **An axis with no edge named is centered where `Center` is set,** so `Left | Center` is left and
  centered down.
- **Otherwise the holder decides.** `Default` is 0 and adds nothing: `Left | Default` is `Left`.
- **A holder ignores the axis it stacks along.**

Every container declares how it places a child that says nothing, as `Aligns`, which `Container` makes
abstract: a column `Wide`, a row `Center`, a grid cell `Wide | Center`, a flow line `Center`, content
`Fill`, a scroll view `Wide | Top`, a reveal `Wide | Bottom`. A length the child's placement fixes is
kept whatever the alignment.

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
each built with an initializer. A style can be changed at any time, and the change shows on an element
once the element is restyled: `Restyle` on an element restyles it and everything inside it.

A style based on another is that style with its own layers laid over it. It reads through to the
other each time it is applied, so a change to a style shows in every style based on it.

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
};
```

A style writes only what it sets. Applying one puts each property that any of its layers sets back to
the neutral value the style type holds as a constant, then lays `Base`, then the states its control is in,
so a state that has ended takes back what it set. A property no layer sets is left as the element's own,
which is how padding set where an element is placed survives every change of state. Replacing an
element's style takes back everything the old one set first.

The framework decides only two states about any element: hot, being what a press would reach, and
enabled. A state that only the application knows, such as a button waiting to be ready, is a different
style the view assigns.

A style is for a control's own look and its own states, and each kind of a control, such as a default
and a cancel button, is a style of its own. A value that carries the application's data,
such as a state light's color or the color of a warning, is set on the element directly over a
default style. A separate style earns its place only where it saves setting many properties at once;
for one or two it is overkill.

A control is composed rather than derived: a button has a label, and is not one. A style sets the
control's own properties, and the control passes them on to the parts that use them: a button's
style sets the button's font, size and color, and the button hands them to its label. No style knows
what a control is made of. Nothing is inherited from what an element sits in.

Any element can be pressed, by being given a press. `Button` is one control among others, with text
of its own, not the only thing that answers a press.

## Text

A font size is a size in UI units, never a scale of the font's own size. The size is the font's line
height as the game draws it, so a label with a 24 font is one line of 24 units.
The game's scale is worked out from it at the point where text is handed to the game.

## Pictures

An image's locator, the `locator` in markdown-lite's `![words](locator)`, means nothing to rich text or
markdown-lite. They carry it from the document to the host as written and draw what comes back, as they
carry a link's target to the host's `Follow` and `LinkTip`.

The host owns the one `IImageCatalog` that resolves locators, and hands it to the graphics it draws with,
where everything finds it as `IGraphics.Images`. `ScreenSpaceImages`, which reads a locator as a material
name, is one resolver a host may use as it is or wrap in its own.

A locator only has to resolve on the host it is written for. A text panel and the screen space draw
different pictures, so the same locator may lead to a picture on one and to nothing on the other, where
the image says its description instead.

## Sizes

Every size the framework and its views use is a multiple of 4: font sizes, spacing, padding and
margins alike.

## Focus

Being focusable is part of the framework, not of each control.

## Commands

What pressing an element does is an `ICommand` the application implements, the way WPF's is. The
framework never learns what a command means to the application.

## Later

- The glyph system is reworked.
- Brushes: a background or edge painted with any material, not only the flat fill. Animating a color
  then means changing a mutable brush's color.
- A bold weight, if one is ever needed: text drawn a second time a whole number of screen pixels to
  the right, about its size divided by 20 and never less than 1, reads as a clean bold in the harness,
  because the game starts text on a whole pixel. Two pixels at size 20 splits the strokes instead. Held
  back because it draws the text twice.

## Open

- Margin: on every element, or in placement.
- The name of the `As` extension.
- What a property the view sets on an element does when a style's state later changes the same
  property: whether the state wins, or the view's value is kept.
- An items control, with a row template, in place of the six lists of rows built by hand.
- Whether rows and columns inside a grid can share its tracks through other containers.
