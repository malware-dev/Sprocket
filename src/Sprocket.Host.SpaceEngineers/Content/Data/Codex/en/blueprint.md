# Blueprint

A blueprint is a copy of a ![](mark:logic-system) [logic system](#logic-system) kept in the [library](#library). It holds the logic system's ![](mark:sequence) [sequences](#sequence), [triggers](#trigger) and [slots](#slot), its description, and what each slot is for. It holds no blocks, since every ![](mark:installation) [installation](#installation) brings its own.

## Making one

On the [home screen](#home), `Make blueprint` on a logic system's heading saves a copy in the library. From then on the button reads `Update blueprint`, and replaces the library's copy with the logic system as it is now. Until you press it, a change stays on this [construct](#construct).

## Putting it on a construct

In the library, choose a blueprint and press `Add to this construct`. It becomes a logic system on this construct, with one installation whose slots are still empty. Fill them on its [installation panel](#installation-panel).

A construct can hold a blueprint only once. To have a second, make a copy.

## Making a copy

`Make a copy`, in the library, puts a new logic system on this construct that starts as a copy of the chosen one. Use it to build something that works like an existing logic system, for example a hangar door made from an airlock, without changing the original.

- It is copied from the row you chose: the logic system on this construct, the backup, or the blueprint in storage.
- It is named with a number, for example *Airlock 2*, and you are its maker.
- It has one installation, whose slots are still empty.
- It has no blueprint until you press `Make blueprint`. It and the original are separate from then on, and a change to one leaves the other as it is.

The copy opens at once, ready to be changed.

## Taking it off

The minus beside a logic system on the home screen takes it and its installations off this construct, after asking. If the logic system was changed after its blueprint, it also asks whether to bring the blueprint up to date. A logic system that has no blueprint has a bin there instead, since it is the only copy. The bin deletes it at once, and undo on the home screen brings it back, switched off.

## Each construct keeps its own

Updating a blueprint changes only the library's copy. A logic system already on another construct keeps running as it was.

Where the library's blueprint was changed after a logic system, that logic system's button reads `Update from library`, on its heading on the home screen and in the library. It puts the blueprint in the logic system's place, after asking, and its installations keep their blocks in every slot the blueprint still has.

::: player
Your blueprints are saved on your computer, so every world you play on it can use them. They don't reach another computer or the game's cloud storage on their own. The game lets a mod save files either inside one world or on the computer itself, and gives it no way to reach the cloud.

To take a blueprint to another computer, or give it to a friend, copy its file:

1. Paste `%AppData%\SpaceEngineers\Storage` into the address bar of File Explorer to open the folder.
2. Copy the blueprint's file. Its name starts with `sprocket-bp-`, for example `sprocket-bp-Airlock.cog`.
3. On the other computer, put the file in the same folder. Then open `sprocket-bp.index` there and add the file's name on a line of its own.

The blueprint appears in the library the next time a world is loaded.
:::
