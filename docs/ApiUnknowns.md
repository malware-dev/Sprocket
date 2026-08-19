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

*Affects* R39, R40, R41 and R24, which between them now carry three different meanings for
"there is no block here".

---

## Answered, and where the answers went

Kept listed so nobody re-opens them: how events reach us, whether levels are readable,
whether appearance is readable, whether a person can be told, what an LCD accepts, whether
docking is observable, where a program can live, whether a block's position is available,
whether being attacked is reported, and whether a change can be missed.

All of them are in Design.md. Two more were struck out earlier for never having been unknown
at all: a mod sees the entire world, so the one-construct limit is a choice rather than a
wall, and a cockpit's controls are readable as vectors.
