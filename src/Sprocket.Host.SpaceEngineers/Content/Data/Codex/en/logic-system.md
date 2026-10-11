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
3. Press `+ step`, then `task`, [pick](#picking) the inner door, and choose the `close` with the clock beside it. The sequence page explains the clock.
4. Add `depressurize` for the vent and `open` for the outer door the same way.
5. Go back. Under `Triggers`, press `+ trigger`, then `when`, pick the airlock's button, and have it run *Cycle Out*.
6. Leave the screen, and switch the logic system on when asked.

## Switch it off to change it

A logic system can only be changed while it is switched off, so a half-built sequence never starts by accident. A new one starts switched off. Its switch is on its heading on the home screen, and at the top right of its own screen.

Switching it off stops anything running and leaves the blocks as they are. When you leave a changed logic system that is still off, you are asked whether to switch it on.

## Real blocks, kept as slots

The blocks you pick become [slots](#slot), listed under `Slots`, below the sequences. There are two kinds:

- A ![](mark:block) **block slot**, such as *the inner door*, holds blocks that a sequence acts on or watches.
- A ![](mark:signal) **signal slot**, such as *the button*, holds a block and the toolbar slot on it that sends the signal.

Each slot shows its name, what it is for, and the blocks that fill it. Pressing its blocks picks others, and pressing its name opens the slot's own page, to rename it and say what it is for.

`Showing the slots filled by installation`, at the top of the logic system's screen, chooses which installation's blocks the steps and the slots show, and which installation a block you pick fills.

## The description

`Description`, at the top of the logic system's screen, says what the logic system does and what whoever installs it needs to know. While the logic system is switched off it is a box to write in, with headings, lists, and bold or italic text, and `Markup` opens the page on [writing text](#writing).

It is shown beside the logic system's blueprint in the library, and at the top of every installation's [configuration panel](#configuration-panel). Anyone who receives your blueprint has never seen your construct, so the description and the slots are their only guide.

## More than one airlock

On the home screen, press `+ installation` under the logic system, and fill its slots with the second airlock's blocks on its configuration panel. Both airlocks then run the same sequences on their own blocks, and a change to a sequence reaches both.

## When something goes wrong

On the home screen, a row lights green while it runs and red when it needs mending. Open it to see why.

If a sequence cannot carry on, for example because a block is gone or a slot is empty, that installation is switched off, and its row says why: *Stopped at step 1 of Cycle Out: Airlock Inner Door was gone.* Put right what it names, for example by filling the slot on the configuration panel, and switch the installation on again.

## Keeping and sharing it

`Make blueprint` saves a copy in the [library](#library) as a ![](mark:blueprint) [blueprint](#blueprint). From then on, the same button reads `Update blueprint`, and brings the library's copy up to date. Putting the blueprint on another construct makes a logic system with one installation, whose slots are filled on the configuration panel.
