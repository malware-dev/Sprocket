# Logic system

A logic system automates one job, for example an airlock, a hangar door or a drill rig. Each one on this [construct](#construct) has its own heading on the [home screen](#home).

## What it is made of

- ![](mark:sequence) [Sequences](#sequence) say what happens, step by step.
- [Triggers](#trigger) say when, for example when a ![](mark:signal) [signal](#signal) arrives, a door finishes closing or a set time passes.
- ![](mark:installation) [Installations](#installation) say which blocks take part, for example the doors and the vent, and the blocks that send its signals.

Because the blocks are kept apart from the job, one logic system can run every airlock on your ship.

## A first airlock

1. On the home screen, press `+ logic system`, and name it *Airlock* on its heading.
2. Under `Sequences`, press `+ sequence`, and name it *Cycle Out*.
3. Press `+ step`, then `task`, [pick](#picking) the inner door, and select the `close` with the clock beside it. The sequence page explains the clock.
4. Add `depressurize` for the vent and `open` for the outer door the same way.
5. Go back. Under `Triggers`, press `+ trigger`, then `when`, pick the airlock's button, and have it run *Cycle Out*.
6. Leave the screen, and switch the logic system on when asked.

## Switch it off to change it

What a logic system does can only be changed while it is switched off, so a half-built sequence never starts by accident. A new one starts switched off. Its switch is on its heading on the home screen, and at the top right of its own screen.

Switching it off stops anything running and leaves the blocks as they are. When you leave a changed logic system that is still off, you are asked whether to switch it on.

Deleting is a change like any other. A sequence's bin, an installation's bin, and the bin at the top of the logic system's own screen delete at once. Undo on the logic system's screens brings back a sequence, and undo on the home screen an installation or a logic system. While the logic system runs, the bins stay in their places but can't be pressed, and say why. Copying a sequence waits for the switch the same way.

## Real blocks, kept as slots

Below the sequences, `Installation panel` shows what every installer reads, laid out as their [installation panel](#installation-panel) shows it: the description, then the slots.

The blocks you pick become [slots](#slot), listed under `Slots`. There are two kinds:

- A ![](mark:block) **block slot**, such as *the inner door*, holds blocks that a sequence acts on or watches.
- A ![](mark:signal) **signal slot**, such as *the button*, holds a block and the toolbar slot on it that sends the signal.

Each slot shows its name, what it is for, and the blocks that fill it. Pressing its blocks picks others, and pressing its name opens the slot's own page, to rename it and say what it is for.

The `Installation` list, at the top of the logic system's screen, selects which installation's blocks the steps and the slots show, and which installation a block you pick fills.

## The description

The description, the first part of `Installation panel`, says what the logic system does and what whoever installs it needs to know. While the logic system is switched off it is a box to write in, headed `Description`, with headings, lists, and bold or italic text, and `Markup` opens the page on [writing text](#writing).

It is shown beside the logic system's blueprint in the library, and at the top of every installation's installation panel. Anyone who receives your blueprint has never seen your construct, so the description and the slots are their only guide.

## Undo and redo

Each logic system keeps one list of the changes made to it, on any of its screens: its own, its sequences', its slots' pages, and its installations' installation panels. The undo arrow on each of those screens takes back the last change on that list, wherever it was made, and the redo arrow makes it again. Both walk the list in the order the changes were made, and the list lasts for the game session.

On the list is every change to its steps, sequences and triggers, its slots, its description and its name, and to its installations' names and the blocks that fill their slots. Adding a trigger is one change, together with the sequence it made and any button it took over. Undo passes over switching on and off, running a sequence by hand, which installation is shown, and how the picker shows its blocks.

Undo opens the screen where the change was: a step's sequence, an installation's installation panel for its blocks, and the logic system's own screen for everything else.

A change to what the logic system does, such as a step, is taken back and made again only while the logic system is switched off, as it was made. An installation's blocks are taken back only while that installation, or the logic system, is switched off. Whatever can be changed while it runs, such as a name, is taken back at any time. Until a change can be taken back, its arrow can't be pressed.

## More than one airlock

On the home screen, press `+ installation` under the logic system, and fill its slots with the second airlock's blocks on its installation panel. Both airlocks then run the same sequences on their own blocks, and a change to a sequence reaches both.

## When something goes wrong

On the home screen, a row lights green while it runs and red when it needs mending. Open it to see why.

If a sequence cannot carry on, for example because a block is gone or a slot is empty, that installation is switched off, and its row says why: *Stopped at step 1 of Cycle Out: Airlock Inner Door was gone.* Put right what it names, for example by filling the slot on the installation panel, and switch the installation on again.

## Keeping and sharing it

`Make blueprint` saves a copy in the [library](#library) as a ![](mark:blueprint) [blueprint](#blueprint). From then on, the same button reads `Update blueprint`, and brings the library's copy up to date. Putting the blueprint on another construct makes a logic system with one installation, whose slots are filled on the installation panel.
