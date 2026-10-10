# Trigger

A trigger says when a ![](mark:sequence) [sequence](#sequence) runs. It is one line: when something happens, then do one thing. For example, when the airlock's button is pressed, run *Cycle Out*.

To add one, press `+ trigger` under `Triggers` on the ![](mark:logic-system) [logic system](#logic-system)'s screen, and choose `when`, or `every` for one that starts on a set time. Pressing a part of a trigger opens the [picker](#picking) on that part, to change it.

## What starts it

- A ![](mark:signal) [signal](#signal) arriving, such as a button being pressed or a door finishing closing.
- Blocks reaching a state, such as a door being closed. With several blocks, `any one` starts it when the first gets there, and `all of them` when the last one does.
- A set time passing, again and again, such as every minute.

Where the game reports a change, such as a door closing or a block being switched on, the trigger starts at once. For other changes, such as a room's air filling or emptying, the trigger checks the block itself, often enough to catch it. A trigger that starts on a set time chooses that time under `How often`.

## What it does

It runs a sequence or stops one.

A trigger does its one thing at once. Anything that waits, such as closing a door before opening another, belongs in a sequence.

## Which installation it starts

A trigger started by a signal runs only for the ![](mark:installation) [installation](#installation) whose [slot](#slot) holds the block that sent it. So the port airlock's button cycles the port airlock, and the starboard airlock stays as it is.
