# Signal

A signal is what a block sends to fire a [trigger](trigger). It comes either from a slot on the block's toolbar, or from a change in the block itself, and once it arrives, the two work the same way.

## From a toolbar slot

Any block with a toolbar can send signals from it, like a button panel when a button is pressed, or a sensor when it detects something. Each toolbar slot sends a signal of its own, so one block can fire several triggers. You choose the slot in the [signal selector](selector).

The signal reaches the ![](mark:logic-system)&nbsp;[logic system](logic-system) through a ![](mark:signal)&nbsp;[signal slot](slot), which holds the block and the toolbar slot. Filling the signal slot sets that toolbar slot up for you. Where the toolbar slot already did something else and you take it over, undo gives it back, as long as nothing else has been put there since.

Each toolbar slot set up this way says the signal slot's short name, the few words you write under `Short name` on the slot's page. A button panel's button is named with it, and a toolbar slot shows as much of it as fits under its icon. Left empty, they show the slot's name. Like the rest of the slot, the short name can be changed only while the logic system is switched off.

If somebody clears the toolbar slot or gives it another job, the signal stops arriving. Fill the signal slot again to set it back up.

## From a change in the block

A block may also send a signal when something about it changes, like a door finishing closing or a seat becoming occupied. These come from the block itself, so it fills an ordinary ![](mark:block)&nbsp;block slot, and its toolbar is left as it is.
