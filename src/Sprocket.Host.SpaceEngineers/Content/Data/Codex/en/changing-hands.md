# Changing hands

A [construct](construct)'s automation belongs to its owners: the engineers who own the most blocks on it, counted over all its grids, and all of them where there's a tie. Only they can open it. An unowned construct opens for anyone. When a ship is captured or handed over, its automation changes hands with it.

The automation can use the blocks its owners may use, which are the ones they own and the ones shared with their faction or with everyone. Any other block reads as lost, as a destroyed block does, and comes back by itself once it's shared.

## When the construct changes hands

A construct passed to someone it was shared with keeps running, like a ship shared with a faction and handed to a faction mate. Passed outside its sharing, in a capture or a hand-over, the automation pauses: its buttons go dead, and a ![](mark:sequence)&nbsp;[sequence](sequence) part way through holds at its step. It stays paused until one of the new owners resumes it, much as a programmable block asks its new owner to recompile its script. A reload doesn't change that.

Opening a paused automation shows what changed first, and lets you look it over before it runs again.
