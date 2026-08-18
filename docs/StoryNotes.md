# Notes on the user stories

Working notes behind [UserStories.md](UserStories.md), kept separate from it because
that document is public and this one is not. Stories are what people want. Everything
here is our own judgement about cost, mechanism and order, and none of it belongs in
front of a reader who is being asked whether we understood them.

Stories are referred to by letter.

## How the set is used

**An idea that cannot be written as a valid story is not a feature.** At best it is
deferred; more often it should be rejected outright. A wording, a mechanism or a data
shape is an *answer* to a story and belongs in [Design.md](Design.md).

**The rule runs one way only.** A story that passes is eligible, not scheduled.
Whether it gets built, and when, is a priority decision taken separately - and some of
these will cost more than they are worth. Capturing and restoring how a set of blocks
looks is the obvious candidate, and we will not know until we try.

So the set errs deliberately towards ambition. Two stories too many costs a re-read;
one too few costs a language that cannot say something people wanted, discovered at
the point where it is expensive to change. **When a story is judged not worth
building, that verdict is written into these notes rather than the story being
deleted** - the reasoning is the part worth keeping, and it is what stops the same
idea being proposed again in six months as if it were new.

## Where each story stands

| | Story | Today |
|---|---|---|
| A | One switch, out and back | no |
| B | Match the side I'm about to open | no |
| C | Call it off, and put it back | no |
| D | Everything red, then exactly as it was | no |
| E | Three doors, one procedure | half |
| F | Keep the hydrogen above half | nearly |
| G | Tell me before it runs out | no |
| H | All of it in one place | yes, untested |
| I | Two airlocks that don't confuse each other | no |
| J | Where did everything go? | no |
| K | Give it to a friend | no |
| L | Add a condition to something I already built | no |
| M | Nothing happened | no |
| N | The alarm stops when I say so | nearly |
| O | A screen that tells me how things are | no |
| P | One door gone, not the whole airlock | half |
| Q | I pressed it twice | half |
| R | Two things fighting over my door | no |
| S | Leave me alone, I'm building | half |
| T | When my miner docks | no |
| U | I renamed everything | yes |
| V | Let me watch it work first | half |
| W | Drive the crane from the seat | no |

Two of twenty-three work. Nothing is scheduled.

## Per story

**A - one switch.** Gates live on steps and are evaluated when their step runs, so a
guard meaning "we are deploying" asks about a world step 1 has already changed; by
step 3 it answers the opposite. Wants a condition on the *trigger*, decided before
anything moves. The same shape fits a hangar - mass off, doors open, lights up, and
the reverse - so the want does not rest on one example.

**B - match the side.** Decides how far quantities have to go, and asks for the
expensive answer. Every other story compares a reading to a number the player chose;
this one compares two live readings, and neither side is ever a constant. A
thresholds-only feature satisfies F and G completely and leaves B untouched.

Open: reading the far side means the far side needs something that can report it, so
the airlock's block list grows by a vent in the next room. What the sequence does when
nobody put one there is unresolved.

**C - call it off.** Stopping is two wants: halting, and settling somewhere safe. The
way back is its own procedure and not the forward steps reversed - re-pressurising is
not un-depressurising, and the doors go in a different order.

What it does *not* need: if a sequence can be stopped by name and a step can call a
sequence (E), graceful restoration follows for free - the button stops the preparation
and calls "put the hangar back". The only new primitive is *stop*, and it is worth
nothing until E lands.

**D - everything red.** Two features, and the second is the expensive one. Setting a
value is small. Getting back means capturing how a set of blocks looks and restoring
it, which cannot be pushed onto the player: two dozen lights is exactly the scale at
which writing the values down by hand is both tedious and wrong the moment the
lighting is retuned.

What is captured is not colour - a light that was off is normal when it is off again -
so it is how the block *is*, not one property of it. This is I's *remembered* state at
a scale nobody would ever author, which is the first real argument that remembered
state cannot be only a thing players create by hand.

**E - three doors.** Half-built: `CalledTrigger` exists and `Runner.Call` exists, but
its only caller is the editor's run-by-hand button, so no step can call a sequence.

Settles a question we had been treating as open. Template versus duplicate is decided
by E's own done-when - "changing the shared procedure changes all three" - and
duplication cannot satisfy it.

**F - hydrogen.** Needs no new concept: *every 100 ticks, only if the tank is below
half, turn the generator on*. Repeat plus guard already is regulation. Only "below
half" is missing, which is B's groundwork. Kept in the set precisely because it looks
like a feature and is not.

**G - tell me.** Two questions, and only one is about levels. The other is *tell me
how*: Reactor can operate blocks and nothing else, so "warn me" can only mean "switch
on a lamp somebody has to be looking at", which is not a warning. Telling a person
something is a capability the system has never had.

**H - one place.** The point to take home is one sentence: *just because it can be
done in vanilla automation does not mean Reactor should not be able to do it as well.*
The sensor is the example, not the subject.

It is the only story whose done-when is a usability bar rather than a capability, and the only one where the game's own terminal is a serious rival. A
sensor wired to a light is one of the few things vanilla makes genuinely easy - most
of the set it makes hard or impossible - which is exactly what makes H the fairest
test available: no capability advantage to hide behind, so it has to win on being
pleasant.

It matters that it does. Every easy case set up outside Reactor is automation Reactor
cannot see, cannot show on a screen, and cannot account for when the player asks why
nothing happened. H is load-bearing for M.

**To measure, before H can produce a requirement.** Count the interactions to build H in
Reactor, and count them for the same thing through the game's terminal. Two numbers, one
of which needs somebody in the game. Until both exist, H's done-when is an aspiration
rather than a bar, and two attempts at writing it as a requirement have been withdrawn.

Status is "yes" for the language and untested for the story. It cannot be settled by
argument - somebody has to be watched doing it.

**I - two airlocks.** No named state and nothing to scope it to. Forces the fork
between derived state (recomputed, cannot desync) and remembered state (stored, can).
Both are wanted; they must not share a name in the UI.

**J - forty sequences.** One flat list. With I and K, this is what actually justifies
the "system" concept.

**K - give it to a friend.** Nothing is exportable, and a role is not yet something a
sequence has. The auto-derived resource set idea lives or dies here.

**L - add a condition.** A distinct want from A: A is somebody who knows the condition
before they start, L is somebody who learns it afterwards. Both routes have to reach
the same construct or one becomes an advanced path nobody finds. The retrofit route is
nearly free - the sentence already carries add-points.

**M - nothing happened.** Last on purpose: failure cannot be designed until the
language can say what was supposed to happen. The cheapest version arrives long before
stuck sequences do - pressing the rig button while the arm is halfway out, so neither
"stowed" nor "deployed" holds, is a dead button with no explanation, and halfway is
most of the time an arm is moving.

**N - the alarm.** Needs nothing new. Two short sequences: one switches the siren and
lights on, another switches them off. Nothing is left running, so there is nothing to
stop. Open only on whether the game reports being attacked as something a block can
trigger on.

Kept because it looks exactly like C and is not. "It keeps going until I say so" is
usually a pair of switches, and mistaking one for the other is how a feature gets
built for a want that never needed it.

**O - a screen.** The same capability as G, in its ambient form: G interrupts, O
displays. A system that can do one and not the other will look arbitrary.

Ladders, and prioritisation will want to cut it somewhere. A heading and live text
needs values readable and writable as text. A progress bar needs quantities, so it
waits on B, F and G. A graph needs **history** - samples kept over time - which would
be the first thing Reactor stores that is not a fact about right now.

Probably not a sequence at all. A screen is a standing arrangement that is always
true, not a series of steps, and driving it from steps ("set line 2 to the tank
level") is the programmer's shape of the problem. Whether that means a second kind of
thing alongside sequences is a design question.

**P - one door gone.** Half, and the half that exists is the good half. Reactor
already notices: a lost block reads as "something's gone" in the sentence, and the
sequence is flagged rather than quietly doing nothing. What is missing is repair. A
device is referenced by `long Id`, and a rebuilt block in Space Engineers is a new
entity with a new id and usually a default name, so nothing re-binds on its own. One
door used by six steps across two sequences is six repairs today.

This converges with K. K wanted named roles so a shared system could be pointed at
somebody else's blocks; P wants the same structure so a rebuilt block can be pointed at
once. Two unrelated stories arriving at one mechanism is the strongest signal in the
set so far, and it moves roles out of "deferred with export" and into its own right.

The comparison that makes this urgent: vanilla automation keeps the behaviour inside
the event controller or timer block, so destroying that block destroys the program and
the player rebuilds from scratch. Reactor only beats that if its own program does not
live in a destructible block either, which is a constraint on where we store things
rather than on the language. It is written into Design.md rather than left implied.

**Q - pressed it twice.** Half, and the half that exists is already decided:
`SequenceRunner` ignores a trigger that arrives while it is running, and counts how
often that happened. So the behaviour is safe and the question "restart, queue or
ignore" is answered. What is missing is the other half of the story - the player cannot
see that it is already running, so from the outside an ignored press and a broken
button look identical. That is M's problem in miniature, and much cheaper.

**R - two things fighting.** Nothing. This is the cost of the decision that killed the
interlock primitive: "both doors never open" being two ordinary sequences is right, and
it guarantees that several sequences touch the same block. Conflict is therefore
designed in, not an accident, and we owe the player a way to see it. Note it is not
solvable by ordering alone - the honest answer is probably to show the disagreement
rather than to resolve it silently.

**S - leave me alone.** Half. `Sequence.Enabled` exists and the runner honours it, so
one sequence can be switched off. The story wants it for a *part of the ship*, which is
a system, and wants it obvious at a glance - neither of which exists. Cheap once
systems do, and it is the sort of thing that gets missed because it is unglamorous and
then gets rebuilt badly by every user with a spare button.

**T - when my miner docks.** Nothing, and it is the largest new idea in the set. Every
other story references blocks the player picked while authoring. This one cannot: the
ship is not there yet, and it is a different ship each time. That means a reference
resolved when the sequence runs rather than when it is written, by role and kind rather
than identity - which is decision 8 again, arriving from a third direction and now
carrying a requirement the other two did not have.

Also raises undocking as an event that ends things, which is close to C's *stop*.

**U - I renamed everything.** Yes, and worth stating because it is the one place the
design is already right by accident of an early decision: references are by id, so
renaming cannot break anything, and the sentences re-read with the new names because
they are rendered from the world each time.

It is also the counterweight to P's speculative half. Re-binding by remembered name
would make renaming meaningful again, and could adopt the wrong block precisely because
somebody tidied up. U is the reason that idea has to stay conservative, and the reason
it must never be the only way a reference is held.

**V - watch it work first.** Half. `Runner.Call` exists and the sequence page has a run
by hand button, so a sequence can be set off deliberately. What is missing is watching
it - seeing which step it is on - and stopping it, which is C's primitive again. Worth
noting how much this overlaps M: the thing that answers "why didn't it happen" is
mostly the same thing that answers "what is it doing right now".

**W - drive the crane.** One plausible reading, held loosely: it may need no new kind of
thing. If a cockpit's controls are readable, then *when the cockpit's stick is forward* is
an ordinary trigger with an ordinary condition, and setting a rotor's velocity is R7. It
would be sequences all the way down.

That reading dissolves two objections raised against W. It would not be a second kind of
thing alongside sequences, and watching a control is polling exactly as watching a tank
is, so T2 is not violated in any new way.

**Held loosely on purpose.** It is one implementation, arrived at before we know what the
game exposes, and committing to it now would be deciding the shape of the feature from a
guess.

**And the shape of the input is the problem.** A crane's swing is the rotational axis,
and rotation in a cockpit is driven by the mouse - which gives movement since the last
frame, not a position. There is no "how far over" to read for the one control the story
most wants. Forward and back, and roll, are held keys rather than axes; whether anything
reports a magnitude at all is unknown.

So "when the stick is past a half" may not describe anything that exists. It might be
"while forward is held", which is a different sentence and gives on-and-off control, or it
might be a stream of deltas, which is a different thing again and fits nothing in the
language.

**Proportional control** is what suffers either way. W's done-when says further over means
faster. Held keys give stepped control at best - a few speeds, chosen by how many
thresholds somebody authors. Real proportionality needs a magnitude to exist and then to
flow from the reading into the action, *set the rotor's speed from the stick*, which is a
step past R7's fixed value and is wanted by nothing else in the set.

Whether stepped control is good enough is a question for somebody who has actually
operated a crane, not for us. It may be that three speeds and a stop is exactly what
people build by hand today and nobody minds.

**Also survives: whether the axes are readable at all.** ApiUnknowns 11.

*A note on an earlier claim.* This was briefly written up as the second story wanting
something that is not a sequence, alongside O's screen. That was wrong, and O stands
alone.

## Rejected framings

Kept so they are not proposed again as if new.

- **One button that cycles an airlock both ways.** An airlock has a button inside and a
  button outside, each of which can only mean one thing from where it stands, so there
  is nothing to alternate. It was written backwards from a mechanism already decided
  on, which is what the rule exists to catch. The mechanism survives under A; the
  airlock framing does not. Note the consequence: the airlock never justified the
  trigger condition, which now rests entirely on A and L.
- **Doors that open for friends and not for strangers.** Not a want, and not a
  judgement about scope either. Friend-or-foe is a setting on the sensor, like its
  range: part of configuring the block, which players already know how to do and do
  without thinking about it. The story was really asking Reactor to take over a block's
  own settings panel.

  The line is not that block settings are out of bounds - D wants to set a light's
  colour, which is a block setting. It is that *configuring a block once and leaving it*
  belongs to the game, while *changing it because something happened* belongs to
  Reactor. A sensor's filter is set when the sensor is built and never touched again.

  Nor does it conflict with H. H is about behaviour between blocks, which is Reactor's
  subject; the sensor's filter is not behaviour at all.

  Written up because it also killed a design decision: "events that carry what set them
  off" had exactly one story behind it, and nothing else in the set wants an event to
  know anything about its cause.
- **"When" versus "while" as the wording for a trigger condition.** An answer to A.
  Design.md.
- **Resolving call names inside a system.** Nobody has ever wished for a namespace. It
  is a consequence of J and K, and was doing illegitimate work as one of four
  justifications for systems until the rule was applied to it.
- **Template versus duplicate.** The open question inside E, and settled by E's
  done-when rather than by preference.
- **Teaching the event/state distinction when somebody types a state as a trigger.** A
  test expectation on A and H. Watch for it; do not build for it.

## What the set has settled on its own

Findings that came out of writing the stories rather than from arguing about design:

- **Stop depends on call.** C's restoration needs nothing beyond E plus a stop
  primitive, so stop is worth nothing before E.
- **Quantities split in two.** Thresholds against a chosen number (F, G) and readings
  compared to each other (B). Only B needs the second, and only B decides it.
- **Telling a person is one capability with two faces** (G, O), and building half of it
  will read as arbitrary.
- **Remembered state cannot be only player-authored** (D against I).
- **Roles are wanted three times over.** K for sharing, P for repair, T for blocks
  that are not there yet. Three unrelated stories, one mechanism.
- **Diagnosis is one feature, not three.** Q wants "it is already running", R wants
  "these two disagree", V wants "which step is it on", M wants "why didn't it happen".
  All four are the same window onto a running sequence, and building any one of them
  alone would be building a quarter of it badly.
- **Conflict is designed in, not accidental.** Dropping the interlock primitive means
  several sequences will touch one block by design, so R is a bill that came with a
  decision we already made and were pleased with.
- **Systems are justified by I, J and K** - scoping, navigation and sharing - and not
  by anything about names resolving.

## Using these as tester tasks

Hand over the goal and nothing else. Watch where they go first, what they expect the
words to mean, and what they try that is not there. A tester who confidently builds
the wrong thing has found a worse problem than one who gets stuck.

H first, always. If that one is hard, nothing measured after it means anything.
