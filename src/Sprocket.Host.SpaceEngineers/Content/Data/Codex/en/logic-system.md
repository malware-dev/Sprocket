# Logic system

A logic system is the automation for one machine, like an airlock or an elevator, with all the jobs that go with running that machine. It works within a single [construct](construct), and cannot reach beyond it. You can find an overview of all the logic systems on your construct on the [home screen](home), which you can reach by finding a programmable block in the terminal and pressing its `Automation` button.

A logic system consists of the following parts:

- ![](mark:sequence)&nbsp;[Sequences](sequence) say what happens, step by step.
- ![](mark:trigger)&nbsp;[Triggers](trigger) say when, for example when a button is pressed or a door finishes closing.
- ![](mark:installation)&nbsp;[Installations](installation) say which blocks take part.

The job is kept apart from the blocks, so one logic system can run every airlock on the ship. The [simple airlock](simple-airlock) builds one from the start.

## Switch off before you change it

A logic system can only be changed while it's switched off, so a half-built sequence never starts by accident. New logic systems are made switched off. The switch sits on the logic system's heading on the home screen, and at the top right of its own screen.

Switching it off stops its triggers from firing, and aborts any running sequences, apart from those started by hand.

![](mark:delete)&nbsp;deletes at once; undo brings back what it took.

## The installation panel

Below the sequences is what everyone installing this logic system sees on their own [panel](installation-panel): a description, then the slots.

To help people understand what your logic system is for, or just to remind yourself, consider writing a description. Whoever receives your blueprint has never seen your construct, so the description and the slots are all they have to go on. The description is written while the logic system is off, and can hold [rich text](rich-text).

Each set of blocks you select for a step or a trigger becomes a [slot](slot). A slot of one block takes that block's name; a slot of several is numbered. A ![](mark:block)&nbsp;**block slot**, like *the inner door*, holds the blocks a sequence acts on or watches. A ![](mark:signal)&nbsp;**signal slot**, like *the button*, holds a block and the toolbar slot on it that sends the [signal](signal).

When you have more than one installation, the `Installation` list at the top of the screen selects the one you're working on: its blocks and signals show up in the triggers and sequences, and as you make changes in your logic system, this selected installation gets updated directly.

## More than one airlock

On the home screen, press `+ installation` under the logic system and fill its slots with the second airlock's blocks. Both airlocks then run the same sequences on their own blocks, and a change to a sequence reaches both.

## When something goes wrong

The home screen will tell you when something goes wrong with a logic system. If a sequence can't carry on, for example because a block was lost or a slot is empty, that installation switches off and its row says why:

*Stopped at step 1 of Cycle Out: Airlock Inner Door was lost.*

Put right what it names, then switch the installation on again.

## Blueprints

To keep a logic system for later, or to use it on another ship, press `Make blueprint`. It saves a copy to the [library](library) as a ![](mark:blueprint)&nbsp;[blueprint](blueprint), and when you change the logic system afterwards, `Update blueprint` brings the copy up to date. Put the blueprint on another construct, and it gets the same logic system, ready for its own blocks.
