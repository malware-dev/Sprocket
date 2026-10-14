# Signal

A signal is what a block sends to start a [trigger](#trigger). It comes from one of two places: a slot on the block's toolbar, or a change in the block itself. Once it arrives, both kinds work the same way.

To start a trigger with one, add the trigger with `when`, pick the block, and select the signal under `What happens`. Both kinds carry the ![](mark:signal) **signal mark** wherever they are listed.

## From a toolbar slot

Any block with a toolbar can send a signal from it, for example:

- a button panel, when one of its buttons is pressed,
- a sensor, when it detects something or stops detecting it,
- a timer block, when it goes off,
- a cockpit, when a slot on its toolbar is pressed.

Each button or toolbar slot sends a signal of its own, so one block can start several triggers.

### Setting one up

The signal reaches a ![](mark:logic-system)&nbsp;[logic system](#logic-system) through a ![](mark:signal)&nbsp;[signal slot](#slot), which holds the block and the toolbar slot that sends it. When the signal slot is filled, that toolbar slot is set up for you, and shows `Automation Signal`. If it already does something else, you are asked whether to take it over, replacing what it did, or leave it alone. Undoing the take-over gives the toolbar slot back what it did, as long as nothing else has been put there since.

### When the toolbar slot is changed

If the toolbar slot is cleared, or given something else to do, the signal stops arriving, and the trigger says so: *Airlock Button's button was cleared, so it can't start this.* Fill the signal slot again to set the toolbar slot up again.

## From a change in the block

A block also sends a signal when something about it changes, and the automation hears it from the block itself. For example:

- a door, when it finishes closing or finishes opening,
- a seat, when it becomes occupied or becomes empty,
- a block, when it starts or stops working, or turns on or off.

The trigger then reads, for example, `When Airlock Inner Door finishes closing`. The block fills a ![](mark:block) **block slot**, the same kind a step's blocks fill, and its toolbar stays as it is.

## When a signal arrives

A signal starts its trigger only for the ![](mark:installation)&nbsp;[installation](#installation) whose slot holds the block, so the port airlock's button cycles only the port airlock.

A signal that arrives while the ![](mark:sequence)&nbsp;[sequence](#sequence) it starts is already running is skipped, and the sequence counts it: `skipped 1x while busy`. One that arrives while the installation is switched off is skipped as well, and only those from a toolbar slot are counted: `skipped 1x while off`.
