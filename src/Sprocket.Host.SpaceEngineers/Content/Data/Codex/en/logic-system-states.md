# Logic system states

Sometimes a ![](mark:logic-system)&nbsp;[logic system](logic-system) needs to remember something from one run to the next, like which side of the airlock its engineer went to. With both doors shut and the room empty, the blocks look the same either way. The logic system's states are for that: each state keeps one value, and each ![](mark:installation)&nbsp;[installation](installation) keeps its own value in it for as long as the installation exists, through saves and reloads. The port airlock remembering that its engineer is outside leaves the starboard airlock's value as it was.

A state holds either on or off, like whether a hangar is locked down, or one choice of a **type**. A type is a named set of at least two choices, like *side*, with the choices *inside* and *outside*. The airlock keeps its engineer's side in a state called *where*, which holds *side*.

## The State tab

The logic system's states are on the `State` tab of its screen, with its types above them.

Under `Types`, each type shows its name and then its choices. `+ type` adds one called *type 1*, with the choices *choice 1* and *choice 2*, and `+ choice` after a type's choices adds another. Press a type's name to rename it; the new name reaches everywhere the type is used. Press a choice for a menu with `Rename`, and with `Remove` while nothing uses the choice and its type keeps two others.

Under `States`, each state shows its name, what it holds (a type's name, or `on or off`), `starts` with the value every installation starts at, and `now` with what the `Selected installation` holds at this moment. `+ state` adds *state 1*, holding on or off and starting off.

Press the value after `starts` to choose another. The list offers `on`, `off` and every choice of every type, so choosing one also decides what the state holds. Once a [step](step) or a [trigger](trigger) uses the state, it's offered only the values of what it already holds.

Drag a type or a state to move it. One that something uses stays: its ![](mark:delete)&nbsp;is dimmed, and its tip says what uses it.

## Setting a state

In a ![](mark:sequence)&nbsp;[sequence](sequence), `set state` in the `+ step` menu gives a state a new value. The step sets states alone, so it opens the **state selector**, the [selector](selector) for the logic system's states, straight away: select the state on the left, then its value on the right: one of its choices, or on or off. Below those come the other states holding the same type; select one, and the step copies whatever that state holds when the step runs. The step reads *set where to outside*.

## Checking a state

An [if](if) can check a state where it would check blocks. The first column of its selector is headed `Source`, with a box choosing `Blocks` or `States`. Choose `States`, and the state selector lists the logic system's states: select one, then `is` or `is not`, and the value, and the if reads *if where is inside*. It works like any if: the steps under it run only if it holds when the sequence reaches it. Press `is` on its row to switch between `is` and `is not`.

An `otherwise if` has the same box, and so does the `if` on an `otherwise` row's plus, so any branch can check a state.

With these, an airlock with one button can do the right thing every time. Its sequence checks *if where is inside*, cycles out and sets *where* to *outside*; its `otherwise` cycles in and sets *where* back to *inside*.

## A trigger on a state

On the `Triggers` tab, `when` in the `+ trigger` menu opens a selector with the same `Source` box. With `States`, it makes a trigger that fires as a state comes to hold a value: select the state, `is` or `is not` and the value, then what it does, as for any other trigger: *When where is outside*. It fires the moment a step sets the state so that it holds, for the installation the step ran on. A state's starting value never fires it.

## Making a state as you go

The state selector ends its list of states with `A new state`, which makes one on the spot. Select it, and the second column asks for the state's `name`, filled in with a numbered one like *state 4* for you to type over, and the value it `starts` at, which also decides what it holds. Then select the value as usual.

To give the new state a type of its own, select `+ A new type` at the end of the `starts` list. The type appears under the list as the `State` tab shows one: its name, numbered like *type 3*, then *choice 1*, *choice 2* and `+ choice`. Press the name or a choice to rename it; a choice can also be removed while the type has more than two. The state starts at the type's first choice, and the value the step sets or the if or trigger compares with is one of its choices.

The state's name, and the new type's, have to be ones that no slot, type or state of the logic system has, and the column says so until they are. `Done` adds everything made here together with the step or trigger, and one ![](mark:undo)&nbsp;[undo](undo) takes all of it back.

Press any part of a set state's row to open the state selector again. Press a part of an `if`, an `otherwise if` or a `when`, and its selector opens on the source it uses; switching the `Source` box there turns it from blocks to a state or back, and an if keeps the steps inside it.
