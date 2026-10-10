# The Sprocket script language

A Sprocket program is a script, written so that a person can read and repair it.

A host is what runs a program in a game. It supplies what the language leaves open: the members
a block has, and how a block is found again.
What a host may add is set out under [What a host adds](#what-a-host-adds).

A construct is what a program runs on, and a program belongs to one construct: its own. A block is
one thing on a construct that the host can find again: it has a type, and it has the members the
host gives it.

## The shape of a file

```
file        := version entry*
entry       := annotation? statement
statement   := head (value | keyword | operator)* newline
             | head (value | keyword | operator)* block
block       := "{" newline entry* "}" newline
annotation  := ("#" (" " line)? newline)+
```

After the version, a file holds variables, systems, system blueprints and instances. A variable comes
before anything that uses it, and an instance comes after the system blueprint it is one of; the rest
may come in any order.

A newline ends a statement. There are no continuations: no semicolons, no line joining.

A head is a bare or quoted name, or one of the language's words. Keywords are contextual, and a
quoted name is never a keyword, so an action called `every` is written `'every'`.

Names and keywords are matched exactly, case included: `Run` and `run` are different words.

An annotation describes the statement below it. An annotation with nothing under it is an error.

## Values

| Value | Written | Notes |
|---|---|---|
| Name | `inner`, `'Airlock System'` | A letter, then letters, digits and `_`. Quoted where it holds anything else. Write `''` for a quote within one: `'Mal''s airlock'` |
| Qualified name | `MalTek.Airlock`, `'MalTek Inc.'.'Airlock System'` | Names joined by the `.` operator, kept as parts: `'a.b'.c` and `a.'b.c'` differ. Spaces around the dot are nothing |
| Text | `"Airlock Inner Door"` | Write `""` for a quote within one: `"the ""inner"" door"` |
| Integer | `60`, `74857693847562934` | Whole, 64-bit |
| Real | `0.25`, `1E-05` | A dot or an exponent makes a real |
| Duration | `600t` | A number and its unit as one token. `t` is ticks |
| Flag | `on`, `off` | |
| Color | `#FF8800`, `#FF8800CC` | |
| Vector | `(3 0 -2)` | Three integers or reals, separated by spaces |
| List | `[ 1, "two", three ]` | An item may be a list or a vector: `[ 7, [ (0 2 -1), (3 0 0) ] ]` |
| Block | `block 'Door/LargeBlockDoor' [ … ]` | One actual block. See Variables |

### References

Anything with an id and a name may be referred to by either.

```
run 2
run 'Let me out'
instance 1 of "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43"
instance 1 of MalTek.Airlock
```

A bare number or a quoted GUID is an id; a name is a name. A sequence called "2" is written `'2'`.

A reference written as an id survives a rename; one written as a name has to follow it.

## What a host adds

Everything a program says about a block beyond its name is one of its members, and the host's. The
sections below say how members are written. The examples throughout this document are written for a
host with a namespace called Gearbox.

### Members

Every member the host gives a block is an action, a value, a function or a signal.

An action is something a block does, done with `call`. A value is something a block has: `if` and
`when` read it, and `set` writes it where the host allows. A function is read like a value, and is
given arguments. A member the host does not give a block is an error, and so is a member used as a
kind it is not, such as `call` on a value or `set` on a function.

Each member of a block has an id of its own, except that an action and a value may share one.

An action may declare parameters, and a function declares at least one. Each parameter is given an
argument, in parentheses after the member, in order, separated by commas:
`Gearbox.'rotate-to'(0, 90, 2)`, `Gearbox.amount("Ingot/Iron")`. Parentheses are written only where
there are arguments, and empty ones are an error: `call Gearbox.close on [ inner ]`.

Each argument is written as the kind it is. An argument missing or left over is an error. Where a
parameter declares a type, an argument of another kind, or an integer too large for that type, is an
error.

A value or a function gives back a flag, an integer, a real, text, a color, or one of a list of names
the host gives that member. A flag is `on` or `off`. One of a list is written as a name: `closed`.
Wherever a real is taken, an integer stands for one.

A signal is something a block sends: `Gearbox.closes` is sent as a door finishes closing. A function
may give back a signal too, named by the place it is sent from, a place the host offers. Either starts
a step with `whenever`, and is written in place after `whenever`, in a variable, or answering a slot that expects
`signal`. Its blocks are a set like any other, and the signal is sent from any of them.

```
# The signal sent from a place on the panel's toolbar, by toolbar, page and position, each counted
# from 1
let 'call pressed' = Gearbox.toolbar(1, 1, 4) on [ panel1 ]
```

```
call Gearbox.close on [ inner ]
set Radius on [ light ] to 4.5
if all Gearbox.status on [ inner ] == closed then {
    call Gearbox.open on [ outer ]
}
```

### Namespaces

Every member of a block is the host's, and lives in a namespace. A member of the root namespace is
written bare; a member of a named namespace is written qualified, the
namespace's name, a dot, and the id.

```
call Reverse on [ arm ]
set Radius on [ light ] to 4.5
call Gearbox.close on [ inner ]
call Gearbox.'rotate-to'(0, 90, 2) on [ arm ]
whenever Gearbox.closes on [ inner ] then run 2
```

Each part of a qualified name is a name, judged on its own, so a part spelled like a keyword is
quoted: `Gearbox.'on'`.

A host may have a root namespace, named namespaces, or both. A host with no root namespace has every
member written qualified. Which namespaces there are, and what each holds, is the host's to say. A
member in no namespace the host has is an error.

## `sprocket <format> [alpha <draft>]`

The first statement of every program, and the version of the language it is written in.

```
sprocket 1 alpha 10
```

`<format>` goes up only when a change would stop existing programs working.

`alpha <draft>` says the format is not finished, and numbers the drafts of it. It goes up for a
change that makes an existing program read differently or not at all; an addition that no existing
program uses leaves it where it is. A program with no `alpha` was written against the finished
format, which comes after all of its drafts, however many there were.

```
# Oldest first
sprocket 1 alpha 1
sprocket 1 alpha 10
sprocket 1
sprocket 2 alpha 1
sprocket 2
```

## Systems and system blueprints

A system is a set of sequences, together with the triggers, watchers and clocks that start them. It
runs on the program's own construct, and the blocks it acts on are the ones the file's variables name.

A system blueprint is the same set, written against slots rather than blocks, so that it can be used
many times over. Each instance of it answers the slots with blocks of its own, and runs the set on
them. A system blueprint is also what is exported, imported and shared.

Everything said of a system below holds for a system blueprint, except where it says otherwise.

```
system "<guid>" as <namespace>.<name> [changed "<iso>"] [last sequence <n>] {
    <triggers, watchers and clocks>
    <sequences>
}

system blueprint "<guid>" as <namespace>.<name> [changed "<iso>"] [last sequence <n>] {
    <slots>
    <triggers, watchers and clocks>
    <sequences>
}
```

The GUID is the system's identity, made with it and never changed.

The namespace and name are for people, and a player may change either. Two systems may have the same
name.

`changed` is metadata, and has no effect on what the program does. It is when the system was last
edited, and tells two versions of a system apart. It is written as quoted ISO 8601 in any of its
spellings: `"2026-09-14T19:02:11Z"`. A time with no zone is UTC.

`last sequence <n>` is editing metadata, and has no effect on what the program does. It is the
highest sequence number the system has handed out. Where both it and the sequences are present, the
higher wins. A sequence added to the system is numbered one past it, so a deleted number is never
handed out again, and it is worth writing only where it is higher than every sequence in the system.

`changed` and `last sequence` may come in either order, each at most once.

### Slots

What a system blueprint needs a construct to provide. Only a system blueprint has slots. Every slot
starts with `required` or `optional`, and everything else in the system blueprint refers to a slot by
its name.

```
# The inner door
required slot inner expects Door

# The hangar door, and only the large one
required slot hangar expects 'AirtightHangarDoor/LargeBlockAirtightHangarDoor'

# A light over the door, where there is one
optional slot lamp expects InteriorLight

# The call button
optional slot button expects signal
```

`required slot <name> expects <what>` asks the construct for it. `expects` names what may fill the
slot: a block's type, or `signal` for a slot answered by a function that gives back a signal. A
block's type is a game type id with the `MyObjectBuilder_` prefix dropped, a quoted `'Type/Subtype'`
for one exact block, or a list of either.

An `optional` slot is one an instance may leave unanswered.

### Triggers, watchers and clocks

```
# The call button runs the cycle
whenever 'call pressed' then run 1

# The stop button
whenever 'stop pressed' then stop 1

# The outer door's own button: it opens the door, and nothing else
whenever 'open pressed' then call Gearbox.open on [ outer ]

# The inner door has shut
whenever Gearbox.closes on [ inner ] then run 2

# Whichever of the two shuts first
whenever Gearbox.closes on [ inner, outer ] then run 2
```

`whenever <signal> then <step>` starts the step each time the signal is sent. The signal is written in place,
`whenever Gearbox.closes on [ inner ] then run 2`, or named: in a system by a variable holding one, in a
system blueprint by a slot that expects `signal`.

A signal is sent as something happens: a toolbar slot used, a door finishing closing. A state the
blocks come to, such as both doors being shut, is watched with `when`.

```
# Not until both of them are shut, and then the hangar opens
when all Gearbox.status on [ inner, outer ] == closed then call Gearbox.open on [ hangar ]

# The room has filled, looked for every half second
when any Gearbox.filled on [ vent ] == on every 30t then run 4

# The tank has filled halfway
when any Gearbox.fill on [ tank ] >= 0.5 then run 5

# The holds have a thousand iron ingots between them
when sum Gearbox.amount("Ingot/Iron") on [ cargo ] >= 1000 then run 6

every 600t stop 3
```

`when <any|all|sum> <member> on [ <names> ] <comparison> <value> [every <duration>] then <step>` starts
the step when the comparison comes to hold on the blocks named:
`when all Gearbox.status on [ inner, outer ] == closed then call Gearbox.open on [ hangar ]` opens the
hangar once both doors are shut. `<member>` is a value or a function, and `<comparison>`, `<value>` and `sum` follow the rules
an `if` sets out for them.

`any`, `all` and `sum` say how the comparison is tested across every block the names stand for, each
block once, however many names reach it. With `any`, the comparison coming to hold on one block is
enough, and each block it comes to hold on fires on its own, so two doors shutting at different times
is two firings. With `all`, it must hold on every block, and the `when` fires once, the instant it
comes to hold on the last of them, and not again until it stops holding on one of them. With `sum`,
the member is read on every block and the readings are added; the `when` fires once, the instant the
comparison comes to hold on the total, and not again until it stops holding.

`every` is how often a `when` is tested, and `10t` where it is left out. A host that is told when a
member changes may test it then, and set the `every` aside.

`every <duration> <step>` is a clock, and starts the step each time the duration passes:
`every 600t stop 3` stops sequence 3 every ten seconds.

A `whenever`, a `when` and an `every` have no name.

### Sequences

```
sequence <n> [as '<name>'] {
    <steps>
}
```

A sequence is its number within its system, one past the last the system handed out. The number
survives export, import and renaming.

The name is optional, and is a name rather than text.

### Steps

```
call <member>[(<arguments>)] on [ <names> ]
set <member> on [ <names> ] to <value>
run <sequence>
stop <sequence>
await
wait <duration>
if <any|all|sum> <member> on [ <names> ] <comparison> <value> then {
    <steps>
}
```

A step starts something and the next statement follows immediately.

A `whenever`, a `when` and an `every` each start one step: `run`, `stop`, `call` or `set`, written as it is
here. `await`, `wait` and `if` belong to sequences alone.

The list after a member is every name the statement acts on or tests: a system's variables, or a
system blueprint's slots. The names stand for one set of blocks: every block any of them stands for,
each once, however many names reach it. The statement acts on, or tests, every block in the set.
`call Gearbox.close on [ inner, outer ]` closes both doors in one step, and so does
`call Gearbox.close on [ hangar ]` in an instance that answers the slot with two blocks:
`hangar = [ door2, door3 ]`.

A step whose line starts with `//`, after its indentation, is switched off. It is written and read as the step it is, under the same rules, and does nothing. Only a step inside a sequence can be switched off.

```
sequence 1 as Cycle {
    call Gearbox.close on [ inner ]
    await
    // call Gearbox.open on [ outer ]
    // await
    wait 60t
}
```

#### `call <member>[(<arguments>)] on [ <names> ]`

`call <member> on [ <names> ]` calls an action on the blocks. The member is the action's id, bare or
qualified by its namespace, as [Namespaces](#namespaces) sets out.

An action's arguments are written as [Members](#members) sets out.

```
call Gearbox.close on [ inner ]
call Gearbox.'rotate-to'(0, 90, 2) on [ arm ]
call Reverse on [ arm ]
call SetColor(255, 136, 0) on [ light ]
call Run("doors open") on [ pb ]
```

#### `set <member> on [ <names> ] to <value>`

`set <member> on [ <names> ] to <value>` writes one of the block's own values, by its id. A value
declares what kind it is, and it is written as that kind, or as an integer where it is real. An
id can name both an action and a value, and `set` always means the value. `set` on a value the host
offers only to be read is an error.

```
set Radius on [ light ] to 4.5
set OnOff on [ light ] to off
set Color on [ light ] to #FF8800
```

#### `run <sequence>`

`run <sequence>` starts a sequence of the same system and carries on. A sequence already running is
not started again. A sequence that would start itself, directly or through the sequences it runs, is
an error.

#### `stop <sequence>`

`stop <sequence>` stops a sequence of the same system where it is running, and carries on. A
sequence may stop itself, and ends there.

#### `await`

`await` waits for every action and every sequence started above it to finish.

#### `wait <duration>`

`wait <duration>` spends time. To let the doors finish and then pause, `await` and `wait` are two
statements.

```
call Gearbox.close on [ inner ]
await
call Gearbox.open on [ outer ]
wait 60t
call Run("doors open") on [ pb ]
```

#### `if <any|all|sum> <member> on [ <names> ] <comparison> <value> then`

```
sequence 1 as Cycle {
    if all Gearbox.pressurized on [ vent ] == on then {
        call Gearbox.close on [ inner ]
        await
        call Gearbox.depressurize on [ vent ]
        await
        call Gearbox.open on [ outer ]
    }
    else if all Gearbox.depressurized on [ vent ] == on then {
        call Gearbox.close on [ outer ]
        await
        call Gearbox.pressurize on [ vent ]
        await
        call Gearbox.open on [ inner ]
    }
    else {
        call Gearbox.close on [ inner, outer ]
    }
    await
    call SetColor(0, 255, 0) on [ light ]
}
```

`if <any|all|sum> <member> on [ <names> ] <comparison> <value> then` tests the member on the blocks
once, when the step is reached, and runs the statements between its braces where the test holds.
`<member>` is a value or a function, and `<value>` is written as the kind it gives back.

The comparison is `==`, `!=`, `<`, `>`, `<=` or `>=`. `==` and `!=` compare any kind; the other four
compare integers and reals, and are an error on any other kind.

With `any`, the test holding on one block is enough; with `all`, it must hold on every block. With
`sum`, the member is read on every block, the readings are added, and the total is compared once.
`sum` adds integers and reals, and is an error on anything else.

```
if sum Gearbox.amount("Ingot/Iron") on [ cargo ] >= 1000 then {
    call Gearbox.close on [ hatch ]
}
```

The statement after an `if`'s closing brace may be an `else if`, tested in turn where nothing before it
held, or an `else`, which runs where nothing held. An `else` comes last, and once. The sequence then carries
on after the whole `if`.

The statements between an `if`'s braces are steps like any other, an `if` among them. An `await` among them waits
for everything started above it in the sequence, inside the `if` or before it.

A `//` before an `if` switches off the whole of it, its `else if` and `else` included. A step inside can be
switched off on its own.

## A construct

What the program's construct has, and which of the system blueprints above it run on it.

### Variables

A variable names something on the program's construct once, so that the rest of the file can use that
name instead of the full definition behind it.

`let <name> = <value>` defines a variable, holding something on the program's construct, like a block
or a signal. A variable is defined once, before anything uses it, and every system and instance in
the file may use it.

```
let door1 = block 'Door/LargeBlockDoor' [ 74857693847562934, [ (3 0 -2) ], "Airlock Inner Door" ]
let door2 = block 'Door/LargeBlockDoor' [ 74857693847562935, [ (0 2 -1), (1 0 0) ], "Hangar Door" ]
let panel1 = block 'ButtonPanel/ButtonPanel' [ 74857693847562937, "Airlock Button" ]

# A signal from the panel above
let button = Gearbox.toolbar(1, 1, 4) on [ panel1 ]
```

`block '<Type/Subtype>' [ <keys> ]` is one actual block on the program's construct. The quoted value
is the block's type and subtype.

The list holds the ways to find the block again - its entity id, where it sits, its name in the game -
in the order they are to be tried: the first that finds a block decides. The language does not read
them, and a host may write whatever it recognizes its own blocks by.

Where a block sits is a list of cells: the cell of each mechanical joint crossed from the grid the
program runs on, then the block's own cell on its grid.

### Instances

An instance is one use of a system blueprint on the program's construct. It says which blocks answer
each of the blueprint's slots, and runs the blueprint on them. A system blueprint may have any number
of instances, each on blocks of its own: one airlock blueprint, and an instance for each airlock on
the ship.

```
instance <n> of <system blueprint> [as '<name>'] {
    <slot> = [ <answers> ]
}
```

`<slot> = [ <answers> ]` says what on the program's construct answers a slot of the system blueprint:
variables holding blocks, or, for a slot that expects `signal`, functions that give back a signal,
written in place, or variables holding one. More than one may answer a slot, and more than one answer to a
slot that expects `signal` means any of them starts it. An instance that leaves a required slot unanswered is missing
it.

```
instance 1 of MalTek.Airlock {
    inner = [ door1 ]
    hangar = [ door2, door3 ]
    button = [ Gearbox.toolbar(1, 1, 4) on [ panel1 ] ]
}
```
