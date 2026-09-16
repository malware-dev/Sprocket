# What we do not know about the game yet

Internal. Questions about Space Engineers that block requirements from being settled.
Gathered here because the same ones kept surfacing while reasoning about stories, and
because guessing at them is how a design acquires assumptions nobody remembers making.

**Most of the original register has been answered**, and the answers are in
[Design.md](Design.md) under *What the game actually gives us*. Levels are readable,
appearance is readable, docking is observable, storage is the save file, position is
available, a person can be told in several ways, and an LCD will take almost anything.

What follows is what genuinely remains.

---

### 1. Which events are events, and which have to be watched

Some things the game will tell us about; others must be sampled and the change worked out.
That is per event rather than one answer, so this is not a question but a catalogue - and
the catalogue has to be built by going through the block kinds we intend to support.

Not blocking, but shaping. Every event that has to be sampled costs against T1 and T2, and
sampled events are the only ones that can be missed entirely. It is worth knowing early
which of the common ones - a button pressed, a door finishing, a sensor tripping, a
connector locking - are told to us.

**Mostly answered, by the toolbar ModAPI.** Every block that has a toolbar can be made to *tell* us,
by putting a Reactor action in a slot, rather than being sampled - so the catalogue below shrinks to
the blocks that have no toolbar. That API is unreleased and unmerged; see `HANDOVER.md`.

**What can be missed is bounded, and the bound is comfortable.** A level that is held
survives being sampled late; the state simply differs from the last sample and the change is
derived. What is lost is only a sampled thing that changes and changes back inside one
update - a door that opens and shuts again between two looks. That is a narrow enough hole
to live with, and to say out loud rather than paper over.

*Affects* T2, T3, R3, R16, and how large the runtime has to be.

### 2. What survives being copied

A construct pasted from a blueprint, or welded up from a projection, is new blocks with new
identities. The game does nothing for us here - detecting merges and splits is entirely our
responsibility - and it is not settled what happens to a copy: whether our data comes with
it, arrives fresh alongside the new grid, or has to be reconstructed.

**Projector building is the sharper case, and it is not an edge case at all.** A projected
construct is welded up block by block, so a system can be running before the blocks it needs
exist. That is not damage, and it is not a role deliberately left empty. It is a third
thing, and it makes R40's healing part of ordinary construction rather than only of repair.

**Toolbar preservation across a copy is imperfect, period** - Mal, 2026-09-12, from experience of
the game rather than from the source. The object builders say it should work: a slot storing a block
action carries `BlockEntityId`, and `MyObjectBuilder_ToolbarItemTerminalBlock.Remap` rewrites it
through `IMyRemapHelper` while `MyObjectBuilder_Toolbar.Remap` walks every slot. So a paste *should*
point a slot at the pasted copy of the block. In practice it does not reliably survive, and a
projector print is the worse case.

*What that settles for Reactor.* Wiring surviving a copy can never be relied on, so the bind-once
rule stands and the reporting carries the weight: a pasted construct arrives with some buttons wired
and some not, Reactor adopts what is there and writes nothing - it cannot tell a lost slot from a
deliberately cleared one - and the sequences whose buttons are gone report as unreachable. Which
makes a repair control load-bearing rather than a nicety; see Design.md 4b.

*Still to measure here:* whether a block's mod storage travels in a blueprint at all. The program
surviving a paste rests entirely on it, and nothing has tested it.

*Affects* R39, R40, R41 and R24, which between them now carry three different meanings for
"there is no block here".

### 3. When does a construct stop being the same construct

**Must be answered before release. It is a correctness hole, not a question of polish.**

`GridWorld.Entities` is this construct only - the grid the Reactor sits on plus whatever is
mechanically connected to it by rotors, pistons and hinges. It keeps that promise by marking itself
stale on the grids' own `OnBlockAdded` and `OnBlockRemoved`, and rebuilding when somebody looks.

**That signal does not cover every way membership changes**, and at least three ways are ordinary
play rather than edge cases:

- **A rotor or piston head detaching.** No block is added and none is removed. The subgrid simply
  stops being part of this construct, and nothing tells us.
- **A grid splitting.** Cutting or shooting a construct in two leaves blocks that are still there
  and no longer ours.
- **Two grids merging**, which is the same problem arriving rather than leaving.

Until this is answered, a sequence can go on cycling a door that floated away five minutes ago, and
the editor will list it as present. It is also what `GridWorld.Revision` rests on, so a reference
now resolves against a construct that may quietly be wrong.

**This is not an unknown; it is deferred work.** The game does say enough - `OnGridSplit`,
`OnGridMerge` and the mechanical connection blocks' attach and detach between them cover it. It sits
in this file because this is where things that must be settled before release are kept, and because
somebody reading `GridWorld` needs one place to be sent.

### 4. Measured: a terminal action cannot be registered once for every block

**Answered by measurement on 2026-09-12, and the answer is no.** It is here rather than in
Design.md because it closes off a design, and somebody will propose that design again.

`MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>` looks like it should reach every
terminal block. The source says so too: `MyTerminalBlock` carries
`[MyTerminalInterface(typeof(IMyTerminalBlock), ...)]`, nothing else links that interface, and
`MyTerminalControlFactory.InitializeControls` walks `BaseType` upward copying each base's actions
down - which every terminal block's chain passes through.

**It does not work.** One action registered that way appeared on a door and an interior light, and
was absent from a battery, a reactor, a button panel, an air vent, a cockpit, a timer and a
programmable block. Unreliable rather than unavailable, which is worse: it would have looked
correct in whatever the author happened to test.

The mechanism, as far as it goes: a concrete type's action list is built **once**, the first time
anything asks for that type, by copying what its bases hold *at that moment*, and is never
revisited. So a type whose list already exists cannot receive a later registration. That much is
our own fault in one case - `TriggerAction.Register` asks for `IMyProgrammableBlock` first, which
builds that type's list before the global action exists - but it does not explain the rest, and the
rest is what makes the approach unusable. `MyTerminalControlFactory.Unload()` clears the cache on
session unload, so staleness between world loads is not it either.

*What it closes off.* A toolbar slot that names **the block owning the toolbar** rather than
Reactor's own block. That would have let wiring survive a paste or a projector print, since the
paste remaps a self-reference and cannot remap a pointer to a Reactor on another ship. It needs the
action present on arbitrary blocks, so it is off the table in that form.

*What it leaves open.* Only blocks that own a toolbar ever need the action - button panels, timers,
sensors, event controllers, cockpits - which is a short enumerable list rather than every block in
the game. Registering per type across that list was never tested and is not ruled out by this.

*Affects* question 2, which is where wiring surviving a copy belongs.

---

## Answered, and where the answers went

Kept listed so nobody re-opens them: how events reach us, whether levels are readable,
whether appearance is readable, whether a person can be told, what an LCD accepts, whether
docking is observable, where a program can live, whether a block's position is available,
whether being attacked is reported, and whether a change can be missed.

All of them are in Design.md. Two more were struck out earlier for never having been unknown
at all: a mod sees the entire world, so the one-construct limit is a choice rather than a
wall, and a cockpit's controls are readable as vectors.
