# Logic system

A logic system is the automation for one machine, like an airlock or an elevator, with all the jobs that go with running that machine. It works within a single [construct](construct), and cannot reach beyond it. You can find an overview of all the logic systems on your construct on the [home screen](home), which you can reach by finding a programmable block in the terminal and pressing its `Automation` button.

A logic system consists of the following parts:

- ![](mark:sequence)&nbsp;[Sequences](sequence) say what happens, step by step.
- ![](mark:trigger)&nbsp;[Triggers](trigger) say when, for example when a button is pressed or a door finishes closing.
- ![](mark:installation)&nbsp;[Installations](installation) say which blocks take part.

The job is kept apart from the blocks, so one logic system can run every airlock on the ship. The [simple airlock](simple-airlock) builds one from the start.

## Its screen

Under its heading, a logic system's screen has the `Selected installation` list. When you have more than one installation, it picks the one you're working on: its blocks and signals show up in the triggers and sequences, and as you make changes in your logic system, this selected installation gets updated directly.

Below the list are four tabs: `Triggers`, `Sequences`, `State` and `Installation panel`, where you design the [installation panel](installation-panel) every installation is filled in on.

A tab whose page holds a problem or a warning carries a mark in the color of the problem or the warning. Each logic system's screen opens on the tab you last left it on, until the world is loaded again.

## Switch off before you change it

A logic system can only be changed while it's switched off, so a half-built sequence never starts by accident. New logic systems are made switched off. The switch sits on the logic system's heading on the home screen, and at the top right of its own screen.

Switching it off stops its triggers from firing, and aborts any running sequences, apart from those started by hand.

![](mark:delete)&nbsp;deletes at once; ![](mark:undo)&nbsp;[undo](undo) brings back what it took.

## Blueprints

To keep a logic system for later, or to use it on another ship, press `Make blueprint`. It saves a copy to the [library](library) as a ![](mark:blueprint)&nbsp;[blueprint](blueprint), and when you change the logic system afterwards, `Update blueprint` brings the copy up to date. Put the blueprint on another construct, and it gets the same logic system, ready for its own blocks.
