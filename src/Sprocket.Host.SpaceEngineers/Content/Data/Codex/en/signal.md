# Signal

A signal is what a block sends from its toolbar to start a [trigger](#trigger). Any block with a toolbar can send one, for example:

- a button panel, when one of its buttons is pressed,
- a sensor, when it detects something or stops detecting it,
- a timer block, when it goes off,
- an event controller, when its condition is met or no longer met,
- a cockpit, when a slot on its toolbar is pressed.

Each button or toolbar slot sends a signal of its own, so one block can start several triggers.

## Setting one up

A signal reaches a [logic system](#logic-system) through a signal [slot](#slot), which holds the block and the toolbar slot that sends it. When the signal slot is filled, that toolbar slot is set up for you, and shows `Automation Signal`. If it already does something else, you are asked whether to take it over, replacing what it did, or leave it alone.

## When a signal arrives

A signal starts its trigger only for the [installation](#installation) whose signal slot holds it, so the port airlock's button cycles only the port airlock. A signal that arrives while the installation is switched off, or while the ![](mark:sequence) [sequence](#sequence) it starts is already running, is skipped. The sequence counts how often that happens.

## When the toolbar slot is changed

If the toolbar slot is cleared, or given something else to do, the signal stops arriving, and the trigger says so: *Airlock Button's button was cleared, so it can't start this.* Fill the signal slot again to set the toolbar slot up again.
