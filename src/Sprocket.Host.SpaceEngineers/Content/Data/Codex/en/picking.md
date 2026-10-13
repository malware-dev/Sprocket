# Picking blocks

The picker is where blocks, and what to do with them, are selected for a [step](#step), a [trigger](#trigger) or a [slot](#slot). Its columns run from left to right: `Blocks`, then what to do with them, then, where what is selected takes options, a column headed by its name that asks for them. The column furthest right describes what is selected. `Done` uses it and goes back.

## Selecting blocks

Press a block to select it, or tick several to select them all.

Type in `Search` to show only the blocks whose name holds what you type, and select a kind to show only blocks of that kind, or a family, such as `Door`, to show every kind in it. The search stays as typed for the screen the picker was opened from, until you leave that screen, so a search for *Airlock* still holds when you pick for the next step.

Blocks the game's toolbar configuration doesn't show are left out. The eye at the end of the `Blocks` heading shows them as well, and pressed again hides them. It stays as you left it, in every picker and every world.

When a slot is being filled, only blocks of the right kind are offered.

## What to do

`What to do` lists what the selected blocks can do, grouped under headings such as `Power` and `Opening`. `Terminal ›`, at the end of the heading, lists instead everything the blocks offer in the terminal, under the terminal's own names, and `‹ Organized` goes back. See [Action](#action).

Each choice carries a mark for its kind:

- ![](mark:action) **An action**, marked with a bolt, is started without waiting for the block to complete it, such as switching a light on.
- ![](mark:awaited) **An action that takes time**, marked with a clock, such as closing a door, can be waited for before the next step begins, or left running while the sequence goes straight on.
- ![](mark:value) **A value**, marked `=?`, is one of the block's properties, to be set or checked, such as a light's `Radius`.
- ![](mark:signal) **A signal**, marked with a square pulse, is something a block sends, such as a button being pressed or a door finishing closing. Signals are offered when picking for a trigger.

## Changing a choice

Pressing a part of a step or a trigger, such as its blocks or its action, opens the same picker, on the column for that part.
