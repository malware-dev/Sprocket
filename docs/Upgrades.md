# Script Upgrades and Backward Compatibility

Sprocket programs are stored directly inside programmable blocks in saved worlds. Because player saves outlive builds of the mod, players and servers frequently skip intermediate releases or drafts. Every program written by an earlier supported format must continue to load without silent breakage or lost logic.

---

## The Cost of Language Changes

Modifying the language, whether renaming an identifier, adjusting grammar, or adding reserved keywords, carries a high, permanent cost. Every change permanently expands the upgrade pipeline, increases parser maintenance overhead, and risks breaking existing player programs or historical saves.

Language changes are not routine refactors. Syntax and vocabulary additions must never be introduced casually or as convenient shortcuts. Before proposing any modification to the language surface or storage representation, exhaust all design alternatives within the existing grammar. Any change to the language specification or on-disk format requires explicit consensus and maintainer sign-off before implementation begins.

---

## The Version Line

Every stored script begins with a version declaration on the first line:

```sprocket
sprocket <format> [alpha <draft>]
```

* `<format>` is the major format version integer (currently `1`). It increments only if a change would break existing programs outright.
* `alpha <draft>` marks an in-development, unfinished format version, where `<draft>` is the draft iteration (currently `alpha 19`).
* The numbers written by the current build are defined by `ScriptFormat.Version` and `ScriptFormat.Draft` in `Framework/Language/Format/ScriptFormat.cs`.

### Understanding `alpha` and Precedence

The `alpha` keyword indicates that the format is still in draft state. When a format reaches final release, `alpha` and the draft number are omitted entirely (for example, `sprocket 1`).

In version ordering, **a finished format counts as strictly higher and later than every draft of that version**:

```
sprocket 1 alpha 14  <  sprocket 1 alpha 18  <  sprocket 1 alpha 999  <  sprocket 1
```

Even an arbitrarily high draft number like `sprocket 1 alpha 999` is considered older than the release version `sprocket 1`. When a finished release loads an alpha script, the engine treats it as an earlier draft and applies the entire chain of upgrades defined during development.

### Incrementing and Reader Compatibility

A reader checks the version line before it reads what a script says. A script from a draft the build does not know is refused whole and kept exactly as written. So that an older build refuses a newer script cleanly instead of misreading it, the draft number must track all grammar and vocabulary additions:

* **Increment `ScriptFormat.Draft` for:**
  * **Breaking or altered syntax:** Any change that causes an existing, valid program to parse differently, behave differently, or fail to load. (Requires a corresponding `FormatChange` upgrade class).
  * **New keywords:** Introducing any new keyword, even a purely additive one, mandates a draft increment. An older reader that lacks the keyword checks the version line, recognizes that the script comes from a newer draft, and refuses it whole instead of misreading tokens it does not know.
  * **New syntax or member additions:** Adding grammar constructs older readers cannot read, or members an older build does not know.
* **Increment `ScriptFormat.Version` only:** When a foundational architectural change would break the upgrade pipeline or existing scripts outright.

---

## When an Upgrade is Required

An upgrade class (`FormatChange`) is mandatory whenever a change alters the meaning of existing code, even if the written text remains identical. 

Common triggers include:
* Renaming an identifier (for example, renaming the value ID `switched-on` to `power` in draft 15).
* Moving a member to a different type or slot (for example, moving screen members onto slots that hold screens in draft 16).
* Changing slot contents or semantics.
* Changing keyword behavior, grammar, or turning an existing identifier into a reserved keyword.

> **Pre-release Check:** An unchanged draft number does not guarantee compatibility. Prior to publishing, diff all program-facing identifiers (values, actions, signals, keywords, slot assignments) against the last published build. Every difference requires an upgrade class or a draft bump.

---

## Architecture: `FormatChange`

All upgrades reside in `src/Sprocket.Host.SpaceEngineers/Framework/Language/Format/Upgrades/` and inherit from `FormatChange`.

```csharp
public sealed class ExampleUpgrade : FormatChange
{
    // The draft where this change took effect
    public override int Arrived => 19;

    // Rewrite raw AST/statements before the main reader processes them
    public override void Statements(List<Statement> constructs) { ... }

    // Mutate program structure or inspect world blocks after reading
    public override string Program(Automation program, GridWorld world) { ... }
}
```

### Hooks
A subclass may implement either hook, leaving unneeded hooks empty:
1. `Statements`: Rewrites parsed statements before the language reader processes them. This ensures the reader only ever needs to support the latest syntax.
2. `Program`: Transforms the post-read program. It has access to world blocks if the migration depends on physical grid state. Returns a descriptive log message of changes made, or empty string if no-op.

For a minimal implementation reference, inspect `PowerValue.cs`.

### Ordering and Execution
All upgrades are registered sequentially in `Framework/Language/Format/Upgrades/FormatUpgrades.cs`. 

* **Chronological Order:** New upgrades must always be appended to the end of the list.
* **Execution:** On load, the engine executes every upgrade with `Arrived > script.Draft` in exact sequential order.
* **Save Invariant:** Upgraded scripts are serialized back to storage using the current format on their next save.
* **Immutability:** Upgrades are permanent. They are never deleted or refactored away; at final release, the draft upgrade list forms the permanent upgrade chain to the next major version.
* **Cutoff Boundary:** `FormatUpgrades.Oldest` (currently `14`) defines the minimum upgradeable draft. Scripts older than this threshold are refused and flagged as incompatible.

---

## Contributor Workflow

Follow this sequence for any change affecting script syntax, keywords, or semantics:

1. **Spec First:** Update `docs/Language.md`. The exact wording must be reviewed and approved by maintainers before writing reader or writer code.
2. **Bump Draft:** Increment `ScriptFormat.Draft` in `Framework/Language/Format/ScriptFormat.cs` (required for breaking changes, grammar additions, and new keywords).
3. **Write the Upgrade (if migrating historical code):** Implement the `FormatChange` subclass in `Framework/Language/Format/Upgrades/` setting `Arrived` to the new draft number. Append the class to the list in `FormatUpgrades.cs`. *(Note: Pure keyword additions that require no AST/program rewrite do not need a `FormatChange` subclass, but still require the draft bump).*
4. **Test:** Add regression tests to `tests/Sprocket.Core.Tests/UpgradeTests.cs`. Verify that:
   * Historical code parses into the expected transformed structure.
   * Transformed code writes back cleanly in the modern format.
   * Whole programs pass round-trip tests (`ScriptTests` and `ProgramTextTests`).
5. **Atomic Commit:** Submit the language spec update, engine modifications, upgrade class (if applicable), and tests in a single commit.

---

## Safety Guarantees & Non-Automatable Migrations

* **Zero Overwrites:** If a stored script fails to parse or cannot be upgraded, the engine leaves the text untouched on the programmable block. Never silently discard or overwrite broken user scripts.
* **Non-Automatable Cases:** Certain migrations cannot be resolved automatically without human intent (for example, a slot repurposed across incompatible block types). The editor must continue to load and edit all legal format representations without silent corruption. Incompatible constructs must surface as explicit warnings in the program checker directing the player how to fix them.
* **Repairs:** Each such case is one `Repair` class in `src/Sprocket.Host.SpaceEngineers/Framework/Language/Format/Repairs/`, listed last in `ProgramRepairs` and never removed while a stored program can still hold it. A repair holds everything about its case: the problem it marks, whether that problem is in the logic system or in one installation, how it is found, its words, the question asked before the picker opens to fix it, and what the picker does differently while the player fixes it. The checker, the texts and the picker ask `ProgramRepairs` and never name a repair. An upgrade fixes what it safely can first, and the repair is left the rest. Every repair has a test, as every upgrade does.

---

## Non-Script Storage (Protobuf)

The world's memory (`Framework/Storage/Memory/`: Sprocket's clock, every run and watch, what installations hold in their states, owners, pauses and player settings) is serialized using Protocol Buffers and does not use the script versioning pipeline.
* Any changes to disk or save formats require prior review and maintainer consensus.
* **Tag Number Invariant:** `[ProtoMember]` tag numbers are strictly permanent. Once assigned, a tag number must never be reused or reassigned to a different type. Dropped fields must be left unused.
* **Enum Number Invariant:** A stored enum, such as `SequenceProblem` or `Cause`, has a number set on every value. A number is never changed or given to another value, and a new value takes the next number.
