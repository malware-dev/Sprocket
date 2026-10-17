---
name: code-review
description: Review a change in this repository before it is committed - run the JetBrains inspection, read the diff against the standards, and report findings. Use whenever Mal asks for a code review, before committing a body of work, or after any change large enough that the rules are easy to lose sight of.
---

A review has two halves and needs both. The inspection finds what a machine can see; reading the
diff finds what only somebody holding the standards can. Neither substitutes for the other - the
inspection has caught a mutating call on a `readonly` struct that silently disabled a cadence, and
the read has caught a whole mechanism built beside the one that already existed.

Load **csharp-standards** and **sprocket-standards** first. This is how to apply them to a diff, not
a second copy of them.

**The job is to hold the change to every rule in both, not to the list below.** The diff passes only
where nothing in it breaks a standard: what a comment may say, what may be static, one type per file,
folders by capability, documentation, names, what a repeating path may do, and the questions to be
answered before changing anything. The sections that follow are the traps this repository falls into
most often, written down because they were missed - they are a prompt, never the whole of it. A
finding that breaks a rule not listed here is still a finding.

## Run the inspection

```
jb inspectcode Sprocket.slnx --swea --format=Sarif --output=<file> --severity=WARNING
```

Sarif, never Xml: the Xml report omits line numbers for some inspections, which hides them from
anything parsing it.

Then **filter to the files the change touched** - `git diff --name-only`. The whole-solution count
is not the subject of the review and reporting it buries what is.

Three results are worth knowing by name, because each has already been a real defect here:

- **`PossiblyImpureMethodCallOnReadonlyVariable`.** `Interval` is a struct. A `readonly Interval`
  field mutates a copy on every call, so the clock never advances and the thing runs every tick
  while the diff looks correct. Nothing else in the repository marks one `readonly`.
- **`UnusedAutoPropertyAccessor.Global`** on something just added means a value is being carried and
  then dropped. Ask what should be reading it rather than deleting it - the answer is usually a
  defect one layer further on.
- **`RedundantUsingDirective`** in a test project often means the project supplies it implicitly.

Anything flat out wrong gets a pragma or an `.editorconfig` entry with a reason. Nothing is left to
be re-reported forever.

## Check the translations

**Every commit keeps the Norwegian translation whole.** It is a requirement before a commit, as this
review is. The build says half of it: `LOC005` is a Norwegian text the English file lacks, `LOC006` an
English text with no Norwegian, and a change goes in with neither.

The other half no build can see: a Norwegian text that has gone stale. Every English text the diff adds
or changes in `MyTexts.resx` has its Norwegian written or rewritten in `MyTexts.no.resx` in the same
change, and is read against it.

## Check the codex

**Every change a player can see keeps the codex true.** For each one in the diff - a new button, a
screen that behaves differently, a rule that changed - name the codex page that covers it
(`Content/Data/Codex/en/`) and read what that page says against the change. A page left saying what is
no longer so is a finding, and so is a new function no page mentions where a player would look for it.
The page is mended in the same change, in both languages, by the `writing` skill.

A function added without its page goes unnoticed: `Update from library` reached the screens and no page
mentioned it until a reader test happened to ask how a construct gets a newer blueprint.

## Weigh every type the diff touches

A diff shows the hundred lines that changed, never the three thousand around them, so a type that grows
a little at a time passes every review that reads only the diff. SprocketEditor reached 3,691 lines that
way.

**Report the size of every type the diff touches**, in lines and public members, whether or not the diff
made it bigger.

**Then apply the split rule to each one**: do its members fall into groups that touch disjoint sets of
fields? Size is the prompt and the field groups are the test. A large type that is one thing is reported
with why; one that is several is a finding, even where the diff added none of its bulk.

## Read the diff

Every added comment, against the rule that a comment says what the code **is**:

- No history. Not what the constant used to be, not what the previous shape did, not what was tried.
- No conversation. A measurement is a citation - "95.4% on an airlock, measured in game" - and never
  an anecdote about finding it.
- No second person, no "we".
- Not a restatement of the line under it.

**Grep the added comments before reading them**, because reading skims and a program does not:

```
git diff -U0 | grep "^+" | grep -iE "//.*\b(before|used to|previously|now|no longer|instead of|originally|formerly|for a while|to come back|at first|at one point|we|our|ours|us|you|your|I)\b"
```

Every hit is read in full and either rewritten or justified out loud. Most are innocent - "before the
next step" - and the few that are not are exactly the ones an eye slides over: "as it did before"
compares with a shape the reader cannot see, and reads as sense until it is looked at.

**Every widening of that pattern was paid for.** `our\b` does not match "ours", which hid twelve
sites. History has idioms no list of verbs catches: a chevron that "carried the word BACK **for a
while**", rows that deepened until "it cost something **to come back**", a heading that said "Your
ship" **for a long time**. All three read as sense, and all three describe a shape the reader cannot
see. A hit that quotes words on a screen - naming a heading rather than addressing the reader -
stays.

**Read every summary alone.** A summary is read on its own, in a tooltip or a generated page, so it says
in full what its member is. A summary that leans on a sibling ("The same, for..."), one that defines its
type by what it is not, and two summaries on one member are all findings. A summary carried to a new
type is read again there: "this one", "the row's" and "this reader" name a place it no longer is.

```
git diff -U0 | grep "^+" | grep -E "/// <summary>(The same|Likewise|As above)"
```

**Grep the added lines for state that outlives a call**, which is the rule hardest to see by reading:

```
git diff -U0 | grep "^+" | grep -nE "(^|[^A-Za-z])static([^A-Za-z]|$)" | grep -vE "\bconst\b"
```

Every hit answers: **what owns this, and when does it die?** A `static readonly` holding an immutable
value is a constant. A collection added to at runtime is state, and it belongs to something with a
lifetime - the world, the unit, the runner - so that it dies with what it is about. That another
class already does it this way is not an answer: copying the flaw is how the second one appears.

**Every static, not only the collections.** A `static bool`, a `static int`, a static event and an
`Instance` are the same question with a smaller type, and a grep naming container types walks past
all four. Most hits are methods and constants and are dismissed in a glance, which is what makes the
wide pattern affordable. Where the game leaves no choice - a session component, a one-time
registration - the static stays and says at its declaration which requirement forces it.

Then the structural questions, in this order, because the early ones invalidate the later ones:

1. **Was an existing surface reinvented?** A new fault, status, reading or question about a block is
   a new *kind of thing*, never a new *mechanism*. `SequenceFault` and `SequenceProblem` report what
   is wrong with a sequence and which step. `Capability` holds knowledge about a kind of block.
   `Offer` is what a block declares. `Field` is what a value asks for. `Features/Sentences/` decides how a
   sentence is composed. The tell is the feeling of designing a way to carry information from A to B.
2. **Does every new type belong where it was put?** A UI type that names nothing of Sprocket belongs in
   Lumel; see sprocket-standards.
3. **Does anything new run every tick?** It must answer both: is this worth a tick, and can the same
   thing be had at a lower cadence. `Interval` is how a cadence is expressed. A reading behind a
   short-circuit is cheap and the comment should say so; a reading on its own is not.
4. **The same sentence in two places.** Two views carrying one sentence is the symptom. Where a
   phrase is written is not a finding: the text is legacy and due a complete rework, done on the
   screens rather than string by string.
5. **Names**: each says what the thing is to a reader who has not opened it. A one-word type name
   that needs explaining is a finding.
6. **What the tests do not cover**, said out loud. The harness models no power and no conveyors, and
   gas only as far as the flow through a vent, so anything resting on more than that is proved in the
   game or not at all.

## When the subject is the whole codebase

A pass over everything is not this review made bigger, and `git diff` says nothing about it.
**csharp-standards, "Cleaning up existing code", governs it**: behaviour does not change, every
change is proved rather than believed, any difference is a defect in the pass, and no test is ever
weakened to make a change pass. The evidence here is `dotnet test` and the reference suite, and a
pass that touches drawing re-shoots the suite and expects no pixel to move.

The mechanical half runs over the tree instead of a diff - the inspection, the two greps above, one
type per file, folders by capability, names - and it is cheap enough to run before deciding whether
to buy any reading. The findings go in a file as the pass goes, and what could not be proved is
named there rather than left implied.

## Report

Findings grouped by whether they need judgement, most consequential first. Say which are fixed and
which are left. Do not use the word "pre-existing": a finding is correct or incorrect, and who wrote
it is not a category.

**Then fix what needs no judgement and leave the tree dirty.** A review ends with a report and an
unstaged diff; committing is a separate instruction.

## The rule this exists to replace

I wrote this codebase. "I do not remember whether that already exists" means go and read it, never
invent it. Every restatement of a standard in conversation is a review that should have happened
here instead.
