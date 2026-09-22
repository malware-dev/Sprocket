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
folders by capability, member order, names, what a repeating path may do, and the questions to be
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

## Read the diff

Every added comment, against the rule that a comment says what the code **is**:

- No history. Not what the constant used to be, not what the previous shape did, not what was tried.
- No conversation. A measurement is a citation - "95.4% on an airlock, measured in game" - and never
  an anecdote about finding it.
- No second person, no "we".
- Not a restatement of the line under it.

**Grep the added comments before reading them**, because reading skims and a program does not:

```
git diff -U0 | grep "^+" | grep -iE "//.*(before|used to|previously|now|no longer|instead of|originally|we|you|I)"
```

Every hit is read in full and either rewritten or justified out loud. Most are innocent - "before the
next step" - and the few that are not are exactly the ones an eye slides over: "as it did before"
compares with a shape the reader cannot see, and reads as sense until it is looked at.

**Grep the added lines for state that outlives a call**, which is the rule hardest to see by reading:

```
git diff -U0 | grep "^+" | grep -nE "static.*(Dictionary|List|HashSet|Queue|Stack|Array)"
```

Every hit answers: **what owns this, and when does it die?** A `static readonly` holding an immutable
value is a constant. A collection added to at runtime is state, and it belongs to something with a
lifetime - the world, the unit, the runner - so that it dies with what it is about. That another
class already does it this way is not an answer: copying the flaw is how the second one appears.

Then the structural questions, in this order, because the early ones invalidate the later ones:

1. **Was an existing surface reinvented?** A new fault, status, reading or question about a block is
   a new *kind of thing*, never a new *mechanism*. `SequenceFault` and `SequenceProblem` report what
   is wrong with a sequence and which step. `IBlockExtension` holds knowledge about a kind of block.
   `Offer` is what a block declares. `Field` is what a value asks for. `Words/` holds every sentence
   a person reads. The tell is the feeling of designing a way to carry information from A to B.
2. **Does anything new run every tick?** It must answer both: is this worth a tick, and can the same
   thing be had at a lower cadence. `Interval` is how a cadence is expressed. A reading behind a
   short-circuit is cheap and the comment should say so; a reading on its own is not.
3. **User-visible words outside `Words/`.** Two views carrying the same sentence is the usual symptom.
4. **Member order**: constants, fields, constructors, properties, methods. A field among the
   properties is the common slip.
5. **Names**: a word, not a description, and no role suffixes.
6. **What the tests do not cover**, said out loud. The harness models no gas, no power and no
   conveyors, so anything resting on those is proved in the game or not at all.

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
