# The editor

Internal, and in progress. What Reactor presents and how somebody moves through it.
[Design.md](Design.md) holds the model; this holds the interface. Where they disagree, the
model is right and this is out of date.

Requirements are referred to by number, from [Requirements.md](Requirements.md).

## The root

What you see when you open Reactor is **the systems on this ship**, not a list of
sequences. A player did not build a procedure, they built an airlock, and that is the thing
they came looking for.

A system is what a system *is*. The ones on the ship are what they *have*.

```
   + New system
   + Load system
   ---------------------------------------------
Airlock                        on this ship: 2
   Airlock 1                   cycling - waiting for the inner door
   Port Airlock                idle
   + add

Drill Rig                      stowed
   + add
```

**A system with one of them on the ship shows as one row.** *Drill Rig > Drill Rig 1* is a
fold that says the same thing twice, and the difference between what a thing is and the one
you have is meaningless until there are two. The fold appears when the second arrives, which
is the moment the distinction becomes true and the natural place to explain it.

**Naming follows the game's own convention.** A new one is called *Airlock 1*, *Airlock 2*,
exactly as the game calls a block *Interior Light 4*. It looks provisional, so it invites a
rename without a dialog asking for one, it never collides with the name of the system, and
it makes no assumption about how somebody pluralises their own words. Front and Port arrive
when the player decides they care.

**No word is needed for "one of them".** *Instance* is programmer vocabulary and is not
used. Where a heading is genuinely needed, *on this ship* does the work. Internally these
notes say "a use of the system", which never has to reach a screen.

## Getting started

Both root actions are equally prominent. Most people will never author anything, and for
them Reactor is the thing where you load somebody's airlock and tell it which doors are
yours - so **Load system** is not a secondary path.

**New system asks what the system is, and then puts you on it.** It does not open an empty
container and it does not stop on a question of its own. The name is the one thing that
cannot be worked out - it is what M says when something is stuck and what K sends to a friend
- so it is asked, and after that you are standing on the system with the ways to start a
sequence in front of you.

*There used to be a screen in between, asking "what should start it?" the moment a system
was made. It bought nothing: two answers, no context, standing between somebody and the thing
they came to build, and it kept the system's own screen off the way back, so leaving the
first sequence dropped you out to the ship. The answers now live on the system, which is
where they were always going to end up.*

That is a sequencing decision, not a vocabulary one. *System* is an ordinary word and the
audience can read it.

**Labels are plain nouns, not sentences.** *New system*, not *build something new*. *Load
system*, not *use one somebody made*. The softer forms were proposed and rejected: this
audience has never been taught programmer vocabulary, which is not the same as having been
robbed of nouns, and a label rewritten into a friendly sentence buys nothing and reads as
being handled.

## The configurator

Filling in which blocks a system uses. Reached from two directions and identical from both:

- **loading a system somebody sent**, where it makes the first one on your ship;
- **+ another**, where it makes the next.

Derived from the system's roles with no effort from its author (R12), and improvable by an
author who wants to name things in their own words, order the questions, or explain the one
that always catches people out. The derived version has to be good enough to use, because
most systems will never be polished by anybody.

Optional roles are shown as optional (R24), so somebody who has no vents skips them
knowingly rather than wondering what they have broken.

## Copying, and not copying

**+ another** always makes one that belongs to the same system. Fix the system, and every one
of them changes - which is E's whole want, and it needs no dialog because the shape says it.

Wanting two that can differ is a different act: **duplicate the system**, giving a second
system with its own life. Rarer, deliberate, and named for what it does.

*This replaces an earlier design where copying asked "does this follow the original, or is it
its own?" at the moment of copying. The structure answers it, so the question does not need
asking.*

## A system's own screen

Close to what the editor already is: where sequences are written, triggers chosen, steps
added, states set. The place somebody spends their time.

It also carries **the system's name and a short description its author wrote** - what this
thing is, in their words. For the person who built it that is a label; for everybody who
receives it later it is the only explanation there will ever be of what they have just
loaded, and it travels with the system when it is shared.

**Nothing is declared before it is used.** This has now come up three times and is a rule
rather than a coincidence:

- a **system** exists because somebody built something, not because they made a container;
- a **role** exists because a sequence refers to a block (R23), not because anybody listed
  what the system needs;
- a **state** exists because a step sets it, not because anybody created a state first.

So there is no screen for declaring things you are about to need, and no moment where
somebody has to know what they will want before they want it. Writing *set the arm to
stowed* is what brings *the arm* into existence, and the values it can hold are exactly the
ones the sequences mention.

That last part is worth having on purpose: because the values are collected from use, a
condition about *the arm* can offer precisely those values to choose from. Nobody types a
value, so nobody can mistype one into existence and spend an evening wondering why *stowd*
never matches.

## What is settled elsewhere

- **Watching a sequence work** is R27 and R28; what it looks like, and whether it is a panel
  beside somebody who is looking at the actual doors, is open - see Design.md decision 9.
- **Telling somebody something** prefers the world over the interface: a light, a sound, a
  panel by the door, before chat or a notification. Chat is for debugging, opt-in.

## Open

- Does **+ another** ask which blocks immediately, or make one and let somebody fill it in as
  they work out what they are doing?
- What the root shows about the ship as a whole - anything running, anything broken,
  anything stuck - and whether that lives here or somewhere of its own.
- Where the **ship browser** sits now that the root is systems rather than sequences.
- Whether timers, if they are a thing distinct from *every so often*, live here too.

## Nothing appears or disappears with the data

A rule for every target, learned the hard way three times in one afternoon: whether something
is on screen must not depend on how much data happens to be there.

A search box that shows up past eight rows vanishes when you filter down to five - taking
away the control you just used, so you cannot undo it. A count that appears when you narrow
shoves everything below it up and down while somebody reads. Each was defensible on its own
and each made the screen move under the person using it.

A screen may be different because it *is* a different screen. It may not rearrange itself
because a list got shorter.

## The picker

Core says: **picking several things in an order, where each one narrows what the next can
be.** Blocks, and then what can happen to them - the second exists only because of the first
and cannot be asked before it. That dependency is the whole of what core states.

**One trip through the picker is one edit.** You look through, tick the things you want,
choose what should happen to them, and commit - and that whole process is a single change to
the sequence. Live editing has always meant the program has no Save button, never that each
tap on the way to an answer is committed on its own. *The picker was originally handed the
step's own list of targets and ticked entries straight into it, so a tentative tap was already
an edit to a system that might be running, and Back had nothing to back out of. The Done
button existed but only closed the screen. It commits now, which is what it always looked
like it did.*

Each stage says what is being asked, what the answers are, which are chosen, whether picking
one adds to the others or replaces them, and that it *can* be narrowed by typing. Whether a
way to narrow is worth showing, and at what point a list is long enough to want one, is the
target looking at its own screen.

This host draws the stages as columns, side by side, because a wide screen makes the earlier
answers worth keeping in view and changing one should be a glance to the left. A narrower
target might ask one stage at a time and be right to. The renderer is called
`ColumnPickRenderer` so that choice is visible in its name.

*This was previously `PathComponent` with `Pane`s, and core counted rows to decide when a
search box appeared. Both were core deciding presentation.*

### The dependency runs one way

Narrowing runs left to right and never back. Picking blocks decides what can be said about
them; it does not decide which blocks exist.

The picker used to enforce the rule in both directions: once you had ticked a block, only
blocks that could still be grouped with it were offered. On a ship where nothing else shares
the button's event, ticking the button emptied the list down to itself. The intent was that an
impossible pair could never be made. What it actually produced was a player watching their own
blocks disappear, which is the one thing a block picker must never do, and no explanation of
why.

So an impossible pair is now allowed to be made, and answered where the answer belongs: the
next stage has nothing in it and says so. A sentence the player can read beats a row that
silently was not there.

A block with no events at all is still absent from an event picker, and that is not the same
rule. That is the question talking, not the selection: there is nothing to ask about a block
that nothing can happen to. It holds from the moment the screen opens and never changes under
the player's hands.

A block that is already ticked is never hidden by narrowing either. A tick you cannot find is
a tick you cannot clear.

## Every screen that holds a list

The ship and a system are the same screen twice: **the ways to add something on top, a rule
under them, then what is already there.**

```
   + When...          + New system
   + Every...                  + Load system
   -----------------------------     -----------------------------
When Airlock Button is pressed     Airlock          on this ship: 2
Every 30 seconds                      Airlock 1     cycling
                                      + add
```

Learning the top half once is learning it everywhere, which matters more than either screen
being individually optimal. It also means a third way to start a sequence is one line in one
place rather than a new screen.

**Everything that adds something carries a plus.** Core does not spell it into the labels -
it says these are the ways to start something, and that a row is an action, which is already
the whole of the claim. Drawing that as a plus is the host being consistent with itself, and
a target with no room for it can leave it off and lose nothing.

## Naming happens on the heading

A sequence's heading is its name if it has one, and the sentence it makes if it does not:
*Let me out*, or *When Airlock Button is pressed*. Underneath that there used to be a second
control, a faint box reading "Name this sequence (optional)". Two places for one thing, one
announcing it and one setting it, and neither of them obviously the other's other half.

They are one thing now. The heading carries the name, and core says three things about it:
this screen can be renamed, here is what to ask, and here is the name somebody actually gave
- which is **not** the same as the heading. The heading always has something to read; the
custom name is only ever what a person typed. A target that confuses the two opens a box
prefilled with a sentence nobody wrote and calls it their name.

This host turns the heading into the box. A pencil sits beside it to say that it can be,
and pressing anywhere across the heading starts editing - an icon is a small target and a
title is a large one, and somebody who has worked out that the name is editable should not
then have to aim. Enter and clicking away both commit; Escape leaves it alone.

*It was a popup first, copied from **New system**, where a popup is right because there is
nothing on screen yet to edit. Renaming is the opposite case: the thing is right there and
the heading already shows the name, so a popup puts a second copy of it over the first and
asks which one is real.* The swap has one thing it must get right - the header cannot change
height when it happens - and that is what the host test measures.

**An empty name is an answer, not a missing one.** It hands the heading back to the sentence,
so clearing a name is as ordinary an act as giving one - which is what keeps naming genuinely
optional rather than a step you are quietly expected to complete. The box says "no name"
where a form would nag.

*This also means any screen can be renameable now, not just a sequence. Systems are the
obvious next one.*

**A sequence's heading has no subtitle.** What it is doing right now - *waiting to happen*,
*running* - used to be repeated there as well as on the sentence, and two lines saying the
same words read as two different claims. State belongs on the sentence, beside the switch and
the run button that change it.

## Taking it back

Every change to a sequence is an operation that knows how to reverse itself: the step it
removed, the name it replaced. Undo is offered on the heading and **says what it would take
back** - *Undo deleting a step*, not a bare *Undo*. Asking somebody to remember what they last
did is asking it of the one person least able to answer, because they have just made a
mistake.

Two designs were weighed and the second was wrong for a specific reason worth keeping.
Snapshot-and-restore, or replaying a log of forward operations from a base, both avoid writing
a reverse for each edit - which matters, because a reverse only ever runs on the unhappy path.
But **Reactor is live.** Sequences run while they are edited, and the runner and the open pages
hold references to the very objects involved. Both of those approaches rebuild the sequence,
leaving every one of those references pointing at an orphan. Operations mutate in place, so
identity survives.

The risk that made the alternatives attractive is answered by a round-trip test on every edit
- apply it, undo it, and the sequence must read as exactly what it was - rather than by care.

**Redo is the same list walked the other way**, and a new edit ends the branch: once somebody
has stepped back three changes and then done something else, those three are a road not taken.
Offering to redo into a sequence that has moved on would put a step back at an index that now
means something different.

Both are drawn as bare arrows of fixed width, with the description in a tooltip, and **they
never come and go.** Naming what would be undone is worth having, but not at the price of two
labels that change length every time somebody edits, and not at the price of the row shifting
under the pointer somebody was about to use. When there is nothing to walk back they stay
where they are and go quiet.

*Greying is right here and was wrong for the sentence, which is worth keeping straight. These
are controls that will become available, so grey means "not just now" and is honest. The
sentence is not a control at all, and greying it said "broken, or not yours to touch".*

**Both wait for the switch**, because both are edits. Which produced the one genuinely awkward
detail in here: the commands are registered whether or not there is anything to walk back,
because issuing a command needs the numbering only a rebuild does, while *whether there is
anything to walk back* changes without one - renaming deliberately does not rebuild. So the
build decides whether the command may exist and the tick decides whether it is offered.

*Deleting a whole sequence is not undoable and asks instead.*

## For the game host: the small window is an opportunity, not only a constraint

One screen at a time, and a layout that assumes no room, was adopted as a floor - the editor
has to work wherever it is embedded, so it may not assume a sidebar plus a detail pane. It
turns out to buy something nobody was aiming for.

**A window small enough to sit off to one side leaves the world visible and usable.** The
player can keep flying, keep walking, keep looking at the airlock they are describing, while
the editor is open beside it. Everything Reactor is about is a thing in the world, and an
editor that covers the world makes somebody hold it in their head instead.

Which opens a stronger possibility: **picking a block by pointing at it.** Core already says
only *choose one or more entities, here is what is on offer, tell me which*. A list with a
search is one way to answer that. Clicking the actual door is another, and for somebody who
knows exactly which door they mean and not what the game calls it, it is obviously the better
one. The picker's abstraction already permits it - `PickStage` never says "list".

*Not now, and not in the Avalonia prototype, which has no world to click. Recorded because it
should shape the game host from its first sketch rather than be retrofitted, and because it
argues for keeping the window small even when a target could afford to sprawl.*

## What a target must provide, and how little of that can be enforced

Core states capabilities and leaves presentation alone. That is the right split and it leaves
a hole: nothing stops a host simply never drawing one of them, and the omission looks like a
design choice rather than a gap.

`Reactor.Ui.Conformance` is a list of the things a target has to answer for - a way to reorder
a step, a way to narrow a long list, a way back, and saying why a running sequence cannot be
edited. Each host carries one test that walks the list and writes down what it does about
every entry. **Adding a point breaks every host until a person has looked at it.**

It is self-reported and proves nothing about the result. No test knows whether a drag target
is big enough to hit or whether a controller can reach it; that is review. What it closes is
*forgetting* - it turns "nobody thought about it" into "somebody decided", which is the same
trade the undo round-trip tests make. Answering "not yet" is allowed and is the point: a known
gap beats an unknown one.

Nothing reads it at runtime. It is a handful of strings built once and touched only by tests,
because core runs inside a game's frame and must not spend any of it checking itself.

### Reordering, as the worked example

Core used to hand a target `MoveUpCommand` and `MoveDownCommand`. That is a control, not a
capability: a host that wanted dragging would have had to synthesise a run of single-step
moves, and one with a number field could not express itself at all. Worse, it made the
cumbersome answer the one you get by doing nothing.

It is one `MoveToCommand` now, carrying a position - which needed a third shape of answer
beside text and a toggle, `SetNumber`, and that is also the shape a move will need on a wire.

This host answers it with **a grip at the left of each line**, dragged to where the line
should go. A grip rather than the whole row being draggable: the row is full of buttons, so
"press anywhere that is not a chip" is a rule nobody can see and one that makes an accidental
drag easy for somebody aiming just past a word. A handle says where to take hold, and says
that taking hold is possible at all.

The drop is sent as **one move**, not a run of single steps - dragging a line three places is
one thing somebody did, so it is one thing to take back.

**The rows give place.** While a line is being carried, the rows between where it came from
and where it is going step aside by exactly its height, so the space it would land in is
visibly empty rather than something to infer from a pointer position. They slide rather than
pop - a row jumping between two places reads as the list glitching, where a row moving out of
the way reads as the list responding. The line being carried is the one row with no easing on
it, because it has to track the pointer exactly.

*The arrows are gone.* Keeping both was clutter, and the drag is strictly better on a pointer.
It does leave this host with no way to reorder without one, which its conformance answer says
out loud - a real gap for a target that may one day be driven from a controller.

## Two switches in one column

The ship's list shows a system with the ones of it nested underneath, and both levels carry a
switch. Stacked in a column at the right edge they were indistinguishable, and reading the
labels to work out which one governs what is not a thing anybody should have to do.

Two things separate them, and they reinforce each other.

**A nested row is inset on both sides.** The left edge already stepped in to say "this belongs
to the row above"; the right edge did not, so a child's switch landed in the same column as the
switch that governs it. Stepping in on both makes the child visibly *inside* the parent, and
its controls inside the parent's.

**A switch over everything below it is filled; one over a single thing is an outline.** Indent
says which is nested; weight says which is the bigger act. Stopping a whole airlock is not the
same as stopping one of them, and the louder control should be the one with more behind it.

Core says which is which - `StatusEntry.SwitchGovernsAll` - because only core knows. A target
reading it off the indent would be guessing from a layout it chose itself.
