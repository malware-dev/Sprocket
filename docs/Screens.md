# Screens

How Sprocket's screens are built: Lumel, the framework under them, the editor's pages, the look, and how a screen is
proved. Where the words on them come from is in [Text](Text.md).

## Lumel

`src/Lumel` is the user interface framework. It knows nothing of Sprocket and ships no colors, sizes or words of its
own: every piece takes them as properties, so any mod can use it as it is. A piece of interface with nothing of
Sprocket in it, a control, a layout or a drawing primitive, belongs in Lumel; Sprocket's look hands it what it needs.

| Folder | What it holds |
|---|---|
| (root) | `Element`, the base of everything, with its children, placement, alignment, style, tip and command; `Container`, `Panel` and `ContentControl`; `Tip`, a tooltip written in markdown-lite |
| `Layout` | Stacks across and down, `Flow`, `Deck`, and `Grid` with its tracks; `Flex` for a share of the room left over, `Fixed` for a set size; `Keyed`, which keeps one child per key from one showing to the next |
| `Text` | `Label`; `RichText`, which draws a parsed document; markdown-lite; `Template` for texts with placeholders; the editable boxes |
| `Controls` | Buttons, switches, sliders, dials, grips, tabs and rules |
| `Styling` | A style for each kind of element, applied on every change of the element's state |
| `Input` | Focus and pointer capture; `ICommand`, which the application supplies, and `Relay` for a command from a delegate |
| `Drawing` | `IGraphics`, everything the interface may draw with; `IScreenSpace`, the game's screen |
| `Images` | `IImageCatalog`, which finds a picture by its locator |
| `Windows`, `Viewing`, `Screens` | Windows, dialogs and popups; scrolling; the game screen a view is shown on |

`IScreenSpace` is one of the two things only the game can do (the [Overview](Overview.md) has the other). The
simulator answers it, which is how a frame is recorded rather than drawn.

## Drawing runs every frame

A screen is drawn sixty times a second and changes perhaps twice a minute, so drawing does no work it can avoid:
nothing measures text while drawing, and nothing lays out unless something changed.

Layout is measure and arrange (`Element.cs`), and each returns at once where nothing changed. A change asks for the
work it needs, and the request travels up the tree:

- a property that changes a size calls `InvalidateMeasure`
- one that changes only a position calls `InvalidateArrange`
- a color calls neither, since the editor sets colors on its elements every tick.

A property setter that gets this wrong breaks layout in a way every other test can miss, so tests count the text
measured: the simulator counts every measurement, and `LayoutWorkTests` asserts that an idle second measures none. An
idle page recounts nothing either (`EditorPage.Recounts`).

## The editor

`src/Sprocket.Host.SpaceEngineers/Features` holds every screen, one folder a screen or a piece shared between them.
`Shell` is the editor itself:

- `SprocketEditor` is all of the editor: which page is open, navigation (`Push`, `Pop`, `OpenCodex`, `OpenLibrary`),
  and what each page shows.
- `EditorView` draws the chrome around the page, the page and its windows, and decides nothing.
- `EditorPage` is a page: `Build` makes its rows when its shape changes, and `Refresh` updates them in place each
  tick.

**A page is two files.** `X.View.cs` builds its tree of elements, with their styles, sizes and names; `X.cs` holds its
words, what its presses do, and its refresh. A page reads its words from the editor's `Texts`.

| Folder | The screen |
|---|---|
| `Home` | The home screen, every logic system on the construct |
| `LogicSystems` | A logic system's own screen, with its triggers |
| `Sequences` | A sequence's screen, one row a step |
| `Picking` | The selectors, and the panels for times and rates |
| `Blueprints` | The installation panel and a slot's page |
| `Library` | Blueprints and backups |
| `Codex` | The codex window |
| `Writing` | The editor for descriptions, with its preview |
| `Developing` | The automation as script text |
| `Ownership` | The screen a construct shows after changing hands |
| `Admin` | The world's settings |
| `Fields` | The fields a value is given in: numbers, colors and the rest |
| `Sentences` | Steps and triggers read as sentences |
| `Terminal` | Sprocket's button and actions in the game's terminal |
| `Doors`, `Gravity` | Terminal controls of their own for doors and gravity generators |
| `Look` | Sprocket's look, below |

## The look

`Features/Look` holds everything Sprocket's screens look like, handed to Lumel's pieces:

- `Palette`: the colors. Every one that names a texture was sampled from it, matching the game's own screens, and
  says where it came from; the few chosen rather than sampled say so too.
- `Sizes` and `Space`: the sizes of things, and the gaps named for what they separate.
- `Lettering`: the faces and their sizes, Maltek Sans and its bold and italic, Maltek Mono for code, and the bold and
  italic cuts of the game's own face.
- `Symbols` and `MarkImages`: the symbols drawn from Maltek Symbols, and the marks a page of text can show.
- `Styles`: every style, built from the above.

A value sampled from the game is a transcription, never a choice: it is not adjusted by eye.

## Proving a screen

**The presentation tests** (`tests/Sprocket.Host.SpaceEngineers.Presentation.Tests`) drive the editor through the
simulator, pressing and reading by name the way a player would:

```csharp
var screen = Screen.Fresh("tick", "3");
screen.Do("press", "+ logic system", "type", "Airlock", "enter");
Assert.True(screen.Shows("Airlock"));
```

`Screen.On(rig)` starts on a construct of chosen blocks, and `Screen.Holding(program)` on one already holding a
program. A press names what the screen says, so a layout that moves leaves the test right.

**The reference suite** walks a handful of screens from an empty construct and dumps each whole; the dumps are
committed, so a change to any of them shows in `git diff` ([Building and testing](Building.md#testing)).

**The simulator** draws any screen to a picture, `--shot`, at the 1920 by 1080 a player sees, for a look with your own
eyes.
