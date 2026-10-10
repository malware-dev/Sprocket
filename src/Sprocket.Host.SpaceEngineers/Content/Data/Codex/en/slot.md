# Slot

A slot is a named place in a ![](mark:logic-system) [logic system](#logic-system), such as *the inner door*. The logic system's [steps](#step) and [triggers](#trigger) use the slot, and each ![](mark:installation) [installation](#installation) fills it with blocks of its own. This is how one logic system can run every airlock on a ship.

## Two kinds

- A ![](mark:block) **block slot** holds the blocks a step acts on or a trigger watches, for example a door, or every light in the airlock.
- A ![](mark:signal) **signal slot** holds a block and the toolbar slot on it that sends the ![](mark:signal) [signal](#signal), for example a button panel and one of its buttons.

## Filling a slot

Slots are filled on the installation's [configuration panel](#configuration-panel). Each slot shows its name and what it is for, as whoever made the logic system wrote them.

Press a slot to [pick](#picking) what fills it. Only blocks of the right kind are offered. A slot made from a door takes doors, and one made from a vent takes vents. If nothing on this [construct](#construct) fits, the list says so.

- A block slot can be filled with one block or several.
- A signal slot needs a block and the toolbar slot on it that sends the signal. That toolbar slot is set up for you, and you are asked first if it already does something else.

The heading over the slots counts how many still need a block. A slot whose block was lost says so, and names the block.

## How a slot is made

When you pick a block for a step or a trigger, a slot is made and named after the block, such as *Airlock Inner Door*. Picking the same block again uses the same slot, so a block that many steps use is still one slot, filled once.

The block you picked fills the slot for the installation chosen under `Showing the slots filled by installation`. Every other installation fills it on its own configuration panel.

::: warning
When you remove the last step or trigger that uses a given block or signal, its slot disappears from the configuration panel, with its name and what it is for. Picking another block in its place makes a new slot, which every installation has to fill again. So does picking the same block again: the new slot takes the block's name, and its description starts empty. To change which block a slot holds, fill it again on the configuration panel instead.
:::

## Name and description

Each slot's name, and a line saying what it is for, are set once, for every installation, with `Edit configuration panel`.

## An empty slot

When a sequence reaches a slot that is empty, or whose block is gone, the installation is switched off and its row says why. A trigger whose signal slot is empty can't start, and switches its installation off the same way.
