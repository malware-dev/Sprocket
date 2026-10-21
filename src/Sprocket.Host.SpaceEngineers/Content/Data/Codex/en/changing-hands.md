# Changing hands

A [construct](construct)'s automation belongs to its owners: the engineers who own the most blocks on it, counted over all its grids, and all of them where there's a tie. When a ship is captured or handed over, its automation changes hands with it.

The automation opens from a programmable block one of the owners owns, for anyone that block is shared with: the owners themselves, their faction where it's shared with the faction, and everyone where it's shared with everyone. An unowned construct opens for anyone. A programmable block owned by anyone else stays outside the automation, however it's shared.

The automation is kept in a programmable block one of the owners owns. If that block passes to someone else, another of the owners' programmable blocks takes it over. If they have none left, the automation is kept and waits until one of them owns a programmable block on the construct again, and a ![](mark:sequence)&nbsp;[sequence](sequence) part way through holds at its step until then. That happens part way through handing over a ship of several grids, since the engineer receiving it accepts it grid by grid: the automation waits until the whole ship has arrived. It's lost only when the last programmable block on the construct is destroyed or removed.

The automation can use the blocks its owners own and the ones shared with their faction or with everyone. Any other block reads as lost, as a destroyed block does, and comes back by itself once it's shared.

## When the construct changes hands

A construct passed to someone it was shared with keeps running, like a ship shared with its faction and handed to a faction mate. Passed outside its sharing, in a capture or a hand-over, the automation pauses: its buttons go dead, and a sequence part way through holds at its step. It stays paused until one of the new owners resumes it, much as a programmable block asks its new owner to recompile its script. A reload doesn't change that.

A change of hands is judged once, from the owners before it to the owners after it, by the sharing as it was before, since the game turns sharing off on every block it gives to another engineer. So when the faction mate hands the ship back, it pauses, unless they shared it with the faction again first.

Opening a paused automation shows what changed first, and lets you look it over before it runs again.
