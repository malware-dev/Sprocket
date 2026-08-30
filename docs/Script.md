# The script, and everything that follows from it

Design worked out in conversation and **not yet built**. Nothing here is in the code. It began as a
question about localisation and turned into something larger, because the localisation problem was a
symptom.

## The finding

**There is the script, and there is the presentation. They are completely separate, and core owns
only the first.**

A sequence, seen by the runtime, is ids and nodes: this trigger, these entity references, this state
id, this action id, this gate, this many ticks. No words, no word order, no reading order. What a
person reads is a *rendering* of that, and rendering is the host's.

Core does not hold to this today. `SentenceSlot` is `Text` + `IsEmpty` + `Command` - a rendered
English fragment with a press target stapled to it - and `SentenceTrigger.Lead` and
`SentenceStep.Lead` are words. So `SequenceComponent` never handed a target the script. It handed
over a finished sentence and asked the host to arrange boxes around it, with word order already
fixed by `Slots` being an ordered list.

The circularity that proves it:

```csharp
var state = entity.Kind.FindState(condition.StateId);
text = state.Text;
```

`EntityKind` is built by the **host** - `BlockKinds.WithState("is-working", …, "is working")`. Core
reaches into the host's vocabulary, pulls out the host's words, arranges them in English word order,
and hands the result back to the host to draw. The words were never core's. Core was only doing the
arranging, which is the part most obviously not its job.

## What that settles about localisation

The localisation question dissolves rather than being answered. **Core has no text at all**, so
there is nothing in core to localise, no phrasebook interface, no ids to key words by, and no
resource mechanism for core to carry.

Each host then localises however its platform allows, and they need not agree: a Space Engineers mod
has `.sbc` localisation, a future MDK2 Hub / net10 / Avalonia build would have .NET `resx`. Any
abstraction spanning both would be their intersection, which is a worse thing than either, carried
by everyone forever.

**For V1 the Space Engineers host hardcodes English.** That is not debt. It is a host doing its job
in the only language it currently speaks, and replacing it later is a change inside one host.

Rejected on the way, and worth recording so they are not re-proposed:

- **A phrasebook on the host** - `string Word(string id)` beside `IWorld.Noun`. Rejected because it
  is still an abstraction core carries, and it needs an id per string, which sprawls.
- **Ids with English defaults in core.** Rejected: a default nobody overrides is the current problem
  wearing a label.
- **Core wording the sentence because it is "meaning, not presentation"** - the reasoning recorded
  in `SequenceText`. The split was drawn one notch too far into words. The meaning is the model; the
  sentence is a rendering of it, and word order proves it - German puts the verb last, and core
  owning the meaning does not change where the verb goes.

## What has to change in core

**`SentenceSlot.Text` is the root.** Delete it and most of the rest follows: `DescribeEntities`,
`DescribeCondition`, `DescribeAction`, the `" and "` and `" or "` joins, `SequenceSummary`'s
flattening, and the two-representations problem its own comment already worries about.

A slot becomes what it *refers to* plus its command. Every slot is one of four things: some
entities, a state, an action, or a quantity. `PickComponent` already hands over entity ids rather
than names, so the surface has precedent for exactly this shape.

Also core's, and smaller:

| Where | Today | Becomes |
|---|---|---|
| `Cadences` | names `fast`/`steady`, and a `Warning` string | id, ticks, native, and a graded `Cost` |
| `Elapsed` | `just now`, `a minute ago` | ticks |
| `SystemSummary.Count` | `"fitted " + n + " times"` | the number |
| `RenamePrompt` | three wordings of one act | nothing; `CanRename` and `NameRequired` are the facts |
| `EmptyText` | sentences | a reason - *nothing yet* and *nothing matched* are different |
| Undo labels | `"changing a step"` | the edit kind; the types already exist |

**A graded fact replaces both the warning and the recommendation.** `Cost.Heavy` says everything the
warning paragraph said and lets a host answer with a paragraph, a red dot, or silence. Whether to
warn stops being core's business without core losing what it knows. No separate "recommended" flag:
the grade is the recommendation.

**Conformance is how the shape stays enforceable** without core holding words - a point saying a
target must render a sequence as a sentence, trigger first, steps in order. Same trade as
`PaperTarget`: a stated requirement that breaks something when unanswered.

**What is given up, honestly:** core stops being where Reactor's language is decided. With one host
that costs nothing. With two it would be a quiet failure - both phrasing an airlock plausibly and
differently, unnoticed until somebody used both.

## The stored script

`ProgramText` already writes the script correctly - ids, nodes, and keywords like `once` and `onlyif`
that are tokens rather than prose. It is right in exactly the way `SentenceSlot` is wrong. Core has
one correct serialisation and one accidental presentation layer beside it; only the second is the
problem.

The format should become **an actual script language**, not prose. A script language's keywords are
*syntax*: nobody localises `if` or `while`, so `when` and `after` being core's is core owning its own
grammar, which is right. **The trap to avoid is "readable" drifting towards English** - that would
put word order and vocabulary back into core and freeze them in a file format, where they become a
compatibility promise.

A worked example, exercising everything settled:

```
reactor 1

# The door onto the ship. Not the one to the outside.
#
# If yours opens onto a corridor rather than a room, use that one - what matters
# is that it is the side you are leaving.
block Door/LargeBlockDoor      inner "the inner door" [ 748576938475629, (3 0 -2), "Airlock Inner Door" ]
block Door/LargeBlockDoor      outer "the outer door" [ 748576938475631, (3 0 2) ]
block AirVent/LargeAirVent     vent  "the vent"       [ 748576938475630 ]
block ButtonPanel/"SciFi Wide" call  "the button"     [ 748576938475632 ]

# Leave this empty if your airlock has no light.
block InteriorLight/SmallLight warn  "the warning light" optional [ ]

# Cycles an airlock. Nothing opens on both sides at once, because each sequence
# locks the side it is not using.
system Airlock on {

    # The one by the bridge.
    instance 1 "Forward airlock" on {
        block Door/LargeBlockDoor  myinner [ 91 ]
        block Door/LargeBlockDoor  myouter [ 92 ]
        block AirVent/LargeAirVent myvent  [ 93 ]
        block ButtonPanel/"SciFi Wide" mycall [ 94 ]

        fill inner [myinner]
        fill outer [myouter]
        fill vent  [myvent]
        fill call  [mycall]
    }

    # Inside to outside.
    when any pressed [call] on {
        close [inner] wait
        lock [inner] now

        # The vent needs the full cycle or the outer door sticks.
        depressurize [vent] wait
        after 60

        run "Warn"
        unlock [outer] now
        open [outer] wait
    }

    # In case somebody left it open, and again when the ship starts moving.
    every 1800 run "Tidy up"
    when any moving [ship] run "Tidy up"

    sequence "Tidy up" {
        only if is-open [outer]
        close [outer] wait
    }

    sequence "Warn" {
        turn-on [warn] now
        once is-shut [outer]
        turn-off [warn] now
    }
}
```

Everything designed is in there: three trigger forms, both gates, `wait` and `now`, `after`, an
optional role with an empty match list, a quoted identifier for a subtype with a space, matchers of
three types in priority order, annotations at four levels including a paragraph break, an inline
sequence under its trigger, two triggers on one named sequence, one named sequence calling another,
and an instance whose fills point at declarations rather than at raw ids.

- **`{ }` is a code block** - statements, in order. Position is sequence, so a step needs no keyword
  and no number; renumbering on insert stops being something the format represents.
- **`[ ]` is a list of values.** It earns itself twice over: a step acting on several blocks is a
  value list inside a statement, and a reference's alternatives are a value list in priority order.
- Gates and `wait` are **modifiers on a statement**, which is what they are in the model, rather than
  siblings that follow one.

Two things in the example are not settled. **`instance` is a proposed word** - the current format
says `one`, which reads badly in a language, but renaming a model concept is not a thing to do in
passing. And **`Real` and `Vector` have no use site yet**: a condition today is *these entities are
in this declared state*, with the operand baked into the `EntityState` the host declared. Those types
arrive properly with expressions and with `set`.

## Block references

**This is the part that cannot look like a normal script language.** A block reference is not a
value. It is a claim about which thing in the world you meant, and it must survive renames,
rebuilds, blueprints, and being handed to somebody else.

Today core stores a `long` and nothing else, and already knows the claim can fail - three separate
messages say *"isn't here any more"*. The failure exists; it has no gradations.

**A reference is a declaration, not a literal**, with three structural parts:

```
block <kind-id> <identifier> [ …alternatives… ]
```

- The **identifier** is structurally required, because the body has to point at a declaration for the
  parser to work. Nothing reads meaning from it. How it is generated is open - `block1`, something
  derived from the block's description later, whatever is chosen - and it may be regenerated freely
  on each save.
- The **kind** is a clause on the header, not an entry in the list, because it is not a matcher. It
  is a *constraint on whatever binds* - so a name match landing on a non-door is rejected before it
  can bind - and it is the one thing that is useful when every matcher has failed, because it tells
  a person what belongs in the slot. That feeds `PickStage`, which already filters by kind.
- The **alternatives** are a plain ordered list: match these, in this priority.

**The list is untagged on purpose, and this is the load-bearing decision.** Core must not know what
a block is. It parses values and lists of values and stores what it read; the host minted the array
and the host interprets it. So the order, the meaning of each entry, and every future matcher are
the host's, and can change without core or the grammar moving. A tagged list - `id …`, `at …`,
`name …` - would smuggle identity semantics into core's parser.

The corollary: **core cannot validate a reference**, and two hosts write arrays meaningless to each
other. Both are already true of action ids and event ids, which core also stores opaquely, so this
is consistent rather than new.

**An empty list must be legal**, so a declaration can exist unresolved. It costs nothing now and it
is the state a configurator will need.

### Resolution has three states, where there are two today

**Bound** - a matcher resolved. **Adrift** - none did, but something matches by a weaker key.
**Lost** - nothing does.

The middle is new and is where the value is. The rule that makes it safe: **a fallback never rebinds
by itself.** If the id is gone and a different door now carries the old name, silently rebinding
cycles the wrong door on somebody's airlock. It is a proposal the editor puts in front of you, and a
sequence with an adrift reference is blocked and says so - which is what the problem line already
exists for.

On keys, one is better than it looks: **grid-relative position survives blueprinting**, where the id
does not. Paste a copy of a ship and every id changes while every position is identical. Name is
weakest, because it is the one a player edits casually.

## Value types

**One value system, not two.** Core understands six: `Flag`, `Integer`, `Real`, `Text`, `Vector`,
`Color`.
The host decides what any of them mean.

This reverses a leaning recorded earlier. The argument for a second, script-only system was that one
system would force every reading to answer "what does it mean to compare a vector" - and there is no
honest answer, since `below` on a position is meaningless. But **that machinery arrives anyway** the
moment somebody can write an expression, and once a table exists saying which operators a type
supports, that combination is a cell in it rather than a special burden.

The rest of the case for two systems does not survive either. Precision was never about operators; it
is about representation, and is fixed by an integer type that a script needs regardless. Vector is not
script-only - a rotor angle is a real, but a ship's position is a vector, and *when the ship is above
2000* is an obvious future reading. And two systems means a conversion where a literal becomes an
operand, which is precisely where precision quietly dies.

`ValueKind`'s existing `Number` is renamed **`Real`**, because the whole point of the change is that
the distinction is visible and `Number` is the name that hid it.

### Integer and real are genuinely different, and this is not tidiness

A `double` holds integers exactly only to 2^53, about 9.0x10^15. Space Engineers entity ids run to
seventeen digits - `74857693847562934` is 7.5x10^16, past it. An id round-tripped through a double
comes back a *different id*, silently, and resolves to nothing or to the wrong block.

The current save path is safe by accident: `Ids()` writes `long.ToString()` and never touches
`EntityValue`. Anything that unifies them without adding `Integer` creates exactly that corruption,
and it looks like data damage rather than a type error.

### The operator table

Core owns a table of which operators apply to which types - `Below` on a `Real` yes, on a `Vector` no,
on `Text` no; `Equals` on everything. It is core's because it is a property of the type system rather
than of any game.

It is load-bearing rather than future work, and two things follow that are cheap now and awkward
later:

**The host declares types, core enforces combinations.** A host says a property reads as a `Real`;
core says what may be asked of a `Real`. Today there is no table because the host declares whole
`EntityState`s and never declares nonsense. With expressions the user composes the comparison, so
core has to know what is offerable - which is the same list the picker needs to show only the
relations that mean something for the property just chosen.

**Unsupported is a stated answer, not an absence.** The table saying *no* is what lets the editor
explain why a relation is not on offer, rather than silently having a shorter list. Same shape as
`Blocked` and the problem line: a refusal that can say why.

## Canonical, not a projection

Settled, and it is forced rather than chosen: **there is nothing else to project from.** The model
lives in memory in a mod the game may unload at any moment, `MyModStorageComponent` is the only
durable thing, and what goes in it is the script. If the parser fails there is no second copy.

Three consequences:

**The parser must never lose more than it has to.** A construct it cannot understand is skipped and
everything else survives - which `{ }` gives and the current line-oriented format does not, since
"skip unknown lines" eats a step's gate and keeps the step.

**A failed parse must not overwrite.** `Save` runs whenever the program might have changed, so a
half-failed load followed by any edit writes the damage over the file. A program that did not load
cleanly has to be marked not-safely-writable until somebody has looked at it.

**It argues for an export, not for moving the canonical copy.** "A save that breaks can be looked at
and fixed by hand" is only true if a person can reach it, and inside the world XML they barely can.

## The grammar

```
file        := version construct*
construct   := annotation? statement
statement   := head value* newline
             | head value* block
block       := "{" newline construct* "}" newline
value       := integer | real | text | vector | colour | list | identifier
vector      := "(" number number number ")"
colour      := "#" hex{6} | "#" hex{8}
list        := "[" value ("," value)* "]"
annotation  := ("#" (" " line)? newline)+
```

Tokens are identifier, integer, real, text, vector, colour, `{ } [ ] ( ) ,`, annotation lines, and
newline. One page, one
small file. Hand-rolled: C# 6, no regex, no LINQ.

**Newline ends a statement, and there are no continuations.** No semicolons, no line joining. A
statement too long for a line means the model is too complex for one, which is worth hearing.

**Strings quote by doubling, and there is no escape character.** Backslash is an ordinary character
with no meaning. This is one rule instead of two, and the rule it removes is the dangerous one: a
writer that forgets to escape a backslash produces a file that still *parses*, just wrong - a Windows
path reading back as something else. Silent corruption. Doubling can only fail loudly.

**An identifier may always be written as a string.** A quoted identifier is legal anywhere a bare one
is. That single rule handles host ids with odd characters, subtypes with spaces, and any collision
with a keyword, so keywords stay contextual rather than reserved.

**A dot or an exponent makes a real.** `60` is an integer, `0.25` and `1E-05` are reals. Scientific
notation is forced rather than chosen: a writer emitting a `double` round-trippably produces `1E-05`
on its own, so refusing to parse it means we cannot read back what we wrote. The alternative is a
fixed decimal format, which loses precision, which is the one thing this format exists not to do.

**Everything parses and formats with the invariant culture, both ways, always.** A player whose
decimal separator is a comma writing `0,25` and reading back `25` is the same silent corruption the
string rule was designed to avoid, and it would only ever appear on somebody else's machine.

**An identifier starts with a letter**, so `-` is unambiguously part of a number rather than the start
of a name.

**Vectors are parenthesised: `(3 0 -2)`.** Bare juxtaposition would do inside a comma-separated list,
but a vector is a general value now - `only if ship above (0 2000 0)` has no commas to separate it
from a statement's arguments.

**Colours are written the way everybody writes them: `#FF8800`, or `#FF8800CC` with alpha.** Six or
eight hex digits, and no three-digit shorthand - a writer never emits shorthand, so accepting it is
a branch that exists only to be lenient about something nobody produces.

**`#` is disambiguated by one character of lookahead, not by position in the line.** A `#` followed
by a space or a newline is an annotation; a `#` followed by a hex digit is a colour. Nothing else is
either.

The obvious alternative - annotations are `#` at the start of a line, colours are `#` anywhere else -
works only while a list cannot span lines, and quietly makes that constraint load-bearing. Lookahead
keeps the lexer context-free, leaves multi-line lists possible if they are ever wanted, and puts the
two rules in the right layers: the lexer decides what a `#` token is, the parser decides that
annotations must be contiguous and precede a construct.

The cost is one wart: `#comment` with no space is a parse error rather than an annotation. A writer
always emits `# `, so it only bites somebody hand-editing who forgets the space, and it fails
loudly.

**`{ }` is a code block** - statements in order, so position is sequence and a step needs no keyword
and no number. Renumbering on insert stops being something the format represents.

**`[ ]` is a list of values**, and it earns itself twice: a step acting on several blocks is a value
list inside a statement, and a reference's alternatives are a value list in priority order.

**Gates and `wait` are modifiers on a statement**, which is what they are in the model, rather than
siblings that follow one.

## Annotations

**There are no comments.** The format is readable for *diagnosis*, not for authoring - nobody is meant
to write one by hand - so comments would be a parser feature serving a workflow we are not offering.
A stray `#` in the wrong place is a parse error rather than being ignored.

What text-for-people *is* needed for is annotations, and an annotation is not a comment. It is **data
somebody typed**: part of the model, shown by a target, edited there, round-tripped like a name. Half
of it exists already - `AutomationSystem.Description` is declared, serialised, loaded, and shown as
`SystemPage.Subtitle`, and **nothing in the editor can set it.** Only the loader writes it. The slot
is there; the way to type into it is not.

That field is renamed **`Annotation`**, and sequences and steps gain the same. What a target *labels*
it on screen - "description", "notes", anything, in any language - is the host's word and has no
bearing on the model's, which is the rule settled above applied to naming rather than to text.

```
# Cycles the airlock. The outer door will not open while the inner is unlocked.
system Airlock on {

    # Inside to outside.
    #
    # Waits for the vent before unlocking, because the outer door sticks otherwise.
    when any pressed [call] on {
        close [inner, outer] wait

        # the vent needs the full sixty seconds
        depressurize [vent] wait
    }
}
```

**An annotation is one or more consecutive `#` lines immediately before a construct, with nothing
between.** Contiguity is what makes it an annotation rather than drifting text - the moment a blank
line can sit between, it can attach to the wrong thing. A bare `#` is an empty paragraph line inside
the block, which is how spacing works without breaking the attachment.

- **An orphan is a parse error** - a blank line after an annotation block, or one at the end of a
  `{ }` or of the file. There is no state where text exists and nobody knows what it belongs to.
- **Leading and trailing empty paragraph lines are trimmed** when stored, so the form round-trips.
- **The text is everything after the `#` and one optional space**, so a writer emitting `# ` and the
  line round-trips exactly.
- **An empty description emits nothing**, so unannotated constructs cost no blank lines.

The word is deliberate. A *comment* sits near something; an *annotation* is made **on** a thing, and
that attachment is the whole property. It is also established usage - Java's and Python's annotations
are metadata attached to a declaration that tools read. The nuance: in those languages an annotation
is usually structured metadata rather than prose, so if Reactor ever grows genuine key-value metadata
that wants a different word, and this one stays with the prose.

One term, everywhere in the model and the format. A proposal to call the field `Description` because
that is the friendlier word for a player was rejected for the right reason: **core does not decide
what the editor says.** The host owns every word a person reads, so the model's name and the screen's
label are unrelated questions, and the model's name should be what the thing is.

**Available for free, not yet modelled:** a block declaration can carry one too, which is genuinely
useful when a reference goes adrift and somebody has to work out which door was meant. That is an
addition to the model, not to the format.

**The honest cost:** somebody hand-editing will write a `#` expecting a comment and it will become a
description that shows up in the UI. That is the right failure - visible rather than vanishing - but
the format has no way to say something *only* to a reader.

## Triggers run sequences

A trigger is not part of a sequence. **A trigger runs one.** That distinction decides the shape of
the language, and the model does not hold to it today.

`README.md` already states the intent: *"a name makes a list legible and lets more than one trigger
start the same sequence"*. The model cannot express that - `Sequence` has exactly one `Trigger`, and
`CalledTrigger` is a stand-in for *no trigger, wait to be called*, which is a trigger type meaning
the absence of one.

So:

- **An anonymous sequence is inline.** It lives under its trigger and exists nowhere else, because it
  has no name and therefore nowhere to be called from.
- **A named sequence stands alone** and can be run from several places.

```
when any pressed [call] {
    close [inner] wait
}

every 1800 run "Tidy up"
when any moving [ship] run "Tidy up"

sequence "Tidy up" {
    only if is-open [outer]
    close [outer] wait
}
```

Three things follow:

**`called` disappears.** A sequence with no trigger is callable; that is what having a name means.

**Two triggers on one sequence becomes sayable**, which is what the README promised.

**Inline versus named stops being a field.** Today `HasName` decides how a sequence reads. Here it is
structural.

**The switch belongs to the trigger.** A sequence is a procedure, and a procedure has no on/off
state - it either gets run or it does not. What has a state is the thing that decides to run it. So
`Sequence.Enabled` moves onto the trigger, and syntactically the switch is a modifier in the same
slot as the others:

```
when any pressed [call] off {
    close [inner] wait
}
```

The failure it avoids is the deciding part: with a switch on the sequence, a step saying
`run "Warn"` would silently do nothing. Reactor does not do silent nothing anywhere else - a step
waits for its own action, an interlock is real actions, a refusal says why - so a call that quietly
evaporated would be the one place it lied.

**Editing stops being gated by a switch and starts being gated by reachability.** Today the
sequence's own switch does that job: `FinishSequence` parks a new sequence so a half-built one cannot
fire, and the editing affordances disappear while it runs. For an inline sequence nothing changes -
trigger and sequence are one construct and the switch sits right there. For a **named** one, *can
this fire while I am editing it* becomes *is any trigger that reaches it armed*, which is derived
rather than stored, and is transitive through `run` - with the usual cycle risk when two sequences
call each other.

**The switches above are untouched.** `AutomationSystem.Enabled` and `SystemInstance.Enabled` still
make sense: those are collections, and switching a collection off switches off everything under it.
It is specifically the sequence that had no business having one.

**The argument on the other side, for the record:** with three triggers on a sequence, stopping that
behaviour means three switches rather than one. That is real, and the answer is that three ways in
are honestly three things to disarm - and that a trigger pointing at something unreachable should say
so, which is Reactor's usual move rather than a new one.

## Every block reference is a declaration

Including an instance's fills. An earlier draft wrote `fill inner [ 91 ]`, where `91` is a raw entity
id on the player's construct - which is the rule forgotten the moment the entity belonged to the
player rather than the author. Those blocks are renamed, rebuilt and blueprinted exactly like the
author's, so they need reference records exactly as much.

**Roles are already in the model, and they are not declared - they are derived.** `SystemRole` says
it: a role exists *because a sequence acts on something*, and its id is the entity the author built
with. So a block declaration and a role are the same construct seen twice. What is authored is only
the wording - `Name` and `Instructions` - and `Instructions` is what an annotation on a block
declaration carries.

## What the terminal surface forces

From `docs/Catalogue.md`, observed rather than anticipated, and it settles three open points.

**Quoted identifiers are mandatory, not an escape hatch.** The interior light ships properties called
`Blink Interval` and `Blink Lenght` - with a space, and with the game's own misspelling. `alignment`
sits lowercase among PascalCase neighbours. So a host id routinely cannot be a bare identifier. The
writer quotes mechanically: bare if it matches `[A-Za-z][A-Za-z0-9_-]*`, quoted otherwise; the reader
accepts either.

**Ids are never transformed.** No case normalisation, no trimming, no correcting `Lenght`. A round
trip is byte-exact. Wording a person sees comes from the game's control title or from an author,
never from prettifying an id - which is the script-versus-presentation rule arriving from the game's
side.

**`ITerminalProperty.TypeName` confirms the value system**, so `Integer` is no longer argued for on
entity-id grounds alone:

| Game | Ours |
|---|---|
| `Boolean` | `Flag` |
| `Single` | `Real` |
| `Int64` | `Integer` |
| `StringBuilder` | `Text` |
| `Color` | `Color` |

**`Color` is the sixth type**, settled. A `Vector` would lose alpha and pretend a colour is a
position; a packed `Integer` is unreadable in a file and unorderable in a comparison. It supports
equality and nothing else, which is a very small row in the operator table, and `#FF8800` being
readable where `4294934528` is not is itself an argument for the type being real.

**Actions come in triples** - `Open`, `Open_On`, `Open_Off` - and they are derived from a boolean
property of the same name.

**The bare toggle is out, whatever else is decided.** A stored "toggle the door" depends on state its
author could not see, and that is true regardless of how anything is worded.

**Whether the `_On`/`_Off` pair is offered as two actions or collapsed into the property is open,
and it is an empirical question rather than an argument.** `ITerminalAction` carries an `Id` and a
`Name`. If the game titles `Open_Off` as "Close", keeping the actions is plainly better. If it
titles it "Open Off", there is nowhere good to word it from and the property plus `set` wins, since
that gives a host a control and a value it can word properly. **Go and look at the real titles before
deciding.**

Two things to carry into that decision. It only affects the **base** layer - `BlockExtensions`
already gives a door real verbs, so this is about a block from a mod nobody has heard of, which is
the case with nobody looking after it. And collapsing would make `set` load-bearing rather than a
convenience, so it would have to land first or an unknown block goes backwards.

If it is ever collapsed, the filter is a naming convention rather than a guarantee - a mod may ship
`Open_On` with no property behind it - so it should fail *open*: anything that does not match the
shape exactly stays offered.

**`set` is reserved and not designed.** Terminal properties are settable, and the value can be any of
those five types - which is why all of them have to exist in the language rather than only the ones
a match list needs. Shape waits for the editor to reach it.

**Numeric readings arrive with their bounds** - `GetMinimum`, `GetMaximum`, `GetDefaultValue` exist
for every numeric property. Not a language question, but it matters to the operator table: a `Real`
reading can carry its range, so a threshold can be offered against real limits without anybody
authoring them.

## Still open

Two, and both are blocked on the same thing rather than on argument.

**What a kind means as a constraint.** `Door/LargeBlockDoor` is a definition, right for finding the
same block again. A premade airlock does not need that exact door - it needs *something that opens
and closes*. The host has both levels already: `EntityKind` is per definition, `BlockExtensions`
decides a block behaves like a door. Whether the constraint is the definition, the capability, or
both, is a configurator question.

**The instance configuration system is deliberately undesigned.** There is a lot to do before we
know what must be configured; we only know the blocks. So the format must claim only that it binds
blocks, and must be able to grow without a break. Named instances may turn out to be rare or
impossible. Designing the configurator by implication through the file format would be worse than
designing it deliberately, because a format is a promise.

**What `set` looks like.** Reserved, not designed; it waits for the editor to reach it. One thing to
avoid while nothing depends on it: every statement so far is `verb [targets] modifiers`, and
`set Open false [inner]` would put the targets last.

**Whether property-derived action pairs are collapsed into the property.** Answerable by looking at
the game'''s real action titles, not by argument.

Everything else that was open is settled above: the value system is one system with an operator
table including `Color`, the script is canonical rather than a projection, the grammar is fixed,
triggers run sequences rather than belonging to them, and the switch belongs to the trigger.

## Order of work, when it starts

1. **Take the presentation out of core** - `SentenceSlot.Text` first, since the rest follows it.
2. **The small ones:** `Cadence`, `Elapsed`, `SystemSummary.Count`, prompts, empty text, undo labels.
3. **`ValueKind` gains `Integer` and `Vector`, `Number` becomes `Real`**, and the operator table
   arrives with them. Independent of the rest and worth doing early, because every later step that
   touches a literal is safer once integers cannot be doubles.
4. **`Description` becomes `Annotation`**, and sequences and steps gain one. Small, and it unblocks
   the editor being able to set what the loader has been able to write all along.
5. **References become opaque host-minted values rather than `long`**, which threads through the
   model, the runtime and the save format. This is the big one, and it is what makes a shareable
   automation reachable at all.
6. **The script language**, once 5 has settled what a reference is.
