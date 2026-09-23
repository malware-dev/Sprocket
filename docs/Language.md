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

A statement the language does not know is dropped, with the block under it.

An annotation is the display text for the statement below it. Its first `#` line is the label; a
blank `#` line separates the label from longer instructions. An annotation with nothing under it is
an error.

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
| List | `[ 1, "two", three ]` | |

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
sprocket 1 alpha 7
```

`<format>` goes up only when a change would stop existing programs working. A reader of a later
format converts an earlier program rather than turning it away.

`alpha <draft>` says the format is not finished, and numbers the drafts of it. It goes up for any
change at all to what is written or read, and a program from another draft is turned away rather
than converted. It is absent from a finished format, so a program with no `alpha` was written
against one.

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

What the system needs a construct to provide. Every part starts with `required`, and everything else
in the system refers to a part by its name.

```
# The inner door
required block inner expects Door

# The hangar door, and only the large one
required block hangar expects 'AirtightHangarDoor/LargeBlockAirtightHangarDoor'

# The call button
required trigger pressed_1 [ call ] to run 1

# Something on the ship, and the program does not say what
required trigger to run 2
```

`expects` names what may fill a part: a game type id with the `MyObjectBuilder_` prefix dropped, a
quoted `'Type/Subtype'` for one exact block, or a list of either.

`required trigger <moment> [ <parts> ] to run <sequence>` says the construct announces that moment
and starts that sequence. A button, a timer, a sensor and an event controller all answer it, because
each is a toolbar slot: the slot holds Sprocket's own action and the construct invokes it. The slot is
the wiring and the only record of it, so it lives in the save beside the program rather than in the
program.

The parts and the moment may be left out - `required trigger to run <sequence>` - which says only
that something starts it. That is a whole sentence, not an unfinished one, and then which slot
answers it is whatever somebody points at Sprocket by hand.

**Which of the two a moment gets is the block's to say, never the author's.** A button press is
announced by the construct, so it is a `required trigger`; a door finishing closing is announced to
nobody, so it is a `when`. Writing the first as a `when` would claim Sprocket watches a property the
game does not have, and Sprocket would be inventing a signal rather than reading one.

Two triggers running one sequence need two slots running it.

## Triggers the system runs itself

```
# The inner door has shut
when any closed [ inner ] then run 2

# Whichever of the two shuts first
when any closed [ inner, outer ] then run 2

# Not until both of them are shut
when all closed [ inner, outer ] then run 3

every 600t run 3
```

`when <any|all> <moment> [ <parts> ] then run <sequence>` watches those parts for that moment
itself, which is what a moment the construct announces to nobody needs.

`any` is each part's own moment, so two doors shutting at different times is two firings. `all` is
one moment on the whole sentence: the instant the last of them is there, and not again until one of
them leaves that state.

`every <duration> run <sequence>` is a clock.

A trigger has no name. Its label is its annotation.

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
<action> [ <parts> ] [with <values>]
action '<id>' on [ <parts> ] [with <values>]
set '<id>' on [ <parts> ] to <value>
run <sequence>
await
wait <duration>
```

A step starts something and the next statement follows immediately.

The head of a step is an action from the host's own vocabulary - `open`, `close`, `depressurize` - and
the list is the parts it acts on.

`action '<id>' on [ <parts> ]` runs one of the game's terminal actions by the game's own id.

`with` carries the values an action takes, in either form. Every value is written as the kind it is,
in order, and a value left out is the default the action declared.

```
'rotate-to' [ arm ] with 0 90 2
action 'Reverse' on [ arm ]
action 'SetColor' on [ light ] with 255 136 0
action 'Run' on [ pb ] with "doors open"
```

`set '<id>' on [ <parts> ] to <value>` writes one of the block's own values, by the game's id for it.
A value declares what kind it is, and it is written as that kind. An id can name both one of the
game's actions and one of the block's values, and `set` always means the value.

```
set 'Radius' on [ light ] to 4.5
set 'OnOff' on [ light ] to off
set 'Color' on [ light ] to #FF8800
```

`run <sequence>` starts another sequence of the same system and carries on. A sequence already
running is not started again.

`await` waits for every action started above it to finish. It does not wait for a sequence `run`
started.

`wait <duration>` spends time. To let the doors finish and then pause, `await` and `wait` are two
statements.

```
close [ inner ]
await
open [ outer ]
wait 60t
action 'Run' on [ pb ] with "doors open"
```

# A construct

What this construct has, and which of the systems above it runs.

## `block '<Type/Subtype>' <name> [ <keys> ]`

One actual block on this construct. The quoted value is the block's type and subtype; the name after it
is what the rest of the file calls this block, and nothing else in the file names a block.

The list holds the ways to find the block again: its entity id, its name in the game, where it sits.
The language does not read them, and a host may write whatever it recognises its own blocks by. Several
of them, so a block that has been rebuilt or renamed can still be matched on what is left.

```
block 'Door/LargeBlockDoor' door1 [ 74857693847562934, "Airlock Inner Door" ]
```

## Instances

One of a system, running on this construct's own blocks.

```
instance <n> of <system> [as '<name>'] {
    <part> = [ <blocks> ]
}
```

`<part> = [ <blocks> ]` says which of this construct's blocks answer a part of the system. More than
one block may answer a part.

```
instance 1 of MalTek.Airlock {
    inner = [ door1 ]
    hangar = [ door2, door3 ]
}
```
