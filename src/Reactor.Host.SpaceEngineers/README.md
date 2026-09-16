# Reactor.Host.SpaceEngineers

The mod: all of it, in one project. Space Engineers compiles a mod from loose `.cs` files rather than
loading an assembly, so this project's sources *are* the mod, and the packer copies them into a folder
the game will load.

Three layers, three places:

| | |
|---|---|
| `Core/` | Reactor itself: the model, the runtime, the language, the editor's decisions |
| `Presentation/` | what a person sees and touches: layout, drawing, controls, and every word on screen |
| the rest | the game. `World/` turns blocks into what Reactor works with, `Terminal/` is the control that opens the editor and the screen it opens, `Input/` turns the game's keyboard into edits, `ReactorSession` and `ReactorBlock` are the game's lifetime, `Content/` is materials, fonts and the mod's metadata |

`Core/` and `Presentation/` each have a README saying what belongs in them.

## The layout is not the end state

The three folders are there because the namespaces still differ and two of them own a `World` and a
`Ui`. What everything is called is the rename sweep's business, and the internal seams between them -
`IWorld`, `IEntity`, the wrapper around `IMyTerminalBlock` - are on their way out: an abstraction over
*the game's* API earns its place, a pretence that this code could run on another game does not.

## What proves it

The harness, `../Reactor.Host.SpaceEngineers.Sim`, compiles these sources and drives them over blocks
it makes up, through the game's own interfaces - so the world layer, the toolbars and mod storage are
exercised outside the game, and the reference frames in `tests/Reference/` see them.

What the harness cannot answer is whether Keen's API still means what it meant. That is answered by
loading the game, and by nothing else.

## What must never appear in it

**Anything the game would refuse to compile.** MDK's analysers fail the build naming the member,
and they know the real whitelist.
