# Sprocket.Host.SpaceEngineers

The mod: all of it, in one project. Space Engineers compiles a mod from loose `.cs` files rather than
loading an assembly, so this project's sources *are* the mod, and the packer copies them into a folder
the game will load.

One folder per capability, each holding the whole of it. Everything that belongs to the editor's
user interface lives in `Features/`, and the code outside it reaches in there:

| | |
|---|---|
| `Features/` | the editor, one folder per screen, with the frame, the editor itself and its page stack in `Shell/`, the sentence as drawn in `Sentences/`, the house look in `Look/`, and the block's own terminal controls in `Terminal/` |
| `Sequences/` | what a sequence is: triggers, steps, and the edits made to them |
| `Runtime/` | running a sequence, waiting for what it started, and what is wrong with one |
| `Storage/` | the script: the language, read and written |
| `World/` | the game's blocks, what they can do, and their toolbars |
| `LcdApps/` | apps for the game's screens, one folder each: the status panel in `StatusPanel/` |
| `Localization/` | the phrasebook, and the `Texts` generated from the mod's resx |
| `Network/` | lending a Sprocket to the machine that edits it, and the channel that carries it there |
| `Ui/` | the UI framework: elements at its root, then layout, text, controls, drawing and input |
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
