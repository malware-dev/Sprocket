# The catalogue

Internal. What Space Engineers offers a mod, surveyed rather than sampled, and **what
Reactor needs in order to use it**.

[UserStories.md](UserStories.md) says what people want to be able to *say*, and the
language that comes out of it is the grammar. This says what there is to talk about, and
what the grammar therefore has to be capable of.

**This document is about what is needed, not about what exists.** It does not describe
what Reactor does today, and it does not propose how anything should be built. Each need
gets dealt with on its own, in its own time, and several will be answered in ways nothing
here anticipates. Recording a need is not a commitment to a mechanism.

The survey is complete on purpose. A catalogue cut down to what today's stories need would
hide the thing a story next month asks for, and there is no cost to knowing about a
reading we choose not to offer. **"We looked and decided against it" and "we never looked"
are different states, and only one of them can be reviewed.**

## How this was built

- Reflection over `Bin64` - `Sandbox.Common.dll`, `SpaceEngineers.Game.dll`,
  `VRage.Game.dll`. **96 mod-side block interfaces**, 807 properties, 558 methods,
  78 block-level events, out of 151 event declarations across the whole ModAPI.
- The MDK2 mod whitelist, `modwhitelist.dat`, extracted from
  `Mal.Mdk2.ModAnalyzers.dll`. 391 lines. Conclusions from it were **test-compiled**
  rather than read off, because its wildcards do not behave the way they look.
- The canonical terminal id list:
  https://malforge.github.io/spaceengineers/pbapi/List-Of-Terminal-Properties-And-Actions.html
- Interface documentation comments, decompiled with `ilspycmd`.

**This drifts with the game.** Re-run the sweep after an update rather than trusting the
numbers here. Nothing should be built so that a new block type or a renamed property is a
code change.

## The three layers

Terminal properties and actions are the base. Every block has them, including a block from
a mod nobody has heard of, so supporting them is not optional. Above that, where we know
the interface, an **extension** does better. Below that, for the few classes the whitelist
allows, direct access does better still.

**The base layer is the terminal, and it is correct.** Its entries are the block's own
terminal controls. That is worth saying because it is easy to mistake for a weakness: it
is the surface the player already knows, and story H is explicitly about Reactor being no
more work than the terminal.

What it is, though, is a **control** surface. It carries what a player can set and press,
so it never carries a reading that is only an observation. A door's terminal `Open` is the
door's open control and does exactly what the terminal does; that a door is *Opening*
rather than *Open* is not a control, so no terminal property exists for it. The sensor is
the same fact told plainly: its terminal is its settings, because settings are what a
terminal is for.

An extension is mostly additive, and **may** also remove base entries and put its own in
their place. That is an option it has, not a rule it follows. An extension that removes
nothing is a perfectly good extension.

**Needed:** all three layers reached through one seam, so that what a block offers is a
fact discovered per block rather than a shape the editor knows in advance, and so that an
unknown block from an unknown mod is a smaller citizen rather than a broken one.

### The base layer, and what it demands

`IMyTerminalBlock.GetProperties` and `GetActions` answer at runtime for whatever block is
in front of us. `ITerminalProperty` carries an `Id` and a `TypeName`; `ITerminalAction`
carries an `Id`, a `Name`, `Apply` and `IsEnabled`. `TypeName` is one of `Boolean`,
`Single`, `Int64`, `StringBuilder`, `Color`.

Four things this surface demands, all observed rather than anticipated:

- **Actions come in triples.** `Open`, `Open_On`, `Open_Off`; `OnOff`, `OnOff_On`,
  `OnOff_Off`. The bare id is a toggle. **Needed:** a way to offer the explicit pair and
  never the toggle, because a stored "toggle the door" depends on state its author could
  not see. Story A wants a toggle *button*, which is a different thing entirely.
- **`Increase` and `Decrease` families** are relative adjustment. Numerous, and not junk.
  **Needed:** either a place for relative change in the language, or a stated decision
  that they are not offered.
- **Ids cannot be prettified.** `alignment` is lowercase among PascalCase neighbours, and
  the interior light ships `Blink Interval` and `Blink Lenght`, with a space and with the
  game's own misspelling. **Needed:** wording that comes from the game's own control
  titles or from an author, never from transforming an id.
- **Numeric readings arrive with their bounds.** `GetMinimum`, `GetMaximum` and
  `GetDefaultValue` are available for every numeric property. **Needed:** a numeric
  reading that carries its range, so a threshold can be offered against real limits
  without anybody authoring them.

**The LCD properties are not noise and must not be filtered by id.** Roughly seventeen
properties per block exist because the block provides text surfaces, and a modded door
with a screen has them legitimately. The game names the exact condition:
`IMyTextSurfaceProvider.UseGenericLcd`, documented as "whether generic LCD terminal
controls should be created".

**Needed:** filtering that tests a capability rather than matching a name. The base layer
filters on capability; an extension filters on knowledge. A denylist of ids would be wrong
the first time a mod added one of its own.

### What the direct layer reaches

Almost nothing. Test-compiled against the MDK2 analyzer:

| Class | Namespace | Verdict |
|---|---|---|
| `MyDoor` | `Sandbox.Game.Entities` | allowed |
| `MyCubeBlock` | `Sandbox.Game.Entities` | allowed |
| `MyTerminalBlock` | `Sandbox.Game.Entities.Cube` | MDK01 prohibited |
| `MyShipConnector` | `Sandbox.Game.Entities.Cube` | MDK01 prohibited |
| `MySlimBlock` | `Sandbox.Game.Entities.Cube` | MDK01 prohibited |
| `MySensorBlock` | `Sandbox.Game.Entities.Blocks` | MDK01 prohibited |
| `MyAirVent` | `SpaceEngineers.Game.Entities.Blocks` | MDK01 prohibited |
| `MyButtonPanel` | `SpaceEngineers.Game.Entities.Blocks` | MDK01 prohibited |

The whitelist grants `Sandbox.Game.Entities.*` and **the wildcard does not recurse**.
`MyDoor` is reachable only because Keen filed it one namespace level up from every other
block class, which is an accident and not a grant.

**Needed:** room for an extension to reach that far, and nothing anywhere that requires it
can.

## What there is to talk about

807 properties across 94 block interfaces, with the universal base members factored out.
This is the shape of the vocabulary the language has to carry.

| What a reading answers with | Count |
|---|---:|
| a flag | 379 |
| a number | 247 |
| **another thing** | ~48 |
| a vector | 39 |
| **one of a known set** | ~22 |
| **possibly nothing** | 14 |
| **a colour** | 11 |
| text | 10 |

**Flags and numbers are 77% of it.** That is the case the language must make effortless,
and everything below is the remaining quarter.

### One of a known set

Twenty-two properties answer with one of a fixed set of values, and they are the richest
readings on the surface because each is a whole vocabulary in a single member.

| Set | Values | On |
|---|---|---|
| `DoorStatus` | Opening, Open, Closing, Closed | door, advanced door, airtight door, hangar door, slide door, **parachute** |
| `VentStatus` | Depressurized, Depressurizing, Pressurized, Pressurizing | air vent |
| `PistonStatus` | extending, retracting, stopped, at limit | piston, extended piston |
| `MyShipConnectorStatus` | Unconnected, Connectable, Connected | connector |
| `LandingGearMode` | | landing gear |
| `MyJumpDriveStatus`, `MyLaserAntennaStatus` | | jump drive, laser antenna |
| `MyAssemblerMode`, `MyConveyorSorterMode`, `ChargeMode`, `FlightMode` | | assembler, sorter, battery, remote control, flight movement |

A door is not open or shut. It is in one of four states, two of which are transitions, and
the whole of a door's vocabulary falls out of that single reading.

**Needed, and this is one need in three parts:**

- **A reading whose possible values are known and finite**, so the editor can offer them
  rather than an author writing each state out by hand. A hand-kept list goes stale the
  first time the game adds a value, which is the argument R23 already makes about roles.
- **A way to ask whether a reading is one of several.** *"While the door is moving"* means
  Opening or Closing. Without it, a four-valued reading can only be compared to one value
  at a time, and the most natural sentence about a door is unsayable.
- **Identity that survives the game changing underneath a saved rule.** These sets belong
  to Keen, and a value inserted into the middle of one must not silently repoint a rule
  somebody wrote a year ago.

`MyShipConnectorStatus.Connectable` is worth calling out on its own. It is a state we
never imagined, and "when the connector is ready to lock" is a sentence somebody would
write.

### Another thing

Around forty-eight properties answer with a thing rather than a reading:

| Reading | Blocks |
|---|---|
| `Pilot`, `LastPilot` | cockpit, cryo chamber, remote control, ship controller, turret control |
| `Target` | every turret, turret control block |
| `LastDetectedEntity` | sensor |
| `OtherConnector` | connector |
| `Other` | merge block, laser antenna |
| `Top`, `TopGrid`, `Rotor`, `RotorGrid` | every mechanical connection: piston, rotor, hinge, suspension |
| `ProjectedGrid` | projector |
| `InputInventory`, `OutputInventory` | assembler, refinery, production block |

**Needed: a rule that can be about a thing that arrived**, rather than only about blocks
chosen when the rule was written. That is the whole of story T, where a station acts on
whatever miner ties up to it, and half of story P. It is also the difference between a
sensor that can say *something* is there and one that can say *what*.

This is the largest thing in the document, and the one that most changes what a sentence
can be about. It should be decided deliberately rather than arrived at.

### A colour

Eleven properties, and colour is one of the five types the base layer reports, so it
arrives whether or not it is planned for. Story D is entirely about restoring lighting
exactly as it was.

**Needed:** a colour as a reading and as a value, and something in the editor that can
show one and choose one.

### Possibly nothing

Fourteen properties can answer with no value at all.

**Needed:** "there is no reading" as an answer distinguishable from a reading. A rule
quietly treating absence as a default is the same silent failure the design already
refuses when a rule refers to a block that no longer exists.

### A vector

Thirty-nine: positions, directions, sensor field bounds, cockpit stick input.

**Not needed yet, and recorded so.** Story W wants cockpit input, and that is a case for
one specific reading rather than for a vector in the language.

## Told and sampled, which is a performance question

[ApiUnknowns.md](ApiUnknowns.md) asks which events are told and which must be sampled, and
says the answer is a catalogue rather than a rule. It is also the largest performance
question in the project: a sampled reading costs on every tick, on every instance, on a
server running many constructs. **Anything the game will tell us is a reading we stop
paying for.**

**Two events are universal:**

```
IMyCubeBlock.IsWorkingChanged        // every block
IMyFunctionalBlock.EnabledChanged    // every block with a switch
```

So "when this is switched off" and "when this stops working" can be told for **every
block, including ones from mods nobody has written an extension for**.

Told, per block: the whole door family, sensor, button panel, connector, landing gear,
merge block, piston, rotor, mechanical connections generally, cockpit, production block,
assembler, battery, power producer, thruster, event controller, transponder, work-area
tool.

`IMyButtonPanel.ButtonPressed` carries the button **index**, so a panel is four triggers
rather than one, and a press is heard without occupying a toolbar slot. All of these are
on the mod-side interfaces rather than the `Ingame` ones a programmable block sees.

**Needed:** a reading that can be fed by a subscription instead of a poll, without whoever
uses it knowing which it was. Every extension that converts a sample into a subscription
removes a per-tick read across every instance on the server, so this is where the
performance of the whole runtime is decided.

### The air vent has no events at all

Not one. Its status must be sampled, and the vent is the block the airlock is built
around. So *"when the vent finishes emptying"* sits inside the bound on what can be
missed: a vent that empties and refills between two looks is invisible. Gas tanks are
silent too, which puts both tank-level stories on sampling as well.

**Needed:** the sampled path treated as the hot path, because the blocks that cannot be
told about are exactly the ones the stories lean on hardest.

### One thing that looks like a trigger and is not

`IMyTerminalBlock.PropertiesChanged` fires on any change, and its own documentation says
"it could be anything, starting from CustomName ending with Inventory items". It is a hint
that a block is worth re-reading, which makes it useful for keeping the sampled set small.
It is not a moment and must never be offered as one.

## Doing things

Plenty of blocks expose plain commands: `OpenDoor`, `CloseDoor`, `Connect`, `Disconnect`,
`Extend`, `Retract`, `Reverse`, `Attach`, `Detach`, `ClearQueue`, `ShootOnce`,
`CycleSelectedAmmoType`. These need nothing that is not already understood.

Two further sources both carry a value:

- **Settable properties.** A large share of the 807 are settable, and setting one is an
  action in every sense except that it needs an argument.
- **`Increase` and `Decrease` terminal actions**, which are relative rather than absolute,
  and which no amount of value-passing turns into the same thing.

**Needed:** actions that take a value, which is Design decision 3, and which the settable
count makes considerably more valuable than it looked. Separately, a decision on relative
adjustment.

`ITerminalAction.IsEnabled(block)` answers whether an action is available on this
particular block right now. **Needed:** somewhere to put that, because it is a free
contribution to story M.

## What every block gives us

- `IMyCubeBlock` - `IsWorking`, `IsFunctional`, `IsWorkingChanged`, `CubeGridChanged`
- `IMyFunctionalBlock` - `Enabled`, `EnabledChanged`
- `IMyTerminalBlock` - `CustomName`, `CustomData`, `DetailedInfo`, `ShowOnHUD`, and
  `CustomNameChanged`, `OwnershipChanged`, `PropertiesChanged`, `VisibilityChanged`
- `IMyCubeGrid` - `OnBlockAdded`, `OnBlockRemoved`, `OnGridSplit`, `OnGridMerge`

**Needed:** these treated as the floor every block stands on, so that a block nobody has
written an extension for still has a switch, a working state, a name and a place on the
grid.

## Two things the survey corrected

**Merging and splitting is observable.** Design.md says it is entirely our problem and the
game does nothing for us. `IMyCubeGrid` raises `OnGridSplit`, `OnGridMerge`,
`OnBlockAdded` and `OnBlockRemoved`. The detection is not ours. What stays ours is
deciding what a split *means* for a system whose parts are now on two grids.

**Construct scope is a predicate the game supplies.**
`IMyTerminalBlock.IsSameConstructAs` is documented as "mechanically connected with rotors
or pistons, but not connectors", which is `IWorld`'s scope rule nearly word for word.

## Where the extensions go first

Not every block needs one, and the ones that need one most are not the ones people
automate most. **An extension earns its place where a block is watched, not where it is
driven.** Lights, sound blocks and LCDs are among the most automated things in the game
and need nothing at all: they are outputs, and the terminal describes them completely.
What wants an extension is a block somebody asks a question about.

Two different things make a block worth the work, and they do not always travel together:

- **It has events**, so the extension turns a poll into a subscription. That is a
  performance win and scales with how many of them are on a server.
- **It has state the terminal cannot reach**, so the extension is the only way to say the
  thing at all. That is an expressiveness win and does not depend on scale.

The air vent has only the second and needs an extension badly. A block with only the first
is worth doing when there are many of them.

### Extensions key on the interface, not the block

This matters more than it looks. The vanilla families collapse into a handful of
interfaces:

| Interface | Covers |
|---|---|
| `IMyDoor` | door, advanced door, airtight door base, hangar door, sliding door |
| `IMyMechanicalConnectionBlock` | piston, extended piston, rotor, advanced rotor, hinge, suspension |
| `IMyPistonBase` | piston, extended piston |
| `IMyMotorStator` | rotor, advanced rotor, hinge |
| `IMyShipToolBase` | drill, welder, grinder |

So one door extension covers five block types rather than five extensions covering one
each. **And any modded block that implements the vanilla interface inherits the extension
for free**, which is the strongest argument for keying on the interface rather than on a
block type id: a mod author who implements `IMyDoor` gets Reactor's whole door vocabulary
without knowing Reactor exists.

`IMyParachute` is the exception that proves the point. It carries `DoorStatus` and the
same four door events, and it does **not** derive from `IMyDoor`. A shared shape is not
always a shared interface, so an extension has to be able to claim a block on the members
it actually finds.

### The first wave

| Extension | Why | Blocks |
|---|---|---:|
| `IMyDoor` | four events, and a four-valued status the terminal has no control for | 5 |
| `IMySensorBlock` | told, and detection is absent from the terminal entirely | 1 |
| `IMyShipConnector` | three events, and a three-valued status including *Connectable* | 1 |
| `IMyMechanicalConnectionBlock` | attach and detach, and what is on the other end | 6 |
| `IMyPistonBase` | status, limits reached, position | 2 |
| `IMyMotorStator` | angle, limits reached | 3 |
| `IMyButtonPanel` | a press carries its index, and the terminal has nothing about presses | 1 |
| `IMyAirVent` | no events, and its entire state is invisible to the terminal | 1 |
| `IMyGasTank` | sampled fill level, and the two tank stories rest on it | 1 |
| `IMyLandingGear` | two events and a lock mode | 1 |
| `IMyShipMergeBlock` | merge state, which nothing else can observe | 1 |
| `IMyShipToolBase` | busy state, told | 3 |
| `IMyParachute` | the door shape, on a block outside the door family | 1 |

Thirteen extensions covering roughly twenty-seven vanilla block types, plus whatever mods
implement those interfaces.

**Afterwards, and in no particular order:** cockpit and ship controller, which know whether
somebody is sitting in them; battery, which reports its charge as an event; assembler and
refinery, which report what they are doing; projector, which knows what it is projecting;
turrets, which know what they are pointed at.

**Needed:** an extension shape cheap enough that adding the fourteenth is a small job. The
first wave is a guess at what people automate, and it will be wrong somewhere. Being wrong
should cost one file.

## The needs, gathered

Nothing here is required to make an airlock work. All of it is required before the answer
to "can Reactor do X" stops being "not to that block". They are listed, not ordered, and
each will be taken on its own.

1. A reading whose possible values are known and finite.
2. Asking whether a reading is one of several.
3. Identity in a saved rule that survives the game changing a set of values.
4. A rule that can be about a thing that arrived, not only about blocks chosen up front.
5. A colour, as a reading and as a value.
6. "No reading" as an answer distinct from a reading.
7. Numeric readings that carry their range.
8. Actions that take a value.
9. A decision on relative adjustment.
10. Readings fed by subscription rather than poll, invisibly to whoever uses them.
