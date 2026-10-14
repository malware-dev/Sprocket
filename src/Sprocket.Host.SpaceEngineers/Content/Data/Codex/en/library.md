# Library

The library is where you keep your ![](mark:blueprint)&nbsp;[blueprints](blueprint) and manage the ![](mark:logic-system)&nbsp;[logic systems](logic-system) made from them. Open it with `Library` on the [home screen](home). It lists three groups:

- `On your constructs`: this [construct](construct), with every logic system on it.
- `Backups`: logic systems kept from constructs that lost their automation.
- `In storage`: your blueprints, which you can use everywhere.

::: player
Blueprints in storage are saved on your computer, so every world you play on it can use them. Copying a blueprint's file takes it to another computer.
:::

Select a row to see who made it, when it last changed and what it's for.

## Making a blueprint

`Make blueprint` puts a copy of a logic system in storage. From then on the two are linked: a change to the logic system stays on its construct until you press `Update blueprint`, and a change to the blueprint reaches a construct only where somebody presses `Update from library` there. Either update asks first, since it replaces what was there.

`Reset to blueprint` takes a logic system back to its blueprint, dropping every change made since the blueprint was last updated. Both this and `Update from library` leave the installations their blocks, in every slot the blueprint still has.

## Putting one on a construct

`Add to this construct` puts the blueprint's logic system here, with one ![](mark:installation)&nbsp;[installation](installation) to fill. It's offered only while no logic system from that blueprint is on the construct, because a second airlock is a second installation of the one logic system, never a second copy of it.

`Make a copy` puts a new logic system on this construct, copied from the row you selected, and opens it so you can change it. The copy has no tie to what it was copied from, and gets a blueprint of its own only when you press `Make blueprint`. It's the way to build something new from something that works, like a hangar door from an airlock.

## Deleting

Deleting a blueprint from storage is for good, but the logic systems already made from it keep running, and `Make blueprint` makes a new blueprint of any of them.

## Backups

When a construct loses its last programmable block, its logic systems are kept under `Backups`, listed under the construct's name with the latest first. A logic system the library already has, unchanged since its blueprint was last updated, is left to that blueprint.

A backup works like a blueprint: `Add to this construct` puts it on this construct as it is, and `Make blueprint` turns it into a blueprint, either updating the one it came from, if that's still in storage, or as a new one. Its installations stayed behind with the construct it came from, so it comes with one new installation to fill.

::: player
Backups are saved with the world they were taken in, so another world's library doesn't show them.
:::

## Sharing

You can share a blueprint from the library with everyone, with your faction, or with engineers you select by name, and it shows up in their library. You can withdraw a share at any time, but anyone who has already put your blueprint on a construct keeps that logic system, and can make blueprints of their own from it.
