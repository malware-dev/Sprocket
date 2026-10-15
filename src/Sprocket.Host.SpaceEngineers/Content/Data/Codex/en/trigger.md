# Trigger

A trigger is a line in a ![](mark:logic-system)&nbsp;[logic system](logic-system) that runs or stops a ![](mark:sequence)&nbsp;[sequence](sequence) when something happens, like running *Cycle Out* when the airlock's button is pressed. Triggers fire only while the logic system and the installation are switched on.

A trigger can fire on any of these:

- A ![](mark:signal)&nbsp;[signal](signal), like a button being pressed or a door finishing closing.
- Blocks reaching a state, like a door being closed. With several blocks, `any` starts it when the first gets there, and `all` when the last one does.
- A set time passing, over and over, like every minute. Add one of these with `every`.

## Each installation on its own

Every ![](mark:installation)&nbsp;[installation](installation) has the logic system's triggers to itself. A signal starts a trigger only for the installation whose [slot](slot) holds the block that sent it, so the port airlock's button cycles the port airlock and leaves the starboard one alone.

## Copying a trigger

A toolbar slot can start only one trigger, so a copy of a trigger started from one comes with an empty slot of its own, for each installation to fill on its [installation panel](installation-panel). Any other trigger is copied whole.
