# Changing hands

A [construct](#construct)'s automation belongs to the construct's owners: the players who own the most blocks on it, counted over all its grids. Where players own the same number, each of them is an owner. When a ship is captured or handed to another player, its automation changes hands with it.

## Who can open it

Only the construct's owners can open its automation. Anyone else who presses `Automation` in a programmable block's terminal, faction members included, is told `Only the owners of {construct} can open its automation.` An unowned construct opens for anyone.

## The blocks it can use

The automation uses the blocks its owners may use under the game's ownership and sharing: the blocks they own, and those shared with their faction or with everyone. Any other block, such as a door another player built and kept to themselves, reads to the automation as gone, as a destroyed block does. It comes back by itself once it is shared.

## When the construct changes hands

Where the construct passes to a player it was shared with, its automation keeps running. A ship shared with its owner's faction and handed to another member of that faction carries on as before.

Where it passes outside its sharing, in a capture or a hand-over to a player it was kept from, the automation pauses. Everything it does stops: its buttons go dead, and a ![](mark:sequence) [sequence](#sequence) part way through holds at its step. It stays paused when the world is loaded again, until one of the new owners resumes it. The game asks the new owner of a programmable block to recompile its script in the same way; the automation keeps everything as it was and waits for one press.

## The Paused screen

While the automation is paused, opening it shows the `Paused` screen first. It says who the construct passed from and to, how many ![](mark:logic-system) [logic systems](#logic-system) it holds, and how many blocks are out of its reach, those the new owners may not use, or back in its reach, those the old owners could not.

Three buttons lead on:

- `Resume` starts everything again as it was, and goes on to the [home screen](#home).
- `Leave paused` keeps it stopped and goes on to the home screen, where the automation can be read, and changed as usual, before it runs. A banner over every screen reads `Paused when {construct} changed hands.`, and its `Review` button opens the `Paused` screen again.
- `Remove all` asks first, then removes every logic system on the construct. Undo on the home screen brings each one back.
