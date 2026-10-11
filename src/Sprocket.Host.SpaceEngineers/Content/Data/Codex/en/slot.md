# Slot

A slot is a named place in a ![](mark:logic-system) [logic system](#logic-system), such as *the inner door*. The logic system's [steps](#step) and [triggers](#trigger) use the slot, and each ![](mark:installation) [installation](#installation) fills it with blocks of its own. This is how one logic system can run every airlock on a ship.

## Two kinds

- A ![](mark:block) **block slot** holds the blocks a step acts on or a trigger watches, for example a door, or every light in the airlock.
- A ![](mark:signal) **signal slot** holds a block and the toolbar slot on it that sends the ![](mark:signal) [signal](#signal), for example a button panel and one of its buttons.

## How a slot is made

The blocks [picked](#picking) together for one step, one trigger, or the check of an [if](#if) are one set, and each set is one slot holding all of its blocks. A slot of one block is named after the block, such as *Airlock Inner Door*. A slot of several blocks is named *Slot 1*, *Slot 2* and so on.

- Picking the same blocks again, in any order, uses the same slot.
- Picking exactly the blocks of two or more slots uses those slots, the fewest that make it up.
- Any other set is a slot of its own, so a block can be in more than one slot.

Slots are worked out again after every change. Six lights picked for a step that switches them off are *Slot 1*. Pick lights 1 to 3 for a second step and lights 4 to 6 for a third, and those make two slots: *Slot 1* goes, and the first step names the two smaller slots. Remove the second and third steps, and the slot of all six comes back.

A slot that stays keeps its name, what it is for, and every installation's blocks. Undo takes back a pick together with every change it made to the slots.

## Changing a step's blocks

Where a step's slot is used by that step alone, picking other blocks for the step keeps the slot, with its name, what it is for, and the other installations' blocks. The new blocks fill it for the installation shown, as long as they are of a kind the slot expects. Otherwise the new blocks are a set like any other, and make their slot by the rules above.

Removing the last step or trigger that uses a slot removes the slot. Blocks another installation had in it are set aside for that installation, and offered first when it fills its slots again.

## Filling a slot

The blocks you pick for a step or a trigger fill its slot for the installation chosen under `Showing the slots filled by installation`. Every other installation fills it on its own [installation panel](#installation-panel).

The logic system's screen lists its slots under `Slots`, each with the blocks the installation shown fills it with. Under them, each other installation with something still to do says so, for example *Installation 2: new, fill it*, *Installation 2: changed, fill it again* or *Installation 2: lost a block*.

Press a slot's blocks on the logic system's screen, or a slot on an installation panel, to pick what fills it. Only blocks of a kind the slot expects are offered. If nothing on this [construct](#construct) fits, the list says so.

- A block slot can be filled with one block or several.
- A signal slot needs a block and the toolbar slot on it that sends the signal. That toolbar slot is set up for you, and you are asked first if it already does something else.

## Its name and what it is for

Pressing a slot's name on the logic system's screen opens the slot's page. What is written there, every installation's installation panel shows. It is changed while the logic system is switched off.

- **The heading** is the slot's name. Rename it with the pencil there. Each slot's name is its own.
- **`What it is for`** is a box to write in, with headings, lists, and bold or italic text. `Markup` opens the page on [writing text](#writing).
- **`Expects`** lists the kinds of block the slot takes, for example *Door, Sliding Door*, or `Any block`.

Write them for someone who has never seen your construct. A slot named *Interior Door 2* or *Slot 1* leaves whoever fills it guessing. Rename it *Inner door* and say it is the door on the pressurized side, and they know which door to choose.

## An empty slot

When a sequence reaches a slot that is empty, or whose block is gone, the installation is switched off and its row says why. A trigger whose signal slot is empty can't start, and switches its installation off the same way.
