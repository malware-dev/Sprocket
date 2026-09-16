# Presentation

Reactor as a person sees and touches it: the layout, the drawing, the controls, the views, and every
word on screen.

`Core/` says a sentence refers to these blocks and this action; this decides that it reads "close the
inner door", in that order, in those colours, with those parts pressable.

## Where the words live

`Words/`, and nowhere else. `SentenceText` words a sentence, `SequenceText` words a sequence where it
can only be read, `TimeText` words a length of time. One file per thing the words are about, so none
of them becomes the place any string goes when it needs a home.

A second language, or a host that phrases things differently, is a change inside that folder and
nothing above it moves.

## What it draws through

The game's screen-space surface, in `Graphics/ScreenSpaceGraphics.cs` alone. Everything else here
hands that class shapes and text.

**That one file is why the harness compiles these sources rather than referencing the mod's
assembly**: the harness declares its own screen-space types so a frame can be recorded instead of
drawn, and a declaration like that only wins inside the assembly that makes it. An abstraction for
drawing would let the harness install a recorder instead, and is the next thing to do here.

## What must never appear in it

**A session, a component, or an object builder** - anything that only exists inside a running game.
The day something here needs one, that is a design change rather than a convenience.
