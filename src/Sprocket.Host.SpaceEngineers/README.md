# Sprocket.Host.SpaceEngineers

The mod: all of it, in one project. Space Engineers compiles a mod from loose `.cs` files rather than
loading an assembly, so this project's sources *are* the mod, and the packer copies them into a folder
the game will load.

One folder per capability, each holding the whole of it - what a screen is asked to show and how it
is drawn together, because a change to how a sentence works should be a change in one place:

| | |
|---|---|
| `Sequences/` | what a sequence is, and the sentence somebody reads and presses |
| `Editing/` | moving through the editor: pages, commands, undo, what may go in a slot |
| `Picking/` | choosing blocks and what happens to them |
| `Lists/` | rows of things to choose or watch |
| `Runtime/` | running a sequence, and waiting for what it started |
| `Storage/` | the script: the language, read and written |
| `World/` | the game's blocks, what they can do, and their toolbars |
| `Words/` | how a sentence is composed: word order, capitals, and the words that join phrases |
| `Screen/` | the editor's surface, and the screen that draws it |
| `Ui/` | the UI framework: elements at its root, then layout, text, controls, drawing, input and the game's look |
| `Layout/` | what pressing a control means, and what a question's fields take |
| `Look/` | icons for kinds of things |
| `Controls/` | the house style, and the slot in a sentence |
| `Terminal/` | the control that opens the editor, and the screen it opens |
| `Content/` | materials, fonts, and the mod's own metadata |

`SprocketSession` and `SprocketBlock`, at the root, are the game's lifetime.

**The one rule the folders no longer enforce**: what a screen is asked to show carries structure and
never words. It is stated on `Screen/EditorComponent`, which is where it binds, and proved by
`PaperTarget` in the tests, which can write a screen down without drawing one.

## What proves it

The harness, `../Sprocket.Host.SpaceEngineers.Sim`, references this project and drives the mod over
blocks it makes up, through the game's own interfaces - so the world layer, the toolbars and mod
storage are exercised outside the game, and the reference frames in `tests/Reference/` see them.

Two things only the game can do, so each is an interface here and the harness answers it:
`Ui/Drawing/IScreenSpace` for drawing and `World/ISlots` for toolbars. Everything else the
harness uses for real.

What the harness cannot answer is whether Keen's API still means what it meant. That is answered by
loading the game, and by nothing else.

## What must never appear in it

**Anything the game would refuse to compile.** MDK's analysers fail the build naming the member,
and they know the real whitelist.
