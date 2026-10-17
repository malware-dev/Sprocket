---
name: csharp-standards
description: Mal's C# rules - naming, documentation, comments, statics, structure, libraries, idiom, repeated work, changing code and cleanup passes. Load for any writing, review or cleanup of Mal's C#.
---

Each rule is absolute unless it names its exception.

## Naming

- A name says what the thing is to a reader who has not opened it. Clarity beats brevity.
- Types and properties are nouns or noun phrases: `SequenceRunner`, `PickPage`, `ToolbarSlot`.
- Methods are verbs or verb phrases: `Load`, `FindPart`, `Invalidate`.
- Booleans read as a question: `IsRunning`, `HasSteps`, `CanUndo`.
- Compound names are normal. Two words that are clear beat one word that needs explaining.
- A type that cannot be named clearly is the wrong type. Change the type, not the name.
- We never use `Manager`, `Helper`, `Util`, `Utility`, `Misc`, `Common`, `Impl`.
- We never abbreviate. Exceptions: `Id`, `Ui`, `Ok`.
- Interfaces start with `I`.
- We never rename a clear name into a longer one that adds nothing.

## Documentation (XML)

- Every non-private member has a `<summary>`: types, members, constructors, fields, operators,
  enum values, interface members. Protected counts as non-private.
- Overrides and interface implementations carry `<inheritdoc />` at the least.
- Private fields get no summary. Private methods and nested types may have one.
- A summary is one short sentence, present tense, saying what the member is. Nothing else.
- A summary stands alone. We never write "The same", "Likewise", "As above", or describe a type by
  what it does not own.
- The default is no `<remarks>`.
- `<remarks>` holds only what a caller must know to use the member correctly and cannot see from
  the signature and summary: a precondition, a cost, when it may be called, a persisted format, an
  outside source. One or two sentences.
- We never document how a member works inside, why it was designed this way, what other types do,
  what it does not do, or anything a caller can see in the signature.
- If the remarks run longer than the member, they are deleted.

## Comments (`//`)

- A comment states one of two things: a fact from outside the file (with its source), or a
  constraint the code cannot show (runs every frame, must not allocate, persisted format).
- We never write history: no "used to", "previously", "changed because", "the first attempt".
- We never restate the line below.
- We never write "we", "you" or "I".
- We state what code does, never what it does not do, unless a reader would assume the opposite.
- Unclear code is rewritten, not explained.
- Code shaped for measured performance keeps its shape and carries a comment saying what the shape
  buys. "Measured" means a number, a profile or a platform limit.
- A transcribed value cites its source. That citation replaces any warning not to change it.
- Comments carried into a new place are held to these rules as if new.
- No `#region`.

## Statics

- Allowed: `const`; `static readonly` holding an immutable value; pure static methods; extension
  methods; static factory methods.
- Never: static mutable fields, static setters, static events, static caches or collections added to
  at runtime, singletons (`Instance`, `Current`, `Default`), service locators.
- Dependencies are passed in through the constructor.
- The one exception is an instance the game makes and the mod cannot hand on, such as a session
  component. It may be reachable through one static, under these conditions:
  - The game calls into the mod at a place where no instance can be passed: a terminal control
    callback, a block's game logic, a handler the game registers.
  - Passing the instance there instead would cost more than it saves. That is weighed and written
    in the static's comment: which entry points need it and why passing it fails or costs.
  - The static is read only at those entry points. The entry point reads it once and passes it on;
    nothing below an entry point reads the static.
  - Code the mod constructs itself is given the instance. Reading the static from such code is a
    breach, even when it is shorter.
  - The static holds a reference to an ordinary instance and has no behavior of its own.

## Structure

- One type per file, named for the type, whatever its access level.
- A type is nested only when it is a small data type used by its owner alone. Anything with
  behavior of its own gets its own file.
- Folders are named for a capability (`Sequences/`, `Storage/`). Never by kind (`Models/`, `Views/`,
  `Helpers/`, `Utils/`, `Managers/`, `Common/`, `Shared/`, `Misc/`).
- A type used by several capabilities moves up a level.
- Namespaces follow folders.
- A type splits when its parts touch disjoint state and change for different reasons. It does not
  split when the pieces cannot be named clearly.
- A method needing a section comment inside it is two methods.
- Member order is not enforced by hand. Rider's cleanup does it, in its own commit.

## Libraries

Applies to any code meant for other code to build on, such as Lumel.

- The public surface offers only operations that keep the library's state consistent.
- State the library maintains itself (an element's parent, its children, layout flags) has no
  public setter and no public mutating method.
- Where the library's own types must change such state across types, the member is an explicit
  implementation of an interface the library defines. A user reaches it only by casting to that
  interface, so bypassing the approved path is always deliberate.
- Public setters exist only for values the user owns: colors, text, sizes, content.
- `internal` is not the tool for this: a shared project compiles into the user's assembly, so
  `internal` hides nothing.

## Idiom

- `var` for locals.
- `foreach`, lambdas, `?.`, `??`, `?.Invoke`, expression-bodied members (`=>`).
- `for` only where the index is needed, the list changes in the loop, or a per-tick path iterates an
  interface type (`foreach` boxes the enumerator).
- `delegate { }` only where it ignores parameters a lambda would have to name.
- Braceless single-statement `if`, statement on the next line.
- `_camelCase` private fields; `private` may be spelled or not, and is never changed either way.
- We never write an implicit conversion to a reference type.
- In a hot loop we never step a struct holding a reference while it lives in a field. Copy to a
  local, loop, write back.

## Work that repeats

- Before writing a body, know how often it runs.
- Nothing runs per tick or per frame without a reason stated in the code. "It was already on the tick"
  and "the guard is cheap" are not reasons.
- An event first. A cadence where there is no event. A tick only where a cadence fails someone.
- Writes push invalidation; the repeating path only reads.
- The third copy of a cache is the sign the fix belongs one level up.
- A cost claim is proved by a count: instrument it, run idle, assert the number.

## Before changing code

- Name the cause in one sentence, with the file. Without it, the change is a guess and is called one.
- Fix at the level of the cause. No local patch for a structural cause.
- Before the change, answer: what else uses this, what it costs per run times how often, what happens
  at ten thousand.
- We never offer a quick fix beside the proper fix, except where players are broken now.
- Unused code is evaluated before deletion: what it does, whether something else does it, whether
  planned work needs it.
- A failing test is a finding. We never change behavior or the test to make it pass.

## Cleanup passes

- Behavior never changes. Structure is free.
- Capture the reference first (test count, captured output), compare after each step.
- Any difference stops the pass until its cause is fixed.
- We never weaken a test.
- What the guards cannot see is still changed, and written in the findings file.
- Awkward code with no comment is evaluated: if the shape buys something real, keep it and comment it;
  if not, rewrite it; if unknowable, keep it and flag it.
- The pass ends with a dirty tree and a report. No commit.
