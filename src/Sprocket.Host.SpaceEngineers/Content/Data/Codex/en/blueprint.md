# Blueprint

A blueprint is a copy of a [logic system](#logic-system) kept in the [library](#library). It holds the logic system's ![](mark:sequence) [sequences](#sequence), [triggers](#trigger) and [slots](#slot), and what its [configuration panel](#configuration-panel) says. It holds no blocks, since every [installation](#installation) brings its own.

## Making one

On the [home screen](#home), `Make blueprint` on a logic system's heading saves a copy in the library. From then on the button reads `Update blueprint`, and replaces the library's copy with the logic system as it is now. Until you press it, a change stays on this [construct](#construct).

## Putting it on a construct

In the library, switch a blueprint's `On this construct` on. It becomes a logic system on this construct, with one installation whose slots are still empty. Fill them on its configuration panel.

A construct can hold a blueprint only once. To have a second, make a copy.

## Making a copy

`Make a copy`, in the library, puts a new logic system on this construct that starts as a copy of the chosen one. Use it to build something that works like an existing logic system, for example a hangar door made from an airlock, without changing the original.

- It is copied as it is on this construct, or as the library keeps it if it isn't here.
- It is named with a number, for example *Airlock 2*, and you are its maker.
- It has one installation, whose slots are still empty.
- It has no blueprint until you press `Make blueprint`. It and the original are separate from then on, and a change to one leaves the other as it is.

The copy opens at once, ready to be changed.

## Taking it off

Switching `On this construct` off takes the logic system and its installations off this construct, after asking. If the logic system was changed after its blueprint, it also asks whether to bring the blueprint up to date. A logic system that has no blueprint is deleted instead, since it is the only copy.

## Each construct keeps its own

Updating a blueprint changes only the library's copy. A logic system already on another construct keeps running as it was.

::: player
Your blueprints are saved on your computer, so every world you play on it can use them. They don't reach another computer or the game's cloud storage on their own. The game lets a mod save files either inside one world or on the computer itself, and gives it no way to reach the cloud.

To take a blueprint to another computer, or give it to a friend, copy its file:

1. Paste `%AppData%\SpaceEngineers\Storage` into the address bar of File Explorer to open the folder.
2. Copy the blueprint's file. Its name starts with `sprocket-bp-`, for example `sprocket-bp-Airlock.cog`.
3. On the other computer, put the file in the same folder. Then open `sprocket-bp.index` there and add the file's name on a line of its own.

The blueprint appears in the library the next time a world is loaded.
:::
