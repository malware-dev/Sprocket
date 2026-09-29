# The Sprocket script language

A saved program is a script. It is read and written by Sprocket, and can be read and repaired by a
person.

## Shape

```
file        := version construct*
construct   := annotation? statement
statement   := head value* newline
             | head value* block
block       := "{" newline construct* "}" newline
annotation  := ("#" (" " line)? newline)+
```

A newline ends a statement. There are no continuations: no semicolons, no line joining.

A head is a bare or quoted name, or one of the language's words. Keywords are contextual, and a
quoted name is never a keyword, so an action called `every` is written `'every'`.

Names and keywords are matched exactly, case included: `Run` and `run` are different words.

A statement the language does not know is an error in the system or instance that holds it.

An annotation describes the statement below it. An annotation with nothing under it is an error.

## Names, text and values

| Value | Written | Notes |
|---|---|---|
| Name | `inner`, `'Airlock System'` | A letter, then letters, digits and `_`. Quoted where it holds anything else. Write `''` for a quote within one: `'Mal''s airlock'` |
| Qualified name | `MalTek.Airlock`, `'MalTek Inc.'.'Airlock System'` | Names joined by the `.` operator, kept as parts: `'a.b'.c` and `a.'b.c'` differ. Spaces around the dot are nothing |
| Text | `"Airlock Inner Door"` | Write `""` for a quote within one: `"the ""inner"" door"` |
| Integer | `60`, `74857693847562934` | Whole, 64-bit |
| Real | `0.25`, `1E-05` | A dot or an exponent makes a real |
| Duration | `600t` | A number and its unit as one token. `t` is ticks |
| Flag | `on`, `off` | |
| Colour | `#FF8800`, `#FF8800CC` | |
| Vector | `(3 0 -2)` | |
| List | `[ 1, "two", three ]` | An item may be a list or a vector: `[ 7, [ (0 2 -1), (3 0 0) ] ]` |
| Toolbar slot | `panel1:toolbar(1, 1, 4)` | A block, then which of its toolbars, the page and the slot, each counted from 1 |

## References

Anything with an id and a name may be referred to by either.

```
run 2
run 'Let me out'
instance 1 of "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43"
instance 1 of MalTek.Airlock
```

A bare number or a quoted GUID is an id; a name is a name. A sequence called "2" is written `'2'`.

Renaming something is far more likely than renumbering it, so a reference written as an id usually
outlives one written as a name.

## `sprocket <format> [alpha <draft>]`

The first statement of every program, and the version of the language it is written in.

```
sprocket 1 alpha 8
```

`<format>` goes up only when a change would stop existing programs working.

`alpha <draft>` says the format is not finished, and numbers the drafts of it. It goes up for a
change that makes an existing program read differently or not at all; an addition that no existing
program uses leaves it where it is. A program with no `alpha` was written against the finished
format, which comes after all of its drafts, however many there were.

```
# Oldest first
sprocket 1 alpha 1
sprocket 1 alpha 8
sprocket 1
sprocket 2 alpha 1
sprocket 2
```

# A system

A system is what the automation is, and the unit that is exported, imported and shared. It says what
it needs from a construct, what starts it, and what it then does.

```
system "<guid>" as <namespace>.<name> [changed "<iso>"] [last sequence <n>] {
    <parts>
    <triggers>
    <sequences>
}
```

The GUID is the system's identity, made with it and never changed.

The namespace and name are for people, and a player may change either. Two systems with the same name
load, with a warning; the editor refuses to make or rename one into a name that is taken.

`changed` is when the system was last edited, as quoted ISO 8601. A time with no zone is UTC. Any
ISO 8601 spelling is read; the full form, `"2026-09-14T19:02:11Z"`, leaves nothing to work out.

`last sequence <n>` is the highest sequence number the system has handed out. Where both it and the
sequences are present, the higher wins, so it says something only once the highest sequence has been
deleted and is worth writing only then.

## Parts

What the system needs a construct to provide. Every part starts with `required` or `optional`, and
everything else in the system refers to a part by its name.

```
# The inner door
required block inner expects Door

# The hangar door, and only the large one
required block hangar expects 'AirtightHangarDoor/LargeBlockAirtightHangarDoor'

# A light over the door, where there is one
optional block lamp expects InteriorLight

# The call button
required trigger call to run 1

# The stop button
required trigger halt to stop 1

# The outer door's own button: it opens the door, and nothing else
required trigger open to act sprocket.open on [ outer ]

# A second call button, where there is one
optional trigger callToo to run 1
```

An `optional block` or `optional trigger` is one an instance may leave unanswered.

`expects` names what may fill a part: a game type id with the `MyObjectBuilder_` prefix dropped, a
quoted `'Type/Subtype'` for one exact block, or a list of either.

`required trigger <name> to <step>` says the construct starts that step from a toolbar slot. Each
instance says which slots: a button, a sensor, a timer, or any other block with a toolbar answers it,
because the slot holds Sprocket's own action and the construct invokes it. The slot is the wiring and
the only record of it, so it lives in the save beside the program rather than in the program.

The step is one of `run`, `stop`, `act` or `set`, written as a sequence writes it.

A trigger is always a toolbar slot. A moment nothing announces, such as a door finishing closing, is
watched with `when`.

## Triggers the system runs itself

```
# The inner door has shut
when any sprocket.closed [ inner ] then run 2

# Whichever of the two shuts first
when any sprocket.closed [ inner, outer ] then run 2

# Not until both of them are shut, and then the hangar opens
when all sprocket.closed [ inner, outer ] then act sprocket.open on [ hangar ]

# The room has filled, looked for every half second
when any sprocket.filled [ vent ] every 30t then run 4

every 600t stop 3
```

`when <any|all> <moment> [ <parts> ] [every <duration>] then <step>` watches those parts for that
moment itself, which is what a moment the construct announces to nobody needs.

`any` is each part's own moment, so two doors shutting at different times is two firings. `all` is
one moment on the whole sentence: the instant the last of them is there, and not again until one of
them leaves that state.

A moment is either reported as it happens or looked for. `every` is how often one that is looked for
is looked for, and `10t` where it is left out. A reported moment takes no `every`, and writing one is
an error. Which moments are which is the host's to say.

`every <duration> <step>` is a clock.

The step is one of `run`, `stop`, `act` or `set`, written as a sequence writes it.

A `when` and an `every` have no name.

## Sequences

```
sequence 1 [as '<name>'] {
    <steps>
}
```

A sequence is its number within its system, one past the last the system handed out. The number
survives export, import and renaming.

The name is optional, and is a name rather than text.

## Steps

```
act <member> on [ <parts> ] [with <values>]
set <member> on [ <parts> ] to <value>
run <sequence>
stop <sequence>
await
wait <duration>
```

A step starts something and the next statement follows immediately.

`act <member> on [ <parts> ]` runs an action on the parts. The member is the action's id: the
game's own id as it is, or an id the host adds, qualified by what adds it.

`with` carries the values an action takes, in either form. Every value is written as the kind it is,
in order, and a value left out is the default the action declared. Where an action says what type a
value has to be, a value of another kind, or a whole number too large for that type, is an error,
and a whole number stands for a real.

```
act sprocket.close on [ inner ]
act sprocket.'rotate-to' on [ arm ] with 0 90 2
act Reverse on [ arm ]
act SetColor on [ light ] with 255 136 0
act Run on [ pb ] with "doors open"
```

`set <member> on [ <parts> ] to <value>` writes one of the block's own values, by its id. A value
declares what kind it is, and it is written as that kind, or as a whole number where it is real. An
id can name both an action and a value, and `set` always means the value.

```
set Radius on [ light ] to 4.5
set OnOff on [ light ] to off
set Color on [ light ] to #FF8800
```

`run <sequence>` starts another sequence of the same system and carries on. A sequence already
running is not started again.

`stop <sequence>` stops another sequence of the same system where it is running, and carries on.

`await` waits for every action started above it to finish. It does not wait for a sequence `run`
started.

`wait <duration>` spends time. To let the doors finish and then pause, `await` and `wait` are two
statements.

```
act sprocket.close on [ inner ]
await
act sprocket.open on [ outer ]
wait 60t
act Run on [ pb ] with "doors open"
```

# A construct

What this construct has, and which of the systems above it runs.

## `block '<Type/Subtype>' <name> [ <keys> ]`

One actual block on this construct. The quoted value is the block's type and subtype; the name after it
is what the rest of the file calls this block, and nothing else in the file names a block.

The list holds the ways to find the block again - its entity id, where it sits, its name in the game -
in the order they are to be tried: the first that finds a block decides. The language does not read
them, and a host may write whatever it recognizes its own blocks by. Several of them, so a block that
has been rebuilt, copied or renamed can still be matched on what is left.

```
block 'Door/LargeBlockDoor' door1 [ 74857693847562934, [ (3 0 -2) ], "Airlock Inner Door" ]
block 'Door/LargeBlockDoor' door2 [ 74857693847562935, [ (0 2 -1), (1 0 0) ], "Hangar Door" ]
```

Where a block sits is a list of cells: the cell of each mechanical joint crossed from the grid the
program runs on, then the block's own cell on its grid.

## Instances

One of a system, running on this construct's own blocks.

```
instance <n> of <system> [as '<name>'] {
    <part> = [ <blocks> ]
}
```

`<part> = [ <blocks> ]` says which of this construct's blocks answer a part of the system. More than
one block may answer a part. A trigger is answered by toolbar slots, and more than one slot means any
of them starts it. An instance that leaves a required part unanswered, block or trigger, is missing
it.

```
instance 1 of MalTek.Airlock {
    inner = [ door1 ]
    hangar = [ door2, door3 ]
    call = [ panel1:toolbar(1, 1, 4) ]
}
```
