# The editor

Internal, and in progress. What Sprocket presents and how somebody moves through it.
[Design.md](Design.md) holds the model; this holds the interface. Where they disagree, the
model is right and this is out of date.

Requirements are referred to by number, from [Requirements.md](Requirements.md).

## The root

What you see when you open Sprocket is **the logic systems on this construct**, not a list of
sequences. A player did not build a procedure, they built an airlock, and that is the thing
they came looking for.

```
   + New logic system    + Load blueprint
   ---------------------------------------------
Drill Rig                      [Make blueprint] [ON|OFF]

Made from Airlock blueprint
   Airlock         cycling                       [ON|OFF]
   Airlock 2                                     [ON|OFF]
   + add another Airlock
```

**A logic system is one row until a blueprint is made of it.** Nobody starting out has to meet
the idea that one airlock's logic can serve several: they make an airlock, and it is a row. The
idea arrives when somebody reaches for it.

**Making a blueprint is said in words on the row**, *Make blueprint*, because a way to turn one
airlock into several that nobody finds is the problem the screen exists to solve. It asks
nothing: the blueprint keeps the logic system's name, the logic system it was made of becomes
the first made from it under the same name, and the blueprint opens.

**Logic systems made from a blueprint stand under it**, headed *Made from Airlock blueprint* with
the blueprint's name in the bright ink. The heading is a sentence rather than in capitals, all of
it, because the name is somebody's and is shown as they typed it. Logic systems of their own come
first, so a row of its own is never read as belonging to the blueprint above it.

**Every logic system has its own switch, and a blueprint has one over all of them.** Switching
the port airlock off to weld on it must not stop the front one; switching the blueprint off stops
every logic system made from it on the construct, so it asks first and names them. A system's
switch is also what guards changing it: its triggers and steps are changed only while it is off,
and leaving it off after a change offers to switch it on.

**Names are numbered, and given in place.** Nothing is asked when something is made: another
logic system made from a blueprint is *Airlock 2*, the first number free, the way the game
numbers blocks. A name is given by renaming, where the name is shown. *Instance* is programmer
vocabulary and is not used on screen; *blueprint* and *logic system* are the words.

## Getting started

Both root actions are equally prominent. Most people will never author anything, and for
them Sprocket is the thing where you load somebody's airlock and tell it which doors are
yours - so **Load blueprint** is not a secondary path.

**New logic system makes one and puts you on it, with its name in hand.** It is *Logic system 1*,
the first number free, and it opens with that name being edited in the heading: typing names it,
and anything else keeps the number. After that you are standing on it with the ways to start a
sequence in front of you. Whether numbered names stay unnamed in use is to be tried with the
playtester.

**Labels are plain nouns, not sentences.** *New logic system*, not *build something new*. *Load
blueprint*, not *use one somebody made*. The softer forms were proposed and rejected: this
audience has never been taught programmer vocabulary, which is not the same as having been
robbed of nouns, and a label rewritten into a friendly sentence buys nothing and reads as
being handled.

## The blueprint

A blueprint's own screen, headed *Airlock blueprint*: the logic systems made from it on the left,
and on the right the chosen one's switch and the blueprint's **parts**, each with the blocks the
chosen one fills it with. The parts' names are the blueprint's and read the same whichever logic
system is chosen; only the blocks change. That difference, seen on one screen, is what tells a
blueprint from the logic systems made from it.

**It is where a blueprint is set up.** Making a blueprint ends here, and so does *add another*,
on the new one, whose parts are empty until somebody says which blocks fill them. A part's blocks
are a slot, the chip a sentence fills, and pressing it offers the construct's blocks of the type
the part expects. A part is renamed from the pencil beside its name.

**It is the one way into what a blueprint does.** *Edit using these blocks* opens the familiar
logic system screen headed *Airlock blueprint*, with **Using the blocks of Main airlock** above
everything else on it: a sentence names blocks, only a logic system has any, and every block
named below that line is the chosen one's. The name is a slot and is pressed to choose another,
which reads the same sentences against another airlock's doors.

**Every sentence stores parts, a blueprint or not.** Picking a block takes a part of the system -
named for the block's kind, *Door*, *Door 2* - and fills it on the logic system being edited, so
making a blueprint names things and moves nothing.

Derived from the sentences with no effort from the author (R12). What an author may add - an
explanation of the one part that always catches people out, the order the parts are asked in -
is still open, as are parts that may be left empty (R24).

## Copying, and not copying

**+ another** always makes one that belongs to the same system. Fix the system, and every one
of them changes - which is E's whole want, and it needs no dialog because the shape says it.

Wanting two that can differ is a different act: **duplicate the system**, giving a second
system with its own life. Rarer, deliberate, and named for what it does.

*This replaces an earlier design where copying asked "does this follow the original, or is it
its own?" at the moment of copying. The structure answers it, so the question does not need
asking.*

**A sequence duplicates the same way, and it is built.** The button is on the sequence's row in
the list, not inside the sequence: wanting another one like this is something somebody decides
while reading what they have, and the row is where they are. The copy has the same steps pointing
at the same blocks and is its own from then on.

It arrives with **no trigger** and **switched off**. A toolbar slot holds one claim on Sprocket's
action, so a copy carrying the trigger it came from would be a second claim on a slot that is
already taken - and what that costs is not an airlock cycling twice but an airlock that stops
cycling at all, with nothing on screen saying why. See *Two triggers on one button*. It is named like any other new sequence, because a script may run a sequence
by its name, and a copied name would leave that name meaning two sequences.
Pressing it opens the copy, which is both what somebody wanted to change and the screen where it
is told what starts it. It is offered whether or not the sequence is running, unlike
every control on the sequence's own screen, because the copy cannot run and so cannot disturb
anything. Not undoable: undo is a sequence's own history and a new one has none, so the way back
is the bin.

*What this is not is reuse. One sequence with several filled sets of its slots - one Cycle serving
cycling in and cycling out, edited once - is the better answer and is not ruled out by this, since
a sequence with one use is what every sequence is now. What decides whether it is worth building
is watching whether people duplicate a sequence and then hand-edit both when one turns out to be
wrong.*

### What starts a sequence is not on the sequence's screen

**Triggers are listed on the system's screen, above the sequences, and changed there.** A sequence
may be started from three buttons and stopped by a fourth, so a line on its own screen could only
ever show one of them or grow into the list the system already keeps. Its screen is what the
sequence is called and what it does; the rows above the list are the reasons it happens.

**A trigger is written out as a sentence, and every part of it is a control.** *When* **Airlock
Button** **is pressed** *runs* **Cycle** - the same chips a step of a sequence is made of, at the
same size, because it is the same act: words with the parts somebody chose sitting in them, each
one opening the question it was answered with. The blocks, the moment and the sequence it runs all
lead to the picker it was made in, where what it does is the column beside what starts it.

It was a card for a while, and a card can only be pressed as a whole - which meant asking which
half was meant, on a screen of its own, for a question whose answer was already on the row behind
it. A trigger that is reading true right now says so on its own row, where the sequence's screen
used to say it for whichever trigger came first.

**What it does is the picker's last column**, *What it does*: *A single action…*, *A new
sequence*, and then the system's named sequences under their own heading, with *run* and *stop* at
the head once there is a sequence to stop. A timer's picker is the rates and the same column. Done
sits under the list, as it sits under the fields of a step's values. Changing when it happens keeps
what it does unless the column says otherwise, and whatever was written about it; a change to both
on one Done is one step back.

**A single action is the picker's next page.** Choosing *A single action…* turns to it: the
trigger's columns slide out to the left, *What it does* folds into a narrow strip that is the way
back, and the action's own columns come in beside it. Back on the trigger's page, the last column
gives up room at the right for a strip that is the way on. The action's page is kept while going
back and on, and let go once *What it does* says something else; nothing is kept until its Done.

**The one act a trigger does opens the picker that act was chosen in**, since it has no screen of
its own.

**A trigger always does something.** There is no way to empty what it does, only to change it. The
trigger itself is kept through any change to what it does, since the toolbar slot the construct
holds names that trigger. The sequence it pointed at before stays, as any sequence does until it is
deleted.

### Two triggers on one button

**A slot holds one thing, so the second trigger is a question and the answer decides it.** Asked to
arrange both, Sprocket arranges neither: the slot is left empty, both triggers stand there naming a
button that answers nothing, and the host then takes the block out of one of them because the
button it names is no longer its own. Which was how a trigger ended up with nothing on its row to
press - the failure that asked for the warning in the first place, and the reason the warning
cannot be the whole answer.

So *Airlock Button is already in use* offers **Leave it alone** or **Take it over**, and taking it
over means what it says: the new trigger gets the button and the old one is left without one, in
the same state the host leaves behind when somebody clears that button in the game. Its row says
nothing can start this, its parts are still there, and pressing them is the mend.

It is one edit, because it was one answer to one question: one press of the way back puts the
button where it was and the blocks back in the trigger that lost them. Both ways in ask - making a
trigger and pointing an existing one - and a trigger never clashes with itself, so settling on what
was already there asks nothing. Only for the moments the construct announces, since Sprocket can
watch one door for two sequences without a slot being involved at all.

Both are **walked back from the system's screen** - see *Taking it back*.

**Each list is named and carries its own ways to add to it**: *Triggers*, then *+ When…* and
*+ Every…*; *Sequences*, then *+ New sequence*. Sprocket's own nouns rather than a phrase talking
round them - a screen holding two lists of things that read alike has to say which is which, and
*what starts things* is longer, has to be re-invented at every site, and is wrong the moment a row
stops a sequence instead.

A sequence made from that second row is one nothing starts, which is the other way round from the
wizard and equally ordinary: somebody who knows what the machine should do writes it first and
gives it a trigger afterwards.

**A fault in what starts a sequence marks the trigger's row**, not the sequence's: a cleared button
is the trigger's trouble and the sequence is fine. The words are the same ones the sequence's own
screen says, and pressing the row is the mend.

**A sequence that is switched off wears the amber ground**, because it is the commonest reason a
button does nothing and the only one where everything else reads as working: the trigger is right,
the blocks are there, and the row said so in the column where every row says something, which is
where nobody finds it. A ground is what somebody scanning a list for the odd one out actually
reads. Not red: switching a sequence off is deliberate, so it is not counted as wrong and does not
send anybody hunting for a block that is not missing. And nothing is said about a sequence with no
steps yet, on the same rule the trigger's warning follows - somebody who has not finished is not
told they are wrong.

**And the words go in the line that was saying "switched off", rather than on a line of their own.**
Beside a switch that says off, that was a word spent on nothing; what somebody needs is what follows
from it - *This is switched off, so nothing starts it on its own*, which is true of the trigger and
of a sequence that calls this one, and leaves the play mark on the same row as the exception it is.
Said in a line the row always has, because a line that exists only while the fault does changes the
row's height when the switch on that row is pressed, and a control that moves as it is used is the
one thing a screen must never do. The sequence's own screen says it in its header for the same
reason, the switch being in that same bar.

The construct's screen counts it as a warning, where before a system with one sequence running and
another parked read as running and said nothing about the second.

### A sequence nothing starts

Four ways to arrive at one: make it from the row above the list, copy one, be sent a program
somebody wrote by hand, or have the block your trigger named ground off. It is an ordinary state
rather than a broken one - somebody who knows what the machine should do writes it first - and what
says so is a warning on its own screen and on its row, *Nothing starts this yet.*

**The mend is a trigger on the system, and not an offer on the sequence.** There was a `+ say what
starts this` on the sentence for a while, and it was the last thing about triggers left on that
screen: a second way to make one, with its own question, for the one case the system's own screen
could not reach. One way to make a trigger is worth more than a shortcut to it, and the way there
is a single press back.

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
- a **slot** exists because a sequence refers to a block (R23), not because anybody listed
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
- **The game's interface makes noises and ours is silent.** A press, a hover, a screen opening
  and closing all have a sound in Space Engineers, and a panel drawn to look like the game's
  that answers nothing to the ear reads as slightly dead once you notice it. Which sounds, on
  which acts, and whether they are the game's own or ours is open - as is whether this belongs
  in the seam at all, since the other host has neither those sounds nor a reason to fake them.

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

### Two ways to tick, and both of them ordinary

**The checkbox toggles that one row. Anywhere else on the row is an ordinary list selection** -
click selects just that one, ctrl adds and removes, shift takes a range.

Neither is a fallback for the other. The checkbox is the whole picker for somebody who has never
met a modifier key, and it is the only route that works on a gamepad, where ctrl and shift do not
exist. The list behaviour is for the far larger group who have played this game, used its terminal,
and already know what clicking a list does. Nothing is reachable only by modifier, so nothing has to
be discovered.

It also fixes a real asymmetry. Toggling alone means a wrong tap leaves a wrong row ticked, to be
noticed and undone; with ordinary selection the next click simply replaces it, so a mistake corrects
itself rather than accumulating.

**Two things this rests on.** The checkbox's hit target has to be comfortably larger than the
checkbox, because a near miss does not do nothing - it replaces the selection, and a list that seems
to forget things at random is worse than one that is merely slow. And the row click is destructive
by design, so what it costs has to stay small: a picker's selection is not yet a sequence, so there
is nothing for undo to walk back.

**The shift anchor belongs to core**, which is the one part of this that is not ordinary. The list
narrows as somebody types, so an anchor held by the target would come to span rows that had stopped
being adjacent. Core has no idea what shift is and should not: the target decides which command a
press invokes, and core offers the row a toggle and a select rather than one command that means
both.

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
When Airlock Button is pressed     Airlock
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

### A heading is a list of parts, and only some of them are yours to name

`EditorScreen.Heading` is a list of parts, each one answering for itself whether it can be
renamed and carrying any words that lead it - *Automation for* **Hangar Bay Alpha** is one named
thing with a phrase in front of it, not two things. Pages get a single part for free:
`EditorPage.Heading` builds one from the page's own title.

Nothing needs two parts now that what starts a sequence is a row on its system's screen rather
than half of its heading. The shape stays because the alternative is a string plus a name, which
is what it was: naming a sequence then replaced the whole line, so anybody who wanted a better
word for what it **does** lost the trigger that started it and got it back only by typing the
trigger into the name by hand and keeping it in step there forever after.

**There is deliberately no joined-up string of the whole heading on `EditorScreen`.** One was
easy to add and every target would have drawn it, because drawing one string is easier than
drawing several - and the entire point of the list is showing which part is somebody's to
change. Where a target really must flatten it, it writes that out in its own file, where the
cost is visible.

Words between the parts are the target's. This one draws the arrow it already uses for a cause
and its consequence; a narrow one could break the line instead. A heading that ever needs a
connective word can carry it as a part with no rename command, which is the same mechanism
rather than a second one.

### Naming as publishing, and why that cannot be silent

A name buys two things, and the second one has never been drawn. `SequencePage` has said from
the start that a name means "more than one trigger can start the same one" - so naming is
what would promote a sequence from something private to its trigger into something other
triggers can point at.

**The naive version of that is an invisible feature.** Nothing about typing a name suggests
you have just published something, and nothing afterwards shows that you could now refer to
it, or that anything does. A person would have to be told, which means they will not find it.

Whatever makes referencing real has to make the promotion visible - at the moment of naming,
in what a named sequence then looks like, and in what a sequence being used from elsewhere
says about itself. That is the actual work, and it is why this is a later extension rather
than a consequence that falls out of naming for free.

**A sequence's heading has no subtitle.** What it is doing right now - *waiting to happen*,
*running* - used to be repeated there as well as on the sentence, and two lines saying the
same words read as two different claims. State belongs on the sentence, beside the switch and
the run button that change it.

## What a note on a control says

**The act, and how far it reaches. Never the identity of the thing the pointer is already on.**
A note reading *"Switch When Airlock Button is pressed > Sequence 1 off"* spends its whole length
telling somebody which row their pointer is resting on. It says *"Switch off"*, and the switch that
governs a whole system says *"Switch the whole system off, stopping every sequence in it"* - because
scope is the part a column of identical switches cannot show, and the consequence is the part
nobody can see coming.

Where the note earns more than the act, it says what cannot be seen: undo names the change it
would walk back, a copy says what the copy arrives with, and refresh says what reading the ship
again is for. A step's bin says *"Delete this step"* rather than *"Delete step 3"*, because the
number is under the pointer and the distinction worth drawing is against the bin in the tools
above, which takes the whole sequence.

**One line, however long.** A note that wrapped would change shape as the pointer moved along a
row of controls, and a note long enough to need two lines is a note saying more than a note
should.

## Taking it back

Every change is an operation that knows how to reverse itself: the step it removed, the name it
replaced, the trigger it swapped out. Undo **says what it would take back** - *Undo deleting a
step*, not a bare *Undo*. Asking somebody to remember what they last did is asking it of the one
person least able to answer, because they have just made a mistake.

**There is a history per level, not one for the program.** Undo means "take back the last thing I
did to the thing I am looking at", and somebody standing on a system who has just repointed a
button did not do the last thing to any sequence inside it. So a sequence's arrows are for what a
sequence is - its steps, its name, what it does - and the system's are for what starts those
sequences: making a trigger, changing one, pointing it somewhere else, deleting one.

**A system's pair is screen furniture**, in the header beside Back and the switch, because that is
what it is: the way back from what you did to the thing you are looking at. It costs no room there,
where a strip of its own was most of a line spent on two arrows. A sequence's pair stays on its
sentence, among the bin and the switch that act on that same sentence.

**A change made outside the editor is not walked back over.** The host takes a block out of a
trigger when somebody clears that button in the game, and that is a decision rather than a mistake;
putting the old trigger back would undo what they meant and claim a slot they had just taken away.
So every edit is asked whether what it did is still what is there - the same blocks and the same
moment is enough - and the way back quietly stops at the point where somebody else's decision
begins. Nothing fails and nothing is lost: the arrows simply say what they can still do.

*This is also the answer to a question that came up as triggers moved off the sequence's screen:
whether a trigger change could be recorded against the sequence it runs. It could, and it would be
wrong twice - the way back would sit on a screen that no longer shows what was changed, and a
trigger that does one plain thing runs a sequence with no screen at all.*

**Deleting a trigger asks nothing and walks back instead.** It can take a sequence with it - the
one that exists only to be the thing that trigger does - and those steps are on no other screen, so
a bin with no way back would be the one control in the editor that loses work silently. A question
in front of every deletion is how people learn to press through questions; the arrows are the
better answer, and the sequence comes back as the sequence it was, with the number everything
pointing at it named it by.

Two designs were weighed and the second was wrong for a specific reason worth keeping.
Snapshot-and-restore, or replaying a log of forward operations from a base, both avoid writing
a reverse for each edit - which matters, because a reverse only ever runs on the unhappy path.
But **Sprocket is live.** Sequences run while they are edited, and the runner and the open pages
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

**On a sequence both wait for the switch**, because both are edits and a running sequence is not
edited. Which produced the one genuinely awkward detail in here: the commands are registered
whether or not there is anything to walk back, because issuing a command needs the numbering only
a rebuild does, while *whether there is anything to walk back* changes without one - renaming
deliberately does not rebuild. So the build decides whether the command may exist and the tick
decides whether it is offered.

*Deleting a whole sequence is not undoable and asks instead.*

## For the game host: the small window is an opportunity, not only a constraint

One screen at a time, and a layout that assumes no room, was adopted as a floor - the editor
has to work wherever it is embedded, so it may not assume a sidebar plus a detail pane. It
turns out to buy something nobody was aiming for.

**A window small enough to sit off to one side leaves the world visible and usable.** The
player can keep flying, keep walking, keep looking at the airlock they are describing, while
the editor is open beside it. Everything Sprocket is about is a thing in the world, and an
editor that covers the world makes somebody hold it in their head instead.

Which opens a stronger possibility: **picking a block by pointing at it.** Core already says
only *choose one or more entities, here is what is on offer, tell me which*. A list with a
search is one way to answer that. Clicking the actual door is another, and for somebody who
knows exactly which door they mean and not what the game calls it, it is obviously the better
one. The picker's abstraction already permits it - `PickStage` never says "list".

*Not now. Recorded because it should shape the game host from its first sketch rather than be
retrofitted, and because it argues for keeping the window small even when a target could afford
to sprawl.*

## Reordering steps

This host reorders with **a grip at the left of each line**, dragged to where the line
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

There is no way to reorder without a pointer - a real gap for a target that may one day be driven
from a controller.

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

The page says which is which - `StatusRow.SwitchGovernsAll` - because only the page knows. A row
reading it off the indent would be guessing from a layout it was given.
