# Selector

A selector is where you select what a step, an if or a trigger works with: blocks and what to do with them or check on them, or one of the logic system's states. There is one for each job:

- The **block selector** selects the blocks for a [step](step) and what it does to them, and fills a [slot](slot).
- The **signal selector** selects the block and the [signal](signal) that fire a [trigger](trigger).
- The **value selector** selects the blocks and the value an [if](if) checks.
- The **state selector** selects one of the [logic system's states](logic-system-states) and the value a step sets it to, an if checks it for or a trigger waits for.

The selector an if, an otherwise if or a when trigger opens has a `Source` box heading its first column: `Blocks` checks or watches blocks, and `States` puts the state selector in its place. A step that sets a state opens the state selector straight away.

`Search` looks for the words you type in the order you type them, each anywhere in a block's name: *airl 1* finds *Airlock 1*, *Airlock 10* and *Airlock Door 1*, and leaves out *1 Airlock*, and every search box matches the same way. What you typed stays until you leave the screen.

Blocks with `Show block in toolbar config` switched off in the terminal are left out. `Show blocks hidden from toolbars`, at the end of the `Blocks` heading, shows them too, and it stays as you left it, in every selector and wherever you are.

Every selector lists only what is known to work with the automation. `Terminal ›` switches it to everything the blocks offer in the terminal, under the terminal's own names, without that promise.

In the signal selector, a toolbar somebody presses is one line: `hotbar slot is pressed` for a cockpit or any other seat, `button is pressed` for a button panel. Selecting it opens the toolbar's slots under it as numbered squares, with page numbers where the toolbar has more than one page, as a hotbar has nine. A square is plain where the slot is free, filled with the picture of the block where it holds one of the game's own actions, and green where it already answers a trigger of the automation. Its tip says what it holds, and any label the slot or button was given. The first free slot is chosen for you, and the line under the squares names it, like `Page 1, slot 3: free`. Press another square to choose that one, turning the page first if it's on another. The trigger then names the slot: *When Cockpit hotbar page 1, slot 3 is pressed*.

A step that does something with a screen, or an if that checks a screen's value, acts on one screen of each block. A block's screen actions and values, like `show text` and `font size`, are listed together under the `Screens` heading. Where the selected blocks share more than one screen, a drop box sits directly under that heading, and the actions and values stand set in under it. Choose the screen there first: everything under it acts on that screen. The drop box lists the screens the selected blocks share, by the names the game calls them, like a billboard's `Front` and `Back`, and the first is chosen to start with. A line beside it says what the chosen screen shows now: `text and images`, `nothing`, or `app` and the app's name, like `app TSS_ClockDigital`. Blocks with one screen, such as a text panel, show no drop box; that screen is the one used.

A toolbar whose slots stand for things that happen keeps a line for each, like a sensor's `starts detecting` and `stops detecting`, or a timer's `goes off`. Any other toolbar, like an air vent's, is one line in the signal selector's terminal view, `runs a toolbar slot`, and its slot is chosen from the squares in the same way.

Each thing the blocks can do carries a mark for its kind:

- ![](mark:action)&nbsp;An [action](action) the sequence starts and moves on from, regardless of how long the block takes to finish it.
- ![](mark:awaited)&nbsp;An [awaitable](sequence) action, one that runs over more than one [tick](tick) and the sequence can wait for.
- ![](mark:value)&nbsp;A **value**, one of the block's properties, to set or to check.
- ![](mark:signal)&nbsp;A **signal** the block sends, offered in the signal selector.
