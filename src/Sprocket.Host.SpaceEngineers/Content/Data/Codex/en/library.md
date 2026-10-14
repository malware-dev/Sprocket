# Library

The library holds the ![](mark:logic-system)&nbsp;[logic systems](#logic-system) on this [construct](#construct), backups of constructs that lost their automation, and your ![](mark:blueprint)&nbsp;[blueprints](#blueprint). Open it with `Library` on the [home screen](#home).

It lists, from top to bottom:

- **`On your constructs`**: this construct, with every logic system on it.
- **`Backups`**: the logic systems kept of constructs that lost their automation, each under its construct's name.
- **`In storage`**: your blueprints, which every world can use.

Type in the search box to show only the rows whose name holds what you type.

## A logic system or a blueprint

Select one to see who made it and when it last changed, its description, and its ![](mark:installation)&nbsp;[installations](#installation) on this construct.

- `Add to this construct` puts it on this construct. It's only there while no logic system made from it is on this construct.
- On a logic system on this construct, `Make blueprint` keeps a copy in storage, and `Update blueprint` brings that copy up to date. `Update from library` takes a newer copy from storage, and `Reset to blueprint` goes back to the copy in storage, leaving out the changes made since. Either way its installations keep their blocks.
- `Make a copy` puts a new logic system on this construct that starts as a copy of this one.
- The bin deletes a blueprint from storage for good, after asking. Logic systems already on constructs keep running. On a backup, the bin forgets that one backup.

The blueprint page explains each of these.

## Backups

When a construct's last programmable block is destroyed or ground down, the logic systems it had are kept under `Backups`, with when it was lost. Only the logic systems the library doesn't already have are kept: one whose blueprint is the same is left to the blueprint. They're listed under the name the construct had, the latest first. Backups are kept in the world, so another world's library doesn't show them.

Each backup works like a blueprint. It can be put on this construct, copied, or kept in storage with `Make blueprint`, so that every world can use it. Where storage has the blueprint it was made from, the button reads `Update blueprint`, and asks whether to update that blueprint or keep the backup as a new one. Its installations stayed with the construct it was kept from, so when it is put on this construct it comes with one new installation to fill.

The bin forgets one backup for good, after asking.
