# User stories

Reactor is automation for Space Engineers, meant for people who do not write code.

This is the list of things players have said they want. It is written so that anybody
can read it and say whether we have understood them - if one of these is wrong, or
yours is missing, that is worth telling us.

**Being on this list is a request, not a promise.** The list exists to find out what
Reactor should support. Some of these will be built, some will turn out to cost far
more than they are worth, and some may not fit the way Reactor works at all. Nothing
here is a commitment to implement it, and a story is not less useful for turning out to
be one we cannot serve - knowing what people want is the point, and we would rather
have too many wants written down than discover a missing one late.

Each story has four parts:

- **context** - what they are building
- **want** - what they said, in their own words
- **so that** - why they care
- **done when** - what they would point at to say it works

They describe goals, not steps. "Make this button work the rig" is a story; "add a
sequence, pick the button, choose *is pressed*" is not.

---

## A. One switch, out and back

**Context** A mining rig on a piston arm: drills on the head, a rotor that turns it,
and one button on the cockpit panel.

> "Press it and the arm swings out and starts drilling; press it again and it stops
> and folds back in. I'm not putting two buttons on a rig."

**So that** the control means "out or in" and cannot be pressed the wrong way round -
retracting while the drills are still spinning is how the arm gets wrecked.

**Done when** the same button deploys the rig when it is stowed and stows it when it
is deployed, and the player never has to know which of the two it is about to do.

## B. Match the side I'm about to open

**Context** An airlock on a ship that lands on planets as well as flying in vacuum,
and that occasionally takes a hit and loses pressure inside.

> "It shouldn't suck the room down to vacuum when there's atmosphere right outside -
> and it shouldn't fill it up either, if the ship's already vented. Match whichever
> side I'm opening."

**So that** air is never wasted in either direction, and the airlock is not built on
the assumption that inside is pressurised and outside is not - which is the assumption
that fails on a planet, and fails again the day the hull is breached.

**Done when** the airlock matches the side it is about to open, both on the way out
and on the way in, without being told which situation it is in - including when the
ship's own interior has no air left.

## C. Call it off, and put it back

**Context** A ship hangar with an airtight inner door to the corridor and a big door
to space. Emptying a room that size takes a couple of minutes.

> "Getting the hangar ready to launch takes a while - inner doors locked, air pumped
> out. If somebody's still walking around in there I need to call it off, and I need
> the air back and the doors unlocked. Not just stopped."

**So that** calling it off is actually safe. A stop that leaves the hangar half empty
with its inner doors locked is worse than having no stop at all, because it strands
whoever is inside.

**Done when** one press ends the preparation and leaves the hangar as it was before it
started - air back, inner doors unlocked - and it can be started again afterwards.

## D. Everything red, then exactly as it was

**Context** The interior lighting of a base - two dozen lights tuned over several
evenings, no two quite the same, and a couple left deliberately off.

> "When we're attacked I want every light red. And when it's over I want my lighting
> back - I spent a long time on that, and no two of those lights are the same."

**So that** using the alarm does not cost the player the work they put into the
lighting. A "normal" that means one flat white is not normal; it is a second and worse
alarm state.

**Done when** after the alarm has come and gone every light is exactly as it was - its
own colour, its own brightness, and still off if it was off - without the player
having listed any of it.

## E. Three doors, one procedure

**Context** A station with three airlocks, all built the same way over a few evenings.

> "I've built the same airlock cycle three times and I want to fix it in one place."

**So that** a fix or an improvement is made once rather than three times, and the
three do not silently drift apart.

**Done when** changing the shared procedure changes the behaviour of all three doors,
and each still acts on its own blocks.

## F. Keep the hydrogen above half

**Context** A base with a hydrogen tank feeding a refuelling pad, and a generator that
can top it up from ice.

> "If it gets low, run the generator until it's full again."

**So that** the tank is simply never empty, without anybody watching it.

**Done when** the generator runs on its own as the level falls and stops once it has
recovered, with no button pressed.

## G. Tell me before it runs out

**Context** An oxygen tank on a small outpost that drains while nobody is there.

> "Warn me when the tank drops under a fifth."

**So that** the warning arrives while there is still time to do something about it.

**Done when** the warning happens once as the level crosses the line rather than
repeatedly while it sits below it, and it reaches the player even when they are
nowhere near the tank.

## H. All of it in one place

**Context** A corridor with a sensor and a few lights, on a ship that already has a
dozen other things set up in Reactor.

> "I could wire the sensor straight to the lights like I always have. But then half my
> automation's in Reactor and half of it's in block settings, and when something goes
> odd I don't know where to look."

**So that** there is one place to look, one way to read what the ship does, and only
one place to go searching when something misbehaves.

**Done when** doing it in Reactor is no more work than doing it in the terminal, so
nobody has a reason to keep it elsewhere - and a person who has never seen Reactor
builds it unaided.

## I. Two airlocks that don't confuse each other

**Context** A station with a front airlock for people and a bigger one onto the
hangar.

> "They should each know what mode they're in, without me having to name everything
> twice."

**So that** building a second one is copying the first, not inventing a naming scheme.

**Done when** both airlocks have a "vacuum mode" and neither can read or disturb the
other's.

## J. Where did everything go?

**Context** A ship whose automation has grown over months - doors, lights, tanks,
hangar, drills.

> "I've got forty sequences and I can't find the one for the hangar."

**So that** a ship's automation stays workable as it grows, rather than only while it
is small.

**Done when** the hangar's sequences can be found without reading the others.

## K. Give it to a friend

**Context** An airlock somebody has built and is pleased with, and a friend who wants
one on a ship with different blocks and different names.

> "I want to send my airlock to somebody else and have it work on their ship."

**So that** good automation can be shared the way blueprints are, instead of being
rebuilt from a screenshot.

**Done when** the recipient is asked which blocks fill each slot - inner door, outer
door, vent, button - and nothing else, and it works.

## L. Add a condition to something I already built

**Context** A sequence written a week ago, before the hangar was built next to it.

> "Now I want it to only happen when the hangar is sealed."

**So that** a sequence can be corrected in place as the ship changes around it, rather
than being deleted and rebuilt to add one clause.

**Done when** the condition is added to the existing sequence without retracing the
steps that built it.

## M. Nothing happened

**Context** Any sequence at all, on the day something it depends on stops working.

> "I pressed the button and nothing happened."

**So that** the player can tell the difference between working, waiting and stuck.

**Done when** the answer to "why didn't it happen?" is on screen without reading the
sequence, and names the thing that is blocking.

## N. The alarm stops when I say so

**Context** A base with a siren and red warning lights.

> "Siren and red lights when we're attacked, and it keeps going until I press the
> acknowledge button."

**So that** an alarm cannot be missed by being brief, and silencing it is a deliberate
act by a person rather than a timer running out.

**Done when** the alarm stays on until acknowledged, and can be set off again
afterwards.

## O. A screen that tells me how things are

**Context** A bridge with a couple of LCD panels, on a ship whose tanks, doors, hangar
and drills all have a state worth knowing from the chair.

> "I want the big screen to show me the ship at a glance - tank levels, whether the
> hangar's sealed, what the drills are doing. I'm not learning C# to get a progress
> bar."

**So that** the state of the ship is knowable without walking to each block and
opening its terminal, and without the player having to become a programmer to find
out.

**Done when** a player can put a heading, some lines of live text and a progress bar
on a panel, say what each one reads from, and never type code.

## P. One door gone, not the whole airlock

**Context** A ship that gets shot at. A cannon round takes out the airlock's inner
door, and the player welds a new one into the same hole.

> "A cannon took out my inner door. I welded a new one in and the airlock's dead. I'm
> not rebuilding the whole thing because of one door."

**So that** battle damage costs a repair job and nothing more. Getting hit is ordinary,
and automation that has to be rebuilt after every fight is automation people stop
using.

**Done when** pointing at the replacement block once puts everything back - not once
per sequence and not once per step - and nothing else about the automation is touched.

## Q. I pressed it twice

**Context** The airlock button by the inner door, and somebody in a hurry.

> "I pressed it twice because nothing seemed to be happening, and then the doors went
> mad."

**So that** an impatient press cannot put the airlock into a state nobody designed.
People press a button again when they are not sure it registered, and that is not
misuse.

**Done when** a second press while the first is still running cannot produce anything
the first press would not have, and the player can see that it is already running.

## R. Two things fighting over my door

**Context** An airlock whose inner door is used by the cycle, and also by a
"seal everything" sequence written a month later.

> "One of them keeps undoing what the other one did. The door just sits there opening
> and closing."

**So that** automation built a piece at a time does not turn into two programs
wrestling, which is the point where people delete all of it and go back to doing it by
hand.

**Done when** the player is shown that two sequences want different things from the
same block, and can say which one wins, without reading both of them line by line.

## S. Leave me alone, I'm building

**Context** A hangar being repaired after a crash, with its automation still live.

> "Every time I get the door open to weld, the thing shuts it on me."

**So that** repairing or extending a ship does not mean fighting its own automation,
and the way out is not deleting the sequences and rebuilding them afterwards.

**Done when** the automation for one part of the ship can be put to sleep and woken
with one action, it is obvious at a glance that it is asleep, and nothing about it is
lost while it sleeps.

## T. When my miner docks

**Context** A station with a docking pad, and three different mining ships that use it.

> "When it docks I want the ore pulled out and the tanks topped up. And I fly three
> miners, so I'd rather not set that up three times."

**So that** the automation belongs to the station and works for whatever ties up to it,
instead of being rebuilt for each ship and then again for the one a friend brings.

**Done when** the sequence can act on blocks that were not there when it was written,
and stops cleanly the moment the ship undocks.

## U. I renamed everything

**Context** A ship whose blocks have just been tidied up, "Door 27" becoming "Hangar
Inner Door" and so on down the list.

> "I spent an evening naming everything properly and I'm dreading finding out what it
> broke."

**So that** keeping a ship tidy is never punished. Renaming is how people make a large
ship comprehensible, and automation that breaks when they do it simply teaches them not
to.

**Done when** renaming any block changes nothing about what runs, and the sequences
read back afterwards with the new names in them.

## V. Let me watch it work first

**Context** A newly built airlock sequence, and a player who does not yet trust it.

> "I want to see it go through once before I'm standing in there with the outer door
> about to open."

**So that** the first time a sequence runs is not also the first time it matters.
Automation that has to be trusted before it can be tested does not get trusted.

**Done when** a sequence can be set off deliberately, watched as it goes, and stopped,
without waiting for the thing that normally triggers it.

## W. Drive the crane from the seat

**Context** A crane on a rotor and a pair of pistons, with a cockpit at its base.

> "I want to sit in it and drive it. Lean the stick and the arm swings, pull back and it
> lifts. Not click buttons in a terminal while squinting at the thing through a window."

**So that** operating the crane is a thing you get good at, the way flying is. Moving a
load by feel is most of why anybody builds a crane, and clicking a rotor on and off in a
terminal is not that.

**Done when** sitting in the cockpit moves the crane directly - further over means faster
- and the crane still behaves itself when nobody is sitting in it.
