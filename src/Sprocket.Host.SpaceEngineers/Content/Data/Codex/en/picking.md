# Picking blocks

The picker is where blocks, and what to do with them, are chosen for a [step](#step), a [trigger](#trigger) or a [slot](#slot). Its columns run from left to right: `Blocks`, then what to do with them, then, where what is chosen takes options, a column headed by its name that asks for them. The column furthest right describes what is chosen. `Done` uses it and goes back.

## Choosing blocks

Press a block to choose it, or tick several to choose them all.

Type in `Search` to show only the blocks whose name holds what you type, and choose a kind to show only blocks of that kind. The search stays as typed for the screen the picker was opened from, until you leave that screen, so a search for *Airlock* still holds when you pick for the next step.

When a slot is being filled, only blocks of the right kind are offered.

## What to do

Each choice carries a mark for its kind:

- ![](mark:action) **An action**, marked with a bolt, is started without waiting for the block to complete it, such as switching a light on.
- ![](mark:awaited) **An action that takes time**, marked with a clock, such as closing a door, is waited for before the next step begins.
- ![](mark:value) **A value**, marked `=?`, is one of the block's properties, to be set or checked, such as a light's `Radius`.

## Changing a choice

Pressing a part of a step or a trigger, such as its blocks or its action, opens the same picker, on the column for that part.
