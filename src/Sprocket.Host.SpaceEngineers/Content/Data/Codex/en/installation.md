# Installation

An installation is a set of blocks and signals a ![](mark:logic-system)&nbsp;[logic system](logic-system) works on, like one airlock among several. It fills the logic system's [slots](slot) with its own blocks, so every airlock runs the same ![](mark:sequence)&nbsp;[sequences](sequence) and [triggers](trigger).

Every logic system has at least one installation. Add more with `+ installation` under the logic system on the [home screen](home), and fill their slots on their [installation panels](installation-panel). A change to the logic system reaches every installation, so fixing a step fixes every airlock.

## Its switch

Each installation has a switch of its own, so one airlock can be switched off while the others keep working. New installations start switched on, and one deleted and brought back with undo comes back switched off.

The logic system's switch outranks theirs. Switching it off stops every installation, and switching it on again starts only those whose own switch is on.
