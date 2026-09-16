# Core

Reactor itself: the model, the runtime, and the editor's decisions. What a system is, what a sequence
does, what the language says, and what a screen is asked to show.

## What must never appear in it

**Words a person reads.** A sequence is ids and nodes - this trigger, these block references, this
action id. What it *says* is a rendering of that, and the words belong to whoever renders it, which
is `../Presentation/Words/`. See `docs/Script.md`, which is the design this rule comes from.

**Presentation decisions.** Word order, colour, spacing, how a duration is spelled, whether a warning
is shown at all.

Those two are the rules that matter and they are the product, not the plumbing: the script is the
saved program, and English in it would be English in somebody's save file.

## What used to be enforced here, and no longer is

This folder was its own netstandard assembly, compiled alone, so a reference to a game type broke
the build before it reached the mod. That project is gone: the mod is one tree now and the separation
is convention. What the game will load is MDK's analysers' business and always was.

It is also on its way out deliberately - `IWorld`, `IEntity` and the wrapper that keeps
`IMyTerminalBlock` out of code only ever compiled beside it are an internal abstraction that earns
nothing. Abstractions over *the game's* API stay; a pretence that this folder could run on another
game does not.

## Who proves it

`tests/Reactor.Core.Tests`, against the mod's own assembly. It sees inside because
`../Visibility.debug.cs` says so, which is a file the game never gets.
