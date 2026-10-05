A slot is a named place in a [logic system](#logic-system), such as *the inner door*. The logic system's [steps](#step) and [triggers](#trigger) use the slot, and each [installation](#installation) fills it with blocks of its own. This is how one logic system can run every airlock on a ship.

## Two kinds

- A **block slot** holds the blocks a step acts on or watches, for example a door, or every light in the airlock.
- A **signal slot** holds a block and the toolbar slot on it that sends the [signal](#signal), for example a button panel and one of its buttons.

## Filling a slot

Slots are filled on the installation's [configuration panel](#configuration-panel). Each slot shows its name and what it is for, as whoever made the logic system wrote them.

Press a slot to [pick](#picking) what fills it. Only blocks of the right kind are offered. A slot made from a door takes doors, and one made from a vent takes vents. If nothing on this [construct](#construct) fits, the list says so.

- A block slot can be filled with one block or several.
- A signal slot needs a block and the toolbar slot on it that sends the signal. That toolbar slot is set up for you, and you are asked first if it already does something else.

The heading over the slots counts how many still need a block. A slot whose block was lost says so, and names the block.

## How a slot is made

When you pick a block for a step or a trigger, a slot is made and named after the block, such as *Airlock Inner Door*. Picking the same block again uses the same slot, so a block that many steps use is still one slot, filled once.

The block you picked fills the slot for the installation chosen under `Using the blocks of`. Every other installation fills it on its own configuration panel.

> [!WARNING]
> When you remove the last step or trigger that uses a given block or signal, its slot disappears from the configuration panel, with its name and what it is for. Picking another block in its place makes a new slot, which every installation has to fill again. To change which block a slot holds, fill it again on the configuration panel instead.

## Name and description

Each slot's name, and a line saying what it is for, are set once, for every installation, with `Edit configuration panel`.

## An empty slot

When a sequence reaches a slot that is empty, or whose block is gone, the installation is switched off and its row says why.
