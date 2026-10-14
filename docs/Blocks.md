# Blocks

How Sprocket knows what a block can do: the kind it builds for each block, the capabilities that describe it, and
what it takes to teach Sprocket a new one. What a step and a trigger do with a block is in [The model](Model.md) and
[The runtime](Runtime.md).

## What a block is to Sprocket

Each kind of block, a type and subtype the game defines, gets one **`EntityKind`** (`World/EntityKind.cs`): every
value, action and change Sprocket can use on it. `World/BlockKinds.cs` builds it the first time a block of that kind is
met, and keeps it. A kind's members:

| Member | What it is | Type |
|---|---|---|
| Value | A property of the block, read after "is": a door's status, a light's color | `EntityAction`, read through a `Being` |
| Action | Something a step does to the block: open a door, extend a piston | `EntityAction`, done through a `Doing` |
| Change | A moment a trigger fires on, read after "when": a door finishing closing | `EntityEvent` |
| Function | A value that takes options | a `Being` made with `Taking` |

A value's reading is an `EntityValue`, eight bytes whose kind, a `ValueKind`, lives with the declaration: on or off, a
whole number, a real, text, a color, or a name from a known set.

## Capabilities

A **`Capability`** (`World/Capability.cs`) is one thing a block can be: a door, a switch, a store of gas. A block is
every capability that claims it, so a cryo chamber is a seat, a store and a switch at once.

**A capability claims a block by the interface the game gives it**, never by its type id. A modded door that
implements `IMyDoor` is a door to Sprocket without its author knowing Sprocket exists.

A capability holds nothing about any one block, so one serves every block of every construct. It has:

- `Claims(block)`: whether it describes this block
- `Describe(kind, block)`: adds its members to the block's kind
- `Listen`: subscribes to the game's events for a block, where the block announces its changes
- `StatusOf` and `IsBusy`: what the block says of itself while a step waits on it.

A kind is built in a fixed order: Sprocket's capabilities first, then each toolbar's signals, the general
capabilities every block has, the game's own actions and values, and last the renaming and hiding of what the game
offers.

## A worked example: the door

`Blocks/Door.cs` is a whole capability. The door's values and actions are declared once, as static readings:

```csharp
static readonly Reading Opened = Reading.Of<IMyDoor>(door => door.Status == DoorStatus.Open);
static readonly Reading Shut = Reading.Of<IMyDoor>(door => door.Status == DoorStatus.Closed);

static readonly Being State = Being.OneOf<IMyDoor>(Statuses, door => Position(door.Status));
static readonly Being Ratio = Being.Number<IMyDoor>(door => door.OpenRatio * 100.0);

static readonly Doing Opens = Doing.Of<IMyDoor>(door => door.OpenDoor(), Opened, Closing);
static readonly Doing Closes = Doing.Of<IMyDoor>(door => door.CloseDoor(), Shut, Opening);
```

A `Doing` is the action, what finishes it (`Opened`), and what would show something else took the door over.
`Claims` is one line:

```csharp
public override bool Claims(IMyTerminalBlock block)
{
    return block is IMyDoor;
}
```

`Describe` declares the members, each with an id, which the script writes, and its words:

```csharp
kind.WithValue("status", "status", State);

kind.WithChange("finishes-opening", "finishes opening", "status", Literal.Name("open")).Announced()
    .Reading("is open", "is not open")
    .WithChange("finishes-closing", "finishes closing", "status", Literal.Name("closed")).Announced()
    .Reading("is closed", "is not closed");

kind.WithValue("how-far-open", "how far open", Ratio).RuledOutBy(Shut);

kind.WithAction("open", "open", Opens, "Open_On").Lasting()
    .WithAction("close", "close", Closes, "Open_Off").Lasting()
    .Renaming("Open", false, "reverse");
```

- A **change** is a value reaching one of its states, declared after the value it watches, and carries the words the
  `if` selector and the sentences use for being in that state and out of it.
- **`Announced`, `RuledOutBy` and `Looked`** say how Sprocket hears of a change, as [The runtime](Runtime.md#triggers)
  sets out. A door announces most of its changes; how far it is open moves silently, and is looked at only while the
  door is not shut.
- **`Lasting`** marks an action that takes time, which a sequence can wait for. The id after the words, `Open_On`, is
  the game's own action it stands for.
- **`Renaming`** gives one of the game's own members Sprocket's words, which also lists it in the organized view.

`Listen`, further down the file, subscribes to the door's events, so a door that announces a change is looked at on the
tick after.

## The organized view and the terminal view

A selector lists a block's members two ways (`Features/Picking/PickPage.cs`):

- **The organized view** lists the members Sprocket has vetted, in Sprocket's words, grouped by category
  (`World/Category.cs`, assigned in `World/Categories.cs`). A member is vetted by being declared by a capability, or by
  `Renaming` or `Vetting` one of the game's.
- **The terminal view** lists the game's members as the terminal shows them, values and actions apart.

The words are written in lower case where a member is declared, and the screen capitalizes the first.

## Adding a kind of block

1. **Write the capability** in `Blocks/`, one file a kind: `Claims` by the block's interface, and `Describe` declaring
   its values, changes and actions. Say for every value and change how it is heard.
2. **Add it to the list** in `Blocks/Capabilities.cs`. The list is written by hand, and its order is the order kinds are
   described in.
3. **Give it a family** in `World/Families.cs`, and a category to each member that needs one.
4. **Give the simulator the block**: `src/Sprocket.Host.SpaceEngineers.Sim/Machine` holds a simulated block for each
   kind, built in `Rig.cs`.
5. **Test it** in `tests/Sprocket.Core.Tests`, one file a kind: `LightTests.cs` is a short one to follow. Where watching
   the block costs calls into the game, `BudgetTests` holds the cost.

A member's words are written where it is declared, in English; a family's, a category's and a trouble's names are
texts (`Sprocket.Family.*`, `Sprocket.Category.*`, `Sprocket.Trouble.*`, see [Text](Text.md)).

## What the game gives

Facts about the game that the code rests on, each measured or read from the game's own code:

**Hearing about changes**
- The air vent has no events of any kind, so everything about it is looked for, and a room that empties and refills
  between two looks is missed.
- Doors announce being told to open, but only a hangar door announces being told to close.
- `PropertiesChanged` fires for anything at all, a tank's every tick its gas moves and a generator's every time it burns
  ice, so it prompts a look, and is never a moment of its own. A tank's client hears it once for every ten-thousandth of
  capacity moved.
- `HasCapacityRemainingChanged` fires only at empty, and when the store comes back from empty.

**What a mod may touch**
- `[StructLayout(Explicit)]` passes the mod whitelist, but `FieldOffsetAttribute` does not, nor do `GCHandle`, `IntPtr`
  and `Marshal`: nothing reinterprets memory. `Buffer.BlockCopy`, `decimal.GetBits` and `new Guid(byte[])` are allowed,
  through arrays.
- `MyDoor` and `MyCubeBlock` are allowed, but `MyTerminalBlock`, `MyAirVent`, `MyButtonPanel` and the rest of
  `Sandbox.Game.Entities` are not, since the whitelist's entry for it does not reach its sub-namespaces.
- A union is impossible in a mod, so a value is eight bytes read by its declared kind.

**Setting values**
- The game's own declarations of a property's type are often placeholders: a color setter declares strings but needs
  numbers, and a radius or a blink length declares a string but needs a real. Every string the game declares for a
  property is one.
- A whole number is sent as 32-bit where it fits, since a boxed 64-bit cast throws, and a real as `Single`.
- A slider's scale cannot be read back, which is why a number is set in a bounded box rather than on a slider.
- A ship controller's handbrake and park setters toggle rather than set.

**Toolbars**
- An action added for `IMyTerminalBlock` reaches some kinds and not others, and a kind's action list is built once, the
  first time it is asked for, so Sprocket's toolbar action is the programmable block's own.
- An empty toolbar slot raises nothing when pressed.
- A slot names its block by entity id, which a paste or a projector print does not reliably carry across.
- The toolbar of a block is reached through its component container; a cast to `IMyToolbarProvider` gives null.

**Text**
- The game's font has 496 glyphs: German, French, Spanish, Polish, Czech, Turkish and Russian draw whole; Chinese and
  Japanese have none.
