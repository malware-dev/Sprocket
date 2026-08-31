# Reactor.Host.SpaceEngineers.Presentation.Mixin

Reactor as a person sees and touches it: the layout, the drawing, the controls, the views, and every
word on screen. **It knows the game's screen. It does not know the game's world.**

Core says a sentence refers to these blocks and this action; this decides that it reads "close the
inner door", in that order, in those colours, with those parts pressable.

## What it may depend on

`Reactor.Core.Mixin`, and the game's *drawing* surface - `VRageMath`, `VRage.Utils`, `VRage.Game`,
and `Sandbox.ModAPI` in `Graphics/ScreenSpaceGraphics.cs` alone.

## What must never appear in it

**A block.** `IMyDoor`, `IMyCubeGrid`, `IMyTerminalBlock`, `IMyInput` - anything that reports what
the game is doing rather than accepting what to draw. Reactor meets the world through
`Reactor.World.IWorld`, and the thing that implements it is the mod's.

**A session, a component, or an object builder.** Nothing that only exists inside a running game.

The day something here needs one of those, that is a design change rather than a convenience, and it
is worth stopping over.

## Where the words live

`Sequences/Wording.cs`, and nowhere else. It is the only file in the repository that knows the editor
speaks English. A second language, or a second host with different phrasing, is a change to that file
and nothing above it.

## The rule the three projects follow

**A mixin holds behaviour that is Reactor's own and can be proved without the game running. The mod
holds behaviour that is the game's.**

That is why drawing is shared and the world is not. What sprites to emit for a row is Reactor's
decision, and a picture can be compared against a committed one. What a door is doing is Space
Engineers' answer, and a substitute for it would only ever prove that Reactor calls a copy of Keen's
API correctly - a test that grows more confident exactly as the real API moves away from it.
