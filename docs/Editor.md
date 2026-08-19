# The editor

Internal, and in progress. What Reactor presents and how somebody moves through it.
[Design.md](Design.md) holds the model; this holds the interface. Where they disagree, the
model is right and this is out of date.

Requirements are referred to by number, from [Requirements.md](Requirements.md).

## The root

What you see when you open Reactor is **the machines on this ship**, not a list of
sequences. A player did not build a procedure, they built an airlock, and that is the thing
they came looking for.

A system is what a machine *is*. The ones on the ship are what they *have*.

```
Airlock                        on this ship: 2
   Airlock 1                   cycling - waiting for the inner door
   Port Airlock                idle
   + another

Drill Rig                      stowed
   + another

   + Build something new
   ↓ Use one somebody made
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
yours - so **Use one somebody made** is not a secondary path.

**Build something new is named after the outcome and behaves verb-first.** It does not open
an empty container asking to be named. It goes straight to *what should set it off?*, builds
the sequence, and asks what the machine is called once there is a machine to call something.
The label describes where you end up; the flow never asks anybody to declare a thing before
they have done anything.

That is a sequencing decision, not a vocabulary one. *System* is an ordinary word and the
audience can read it.

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
- Where the **ship browser** sits now that the root is machines rather than sequences.
- Whether timers, if they are a thing distinct from *every so often*, live here too.
