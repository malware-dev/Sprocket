# Reactor

Automation that someone with no programming knowledge can build, read, and fix.
The target is a Space Engineers mod; the editor is abstract, and this host is one
implementation of it.

This document is the model, and it is internal. Where something is undecided it says
so, and those are the questions to settle rather than gaps to quietly fill in.

The wants it answers to are in [UserStories.md](UserStories.md), which is public, with
our own notes on them in [StoryNotes.md](StoryNotes.md). Nothing here belongs in the
first of those.

## Who it is for, and what that forbids

The audience is a player who wants a door to open. They are not stupid and they are
not children; they simply have never been taught the ideas programmers take as
furniture, and every one of those ideas we import is a tax charged to every user
forever.

**Be precise about what they lack.** Space Engineers is played overwhelmingly by adults who
run conveyor networks, balance power budgets, work out thrust to weight, and drive
projectors and blueprints without help. What they have not been taught is *programmer
vocabulary and structure* - classes, instances, loops, variables, scope. That is a specific
gap, not a general one.

So there is a line, and it is easy to cross while trying to be kind:

- **Removing a programmer concept is the job.** No loops, no nesting, no instantiation step,
  no words that mean one thing here and another thing everywhere else.
- **Removing information is condescension.** Hiding what something is, refusing to name a
  thing they can plainly see, or protecting them from a choice they are entitled to make,
  treats a capable adult as a liability.

When a simplification is proposed, that is the question to ask of it: is this taking away
jargon, or taking away the truth? The first earns its keep. The second is how a tool
acquires the tone that makes people put it down.

That gives a test which has already killed several features: **if a concept only earns
its keep by being general, it does not earn its keep.** Reactor is for building
automation, not a set of shipped automations - but the difference between the two is
expressiveness, not abstraction.

One thing that is *not* a reason to leave something out: **just because it can be done
in vanilla automation does not mean Reactor should not be able to do it as well.** The
easy cases are not beneath the system - they are what keeps a ship's automation in one
place, and Reactor can only explain behaviour it can see. Every case somebody sets up
in the game's own terminal instead is a hole in every answer we later try to give them.

That is not a licence to import every vanilla feature as a want. It says vanilla
capability never disqualifies a story; there still has to be a story.

**Where the boundary actually falls: setting a block up is not automation.** A sensor's
friend-or-foe filter, its range, a door's speed - these are configured on the block
once, the player already knows where they live, and Reactor has no business
reimplementing the terminal. What Reactor owns is what happens *between* blocks, and
over time: this happened, so do that.

The line is not that block settings are out of bounds, because changing one is a
perfectly good action - a step that turns the lights red is setting a block's colour.
It is that *configuring a block once and leaving it* belongs to the game, while
*changing it because something happened* belongs here.

**And a second principle, about motion.** Reactor never moves anything the player did not
ask it to move. That is what rules out reversal: inferring "back" means moving things
nobody asked to have moved.

It does not settle the harder question, which is what a player may ask for. Restoring a
piston or a rotor is travel through a world that is not the world it was captured in, and
a bulk restore has no order in it, so it moves everything at once with no idea which
order mattered - a piston returning to where it was crushes what has since parked there.
That is a reason for care rather than a prohibition: somebody who explicitly asks for a
piston's position to be remembered has asked. Whether the answer is a safe default with
an opt-in, an ordering the player controls, or something else, depends on how it is
built. Unresolved, and low priority.

The quick test, when a want sounds like this one: would serving it mean rebuilding a
block's terminal screen inside Reactor? Fifty-odd block types, every setting on each of
them, duplicated, and re-checked against the game every update. The answer is no, and
it is not close.

**The game's own words are used for the game's own things.** A merge block welds two
constructs into one and is indistinguishable from a repair; it is not a docking block,
however many people use it as one, and Reactor must not describe it as one. Borrowing a
convenient word for a thing that does not work that way teaches the player something false
and then relies on them not finding out.

**A word that programmers already know must not be given a different meaning.** They are
part of the audience, and a false friend costs an experienced reader real confusion while
buying a beginner nothing. This is what ruled out *while* for a trigger condition.

**What core says, and what it does not.** Core states what has to be possible and what must
come back from it: there must be a way to choose one of these, and it must tell me which. It
does not say what that looks like, where it sits, or how somebody works it. A list, a wheel,
a row of buttons, a voice prompt - all of them satisfy the same statement, and a target that
knows its own medium will pick better than we can from here.

**Core does have leanings, and needs them.** The order components are given in is an order.
*Fills* says which one should take whatever room is left rather than letting the whole screen
scroll and drag a search box off the top. A screen has a way onward and a way back, and which
is which matters. All of that is shape, and core is right to say it.

What core does not say is the detail: sizes, positions, what a thing is made of, how it opens,
what gesture works it. "Ask for a name" is core's; "a panel two hundred wide, anchored under
the button, dismissed by clicking away" is the target's, and had no business in core when I
put it there.

The line runs the other way too. A target may decide that double-clicking a choice takes it
and moves on, because that is how somebody reaches for two things at once. It may not decide
what taking a choice means.

**Rebuild only when it is unavoidable. Otherwise update.** This holds everywhere - in core,
in every target, at every level.

Anything torn down and made again loses what somebody was doing with it: the caret in the box
they were typing into, the row under their pointer, the second half of a double-click, the
place they had scrolled to. Those are not cosmetic losses; they are the difference between a
tool that can be used and one that fights back. And a screen that rebuilds cannot be watched,
because everything on it moves at once.

So the question is never "is it cheaper to rebuild" - it is "did the shape actually change".
A different number of things is a change. Different words in the same number of things is an
update. Choosing one of the things on offer is an update. Somebody typing is an update.

**A command id says what it is.** It is a small purpose - back, undo, the up arrow on step
three, the second slot of the trigger - packed into a number, so the same control gets the
same id on every rebuild by construction. A target may hold one.

*This was got badly wrong first.* An id used to be a counter reset on every rebuild, so it
meant "the seventh thing this page registered this time round" - a position, not a reference.
It was stable only while nothing about the page's shape changed, nothing enforced that, and
nothing noticed when it stopped being true. It cost us a search box that went dead whenever
its own list narrowed, because narrowing shifted every id registered after it. The rule that
grew out of that - *never remember a command id* - was a warning about the flaw rather than a
principle, and it is retired.

Two things came with the fix. Registering the same identity twice now **throws**, where the
equivalent mistake under a counter could not be detected at all. And a command no longer has
to be born during a rebuild, because withholding one does not renumber its neighbours.

It is the same answer as everywhere else in Reactor: references are by identity, never by
position. R47 says it for blocks, the edit operations say it for what they point at, and this
was the last place still asking which slot something was in.

### What counts as one operation

Every change to a sequence is recorded so it can be taken back, which forces a question that
had never been asked out loud: **where does one change stop and the next begin?** It can be as
small as one press of *move up*, or as large as searching a whole ship, selecting eight blocks
and settling on one thing to do to them.

"Does this affect the integrity of the system" is the obvious test and it is not enough - the
picker writing through *did* affect it. Three questions do the work:

- **An operation is what the user would say they did.** Ask a player. They say "I moved that
  step up", or "I made it close the outer door instead". Nobody has ever said "I ticked the
  outer door" - that was not a thing they did, it was a keystroke inside a thing they were
  doing. This is the same boundary undo wants, and the same one that will go over the wire.
- **Anything the runtime can see may only hold a state somebody chose.** Not "did it need to
  affect integrity" but "was anything watching". A sequence runs while it is edited, so a
  value written before somebody has settled on it is a system acting on a decision nobody
  made. Half-made choices belong somewhere nothing is looking.
- **Fold a run of changes into one exactly when the states in between were not chosen.**
  Moving a step up five rows is five operations, because each row it passes through is a
  position somebody clicked to and would be content to leave it at. Typing a nine-letter name
  is one, because "Let me o" is not a name anybody chose - it is debris on the way to one.

The first two are independent and both have to be asked. Correct boundaries with a live model
still lets the runtime watch a half-choice; a scratch buffer with wrong boundaries still gives
an undo that either throws away too much or has to be pressed eleven times.

Three consequences worth stating outright, because they are what make the rest cohere:

- **Nothing nests.** Steps are a flat numbered list. Nesting is what defeats novices,
  and every feature that would have needed it has so far had a flat form that reads
  better.
- **Nothing iterates.** A trigger or a step takes several blocks at once, so the
  collection is the ordinary case and the loop never appears. There is no counter, no
  termination condition and no iteration variable.
- **The program is live.** There is no build, no run, no save. A change takes effect as it
  is made, and the program as a whole never needs committing.

  *That is about the program, not about every tap on the way to a decision. This bullet used
  to end "and Done only ever means I have finished, never apply this", and the picker was
  written to match: it was handed the step's own list of targets and ticked entries straight
  into it, so a half-made choice was already live on a running system. Choosing a thing is
  one process with a beginning and an end, and Done is its end.*

## The model

### Ship and blocks

Given, not ours. A block has a kind, a name, some signals, and some things that can be
done to it. Reactor reads and writes; it does not model the world.

Two things about the target constrain the design more than they look. A mod can add
subtypes but not block types, so the vocabulary is bounded by what the game already
has - on the order of fifty or sixty terminal types, not hundreds. And a mod's code is
compiled in-game under a whitelist, which bounds the primitives available but not the
abstractions built from them.

### Where the program lives

Not in a block. Vanilla automation keeps the behaviour inside the event controller or
timer that runs it, so a cannon round through that one block takes the automation with
it and the player starts again from nothing. Reactor's program belongs to the
construct, and losing any single block may cost the reference to that block and
nothing else.

This is a constraint on storage rather than on the language, and it is the reason P is
answerable at all rather than being the same defeat in a nicer editor.

### One construct, by choice

A mod can see the entire world. Nothing stops Reactor watching every grid on the server and
operating any of it. The limit to one construct is ours, chosen for reasons that are worth
writing down because the constraint will otherwise look like ignorance of what is possible.

- **Operating other people's property.** If automation reaches past its own construct, then
  tying up at a stranger's station hands them your thrusters, your reactors and your cargo.
  Ownership might police that; needing policing is the tell.
- **Cost.** T1 says the total price is what is happening, not what exists. World scope means
  every construct is potentially watching every other, and the arithmetic stops being
  bounded by anything the player can see.
- **Where the program lives.** It belongs to the construct, which is what makes it
  survivable and shareable. Automation with no home is neither.
- **Being able to answer for itself.** M rests on Reactor knowing what is going on. Its
  chance of explaining anything drops sharply once the answer might be on a different ship.

Story T is the first thing to push back on this, and it does not push hard enough to move
it - the useful actions in a docking turn out to sit on the side that owns them, and the
two sides coordinate through the world they already share. If something later pushes harder,
this is the argument it has to beat, not a technical wall.

### Behaviour of our own, where the game makes something impossible

Open, and worth keeping open. A mod can attach behaviour to blocks that already exist, and
can add subtypes of its own, so Reactor is not limited to automating what the game already
provides - it can make an automatable thing possible at all.

**The cheap form is a setting on a stock block**, alongside the ones already there. No
model, no icon, no build-menu entry, no balance argument, and it applies to every such block
everybody already owns. Both candidates below look like settings rather than blocks, which
makes this a far smaller proposition than "we ship blocks".

Two candidates have already surfaced, both from story T, and both fix flaws that will
otherwise be blamed on us:

- **Stockpile that fills what you meant.** A tank on stockpile pulls from everything it can
  reach, including other people's ships parked across the same station. A targeted version
  asks only from a **directly connected construct** - one hop rather than the whole conveyor
  graph - so a docked miner fills from the station and cannot reach the ship on the far side
  of it. Small rule, comprehensible, and it reads as obviously right once said.

  A setting on the tank, mutually exclusive with ordinary stockpile: a tank is doing one or
  the other, never both, so there is no combination to explain or to get wrong.
- **A connector that pushes onward instead of filling up.** Sorters fill the connector
  before anything reaches the docked ship, so pushing cargo across a dock barely works. The
  fix is a setting on the ordinary connector - alongside drain all and the rest - that makes
  it push its contents onward rather than hold them: storage on its own construct first, and
  across to the counterpart only when its own side has nowhere to take them.

  That leaves the control with the builder and with blocks that already exist. A one-way
  sorter on the near side closes off the return path, and the connector then has nowhere to
  push but across. Nothing needs to guess what the player meant.

  *Name undecided.* "Rejecting" describes what it does not do, which is a poor label for a
  toggle somebody has to find and understand.

**Sharing does not suffer.** Anyone running a shared system has Reactor, so a Reactor block
is always available where a third-party dependency would not be.

**What would have to be true before doing it.** A setting on an existing block is cheap; a
block of our own is content rather than code - model, icon, definitions, balance, and an
argument with everybody who thinks it is overpowered. Either way the bar is that a story is
*blocked* by a game mechanic rather than merely inconvenienced by one, that it does one
thing the game's own version cannot, and that it is not a way of avoiding a design problem
we should be solving in the language.

**And they may not belong to Reactor at all.** Both are fixes to the game rather than
automation, and they are useful to people who have no interest in Reactor whatsoever. Making
them one or two small mods in their own right, which Reactor depends on, costs little and
buys several things: they stand or fall on their own merit rather than as riders on
something larger, somebody who only wants a connector that pushes onward can have exactly
that, and Reactor's own scope stays what it says on the tin.

It costs nothing to sharing either. Anyone running Reactor has the dependency, so a system
that relies on one of these behaviours works for whoever receives it.

Nothing is decided. It is written down because "we could add behaviour to the game" is an
option that would otherwise not occur to somebody reasoning only about automation, and
because the packaging question is easier to answer now than after the code exists.

### What the game actually gives us

Answered rather than assumed. Most of these were hedged in requirements until they were
asked out loud.

- **Events vary by event.** Some things the game will tell us about; others must be sampled
  and the change worked out. This is per event, not a single answer, so the runtime has to
  do both and the vocabulary has to know which is which.
- **Levels are readable directly** - vent oxygen, tank fill, battery charge, as values.
- **A block's appearance is readable**, to varying degree but mostly. Reading back a colour
  and an on-or-off is available.
- **A person can be told**, in several ways: chat, notifications, LCDs. See the endemic
  preference below.
- **An LCD will take almost anything.** The limit on screens is design and cost, not
  capability.
- **Docking is observable.**
- **Storage is the save file**, or a separate data file. The save for everything except
  exports and imports, which is what a separate file is for.
- **A block's position is available**, and a block being placed can be noticed. A destroyed
  block is destroyed, which needs no explaining.
- **Being attacked** is probably an event, and failing that there are damage values to
  sample.
- **Constructs merging and splitting is entirely our problem.** The game does nothing for
  us. We detect it, and we decide what it means.

**And a preference that shapes several of these.** Where Reactor tells somebody something,
prefer doing it *in the world* - a light, a sound, a screen on a wall - over the game's own
interface. Endemic where possible. A warning that arrives as a chat line is a message from a
mod; a warning that arrives as a red light in the corridor is the base telling you something,
and the second is the game people came to play. Chat and notifications remain available and
are sometimes right, particularly for somebody who is nowhere near the base at all.

### System

A named set of sequences and states that belong together: an airlock, a hangar, a
defence system. The unit of grouping, of naming, of export and of configuration.

The word is the player's own - people already say "my docking system" - and it is
worth being strict about using it only in that sense from here on.

It exists for three separate reasons, each with a story behind it, and any two of them
would have justified it:

- it is the scope that makes a named state unambiguous, so two airlocks can both have
  a "vacuum mode" without the player inventing naming discipline (I);
- it is what keeps the sequence list navigable once a ship has forty of them (J);
- it is the unit of sharing - what is exported, and what is pointed at your own blocks
  on import (K).

A fourth was struck out: that a system is the scope a call resolves in, so a sequence
can be called "cycle out" rather than "front airlock cycle out". Nobody has ever wished
for a namespace. It is a consequence of the three above, and was doing illegitimate
work as a justification for them.

Every sequence belongs to exactly one system. Creating one must never stand between
somebody and their first sequence: one is provided, and named after the fact.

### Sequence

A trigger and an ordered list of steps. Optionally named; the name is what makes it
callable, and what makes a diagnosis readable.

### Trigger

What starts a sequence. Always a moment, never a situation.

- **When** - a block event. Several blocks means *any* of them by default, with *all*
  available where the sentence genuinely differs: "when tank A **and** tank B are
  empty" fires as the last one empties, not the first.
- **Every** - a recurring interval. This is what replaces the loop.
- **Called** - started by name from elsewhere, which is what lets two buttons run the
  same sequence.

Intervals come from the game's frame counter and are never counted by Reactor. Space
Engineers offers update rates of 1, 10 and 100 only, and the slower two do not land on
exact multiples, so anything self-counted drifts with server load.

### Step

One numbered line: a verb and the blocks it acts on.

**A step waits for its own action to take effect.** "Close the inner door" holds until
the door is actually shut. This is the single most important decision in the language:
a numbered list already means "and then" to anyone who has followed a recipe, and a
duration is a programmer's substitute for a completion signal that produces airlocks
which vent the ship the day a door sticks.

It has a consequence worth naming, because it removes a whole category of thing:
**"make sure X" and "do X" become the same sentence.** An already-shut door completes
instantly, so ordinary procedures need no conditionals at all.

### Gate

An optional condition on a step, for the cases blocking alone does not cover.

- **once** - hold here until a situation is true. Needed only for something this
  sequence did not itself cause: "once the tank is full".
- **only if** - a guard. If it does not hold, the step is passed over and the sequence
  carries on, which is a different sentence from "hold until it is safe". Being passed
  over is recorded, because a step that quietly does nothing is indistinguishable from
  one that worked.
- **after** - a duration. Still needed for things with no completion signal, such as
  flashing a beacon. Offered second, never first.

### Events and states

Structurally separate, and this is deliberate. An **event** is an edge and can only
start a sequence. A **state** is a level and can only gate or guard one. Both are read
off the same signals, but keeping them apart in the model makes the "when it opens"
versus "while it is open" confusion impossible to express rather than merely
discouraged.

Several blocks in a trigger means *any*; several in a condition means *all*. That
asymmetry is not an inconsistency, it is how the two sentences read aloud.

## Deliberately not in it

- **An interlock primitive.** "Both doors must never be open" is two ordinary
  sequences, and that version is stronger: locking is a real action, so it also stops
  a player opening the door by hand. The same test kills "docking mode", "power
  management" and every other packaged behaviour.
- **Precedence between sequences.** "Which one wins" is a hidden global rule that changes
  what a sequence does without appearing anywhere in it, and which one should win nearly
  always depends on the situation rather than on a fixed ranking - which is a guard, and
  guards already exist. Conflicts are detected and shown (R43); resolving them is authored.
  Same reasoning as the interlock.
- **Named block groups.** Blocks are picked inline and belong to the sequence that
  picked them. Nothing to maintain, and nothing to fall out of date.
- **Loops, counters, nesting, and variables as a general facility.** Covered above.

## Open decisions

These are what designing proper now means. They are ordered by how much else waits on
them, and each names the stories that want it. A question with no story behind it does
not belong on this list.

### 1. A condition on the trigger

*Wanted by: A, L.*

Gates live on steps and are evaluated when their step runs, so a guard meaning "we are
deploying" is asking about a world that step 1 has already changed. The language cannot
currently decide, once and up front, which of two procedures this is.

A condition on the trigger settles it: *when the button is pressed, while the arm is
stowed*. Two sequences, opposite guards, decided before anything moves.

Two things the stories settled rather than argument. The sentence simply continues with
*and* - "when the button is pressed and the arm is stowed" - and no new word is
introduced. *While* was proposed for the level half and rejected: programmers are not the
primary audience but they are in it, and *while* means a loop to all of them. And it has
to be reachable
both while authoring and afterwards, because A and L are the same construct approached
from opposite ends: if the retrofit route is second class, half the people who want it
never find it.

Still open: what happens in the resting state where neither guard holds. Not academic -
the arm is halfway out for most of the time it is moving.

### 2. Quantities

*Wanted by: B, F, G, and the middle rung of O.*

**The model half of this is now built.** A reading is an `EntityValue` - a flag, a number
or a text - and a state is a `Relation` against one: is, is not, above, at least, below, at
most. A host declares what kind each property gives, so the editor can offer the comparisons
that mean something and no others. The simulated tank publishes its level as a number and
"is low" is `Below 0.25` declared on the kind, where it used to decide it was low and hand
core a bare bool, throwing the number away before anything could ask a different question.

*It got there by way of a mistake worth remembering.* The model had boolean signals as its
primitive and treated a number as something it might support one day. That is backwards: a
flag is a property whose readings happen to be true and false, no more special than a fill
level or the name somebody gave a block. Every attempt to keep "state" and "property" as
separate ideas ran into the fact that they are the same idea.

What is **not** built is the authoring. Only a host can declare a state today; nobody using
Reactor can write "below a fifth" for themselves, which is what F and G actually ask for.

The stories split this cleanly in two, and the split is the decision:

- **against a chosen number** - below a fifth, above half. Satisfies F and G, and a
  progress bar. *The runtime can do this now; only the authoring is missing.*
- **against another reading** - match the far side. Only B wants this, and B cannot be
  satisfied without it, because neither operand is ever a constant: not the outside,
  which is whatever the planet is, and not the inside, which is whatever damage has
  left it. *Not modelled: a relation compares a reading to a fixed operand, never to a
  second reading.*

The first is much the cheaper and does most of the work. Building only the first is a
defensible answer, so long as it is a decision rather than an accident.

B also needs the far side to have something that can report it - a vent in the next
room - which grows the block list of an airlock by one. What happens when nobody put
one there is unresolved.

**Open inside this: how often a condition is evaluated.** Space Engineers offers update
rates of 1, 10 and 100, and the choice is a real trade. Checking rarely is cheap and
lets a level overshoot before anybody notices; checking every tick is accurate and is
multiplied by every waiting sequence on the server. It is not obvious that one answer
fits an airlock door and a hydrogen tank equally, and it is not obvious that the player
should be asked either, since "how often should this be checked" is exactly the sort of
question this language exists to remove.

Unresolved. It was briefly written up as a requirement saying the player is never asked;
that was an opinion, not a specification, and it was withdrawn.

### 3. Values on actions, and putting them back

*Wanted by: D.*

A step is a verb plus its targets, with nowhere to put a colour, a percentage or a
name. That is the small half.

The large half is that D wants them *restored*, and what gets restored is how a block
is rather than one property of it - a light that was off is normal when it is off
again. Two dozen hand-tuned lights is exactly the scale at which the player cannot be
asked to write the values down, so the language has to hold them: a step that remembers
how a set of blocks looks, and a step that puts them back.

The most likely candidate on this list to be judged not worth it.

### 4. Calling a sequence

*Wanted by: E. Needed by 6.*

Half-built: a sequence can be started by name, but no step can call one. Resolving
within the system keeps names short.

**Template, not duplicate.** This was being treated as an open fork; E's done-when
decides it, because "changing the shared procedure changes all three" is not something
duplication can satisfy. That brings the hardest idea in the language with it - changing
this changes that - and the cost belongs to E rather than to our preference.

**And E is larger than calling.** A system used more than once is a template with
instances, its roles are what an instance fills in, and roles are wanted by four stories
independently. Two questions inside it are not settled: where a template lives when it
came from somebody else and is used on several constructs, and whether an instance's
roles are filled once when it is placed or supplied by whoever calls into it. The first
is closer to how blueprints already work and is probably right; the second is closer to
passing arguments and is probably too programmer-shaped, but neither is decided.

### 5. Named state

*Wanted by: I, C, D.*

Two different features that must not share a name in the UI:

- **derived** - "vacuum mode" *means* the outer door is unlocked and the vent is
  depressurised, recomputed whenever asked. Cannot fall out of step with the world.
- **remembered** - a fact stored because nothing in the world records it. Can fall out
  of step with the world, and "it thinks it is in vacuum mode but it is not" is exactly
  the failure this audience cannot diagnose.

D changes the shape of the second. Remembering two dozen lights is remembered state at
a scale nobody would ever author by hand, so it cannot be only a thing players create
and name. Some of it has to be something the language holds on their behalf.

### 6. Stopping a sequence from outside

*Wanted by: C. Worth nothing before 4.*

Nothing can stop a sequence but the sequence.

C shows this is two wants rather than one - halting, and settling somewhere safe - and
that the way back is its own procedure, not the forward steps reversed. But the second
half needs no new concept: a button that stops the preparation and then calls "put the
hangar back" is an ordinary sequence. So the only primitive here is *stop*, and building
it before calling exists would deliver the useless half.

### 7. Telling a person something

*Wanted by: G, O.*

Reactor can operate blocks and nothing else. It has never been able to tell anybody
anything, so "warn me" can only be built today as "switch on a lamp somebody has to be
looking at".

One capability with two faces: G interrupts, O displays. Building one without the other
will read as arbitrary. O then ladders - a heading and live text, then a progress bar
which waits on 2, then a graph which needs **history**, samples kept over time, and
would be the first thing Reactor stores that is not a fact about right now.

O also raises a structural question rather than a feature one. A screen is a standing
arrangement that is always true, not a series of steps, and driving it from steps is the
programmer's shape of the problem. It may want to be a second kind of thing alongside
sequences.

### 8. References that survive a repair

*Wanted by: P, K.*

An entity is referenced by id. A rebuilt block is a new entity with a new id and usually
a default name, so every reference to the destroyed one is dead and each has to be
repaired separately. A door used by six steps across two sequences is six repairs.

Two things are wanted here and they are not the same thing.

**Repair in one place.** A block reference becomes a named role on the system - "the
inner door" - which every sequence that needs it points at, so re-pointing happens
once. This is exactly the mechanism K needs for sharing, arrived at from a completely
unrelated direction, and two independent stories asking for one structure is the
strongest signal the set has produced. It also promotes roles out of "part of export,
defer them together with it" and into their own right.

**References that cannot be bound at all when written.** T asks for a station sequence
that acts on whichever ship is docked, which is not the same problem wearing a hat: the
blocks do not exist yet, and are different ones each time. That forces a reference
resolved when the sequence runs, by role and kind, against whatever is currently
attached. It subsumes the other two - a role that can be filled at run time can
certainly be re-pointed by hand - so if this is built, it is built first and the rest
falls out.

**Re-binding without being asked** is the speculative half: remember the name alongside
the id, and when the id is gone and a block of the right kind carrying that exact name
appears, adopt it. It covers the common case, because somebody who rebuilds a door
usually renames it back to what it was. It has to stay conservative - silently adopting
the wrong block is worse than asking - and it is worth nothing without the first half.

**Open inside this: what watching actually looks like.** R27 and R28 say what must be
knowable; they say nothing about where somebody stands while knowing it. The useful version
is probably not a step list read in isolation - it is watching the doors themselves with
something beside you saying what Reactor thinks is going on, because the failure being
diagnosed is usually a disagreement between those two. That implies the editor is legible
while a person is looking at the machine, which is a different demand from being legible
while they are sat in a terminal.

Undecided, and a question about the shape of the editor rather than about the language.

### 9. Where an editing history lives

*Wanted by: nothing in the stories - this one is ours.*

Every change to a sequence is a reversible operation and the undo they make is per sequence and
in memory. Persisting it was asked for at the time and is the next thing to build, and the
mechanics are the small half: the operations are already compact records shaped to be sendable,
so writing them out and putting them on a wire is one job.

**Where it goes is the question.** A history is a fact about somebody's editing session, not
about the ship - a system that is exported, shared or saved should carry what it is and never
how its author got there, which is why the history is held on the editor rather than on the
sequence. But a mod has nowhere else that survives a session: the world save is the only
durable place, and putting a session there is exactly the mixing that reasoning warns against.

Multiplayer sharpens it. "Somebody's session" is a different person on each client, so one
history in the save is either wrong for everybody or has to be kept per player - and undoing
somebody else's edit is a question nobody has asked yet, let alone answered.

*Decide before writing any of it. The cost of being wrong is a save format that has to change.*

### 10. Failure

*Wanted by: M. Load-bearing on H.*

A step that will never complete, a sequence stuck forever, and the question people
actually ask - which is not "what went wrong" but "why didn't it happen". Last on
purpose: failure cannot be designed until the language can say what was supposed to
happen.

A system is the natural unit for the answer. "Front Airlock is stuck on step 3" is a
sentence about a thing the player built and named.

**This is one window, not four features.** M asks why nothing happened, Q asks whether
it is already running, V asks which step it is on, and R asks which of two sequences is
winning. They are the same view of a running sequence seen from four sides, and
building any one of them on its own would be building a quarter of it badly.

R also deserves noting as a bill we chose to run up. Dropping the interlock primitive
was right, and it guarantees that several sequences will act on one block by design, so
conflict is a designed-in condition rather than a fault. The honest answer is probably
to show the disagreement rather than resolve it quietly.

H is the constraint nobody would guess. Reactor can only explain what it can see, so
every case somebody sets up in the game's own terminal instead is a hole in the
explanation. Losing the easy cases is how the hard ones become unanswerable.

## Where the prototype stands

The editor works, and the abstraction has held up under a lot of pushing. Core hands a
target a small set of domain-shaped components - a `StartComponent`, a `StatusListComponent`,
a `PickComponent` of stages, a `SequenceComponent` - and decides nothing about layout. Even
the noun is the host's: core says *entity* and Space Engineers says *block*, because the two
are not the same claim and only one of them belongs to a game.

Of the ten questions above, quantities is now half answered - see decision 2 - and none of
the rest contradicts what is built. Reading a block's properties well enough to put them back
is the one still reaching past the language into the world model, so it is the one to design
most carefully before writing any of it.

**A system is a template now, not a label.** Its roles are derived from what its sequences
act on, one of it is a substitution saying which of your own things stand in for the author's,
and the runtime runs each sequence once per one of them. That was the centre of gravity of the
whole project - four unrelated stories wanted it - and the half that remains is a screen to
fill the roles in from, not a model question.

*The editor's own interface has been worked over hard, and `docs/Editor.md` is where that
lives rather than here. The load-bearing conclusions: narrowing runs one way only, one trip
through a picker is one edit, a system cannot be rewired while it runs, and a running
sequence is shown as a thing being watched rather than as an editor with its hands tied.*
