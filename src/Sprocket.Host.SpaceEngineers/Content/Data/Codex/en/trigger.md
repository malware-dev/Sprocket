# Trigger

A trigger says when a ![](mark:sequence) [sequence](#sequence) runs. It is one line: when something happens, then do one thing. For example, when the airlock's button is pressed, run *Cycle Out*.

To add one, press `+ when` under `Triggers` on the ![](mark:logic-system) [logic system](#logic-system)'s screen, or `+ every` for one that starts on a set time. Pressing a part of a trigger opens the [picker](#picking) on that part, to change it.

## What starts it

- A ![](mark:signal) [signal](#signal) arriving, such as a button being pressed.
- Blocks reaching a state, such as a door finishing closing. With several blocks, `any one` starts it when the first gets there, and `all of them` when the last one does.
- A set time passing, again and again, such as every minute.

Some states, such as a room's air or a battery's charge, are found by looking at the block. How often the trigger looks is chosen under `How often`, and the fast choices say what they cost.

## What it does

It runs a sequence, stops one, or does `A task on blocks`, such as switching the airlock's lights on.

A trigger does its one thing at once. Anything that waits, such as closing a door before opening another, belongs in a sequence.

## Which installation it starts

A trigger started by a signal runs only for the ![](mark:installation) [installation](#installation) whose signal [slot](#slot) holds that signal. So the port airlock's button cycles the port airlock, and the starboard airlock stays as it is.
