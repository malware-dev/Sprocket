# Reactor.Host.SpaceEngineers

The mod. **Everything here is the game's behaviour rather than Reactor's**, plus the packaging that
makes a folder Space Engineers will load.

Four adapters and a wrapper:

| | |
|---|---|
| `World/` | SE blocks become a `Reactor.World.IWorld` - kinds, states, actions, live readings |
| `Terminal/` | the game's terminal: the control that opens the editor, and the screen it opens - which becomes an `IGraphics` and a pointer |
| `ReactorSession`, `ReactorBlock` | the game's lifetime, and the component that rides on a block |
| `Input/` | the game's keyboard becomes edits to a text box |
| `Content/` | materials, fonts and the mod's own metadata |

## What it may depend on

Anything Space Engineers offers, within the mod whitelist, plus both mixins - whose sources it
compiles directly, because SE compiles a mod from loose `.cs` files rather than loading an assembly.

## What must never appear in it

**Anything Reactor could decide for itself.** A layout, a colour, a word on screen, a rule about what
a sequence may do. If it would still be true on a target that was not this game, it belongs in a
mixin.

## Why it is not simulatable, and why that is right

`src/Reactor.Host.SpaceEngineers.Sim` compiles both mixins and substitutes at `IWorld` - it invents a
ship rather than faking `IMyDoor`. Nothing in this project is exercised there, deliberately: a
hand-written stand-in for an API Keen keeps developing would keep passing while the game changed
underneath it. This code is verified by loading the game, and by nothing else.

## The rule the three projects follow

**A mixin holds behaviour that is Reactor's own and can be proved without the game running. The mod
holds behaviour that is the game's.**

That is why drawing is shared and the world is not. What sprites to emit for a row is Reactor's
decision, and a picture can be compared against a committed one. What a door is doing is Space
Engineers' answer, and a substitute for it would only ever prove that Reactor calls a copy of Keen's
API correctly - a test that grows more confident exactly as the real API moves away from it.
