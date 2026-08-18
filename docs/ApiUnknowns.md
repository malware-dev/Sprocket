# What we do not know about the game yet

Internal. A register of questions about the Space Engineers API that block requirements
from being settled. They are gathered here because the same ones keep surfacing while
reasoning about stories, and because guessing at them is how a design acquires
assumptions nobody remembers making.

None of these can be answered on the machine this is being written on. Each needs
either the game, its API documentation, or an existing mod that already does the thing.

An answer belongs in Design.md once it is known. This file only holds the question.

---

### 1. How a block event reaches us

What does Reactor actually get told when a button is pressed or a door finishes
opening? Are these events we can subscribe to, or must the world be polled and edges
worked out by comparing samples?

*Gates* the whole trigger model, and R4 specifically: whether Reactor can know that a
button was pressed when nothing was listening for it.

### 2. Whether a level is readable as a number

A vent's oxygen level, a tank's fill, a battery's charge. Is any of that available as a
value, or only as the text the terminal displays?

*Gates* R5 and R6, and with them stories B, F and G, and the progress bar in O.

### 3. Whether a block's appearance can be read, not only set

Setting a light's colour is presumably straightforward. Reading back what colour it is
now, and whether it is on, is the half D needs and the half nobody normally asks for.

*Gates* D entirely. If this cannot be read, D cannot be served at any price.

### 4. Whether a person can be told something

Can a mod put a notice on a player's screen, or make a sound that carries beyond the
room? "Warn me" needs to reach somebody who is not looking at the tank.

*Gates* G, and the interrupting half of decision 7.

### 5. What can be drawn on an LCD

Text is presumably fine. Headings, bars and graphs need to know what a mod may draw and
how often it may update without cost.

*Gates* O.

### 6. Whether docking is observable, and what it brings with it

When a ship connects, does Reactor learn about it, and can it reach the blocks on the
other grid? Does it learn when the ship leaves?

*Gates* T entirely, and with it the run-time half of decision 8.

### 7. Where the program can live

It must survive losing any single block, or Reactor loses to vanilla on the one thing it
should win: a cannon through the timer block should not cost the automation. Where can a
mod store data such that it belongs to the construct rather than to a block?

*Gates* the storage constraint in Design.md, and therefore P.

### 8. Whether a replacement block can be recognised

A rebuilt block is a new entity with a new identity. The signal we would use is where it
sits and what kind it is: same kind, same place on the construct, therefore the
replacement.

So the question is narrower than it was. Does the game give a mod a block's position on
the construct, in a form that survives the construct moving, being welded onto, or being
cut in half? And can a mod notice a block being placed at all, or must it look?

*Gates* the healing half of R40. Without it, re-pointing by hand is the whole answer,
which is a worse day for the player but not a broken one.

### 9. Whether being attacked is reported

Story N assumes the game can tell a block that the base is under attack. It may be that
players do this with sensors, turret state, or block damage, and that "under attack" is
not a thing at all.

*Gates* N, which otherwise needs nothing new.

### 10. Whether a change can happen without us ever seeing it

Blocks run on update tiers, and the tier is not ours to choose. If a door opens and shuts
again between two of our updates, did anything tell us it happened, or is that event
simply gone?

This decides whether Reactor is watching a world or being told about one, and the two
produce quite different runtimes. It also decides whether "nothing happened" can ever
have the answer "it did happen, we were not looking".

*Gates* T2 and T3, the check-rate question inside decision 2, and part of M.

### 11. What a cockpit's controls actually look like from a mod

Can a mod see what somebody sitting in a cockpit is doing, and in what shape? The three
kinds of input are not alike:

- **movement** - forward, back, up, down. Held keys, so probably on or off with no
  magnitude at all.
- **rotation** - the mouse, which reports movement since the last frame rather than a
  position. A crane's swing is this one, which is unfortunate, because a delta stream fits
  nothing in the language.
- **roll** - held keys again.

Also: can it be read without taking the controls away from the ship the cockpit would
otherwise be flying?

*Gates* W entirely, and decides how much of it can be served. If controls are readable as
held-or-not, W is ordinary triggers and conditions and needs almost nothing new, but gives
stepped control rather than the proportional control the story asks for. If nothing is
readable, W cannot be served at all.

### 12. What happens to a mod's data when constructs merge, split or are copied

Constructs are welded into one with merge blocks and cut apart again, get pasted from
blueprints, and get welded up from projections. (Merging is not docking - a merge block
joins two constructs structurally and is indistinguishable from a repair. Docking is
connectors, which is a different question entirely.) Each of those makes or unmakes a construct, and Reactor's
program is attached to one.

Does a mod get told when a grid merges or splits, and can it tell which of the two the
data should follow? Does anything a mod stored survive being copied to a blueprint and
pasted back? Is a projection-welded ship a copy in that sense, or a new thing with nothing
attached?

*Gates* R41, and the healing half of R40 for the copied case. Merge blocks in particular
are common enough that guessing wrong here would be found late and cost a lot.

