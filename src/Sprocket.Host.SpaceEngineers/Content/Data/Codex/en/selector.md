# Selector

A selector is where you select blocks, and what to do with them or check on them. There is one for each job:

- The **block selector** selects the blocks for a [step](step) and what it does to them, and fills a [slot](slot).
- The **signal selector** selects the block and the [signal](signal) that fire a [trigger](trigger).
- The **value selector** selects the blocks and the value an [if](if) checks.

`Search` looks for the words you type in the order you type them, each anywhere in a block's name: *airl 1* finds *Airlock 1*, *Airlock 10* and *Airlock Door 1*, and leaves out *1 Airlock*, and every search box matches the same way. What you typed stays until you leave the screen.

Blocks with `Show block in toolbar config` switched off in the terminal are left out. `Show blocks hidden from toolbars`, at the end of the `Blocks` heading, shows them too, and it stays as you left it, in every selector and wherever you are.

The block selector and the value selector list only what is known to work with the automation. `Terminal ›` switches either one to everything the blocks offer in the terminal, under the terminal's own names, without that promise.

Each thing the blocks can do carries a mark for its kind:

- ![](mark:action)&nbsp;An [action](action) the sequence starts and moves on from, regardless of how long the block takes to finish it.
- ![](mark:awaited)&nbsp;An [awaitable](sequence) action, one that runs over more than one [tick](tick) and the sequence can wait for.
- ![](mark:value)&nbsp;A **value**, one of the block's properties, to set or to check.
- ![](mark:signal)&nbsp;A **signal** the block sends, offered in the signal selector.
