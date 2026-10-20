# Installation panel

The installation panel is where you set up an ![](mark:installation)&nbsp;[installation](installation). It lists every [slot](slot) the ![](mark:logic-system)&nbsp;[logic system](logic-system) needs filled, with any notes on what each is for, and you select the blocks and ![](mark:signal)&nbsp;[signals](signal) that go in them. Press an installation's row on the [home screen](home) to open its panel.

## Designing it

The `Installation panel` tab on the logic system's screen shows what everyone installing this logic system sees on their own panel: a description, then the slots.

To help people understand what your logic system is for, or just to remind yourself, consider writing a description. Whoever receives your ![](mark:blueprint)&nbsp;[blueprint](blueprint) has never seen your construct, so the description and the slots are all they have to go on. The description is written while the logic system is off, and can hold [rich text](rich-text).

Each set of blocks you select for a step or a trigger becomes a slot. A slot of one block takes that block's name; a slot of several is numbered. A ![](mark:block)&nbsp;**block slot**, like *the inner door*, holds the blocks a sequence acts on or watches. A ![](mark:signal)&nbsp;**signal slot**, like *the button*, holds a block and the toolbar slot on it that sends the signal.

On this tab, a slot's name is a button that opens the slot, to name it and write what it is for. On an installation's own panel, the name is plain text.

Slots stand in the order they were made. Drag a slot by its grip to move it to another place.

To group slots, press `+ category`, which adds *Category 1*, then *Category 2* and so on. Press a category's name to rename it. ![](mark:delete)&nbsp;on a category removes it and leaves its slots among those at the top. The slots outside any category come first, then each category's name with its slots set in under it. Drag a slot under a category's name to put it in that category, or among the slots at the top to take it out. A category's own grip moves it with all its slots.

Each of these changes is one step of ![](mark:undo)&nbsp;[undo](undo), and like every other change it can be made only while the logic system is switched off. An installation's own panel shows the same order as plain text, with every category that holds slots, by the same name.

## Screens

Every installation chooses its own screen. Filling a slot of screens opens the [selector](selector) with the step's own action alone, and the screen drop box under it once blocks are selected. It asks for no values, since those belong to the step.

A step shows a problem when its block lacks the screen its installation chose, for example after the block was replaced by one with fewer screens: *{block} doesn't have the screen this step uses. Choose its screen on the installation panel.* The step leaves that block alone.

## Filling a slot again

A change to the logic system can split a slot this installation had filled, or merge it into a bigger one, and the panel then asks for it to be filled again. The blocks the slot had are set aside and offered first, and kept until every slot of the installation is filled again.
