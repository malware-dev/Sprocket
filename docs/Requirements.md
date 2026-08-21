# Requirements

What Reactor must do, derived from [UserStories.md](UserStories.md). Internal.

A story is a want. A requirement is what the system has to do to serve it, stated so
that it can be argued with, tested, and built from. Tests prove requirements; they do
not replace them.

A requirement is a thing Reactor needs, together with how it needs to work - "we need a
stop statement, and this is what stopping does". It is not split into the smallest
assertions that can be independently tested; that is what tests are for. A requirement
should be something somebody can be asked to build.

Two things it is not, both of which have been mistaken for one already. It is not an
**opinion** - "the player should never be asked how often to check" is a position, and
positions get argued, not built. And it is not a **permission** - "vanilla being able to
do it is no reason we should not" obliges nobody to do anything; it only removes an
objection. Both belong in Design.md.

There are two kinds, and they are sourced differently.

**User requirements** come from a story. They are worked out one story at a time and
agreed before they are written here; one story usually yields several. Each names the
story it came from, and a user requirement with no story behind it is visibly wrong.

**Technical requirements** come from engineering judgement - performance, the mod
whitelist, what a target may and may not be asked to do. There is no story behind them
and none is needed. What gets recorded instead is the reasoning, because there is no
want to point back at when somebody asks in a year why this is here.

Numbers are permanent from the point anything references them. The set was re-cut once,
early, when it became clear requirements had been split far too finely; nothing pointed
at them yet, so they were renumbered rather than carrying holes from a definitional
mistake. A requirement dropped from here on keeps its number, and the number is not
reused.

Tickets are made from requirements, not from stories, and only once the design
decisions behind them have been made. A decision is not a ticket - it is what has to
have happened before the work can be described. A requirement whose decision is still
open simply has no tickets yet.

A requirement is sourced by the story we were reviewing when it surfaced, even where it
turns out to serve others as well. If a later story wants the same thing, it is added
as a second source rather than the requirement being moved.

User requirements are numbered R, technical ones T. Both sequences are permanent.

---

# User requirements

## From story A - one switch, out and back

### R1 - a sequence decides once, before it acts

Whether a sequence runs must be settled at the moment its trigger fires, before any step
has acted. A sequence that has begun must not change its mind because the world changed,
least of all because of what it did itself.

*Waits on Design.md decision 1. No tickets until that is settled.*

### R2 - how a step completes

A step is not complete until the thing it asked for has actually happened, and the
following step does not begin until it is.

A step whose action would change nothing completes immediately rather than waiting or
failing. "Make sure the arm is stowed" and "stow the arm" are the same sentence, which is
what removes conditionals from ordinary procedures.

*Already true, and untested. The central decision of the language, and the one most
likely to be quietly broken by an optimisation.*

### R3 - a trigger that starts nothing is not silent

When an event fires and every sequence listening to it declines to run, the player must
be able to find out that it happened and why nothing followed. A press that could not do
anything must not look identical to a press that worked.

This is deliberately about any trigger rather than about controls a person operates.
Reactor cannot tell a person from a timer - a button press is a button triggering - so
the requirement cannot draw a line the model does not have.

Note "able to find out" rather than "is told". A sensor that trips every few seconds and
starts nothing cannot push a notice at anybody, so this is a question the player asks,
not an announcement Reactor makes. That is what keeps the general form affordable.

*Nothing exists. Waits on a decision about what the player is shown, and on ApiUnknowns 1
- whether Reactor can know a trigger fired when nothing was listening.*

## From story B - match the side I'm about to open

### R4 - quantities

Where a block reports a level, that level must be available to the language as a value.
Today a vent's "62%" exists only as text for the player to read, so nothing can be said
about it.

A condition must be able to compare such a value against a level the player chose, and
against another block's reading, with neither side needing to be a fixed number. The
second of those is what B needs and what nothing else in the set does.

**The comparisons themselves must be sayable.** More than, at least, less than, at most,
the same as, not the same as. Not a single "is it low" that we chose the meaning of - the
player says where the line is and which side of it they mean.

They are said in words rather than symbols, for the reason the false-friend rule gives:
*is at least* reads correctly to somebody who has never seen a comparison operator, and
correctly to somebody who has seen thousands.

*Open: asking whether a reading is exactly a value.* A tank that is precisely half full
is a thing that essentially never happens, so an equals test on a continuously varying
reading is a trap that reads perfectly and never fires. It either needs a tolerance, or
needs not to be offered for readings at all. Not settled.

Waiting on such a condition needs no new kind of step. *Once* is already "hold here until
this is true", and already a sibling of *after so many ticks* - B only widens what the
situation it waits on may be about.

*Waits on Design.md decision 2. The game exposes levels as values directly, so nothing here
is blocked by the game.*

## From story C - call it off, and put it back

### R5 - a stop statement

Reactor needs a way to stop a running sequence, available to the player's own automation
rather than only to the editor, so that stopping and putting right can be one authored
thing.

Stopping ceases the sequence at once. Whatever it set moving is halted where the game
allows a thing to be halted - pistons and rotors stop where they are. Where a motion
cannot be interrupted part way it finishes, and Reactor does not pretend otherwise.

Nothing is put back, nothing is reversed, and nothing else about the world is touched.
Reactor must not attempt to restore, reverse, or otherwise infer what "back" means.

*Why that last part is written down rather than simply absent.* Reversal cannot be done
honestly. Not every action has an inverse; the way back is not the way out reversed, and
C's own hangar is the example - re-pressurising is not un-depressurising and the doors go
in a different order; other sequences may have changed things in between; and the state
at the start was itself mid-something. Worst of all, the things worth restoring are
mostly things that move, and moving them back is motion through a world that has changed:
a piston returning to where it was crushes whatever has since parked there.

Putting things right is an ordinary sequence, ordered and intended by the person who
knows what order it goes in. If they get it wrong, that is theirs to get wrong, which is
the only honest place for it to sit.

*Waits on Design.md decision 6, which waits on 4.*

### R6 - a sequence that was stopped is not mistaken for one that finished

Where a sequence's state is shown, having been stopped part way must be distinguishable
from having run to the end.

*Added beyond what C strictly asks for. A stop deliberately leaves the world halfway, so
"it stopped" and "it finished" describe very different hangars. Serves M as well.*

## From story D - everything red, then exactly as it was

### R7 - a value on an action

An action must be able to carry a value: make these lights red, set this to that. A step
today is a verb and its targets, with nowhere to put a colour, a level or a name.

The value belongs to the step and not to each block: one step, one value, however many
blocks it acts on. D's two dozen lights are told once, not listed one at a time.

**A value is not always a number or a colour.** T needs a sorter's filter list set - pull
ore now, components later, everything after that - which is a list of item kinds. Whatever
a block's settings actually are, the ones worth changing because something happened are the
ones this has to carry.

That is inside the boundary Design.md draws rather than a widening of it: configuring a
sorter once is the game's business, changing its filter because a ship docked is ours.

*Waits on Design.md decision 3.*

### R8 - capture and restore

Reactor needs a step that remembers how a set of blocks is, and a step that puts them
back as they were. The player says when each happens. Nothing is captured on its own and
nothing is restored on its own.

What is remembered is how the block *is*, not one property chosen in advance. A light
that was deliberately off is normal when it is off again.

**What may be captured is open.** The safe subset is obvious - colour, brightness, on or
off, and their like, where putting it back is a value changing and nothing else. The
question is whether it stops there.

Things that travel are the hard case: doors, pistons, rotors, connectors. Restoring one
of those is motion through a world that is not the world it was captured in, and a
snapshot has no order in it, so a bulk restore moves everything at once with no idea
which order mattered. A piston returning to where it was crushes what has since parked
there.

That is an argument for care, not for a prohibition. If the player says they want the
piston's position recorded, they have asked, and it is not obvious we should refuse.
Whether the answer is a safe default with an opt-in, an ordering the player controls, or
something else, depends entirely on how this ends up being built.

*Priority: low.* Likely a nice-to-have until something proves otherwise. Nothing else in
the set depends on it.

*A block's appearance is readable, to varying degree but mostly, so D is servable. What is
not readable for a given kind of block simply cannot be captured for it, and saying so is
better than pretending otherwise.*

## From story E - three doors, one procedure

E turns out to be the largest thing in the set. It is not "fix it in one place", it is
the system-as-template concept: reuse, configuration, and a second kind of person who
uses systems without ever having made one.

### R9 - a step can call a sequence

A step must be able to run another sequence by name and, since steps wait, not continue
until it has finished. Half of this exists: a sequence can already be *started by name*,
but nothing can do the starting except the editor's run-by-hand button.

R5 needs this too - stop the preparation, then call *put the hangar back*.

*Waits on Design.md decision 4.*

### R10 - a system is a template, used as many times as wanted

A system must be usable more than once on the same construct, each use pointed at its own
blocks. Changing the system changes every use of it, which is E's done-when and is what
duplication cannot give.

What a system needs from the construct is a named list of **roles** - the inner door, the
outer door, the vent, and values as well as blocks, such as how full counts as full. A
role may be one block, several, or a value. Configuring a use of a system is filling its
roles, and nothing else about it is exposed.

Roles are wanted by four unrelated stories now: E for reuse, K for sharing, P for repair
after damage, T for blocks that are not there when the sequence is written. That is the
strongest signal in the set, and it means roles are not part of export - export is one
thing that uses them.

*Waits on Design.md decision 4 and 8. The largest single piece of work identified so
far.*

### R11 - using a system requires no authoring skill

Somebody who has never built a sequence must be able to take a system made by somebody
else, put it on their construct, fill in its roles and have it work. Using must not
require understanding how it was made, and must not be a lesser path than authoring.

*Why this is separate from R10.* R10 is the mechanism; this is the audience. A template
system that only its author can configure would satisfy R10 completely and be worth very
little - most people who benefit from Reactor will never write a sequence, exactly as
most people who use a blueprint never designed one.

*Serves K as well. Waits on nothing technical; it is a bar on whatever R10 becomes.*

### R12 - setup is derived, and may be improved

Reactor must derive a workable setup from the roles a system uses, with no effort from
its author. A system whose author did nothing beyond building it must still be
configurable by somebody who has never seen it.

The author may then improve on that: name a role in their own words, put the questions in
an order that makes sense, add an explanation where one helps, note the thing that always
catches people out. That is an option and never a prerequisite.

**A system also carries its own name and a short description.** What this thing is, in the
author's words. To the person who built it that is a label; to everybody who receives it
later it is the only explanation there will ever be of what they have just loaded, and it
travels with the system (R22).

**The derived version has to be good enough to use, not a placeholder.** Most systems
will never be polished by anybody, so the unauthored path is the one most people meet.
If it only works when somebody has been diligent, R11 does not hold.

**The author supplies words, not layout.** They say what to ask and how to explain it;
Reactor decides how that is arranged and the target decides how it is drawn - the same
division as everywhere else. A system author is not a programmer either, and improving a
setup screen must not become laying one out.

*Open: how far the authored version goes.* Grouping, questions that only appear depending
on an earlier answer, checking that an answer makes sense - each is a step towards a form
designer, and none of them has a story behind it yet.

*Waits on R10. Serves E, K, and R11.*

## From story F - keep the hydrogen above half

### R13 - a step may be guarded or made to wait

A step may carry a condition, in one of three senses.

**Only if** is a guard. If the situation does not hold when the step is reached, the step
is passed over and the sequence carries on. Being passed over must be recorded, because a
step that quietly did nothing is otherwise indistinguishable from one that worked.

**Once** is a wait. The sequence holds at that step until the situation becomes true.
This is for waiting on something the sequence did not itself cause - a tank filling, a
pressure equalising - since a step already waits for its own action by R2.

**After** is a wait of a set length, for things that have no completion signal at all.
Offered second, never first: a duration is what people reach for when they cannot express
what they are really waiting for, and it produces automation that works until the day
something is slow.

**When each is evaluated matters.** A gate is evaluated when its step is reached, not
when the sequence started. That is precisely the distinction R1 draws at the other end:
whether the sequence runs at all is settled up front, while whether any given step runs
is settled at that step, with the world as it is by then.

Regulation needs nothing beyond this. *Every hundred ticks, only if the tank is below
half, turn the generator on* is a rule that holds continuously, built from a repeating
trigger and a guard, with no loop, no counter and no new construct. F needs only the
guard to be able to speak about a level, which is R4.

*Already true in the runtime, and never written down until now, which is the argument for
doing this pass at all.*

### R14 - conditions on a trigger

A trigger may carry conditions as well as an event, and reads as one sentence: *when the
button is pressed and the arm is stowed*. More than one condition can be given, and it
must be possible to say both "all of these" and "any of these".

No new word is introduced for this. The sentence continues with *and*, which means what
it appears to mean.

**Deliberately not "while".** It was proposed and rejected. Programmers are not the
primary audience but they are certainly in it, and *while* means a loop to every one of
them - a word that misleads the experienced reader while buying the beginner nothing.

**The event stays the event.** The first part of the sentence is an edge and the rest are
levels, and only the event can start anything. Joining them with *and* is a wording, not
a merging: the editor offers events in the first place and situations in the others, and
the model keeps them apart as it does now.

*The risk this carries* is that "when the button is pressed and the arm is stowed" can be
misread as firing when the arm becomes stowed. The structure prevents building it, but
nothing prevents misreading it, and that is a thing to watch for the first time somebody
is put in front of it.

*Deeper expressions - not, either-or, anything nested - have no story behind them and are
not specified.*

*Waits on Design.md decision 1. Serves A and L.*

### R15 - named state, holding a named value

Reactor needs named state that a step can set and a condition can read: *the arm* is
*stowed*.

**A named value, not a flag.** The values are named by whoever made the system - stowed,
going out, deployed, coming in - and a state holds exactly one of them. Two booleans can
be both true or both false and mean nothing; a state with named values cannot be in a
situation nobody named.

That is what makes the middle addressable. A rig is halfway out for most of the time it
is moving, and with only two values that period has no name and the button does nothing
in it. With four, *when the button is pressed and the arm is going out* is a sentence the
player can write.

It also survives a stop cleanly. Interrupt the deploy sequence and the state stays *going
out*, which is exactly what happened. Nothing needs tidying for it to stay honest.

**What it costs.** A remembered state is only as true as the sequences that maintain it.
Somebody who drags the piston by hand leaves Reactor believing *stowed*, and this is
story I's known hazard arriving early: a state that disagrees with the world is the one
failure this audience cannot diagnose.

**Open: how this relates to derived state.** The alternative is working it out from the
world - *the pistons are retracted* - which cannot lie, but needs the game to report
position and has no way to name *going out*, because "in between" is not something the
world reports. Current reading is that both exist and neither replaces the other: derived
where the world can tell you, remembered where it cannot or where the middle matters. Not
settled.

*Scope: a state belongs to a system (I), so two airlocks each have their own. Read by
conditions on triggers (R14) and by gates (R13). Serves A and I. Waits on Design.md
decision 5.*

## From story G - tell me before it runs out

### R16 - an event on a value crossing a line

An event must be able to be a value crossing a threshold: *when the tank drops under a
fifth*. It fires at the crossing, once, and not again while the level sits below.

**Direction is part of the event.** Dropping under a fifth and rising above a fifth are
two different things that happen, and both are wanted - the first warns, the second says
it recovered.

The comparison itself is R4's: the same words, the same choice of where the line is, the
same open question about testing a reading for exact equality - which matters more here,
since an event that never fires is harder to notice than a condition that is never true.

This is distinct from R4. R4 lets a *condition* speak about a level, which is a question
asked when something else already happened. This is the level itself being the thing that
happened.

*Open: a level sitting on the line.* A tank hovering at exactly a fifth will cross it
repeatedly, and something has to stop that becoming a warning every few seconds. How much
allowance, and whether the player ever sees that number, is a question for measurement
rather than argument.

*Open, and worse: whether a crossing can be missed entirely.* On a coarse update tier a
level may drop below and recover between two of our updates, and the event never
happened as far as Reactor is concerned - but only where the event is one we have to watch
for rather than one the game tells us about, and only where it changes and changes back
inside a single update. See ApiUnknowns 1 and T3. This is the first
requirement where being told rather than looking would change what can honestly be
promised.

*Waits on Design.md decision 2. Levels are readable, so the question is how the event is
detected rather than whether the value exists.*

### R17 - telling a person

Reactor must be able to tell a person something. Today it can only operate blocks, so a
warning can only be built as switching on a lamp somebody has to be looking at, which is
a hope rather than a warning.

It must reach somebody who is not in the room with the tank. That is the whole of G's
*done when* and the part no arrangement of blocks can satisfy.

**Open: who is told.** A message can be aimed at one person, everyone, or anyone nearby, so
this is a choice rather than a limit. It is still not obvious whose warning it is - whoever
owns the construct, whoever is aboard, whoever set the sequence up - and no story answers it.
Being able to pick makes the question sharper rather than easier.

**In the world before the interface.** Several ways exist - chat, notifications, screens -
and the preference is to tell somebody through the world where that will reach them: a light
in the corridor, a sound, a panel by the door. A warning arriving as a chat line is a message
from a mod; a red light in the corridor is the base telling you something, and the second is
the game people came to play. The game's own interface stays available and is sometimes
right, particularly for somebody nowhere near the base.

**Chat is for debugging, and only when asked for.** A running commentary in chat is genuinely
useful while somebody works out why a sequence misbehaves - the cheapest possible form of
watching. It is never on by default: a commentary nobody asked for is noise, and noise is
what gets Reactor switched off rather than the commentary.

A chat message can be shown to one client alone, so it goes to the person who asked and to
nobody else. That is a capability rather than a courtesy, and it means the debugging channel
costs bystanders nothing.

This is the interrupting half of one capability. The ambient half is a screen that always
shows how things are, which is story O and is not this requirement.

*Nothing about this is blocked by the game.*

## From story I - two airlocks that don't confuse each other

Mostly served before it was reached. R15 gives named state and scopes it to a system;
R10 makes a system usable as many times as wanted, each use pointed at its own blocks.
Two airlocks are two uses of one system, and the naming problem I complains about never
arises. One thing was missing.

### R20 - each use of a system is named

Every use of a system carries a name the player gave it - the front airlock, the hangar
airlock - and everything inside it is spoken of in those terms.

Without it, "vacuum mode" is ambiguous the moment there are two of them, which is the
whole of I. It is also what lets J find the hangar's sequences among forty, and what lets
M say something a person can act on: *the front airlock is stuck on step 3* is a sentence
about a thing they built and named, where *sequence 14 is stuck* is not.

*Serves I, J, M. Waits on R10.*

### Not taken from I: derived state

I could equally be served by working the state out from the world - "vacuum mode" meaning
the outer door is unlocked and the vent is depressurised, recomputed whenever asked,
incapable of disagreeing with reality. R15 records that fork and does not settle it.

It stays unsettled and unspecified, because no story asks for it. I is served by the
remembered form, and every other story that touches state is served by it too. The
argument for the derived form is that a remembered state can lie, and nobody in this set
has yet said that it did.

*Watch for it.* "It thought the airlock was in vacuum mode and it wasn't" is a real
experience and would be the story that demands the derived form. It has not been heard,
and inventing it to justify a feature we already fancy is exactly what the rule is for.

## From story J - where did everything go?

### R21 - every sequence lives in a system, and there is always one to live in

Every sequence belongs to exactly one system, and sequences are found through the system
they belong to rather than in one list of everything. That is what makes J's forty
sequences navigable: the hangar's are wherever the hangar is, and the other thirty-six
are not in the way.

**Creating a system must never stand between somebody and their first sequence.** One
always exists to put things in, and naming it is something that happens afterwards if at
all. Somebody who wants a door to open gets a door that opens, and is told where it
landed - never an empty container to fill in first.

*Why that second half is part of the requirement rather than a nicety.* The whole
authoring design is verb-first: the root offers "when something happens", not "make a
project". A container that has to exist before anything can be built reverses that, and
the person it costs most is the one who has never done this before.

*Serves J, and I and K through them. Waits on R10 and R20.*

## From story K - give it to a friend

Much of K was served in advance: R10 makes a system a template with roles, R11 puts the
bar at somebody who has never authored anything, R12 makes its setup derived and
improvable. Two things are K's own.

### R22 - a system can be given away

A system must be able to leave the construct it was built on as a thing that can be
handed to somebody else, and be put on a different construct with different blocks and
different names.

What travels: the sequences, the states, the list of roles, and whatever the author wrote
to explain them. What does not travel: the blocks. A system arriving on a new ship knows
what it needs and nothing about what it used to have.

**Not being able to satisfy a required role is said plainly, and said early.** If the
receiving construct has nothing that could fill one - no air vent anywhere on it - the
person is told that before they start filling in the rest, not after it half works. A
system that cannot run must not look like one that does.

*The means of handing it over is deliberately unspecified.* Whatever the game makes
possible, and whatever we decide is pleasant, is a separate question from the system being
portable at all.

*Serves K. Waits on R10.*

### R23 - roles are worked out from what the system uses

The list of roles a system needs is derived from the system itself rather than maintained
by hand. If a sequence acts on a block, that block is something the system needs, and it
appears as a role without anybody having declared it.

**The same block used in five places is one role, asked once.** An airlock's inner door
appears in the cycle, in the interlock and in the recovery sequence, and the person
setting it up answers "which is the inner door" a single time.

*Why derived rather than declared.* A hand-maintained list is a second thing to keep in
step with the first, and it goes stale the moment somebody adds a step. Deriving it means
a system's needs cannot disagree with what it actually does.

**A role may be filled with more than one block**, and needs no new rule to read
correctly. A wide airlock's "inner door" filled with two doors gives *when any inner door
opens* and *once all inner doors are shut*, because several blocks in a trigger already
means any and several in a condition already means all. The asymmetry the language settled
long ago turns out to be exactly what a multi-block role needs.

A role that supplies a value needs no special treatment either. *Any of the far side's
vents is above a fifth* and *all of them are* both mean something, so a role can hold as
many blocks as the player has, whatever it is used for. What is needed is that the
condition says which it means, which is R25.

*This is also what P and T need, from the other side: a role that can be re-pointed after
damage, or filled by whatever ship is currently docked.*

*Serves K, E, P, T. Waits on R10.*

### R24 - roles may be optional

The author of a system may mark a role as optional. A use of the system that leaves it
empty is a working use, not a broken one.

That is what lets a system be more than one size. An airlock that supports vents, lights
and a warning horn can still be an airlock built from two doors and nothing else, by
somebody who does not have the rest or does not want it, and the parts they did fill in
simply work.

**Anything depending on an unfilled optional role is not part of that use.** The steps
that act on it, the sequences triggered by it, the conditions that ask about it: absent,
as though never written. Not skipped at runtime with a complaint - absent.

**And it must never be reported as a missing block.** This is where it goes badly wrong if
we are careless. Deliberately empty and destroyed by cannonfire look identical from
underneath and mean opposite things, and only one of them is a problem. R2 says a lost
block is reported and never silently skipped; R24 says an unfilled optional role is
silently skipped and never reported. The system has to be able to tell them apart, or one
of the two requirements is a lie.

**Optionality is the author's, not derived.** Whether an airlock still makes sense without
its lights is a judgement only the person who built it can make. R23 derives what the
roles *are*; this is the one thing about them that has to be said.

The setup shows which roles are optional, so somebody skips them knowingly rather than
guessing whether they have broken it.

*Serves K, E, and R11 - a system that degrades gracefully is one a stranger can succeed
with. Waits on R10 and R23.*

### R25 - any or all, said rather than assumed

Where a condition is about several blocks, it must say whether it means any of them or all
of them, and the player chooses.

Today the answer is decided by position: several blocks in a trigger means any of them,
several in a condition means all of them. Those are the right defaults, because they are
how the two sentences read aloud - "when any of these doors opens", "once these doors are
shut" - but they are not always what is meant. *Any tank below a fifth* warns early and
*all tanks below a fifth* warns late, and both are things people want.

A trigger can already say it. A condition cannot, and that is the gap.

**This is what makes roles simple.** Without it, a role that supplies a reading would have
to be limited to a single block, because "is the far side above a fifth" has no answer
when the far side is two vents. With it, a role holds however many blocks the player has
and the condition says what that means - and no arity constraint has to exist anywhere.

*Serves K. Also removes a constraint that would otherwise have landed on R23. Waits on
nothing beyond conditions themselves.*

## From story L - add a condition to something I already built

### R26 - a sequence can be changed after it is built

Everything a sequence has can be changed once it exists: its trigger, its conditions, its
steps, the order they run in, and the blocks any of them act on. Changes are reached from
the sequence as it reads, rather than by retracing the questions that built it.

**Adding one clause must not cost the rest.** Somebody who wants "only when the hangar is
sealed" on a sequence written last week must not have to delete it and start again, which
is the difference between an editor and a wizard that only runs forwards.

*Not taken from L: that authoring and editing must be the same surface.* L is served by
editing being possible and easy to find. Whether it is literally the same flow as
authoring is a decision about the editor rather than something the system must do, and it
belongs in Design.md. Recorded because it was proposed and is worth deciding, not because
it is a requirement.

## From story M - nothing happened

M is short to say and large to serve. It is the only story that helps somebody whose
automation is already broken, and the audience has no fallback: a programmer reads the
code, and the person this is built for cannot. Four requirements, and they are the
diagnosis window that Q, R, V and R13 have each been asking for a piece of.

### R27 - a running sequence can be watched

While a sequence is running it must be possible to see which step it is on, what that step
is waiting for, and how long it has been waiting. Named in the player's terms - *waiting
for the inner door to shut* - not in ours.

This is the material every other answer is made from. Without it "why didn't it happen"
has nothing to draw on, and V's watch-it-work-first is the same view seen deliberately
rather than in anger.

*Serves M, V, Q.*

### R28 - a sequence can be walked

A sequence can be run deliberately, a step at a time, with each step's effect visible
before the next begins, without waiting for its trigger.

This is how somebody finds the step that fails, as opposed to being told that something
did. It is also V's whole story: watching it go through once, in a controlled way, before
trusting it with a door you are standing behind.

*Serves M, V.*

### R29 - why nothing happened is answerable afterwards

When a trigger fires and nothing follows, the reason must be recoverable after the moment
has passed: the condition that declined and the block it was about, the step that was
passed over, the fact that the sequence was already running.

**Afterwards is the hard part.** By the time somebody presses the button twice, walks
round to the panel and asks, the press is over and the world has moved on. This is the
first thing in the set that requires remembering something rather than reading the
present.

*Bounded deliberately.* One recent reason per sequence, overwritten, is worth almost as
much as a log and costs a fraction. T1 and T2 mean a per-sequence history across a
populated server is not affordable, and "we kept everything" is how this requirement turns
into the thing that gets Reactor uninstalled.

*Serves M, Q, R3, R13.*

### R30 - stuck is noticed without being asked

A step that has been waiting far longer than that step usually takes must be surfaced
without anybody going looking. A sequence waiting forever and a sequence working correctly
look identical from outside, and the difference is the entire story.

*Open: what "far longer" means.* A door takes a second or two and emptying a hangar takes
minutes, so a fixed number is wrong for one of them. Three candidates, none chosen:
learned from how long the step took on previous runs, stated by whoever authored the
system, or a single generous bound that only catches the truly hung. The first is the most
useful and the most expensive; the last is the cheapest and catches least.

*Serves M. Related to R6 - stopped, finished and stuck are three different endings and
must not look alike.*

## From story N - the alarm stops when I say so

N needs no new construct - the alarm is two short sequences, one that switches things on
and one that switches them off - but it does rest on a guarantee nothing had stated.

### R31 - what a sequence did, stays done

Nothing a sequence did is undone by the sequence finishing, by time passing, or by the
condition that started it becoming false again. There is no decay, no timeout and no
implicit reversal. Undoing is always another sequence.

This is what makes an alarm that runs until acknowledged possible with nothing running:
the siren is switched on and stays on, because nothing is going to turn it off but a
person. Take this away and holding any state needs something to keep holding it, which is
both more expensive and less honest.

It is R5's prohibition generalised. R5 says a stop does not put things back; this says
nothing puts things back. Neither is an omission - together they are the reason Reactor
never surprises somebody by tidying up after them.

*Already true, and never written down. Serves N, and quietly underpins A, C and D.*

*N's remaining question is not ours: whether the game reports being attacked as something
a block can trigger on. Probably an event; failing that, damage values can be sampled.*

## From story O - a screen that tells me how things are

### R32 - a screen is an arrangement, and holds no logic

A screen is a named arrangement of parts on a panel: a heading, lines, a bar showing how
full something is. The player says what parts there are and in what order; the target
decides how that is drawn, on an LCD or in a window.

**It reads nothing and decides nothing.** A screen holds no conditions, watches no blocks
and updates itself on no schedule. It is somewhere to put things.

*Why not self-updating.* A screen that fetched its own values would need an evaluator, a
schedule and a cost budget of its own - all three of which the sequence runtime already
has. That is a second system doing the first system's job, built because screens felt
special. They are not special; they are output.

*Serves O. An LCD will take almost anything, so what a screen can be is limited by design
and cost rather than by the game.*

### R34 - building a screen is its own authoring surface

Somebody has to make the screen, and that is a thing they do in Reactor: put a heading on
a panel, add lines, add a bar, say what each one reads from, put them in an order, and
never type code. O's done-when is that sentence, and it describes a second place to author
things, not a second component.

It is comparable in size to authoring a sequence, and shares almost nothing with it. A
sequence is ordered and temporal; a screen is arranged and continuous. The editor's
existing vocabulary - pick a block, pick a verb, add a step - does not apply.

*This is the requirement O is mostly made of, and the reason it is a project rather than a
feature.*

### R35 - a line is composed, not a value

*Hydrogen 62%* is three things: a label somebody wrote, a value that is usually an
aggregate across many blocks rather than a reading from one, and a decision about how that
value is written down. A line must be composed of parts, and the way a number is written -
a percentage, a rounded figure, a unit after it - is part of saying what the line shows.

The aggregate part is R37, and it is not a screen feature.

Without this, a screen is a column of bare numbers with no way to tell which is which,
which is worse than the terminal it was meant to replace.

*Waits on R4.*

### R36 - sequences write to screens

A step can put something into a part of a screen: a value, an aggregate, a state, a piece
of text. *Every ten seconds, show the total hydrogen on the bridge panel.* *When the
airlock starts cycling, put "stand clear" on the door panel.*

Readings and messages arrive by the same route, which is one thing to learn rather than
two, and the rate at which a screen changes is whatever the sequence writing to it says.

**The cost of this is a screen that can lie.** Nothing updates a part that nobody writes
to, so a panel showing yesterday's number looks exactly like a panel showing today's - and
a stale reading is worse than a blank one, because it is believed. That is the price of not
building a second system, and it is not obviously the wrong trade, but it is a real one.

*Open: what is owed about staleness.* A part could say when it was last written, Reactor
could notice a screen nothing writes to, or it could be left to the author. Nothing is
decided.

*Serves O. Waits on R7 and R37.*

### R38 - a screen is attached to panels

A screen says which panel or panels it appears on, and blocks that carry several surfaces
can carry different screens on each. That attachment is a role like any other, so a screen
travels with its system and is pointed at the recipient's panels on arrival.

*Serves O, K. Waits on R23.*

## O is under-specified on purpose

The four requirements above are what can honestly be drawn from one story. O is a project
in its own right, wanted as a stretch goal rather than a primary aim, and it needs its own
pass of stories before it can be specified properly. Areas nothing has been written about,
each of which is at least one story of its own:

- **emphasis** - a bar that goes red when the tank is low, text that stands out when
  something is wrong. Highly wanted, entirely unspecified, and it drags conditions into a
  thing that is supposed to be a standing arrangement.
- **arrangement** - how much the player controls where things sit, given the target draws
  and core is not supposed to know about layout.
- **what can be read** - block levels and states are obvious. Reactor's own state is not,
  and *this sequence is stuck* on the bridge screen would serve M better than anything in
  M's own requirements.
- **history** - anything that shows a trend needs samples kept over time, which nothing
  else in the set needs and T1 will have opinions about.
- **how many** - a ship with a dozen panels showing different things, and what that costs.

*Priority: stretch. Highly desirable, not primary, and not to be half-built.*



### R33 - a screen costs nothing of its own

A screen adds no running cost beyond the sequence that writes to it. There is no
background refresh, no watching, and nothing that happens because a panel exists.

*Why it is worth stating rather than assuming.* A screen is the one thing in the design
whose purpose is to look current, which makes it the most natural place in the whole system
for somebody to add a quiet refresh loop with the best of intentions. On a populated server
that is many panels across many grids, most of them in rooms nobody has entered in a week.

*Serves O, T1, T2.*

### R37 - values can be aggregated across blocks

Where a value is wanted from several blocks at once, it must be possible to say which
value: the total, the average, the lowest, the highest, how many there are.

**This is not a screen feature, and it corrects R25.** *When hydrogen is below half* across
five tanks is not answered by any-or-all. Four full tanks and one empty makes *any below
half* true while the ship has plenty of fuel, and *all below half* false while it is nearly
dry. What the player means is the total, and neither of the two readings R25 offers is it.

So the two mechanisms divide cleanly. **Any and all are for situations** - is any door
open, are all the doors shut. **Aggregates are for quantities** - the total hydrogen, the
lowest battery, the average pressure. R25 stands for what it covers and does not cover
this.

Which aggregate is meant is the player's to say and cannot be guessed: total is right for
capacity, average for pressure, lowest for "will any of them run out", and choosing wrong
is the kind of mistake that reads correctly and warns at the wrong moment.

*Serves O, B, F, G. Wanted by conditions (R4), by events on a crossing (R16), and by
anything shown on a screen (R35). Waits on R4.*

## From story P - one door gone, not the whole airlock

### R39 - a lost block is reported, and costs only what it must

When a step, a condition or a trigger names a block that no longer exists, the sequence
says so: it reads as missing where the name would be, and it is marked as having a problem.
It must not behave as though the step worked.

**Only that sequence is affected.** Everything else on the construct keeps running. Losing
a door should cost the airlock, not the lights, the hangar and the drills.

**There are three different meanings of "there is no block here", and only one is a
problem.**

- **Deliberately empty.** An optional role nobody filled. Silently skipped, never reported -
  R24.
- **Gone.** A block that existed and was destroyed. Reported loudly - this requirement.
- **Not yet built.** A construct being welded up from a projection arrives block by block,
  so a system can be running before the blocks it needs exist. That is not damage and
  nobody chose it; it is a thing that is about to be true.

The third only became visible once projector building was considered, and it is not an edge
case - it is how a great many constructs come into existence. Reporting it as damage would
mean a newly projected ship screaming about missing blocks throughout its construction,
which is both wrong and the fastest way to teach somebody to ignore the warnings.

Telling the three apart is something the system has to be able to do, not something we can
leave to context.

*Already true for the reporting half - a missing entity reads as gone and flags the
sequence. Untested, and the R24 distinction does not exist yet because roles do not.*

### R40 - repairing is one action, not one per use

When a block is replaced, getting everything working again is pointing the role at the new
block once. Not once per sequence, and not once per step.

A door used by six steps across two sequences is one role, so it is one repair. That is
the whole of P's *done when*, and it is R23's roles doing the work rather than anything
new - which is worth noticing, because P looked like it needed a repair feature and needs a
consequence of something already wanted.

**Healing itself is possible, and is the better answer where it applies.** A reference
remembers more than an identity: the kind of block it was and where it sat on the
construct. When the block it named is gone and a block of that same kind appears in that
same place, it is the replacement, and the reference follows it without anybody being
asked.

That signal is far stronger than matching by name, and it does not misfire on the person
from story U who has just spent an evening renaming everything - position and kind are not
things you change by tidying up.

**It also covers being copied, and being built in the first place.** A pasted blueprint, or
a projection welded into existence, is entirely new blocks with new identities - but every
one of them is the same kind in the same place, so the same healing that repairs a shot-out
door repoints a whole copied ship.

**Projector building makes this ordinary rather than exceptional.** A projected construct is
welded up block by block, so a system may be live while half its blocks do not exist yet,
each one arriving in the place the system already expects it. Healing is therefore not only
a repair feature - it is how a system built from a blueprint comes into service at all,
filling in as the ship is welded. That is a considerably better reason to build it than
cannonfire.

It does not cover everything, and the requirement does not pretend to. A door rebuilt
somewhere else, a section cut off and re-welded elsewhere, a block replaced with a
different kind that does the same job: none of those heal, and all of them fall back to
being pointed at by hand, which is what the rest of this requirement is for.

*A block's position is available and a block being placed can be noticed, so healing is
buildable. A destroyed block is destroyed, which needs no explaining.*

*Serves P. Waits on R10 and R23.*

### R41 - the program does not live in a block

Reactor's automation belongs to the construct, not to any block on it. Losing any single
block may cost the references to that block and nothing else.

*Why this is here rather than among the technical requirements.* It is technical in nature
and it is sourced from a story, which is fair - P is the want, and this is what has to be
true for P to be answerable at all. Vanilla automation keeps the behaviour inside the timer
or event controller that runs it, so one round through that block and the player starts
again from nothing. If Reactor's program is destructible in the same way, P is not a
feature we offer, it is the same defeat in a nicer editor.

**Open, and not small: what a construct is when constructs change.** Ships merge and come
apart, and the program is attached to something that does not hold still.

- **Merging.** Two constructs are joined into one - permanently and structurally, which
  is what a merge block does. If both carried automation, the result has two sets of
  systems, possibly with the same names, referring to blocks that all still exist. Nothing
  about that is obviously wrong, and nothing about it is obviously right either.

  Merging is not docking, and Reactor must never describe it as though it were. A merge
  block welds two constructs together; a mod cannot tell that apart from a repair, because
  there is nothing to tell apart - they are the same act. People do use them to dock, and
  the resulting confusion is theirs to have, not ours to encourage. Docking is connectors,
  and that is story T.
- **Coming apart.** A grid splits, and half the blocks a system points at are now on
  something else entirely. That is P's situation arriving wholesale, and the healing in R40
  does not apply because the blocks were not replaced - they left.
- **Being copied.** A blueprint pasted, or a projection welded up, is a second construct
  that looks identical. Whether it should carry the automation with it is a design question
  with a good answer either way, and what actually happens to our data on a copy is
  ApiUnknowns 2.

**The game does none of this for us.** Detecting a merge or a split is entirely our
responsibility - nothing tells us, and nothing moves anything on our behalf. That makes it
work rather than policy, and it is the reason this is written down while it is still cheap.

None of this is specified. It is recorded because "the program belongs to the construct"
sounds settled and is not, and because merge blocks are common enough that discovering it
late would be expensive.

*Storage itself is settled: the save file, with a separate data file for exports and
imports. What remains is ApiUnknowns 2.*

## From story Q - I pressed it twice

### R42 - a sequence does not overlap itself

A trigger that fires while its own sequence is still running is ignored. It does not
restart the sequence and it does not queue a second run. The same applies to a sequence set
off deliberately while it is already going.

That is what stops an impatient press putting the airlock into a state nobody designed:
mashing the button four times does the same thing as pressing it once, which is what
everybody expects a button to do and almost no naive implementation delivers.

**Ignored fires are counted**, so the editor can say the press was received and the
sequence was busy. Without that, an ignored press and a dead button are the same
experience, which is R3's problem arriving in a slightly different place - and it is why Q
is only half served today rather than fully.

*Neither of the alternatives has a story behind it.* Restarting would mean a second press
throws away a half-finished cycle, and queueing would mean four presses buy four cycles.
Both are defensible in the abstract; nobody has asked for either.

*The rule is already true in the runtime, and already counts. What is missing is anybody
being able to see it, which is R27 and R29.*

## From story R - two things fighting over my door

### R43 - conflicts are looked for, and never declared absent

Where two sequences drive the same block toward opposite states, Reactor should notice and
say so. An action already records the state it is trying to bring about, so one sequence
driving a door toward shut while another drives it toward open is a comparison of things
already modelled rather than a guess.

**Best effort, and honest about it.** Plenty of conflicts are not visible this way: one
sequence that starts another which then fights the first, two that only collide in a
situation neither describes, a fight that depends entirely on timing. Reactor may say *these
two disagree about this block*. It must never say *there are no conflicts*, because it
cannot know that, and a clean bill of health that is wrong is worse than no bill at all.

**Resolution is authored, not arbitrated.** Reactor does not decide which sequence wins. The
player already has the tools: a guard so the airlock cycle does not run while the base is
sealed, or the sealing sequence stopping the cycle outright, which is what R5 exists for.

*Not taken: precedence.* R's *done when* says the player can say which one wins, and a
ranking between sequences is the obvious reading. It was not taken because it is a hidden
global rule that changes behaviour without appearing in either sequence, and because which
one *should* win almost always depends on the situation rather than on a fixed order - which
is a guard, which we already have. It is also the same argument that killed the interlock
primitive.

*Scope it modestly.* This is easy to specify and much harder to do well, and a half-built
version that cries wolf would get switched off and never trusted again. Better a narrow
detector that is always right about what it does report.

*Serves R, M. Waits on nothing in the language; it is analysis over what is already there.*

## From story S - leave me alone, I'm building

### R44 - things can be switched off, and it shows

A sequence can be switched off, and so can a whole system, **and so can one of a system on
its own.** Off means nothing happens: no trigger starts it, no step runs, and nothing about it
is lost. Switching it back on returns it exactly as it was.

The three are separate switches and each has to survive the others. Parking the port airlock
to weld on it must not disturb the front one, and starting the system again afterwards must
not quietly arm a sequence somebody had deliberately left off.

**Off must look off.** A sequence that is switched off and a sequence that simply has not
been triggered are both doing nothing, and from outside they are the same. That is M's
failure again in the cheapest possible place, and it is the difference between "I turned
this off while I weld" and "why is this not working".

The system-wide switch is what S actually asks for. Somebody repairing a hangar wants that
hangar left alone, not to hunt down its four sequences individually and remember to put
them all back.

**Switching off abandons a run in progress, and that is now load-bearing rather than merely
probable.** It was open on the grounds that leaving a door half shut is a lot of consequence
for what feels like a toggle. It is settled by what the switch turned out to be for: nothing
that changes what a sequence does is offered while it can run, because a system cannot be
rewired mid-operation. That guarantee only holds if switching off means the runner is
genuinely no longer anywhere in the steps. Half shut is also the state somebody needs to see
in order to fix it, and R5 already says nothing a sequence did is undone.

**Starting and stopping must also be things a sequence can do**, not only things a person
presses. Somebody who can park a system by hand while they weld will want the same when the
pressure drops.

*Sequences are the tractable half.* One sequence switching another within the same system is
a plain reference by identity, which is R47. Naming a **system** is not: a system has more
than one of it on a ship, so "switch off the airlock" has to say which airlock, and that is
the roles-and-instances work that nothing has yet solved. Crossing a system boundary at all
runs into the same thing sharing does - a reference to a system the recipient does not have.
Sequences first, systems when instances are understood.

*One of a system cannot be switched off, and this is not a missing control.* A
`SystemInstance` holds a number and a name and nothing else; the runtime never sees one, since
`AutomationRunner` keeps a runner per **sequence**. Two of a system therefore run the same
sequences once, over the same blocks, and the second changes nothing. A switch on it today
would be a label that lies whichever way it was thrown.

It becomes meaningful with roles - when a sequence runs once per one-of-a-system against that
one's own doors, which also means a runner per instance-and-sequence rather than per sequence.
Until then this half of the requirement is blocked on the configurator, not on the editor.

*True today for the other two: `Sequence.Enabled`, `AutomationSystem.Enabled` and
`Sequence.CanRun` exist, the runner honours them, and both are visible controls - a sequence's
on its sentence, a system's on both its own screen and its row in the ship's list. The two
switches are deliberately separate rather than one setting the other, so parking a whole
system and restoring it leaves a deliberately-parked sequence still parked. Not built:
either of them being programmable.*

## From story T - when my miner docks

### R45 - docking and undocking are things that happen

A connector connecting and disconnecting must be available as events, on both sides of it.
That is the moment everything in T hangs off, and neither side of a dock can react to
anything if the moment itself is not sayable.

*Docking is observable, so this is a vocabulary matter rather than a language one. Recorded
because T is unbuildable without it and nothing else in the set needs it.*

### R46 - filling a tank is a wait, not a switch

Where a sequence brings a level somewhere - filling a docked ship's hydrogen from ice, for
instance - it waits for the level rather than assuming a duration. Ice-fed refuelling is
slow enough that any guessed wait is wrong, and the tank itself says when it is done.

This needs nothing new. It is R13's *once* holding until a situation is true, with R4
letting that situation be about a level. It is written down because it is the first
genuinely practical use of waiting on a quantity in the set, and because "wait thirty
seconds and hope" is what people write when the alternative is not offered clearly.

*Waits on R4.*

## T is not finished

The question T actually raises is not settled and is not ready to be:

**Does automation on one construct ever act on another?** A docked ship is a separate
construct throughout - connectors link, they do not merge. So either each side runs its own
automation and they coordinate through the world they share, or a station's automation
reaches into whatever ties up to it.

Two things push towards each side running its own, and one thing is simply unknown.

- **Reaching into somebody else's ship is a griefing vector.** If station automation can
  operate whatever docks, then tying up at a stranger's station hands them your thrusters,
  your reactors and your cargo. Ownership might mitigate that; needing mitigation is the
  tell.
- **The useful actions turn out to be one-sided anyway.** Draining a miner is a sorter on
  the station. Shutting a ship down is that ship's own blocks. Gas moves itself.
- **But a ship carrying no automation gets nothing**, and the station cannot help it. That
  is a real loss, and it is the same loss as "we do not operate other people's property".

*It is entirely ours to decide.* A mod sees the whole world; the one-construct limit is a
choice, not a wall. The reasons for keeping it are in Design.md, and T does not push hard
enough to move it - but it is the first story that pushed at all.

## From story U - I renamed everything

### R47 - a reference is to a block, not to what it is called

A reference is to the block itself. Renaming a block changes nothing about what any sequence
does, and every place that block is named in the editor shows the name it has now - read
when it is read, so a renamed block comes back correct everywhere it is mentioned without
anything being updated or migrated.

**The reason to write down something already true.** It is true by accident of an early
decision rather than by intent, and there is one specific thing likely to erode it. R40's
healing could be tempted into matching on names - "remember what it was called, and adopt a
block carrying that name" - which would quietly make renaming meaningful again, and would
misfire on precisely the person in this story, who has just changed every name on the ship
in one evening.

Healing matches on kind and position for that reason. This requirement is why.

*Already true, and untested.*

## From story V - let me watch it work first

Nearly all of V was served before it was reached, and by other stories: R28 walks a sequence
a step at a time, R27 shows which step it is on and what it is waiting for, R5 stops it.
That is V's *done when*, assembled from M and C. One thing was missing.

### R48 - a sequence can be set off deliberately, and does the real thing

Any sequence can be started without waiting for its trigger, and doing so must produce
exactly what the trigger would have produced. Not a rehearsal, not a test mode, not a
version with the dangerous steps left out - the same sequence, doing the same things to the
same blocks.

**A trial run that behaves differently from the real one is worse than no trial run**,
because its whole purpose is to earn trust it has not tested. Somebody who watches it work
and then stands in the airlock is relying on having seen the thing that is about to happen.

**This is a diagnostic feature and belongs with R28, not before it.** Nobody would open the
Reactor terminal, walk down to a system and then to a sequence, in order to *use* their
airlock - they would press the button they built. It is far too cumbersome to be anybody's
normal route. The only motive for reaching past the button is to watch what happens, which
means the value is entirely in what can be seen while it runs: which step it is on, what it
is waiting for, what got skipped. All of that is R28.

Built on its own it is half a feature with nothing to observe, and R28's walk subsumes it, so
it would be built twice.

*Deferred with R28. The run-by-hand button that existed in the prototype has been removed
rather than left standing with nothing behind it; `AutomationRunner.Call` stays, because a
sequence calling another one needs it.*

## From story W - drive the crane from the seat

### R49 - what somebody is doing at the controls is readable

A cockpit's controls are available as values: how far forward, how far to the side, how much
turn, how much roll. Reactor reads them the way it reads any other level, and a condition
about them is an ordinary condition.

**Named directions, not vectors.** The game reports these as vectors, positional and
rotational. Nobody using Reactor should ever meet a vector. The vocabulary presents them as
named readings on the cockpit - *forward*, *right*, *up*, *turn*, *roll* - each a value in
its own right, which is exactly the shape R4 already handles.

*Serves W. Waits on R4.*

### R50 - an action's value can come from a reading

A step that sets something can take its value from a reading rather than from a number
chosen when the sequence was written: *set the rotor's speed from how far the stick is
over*.

This is what "further over means faster" needs, and it is the difference between driving a
crane and switching it between three fixed speeds.

**Priority: stretch, and W does not depend on it.** W is served without this, by triggers on
thresholds - a few speeds and a stop, which is close to what people build by hand today.
Proportional control is the better version of the same story rather than the price of
serving it at all, and treating it as required would put the most speculative thing in the
set in front of the rest of W.

**And most of it would not arrive anyway.** Nearly everybody plays with a keyboard and
mouse. Keyboard movement is held or not held and has no magnitude to be proportional to, so
for the positional axes there is nothing to read for most players. Only the mouse carries a
magnitude, and that is a rate rather than a position. The expensive feature would therefore
serve a minority on the axes where it works at all, which is the clearest case in the set
for building the cheap version first and seeing whether anybody misses the rest.

**It is a smaller addition than it looks.** B already requires a value read from one block
to be compared against a value read from another, so a reading travelling somewhere other
than its own block is a thing the language will already do. This is the same movement, from
a reading into an action rather than into a comparison.

*Open, and the reason not to promise proportionality yet: what the reading actually means.*
Positional controls from a keyboard are held or not held, and give no magnitude at all;
from a stick they are proportional. Rotational input is a rate rather than a position -
faster mouse movement, faster swing - which suits a crane rather well, but means "how far
over" is not always a question with an answer. What arrives at a rotor is therefore not
uniformly a proportion, and W's *done when* may be satisfiable with a stick and only
approximable with a keyboard.

*Serves W. Waits on R4 and R7.*

## Dropped

**"How often a situation is checked is never the player's problem"** was written up and
dropped. It was an opinion stated as a specification. How often a condition is evaluated
is a real trade between accuracy and cost, both of which land on the user, and it is not
settled by asserting that the question is impolite. The tension is recorded in Design.md
as an open question instead.

**R19 - "the simplest thing is no more work than the terminal"** was written up and
dropped. It is story H's *done when* restated, which is the same fault that dropped B's,
and it obliges nobody to build anything - "be no worse than the terminal" is not something
a person can be handed and start work on.

It could become a requirement in the shape T1 has, a quality bar with a number that can
fail a build: *building one event on one block takes no more than N interactions*. That
needs N, counted on both sides, and the terminal side needs somebody in the game. Until
then it is an aspiration, and story H is where it lives. The number is retired and not
reused.

**R18 - "anything the game can automate, Reactor can say"** was written up and dropped.
It is not a requirement, it is a permission: it obliges nobody to build anything, it only
removes an objection to building it. That belongs in Design.md as a scope rule, and is
already there. The number is retired and not reused.

**"The sequence must work without being told which situation it is in"** was written up
and dropped. It is story B's *done when* restated, and a done-when is how we will know it
worked rather than a thing the system must do. If R4 holds, "which situation" is never
asked in the first place.

---

# Technical requirements

Some of these are written ahead of any evidence. T1, T2 and T3 all rest on how Space
Engineers actually behaves - whether we are told about changes or must look for them,
what update tiers do to a construct nobody is standing next to, what any of it costs on
a populated server - and none of that is knowable from here. They are predictions made
from experience, and the first time Reactor runs in the game some of them will be wrong.

That is not a reason to leave them unwritten. Written down, they are the list of things
to measure on first contact rather than things to notice afterwards, and a prediction
that turns out wrong is worth more than an assumption nobody recorded. When one is
measured, it gets corrected here and the old wording is kept.

### T1 - cost follows what happens, not what exists

The total cost of Reactor on a server must scale with the number of things actually
happening, not with how much automation exists anywhere on it. The unit is the whole
world - many players, many constructs, several sequences each - and not one construct at
a time.

*Why.* A single ship with a hundred sleeping sequences is not the situation. A populated
server is thousands of sequences across dozens of grids, most of them waiting for
something that will not happen this minute. Any design whose cost is paid per sequence
is a design that works in testing and dies on a real server.

### T2 - waiting is not work

A sequence that is waiting must do no work on a tick where nothing it cares about has
changed. Where the game will tell us something happened, we are told; polling is what we
fall back to, not what we build on.

Where polling is forced, three things bound it: it watches only what is actually being
waited on at that moment and not everything the sequence mentions, at the lowest rate
that still meets the accuracy the situation needs, and each check allocates nothing.

*Why.* This is the specific shape T1 fails in, and it is the easiest thing in the world
to write by accident: per-sequence-per-tick work is imperceptible with four sequences
and is the entire budget at ten thousand.

*What this does not rest on.* An earlier version of this leaned on sharing the watching
between sequences - fifty of them watching one door watching it once. That is true and
worth doing where it happens, but it is a rare situation: most sequences act on their own
blocks, so deduplication buys almost nothing. The saving has to come from watching less
and watching it less often, not from watching it together.

### T3 - the same sequence, whatever the update rate

A sequence must do the same thing regardless of how often Reactor is being updated.
Nothing about what it does may depend on being updated often, and nothing may drift
because it was updated rarely.

*Why.* Space Engineers does not let grids sleep, but it does move blocks between update
tiers, and the tier a construct is on can change with distance and with what the server
is doing. That makes update rate a variable we do not control and cannot see coming. A
sequence tested while standing next to the ship, on the fastest tier, is a sequence
tested in the one condition that is least like the general case.

Anything that counts ticks for itself, assumes a steady interval, or assumes it observes
every change as it happens will pass every test we write and fail on a live server when
the player flies away.

*Consequence worth stating separately.* On a coarse tier, a change that happens and
reverses between two updates was never visible at all. Whether that can happen, and what
is owed to the player when it does, is not settled - see ApiUnknowns 1.

*Withdrawn premise.* This requirement previously said "never wake what the game left
asleep", which assumed grids can be left alone. They cannot.
