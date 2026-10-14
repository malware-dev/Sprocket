# The model

What a player builds, as it is held in code: logic systems of sequences and triggers, run on installations, on one
construct. Most of it is in `src/Sprocket.Host.SpaceEngineers/Sequences`. How it is built and tested is in
[Building and testing](Building.md), and the projects around it in the [Overview](Overview.md).

## The screens' words and the code's

The screens and the codex use the player's words. Several types are named from earlier in the project, so the two
differ:

| On screen | In code |
|---|---|
| everything on one construct | `Automation`, kept by a `SprocketUnit` |
| logic system | `AutomationSystem` |
| installation | `SystemInstance` |
| a logic system with installations | an `AutomationSystem` whose `IsBlueprint` is set |
| slot | `Part` |
| sequence | `Sequence` |
| step | `Step`, of a `StepKind` |
| trigger | `Trigger` |
| blueprint, in the library | a `Volume` on the `Shelf` |
| construct | the grids a `GridWorld` covers |

The model holds ids and never words: actions, events and kinds are ids (`ActionId`, `EventId`, `KindId`), and every
word a player reads is looked up by the screen that shows it. What a player types is kept as typed: names and
descriptions.

## A construct and its Sprocket

A **construct** is the grids joined to one another by mechanical blocks (pistons, rotors, hinges), and not those joined
by connectors. `World/GridWorld.cs` covers them and follows the game's splits and merges, so a subgrid that breaks
away leaves the construct.

Each construct has one **`SprocketUnit`** (`World/SprocketUnit.cs`), holding the construct's `Automation`, its
`GridWorld` and the `AutomationRunner` that runs it. The program is stored in the mod storage of one programmable block,
the unit's *hero*. When the hero goes, the unit looks for it again under the same id, which is what a split does, then
takes on the first other programmable block of the construct. With none left, the unit is lost, and leaves a backup
where that came from damage.

## Automation

`Automation` (`Sequences/Automation.cs`) is everything one construct runs: its logic systems, and the references to
blocks and signals they share. A program that fails to read is held as nothing, marked not intact, so a damaged text
never runs as half a program.

## Logic systems

An `AutomationSystem` (`Sequences/AutomationSystem.cs`) is the automation for one machine. It holds:

- its **sequences** and its **triggers**: triggers belong to the logic system, not to a sequence
- its **slots**, and the **installations** that fill them
- a switch: a logic system is changed only while switched off, and its `Halt` says why it was switched off where Sprocket
  did it
- a name and a description
- an `Identity`, a Guid that never changes, and an `Origin` linking it to the blueprint in the library it came from

## Installations and slots

A **slot** (`Part`, `Sequences/Part.cs`) is a named place in a logic system, such as *the inner door*. A block slot
holds blocks; a signal slot holds a block and the toolbar slot on it that sends the signal. A slot can be optional, and
says what kinds of block it expects.

An **installation** (`SystemInstance`, `Sequences/SystemInstance.cs`) is one set of blocks and signals a logic system
runs on, such as one airlock among several. It fills every slot with its own blocks, has its own switch, and runs the
logic system's sequences on its own: runs are kept by sequence and installation, so the port airlock cycling leaves the
starboard one alone. A logic system with no installations runs through a hidden one of its own, numbered 0. A sequence
runs only while both its logic system and its installation are switched on.

**Slots are made, not declared.** `Sequences/Slotting.cs` works them out again after every change, from the groups of
blocks the steps and triggers name:

- The same group named twice is one slot.
- A group that is exactly a union of other slots uses those slots, the fewest that make it.
- Any other group is a slot of its own.
- A slot that keeps its group keeps its name, its description, and every installation's blocks.

`Sequences/Reslot.cs` records what each change did to the slots, so an undo puts them back.

## Sequences and steps

A **sequence** (`Sequences/Sequence.cs`) is a numbered, optionally named list of steps, run top to bottom. The number
is its identity inside its logic system and is never handed out twice. Every step can be switched off and carries a
note of its own.

| `StepKind` | The step |
|---|---|
| `Do` | Does something to blocks: one of their actions |
| `Set` | Sets one of the blocks' values |
| `Wait` | Passes a length of time |
| `Await` | Waits for everything started above it to finish |
| `Run`, `Stop` | Starts or stops another sequence of the same logic system |
| `If`, `OtherwiseIf`, `Otherwise`, `End` | An if: runs the branch whose test holds, each test a `Comparison` |

`Adds` marks the end of a branch, where the screens add steps to it.

## Triggers

A **trigger** (`Sequences/Trigger.cs`) runs or stops a sequence when something happens. There are three kinds:

| Type | Fires |
|---|---|
| `SignalTrigger` | When a signal arrives: a toolbar slot pressed, or a change a block announces, such as a door finishing closing |
| `WhenTrigger` | When blocks reach a state: any one of them, all of them, or a sum across them, compared with a value |
| `EveryTrigger` | Every set length of time, over and over |

A trigger fires only while its logic system and its installation are switched on, and a sequence an installation is
already running is not started again by it.

## Referring to a block

A step names its blocks through **references** (`EntityRef`, `Sequences/EntityRef.cs`), each holding keys in order of
strength: the block's entity id, its position on the construct counted from the hero's grid, and its name. The
position survives a paste and a blueprint, where every id changes.

`Runtime/Bindings.cs` works out what each reference points at afresh, against the world as it is.
A reference is **bound** where its strongest key finds the block, **lost** where nothing matches, and **adrift** where
the strongest key fails and a weaker one finds something. An adrift reference is never bound by itself: a different
door carrying the old name would cycle the wrong airlock. It is offered to the player to accept, and a sequence holding
one does not run.

## Blueprints and backups

A **blueprint** is a copy of a logic system kept in the **library**, on the player's own computer: `Storage/Shelf.cs`
keeps an index, `sprocket-bp.index`, and a `sprocket-bp-*.cog` file for each, in the game's Storage folder, so every
world on that computer can use them. A logic system made from one is linked to it by its `Origin`, and updating it from
the library keeps its identity and every installation's blocks in the slots that remain.

A **backup** is what a construct leaves when the last block that could hold its automation is destroyed: every logic
system the library holds no equal copy of, kept beside the world's save by `Storage/Backups.cs`. A grid deleted whole, by a
player in creative or by the server's cleanup, leaves none.

## Changes and undo

Every change to the model is an edit (`Sequences/Edit.cs`): it applies itself, and undoes itself. A sequence's edits,
a logic system's, and the construct's are each their own kind, and an edit that changes the slots carries those
changes with it. The history of edits belongs to the editor, not the model (`Features/Shell/History.cs`): one for each
logic system and one for the construct, a hundred deep.

## Stored as text

The whole model is written as a script in Sprocket's language and read back from it by `Storage/ProgramText.cs`: the
script is the program. What a running construct has reached, its switches and its part-finished runs, is stored
separately by `Storage/StateText.cs`. The language is set out in [Language](Language.md).
