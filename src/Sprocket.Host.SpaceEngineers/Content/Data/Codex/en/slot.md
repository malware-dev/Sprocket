# Slot

For one ![](mark:logic-system)&nbsp;[logic system](logic-system) to run every airlock on a ship, its [steps](step) and [triggers](trigger) can't name one airlock's doors. They name slots instead: named places like *the inner door*, which each ![](mark:installation)&nbsp;[installation](installation) fills with blocks of its own.

- A ![](mark:block)&nbsp;**block slot** holds the blocks a step acts on or a trigger watches, like a door, or every light in the airlock.
- A ![](mark:signal)&nbsp;**signal slot** holds a block and the toolbar slot on it that sends the [signal](signal), like a button panel and one of its buttons.

The blocks you select fill the slots for the installation selected under `Installation`. Every other installation fills them on its own [installation panel](installation-panel).

## How slots are made

You don't make slots yourself. Each time you [select](selector) blocks for a step, a trigger or an [if](if), the blocks you selected together are a group, and the logic system makes its slots from those groups:

- **The same group twice is one slot.** A step that closes the inner door and another that opens it both use *Airlock Inner Door*.
- **A group made of other slots uses them.** Where the inner and outer doors each have a slot already, a step that closes both doors uses those two slots, and makes no new one.
- **Any other group is a slot of its own.** A step that switches off both the inner door and the airlock's light, where the light has no slot yet, makes *Slot 1* of the two. The inner door is then in two slots, and each installation fills both.

A slot of one block takes the block's name, and a slot of several is numbered.

A slot takes one type of block: the nearest type its blocks all share, by the game's own types. One interior light, or several, make a slot for `Interior light`. An interior light and a spotlight make a slot for `Light`, which takes either. A light and a door make one for `Block that switches on and off`, and blocks that share nothing nearer make a slot that takes any block. The slot's page shows its type under `Expects`.

Because a slot takes a type rather than one exact block, a slot made from a large door takes small doors too, and a ![](mark:blueprint)&nbsp;[blueprint](blueprint) made on a large grid installs on a small one.

The logic system works its slots out again after every change, so they can split and merge as you build. Six lights switched off by one step make *Slot 1*. Add a step that turns lights 1 to 3 red and one that turns lights 4 to 6 blue, and those two groups become slots. Together they're exactly the six lights, so the first step now uses both, and *Slot 1* goes. Delete the red and blue steps, and *Slot 1* comes back.

A slot that stays keeps its name, what it is for, and every installation's blocks. So does a slot used by only one step or trigger when you select other blocks for it, as long as the new ones are of the type the slot takes.

## Its name and what it is for

A slot's name and its `What it is for` show on every installation panel, so write them for someone who has never seen your construct. *Slot 1* leaves them guessing. *Inner door*, with a line saying it's the door on the pressurized side, tells them which door to select.
