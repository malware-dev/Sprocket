# Reactor.Core.Mixin

Reactor itself: the model, the runtime, and the editor's decisions. **It does not know that Space
Engineers exists**, and it does not know what anything on screen looks like.

## What it may depend on

The .NET base library, and nothing else. No game types, no drawing, no layout.

## What must never appear in it

**Words a person reads.** A sequence is ids and nodes - this trigger, these entity references, this
state id. What it *says* is a rendering of that, and the words belong to whoever renders it. See
`docs/Script.md`, which is the design this rule comes from.

**Anything from a game.** `VRageMath`, `VRage.*`, `Sandbox.*` - none of it, ever.

**Presentation decisions.** Word order, colour, spacing, how a duration is spelled, whether a warning
is shown at all.

## How that is enforced

`src/Reactor.Core/Reactor.Core.csproj` compiles these sources alone, as a netstandard2.0 assembly,
with `BannedApiAnalyzers` and the Space Engineers whitelist. It exists to fail. A reference to
anything above breaks that build before it reaches the mod.

`tests/Reactor.Core.Tests/PaperTarget.cs` is the other half: a target made of nothing but text, which
renders the whole editor and refuses anything it has not been told how to write down. Core being
renderable by something that is not this game is a claim, and that file is what keeps it honest.

## The rule the three projects follow

**A mixin holds behaviour that is Reactor's own and can be proved without the game running. The mod
holds behaviour that is the game's.**

That is why drawing is shared and the world is not. What sprites to emit for a row is Reactor's
decision, and a picture can be compared against a committed one. What a door is doing is Space
Engineers' answer, and a substitute for it would only ever prove that Reactor calls a copy of Keen's
API correctly - a test that grows more confident exactly as the real API moves away from it.
