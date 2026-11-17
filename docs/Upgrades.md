# Upgrades

Players' saves outlive builds, and testers skip drafts. Every program an earlier supported format wrote keeps loading,
meaning what it meant. A change to the language is a permanent cost, so the spec changes first, with the maintainer's
approval of its exact wording ([Language](Language.md)), before any reader or writer code.

## The version line

A stored program starts with `sprocket <format> [alpha <draft>]`. `ScriptFormat.Version` and `ScriptFormat.Draft`
(`Framework/Language/Format/ScriptFormat.cs`) hold what this build writes. A finished format drops `alpha` and counts
as later than all its drafts: `sprocket 1 alpha 999` is older than `sprocket 1`.

A reader checks the version line first, and refuses a program from a draft it doesn't know, keeping the text as written.
So the draft goes up for anything an older build couldn't read safely: a change that makes a program read differently
or not at all, and every addition too (a keyword, syntax, a member), so an older build refuses it cleanly instead of
misreading it. The format number goes up when a stable release finishes a version: the stable build writes it without
`alpha`, and Bleeding Edge moves on to the next version's first draft, `sprocket 2 alpha 1` after a stable `sprocket 1`.
Each version's changes then stay the permanent upgrade into the next.

## Upgrades

A change to what a program says, or means, comes with its upgrade in the same commit. Meaning counts: a renamed id, a
member moved to another type, or a slot that now holds something else is a change even where the text looks the same.
An unchanged draft number proves nothing; before publishing, diff what a program can carry (values, actions, signals,
keywords, what slots hold) against the last published build.

Each upgrade is a `FormatChange` in `Framework/Language/Format/Upgrades/`, saying the draft it arrived in, added last
to `FormatUpgrades`. It may rewrite the statements before they are read (`Statements`), so the reader only knows the
latest syntax, or the program after (`Program`), with the world's blocks to hand. `PowerValue.cs` is a small example.
On load, every upgrade newer than the program's draft runs in order, and the program is written in the current format
at its next save. Upgrades are never removed. Programs older than `FormatUpgrades.Oldest` are refused and kept as they
were.

## Repairs

What no upgrade can carry over safely becomes a repair: a warning on what needs fixing, leading the player to the fix.
Each is one `Repair` in `Framework/Language/Format/Repairs/`, added last to `ProgramRepairs` and never removed while a
stored program can hold its form. It holds its problem, where it is, its check, its words, the question asked before
the picker opens, and what the picker does differently while mending it. An upgrade fixes what it safely can first.
The editor opens and edits every form the format allows, and never misreads one silently.

## Tests

Every upgrade and every repair has a test in `tests/Sprocket.Core.Tests/UpgradeTests.cs`: the old text, what it
became, and that it's written back in the current format. A repair's test holds the old form, its warning, its
question and its fix.

## The world's memory

The world's memory isn't text and has no version line. A `ProtoMember` number, once used, is never used again or given
another type, and a stored enum sets a number on every value that never changes. Anything new Sprocket writes to a save
is agreed with the maintainer before it's built. See [Saves and storage](Saves.md).
