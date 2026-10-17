# Construct

A construct is a set of grids joined by mechanical blocks, like rotors and pistons. A ship with a rotor turret is one construct. Grids held by landing gear or connectors stay apart, so a ship docked at a station isn't part of the station. A ![](mark:logic-system)&nbsp;[logic system](logic-system) works within one construct, so a docked ship's blocks can't fill its [slots](slot).

The automation is kept in the construct's programmable blocks, and runs while any of them is working. If all of them are off or without power, it waits, and a ![](mark:sequence)&nbsp;[sequence](sequence) part way through carries on from its step once one of them works again. When the last of them is lost, the logic systems are kept as backups in the [library](library).

When a construct splits, each part with a programmable block keeps its own copy of the automation. When two constructs with automation join, their automations become one. If both had a different version of the same logic system, its heading on the [home screen](home) asks you to choose one.

The automation belongs to the construct's owners, and only they can open it. [Changing hands](changing-hands) says who they are, and what happens when the construct passes to someone else.
