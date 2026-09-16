# The Reactor script language

A saved program is a script. It is read and written by Reactor, and can be read and repaired by a
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
| Name | `inner`, `'Airlock System'` | Quoted where it holds anything but letters, digits, `-` and `_`. Write `''` for a quote within one: `'Mal''s airlock'` |
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

## `reactor <version>`

The first statement of every program, and the version of this language the rest of it is written in.
A file saying any other version is not this language.

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
required trigger to run 1
```

`expects` names what may fill a part: a game type id with the `MyObjectBuilder_` prefix dropped, a
quoted `'Type/Subtype'` for one exact block, or a list of either.

`required trigger to run <sequence>` says something on the construct starts that sequence. A button, a
timer, a sensor and an event controller all answer it, because each is a toolbar slot. The construct's
toolbars hold that wiring. Two triggers running one sequence need two slots running it.

## Triggers the system runs itself

```
# The inner door has shut
when inner.shut sampled every 10t then run 2

when a and b and not c then {
    open [ outer ]
    await
}

every 600t run 3

every 600t {
    open [ outer ]
    await
}
```

`when <condition> then` runs when the condition becomes true. `then` ends the condition and leads
either to `run <sequence>` or to a block of steps belonging to that trigger alone.

`sampled every <duration>` is how often the condition is checked. Without it, it is checked every
tick.

`every <duration>` is a clock, and takes no `then`.

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
<action> [ <parts> ]
action '<id>' on [ <parts> ] [with <values>]
run <sequence>
await
wait <duration>
```

A step starts something and the next statement follows immediately.

The head of a step is an action from the host's own vocabulary - `open`, `close`, `depressurize` - and
the list is the parts it acts on. An action whose id is one of the language's words is quoted.

`action '<id>' on [ <parts> ]` runs a terminal action of the game by its own id, which is how a
sequence reaches anything a mod has added. `with` carries the values that action takes.

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

# Reserved

`set` is a word the language holds before anything writes it.
