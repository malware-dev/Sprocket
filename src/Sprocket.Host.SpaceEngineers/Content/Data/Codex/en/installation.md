# Installation

An installation is one place a ![](mark:logic-system) [logic system](#logic-system) runs, such as one airlock among several. It fills the logic system's [slots](#slot) with blocks of its own, for example its own doors, vent and button.

## Many installations, one logic system

An airlock logic system can have one installation for each airlock on the ship. All of them run the same ![](mark:sequence) [sequences](#sequence) and [triggers](#trigger), each on its own blocks. Cycling one airlock leaves the others as they are.

A change to the logic system reaches every installation at once. Fix a step, and every airlock gets the fix.

## Adding one

Every logic system starts with one installation, named *Installation 1*. To add another, press `+ installation` under the logic system on the [home screen](#home). Its [configuration panel](#configuration-panel) opens, where you name it and fill its slots.

Pressing an installation's row opens its configuration panel again, to rename it or change its blocks.

## Its switch

Each installation has a switch of its own, so one airlock can be switched off while the others keep working. A new installation starts switched on.

The logic system's switch stands above them. Switching the logic system off stops all its installations. Switching it on again starts the ones whose own switch is on, and leaves the others off. While the logic system is off, the installations' switches can't be pressed.

## Its row

On the home screen, each installation has a row under its logic system. The row lights green while one of its sequences runs, and red when it needs mending. The logic system's heading turns red if any of its installations does.

An installation whose slots were split or merged by a change to the logic system says so on its row, for example `1 slot to fill again`. Its configuration panel shows which.

When a sequence can't carry on, only that installation is switched off, and its row says why. The other installations keep running.

## Removing one

The bin on an installation's row deletes it, after asking. The logic system and its other installations are kept. A logic system keeps at least one installation, so the bin of the last one can't be pressed.
