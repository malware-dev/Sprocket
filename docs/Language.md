# Sprocket Script Language Specification

Sprocket is an imperative, event-driven domain-specific language designed for programming automated systems on constructs within host environments. Programs are written and read by the host's editor first: they carry what it needs to find everything again, such as GUIDs and entity ids. They are also kept plain text, so a person can read one and repair it by hand when that is needed.

---

## 1. Execution Model and Terminology

* **Host:** The game or simulation engine executing the script. The host supplies block member definitions, resolves hardware bindings, and implements runtime execution mechanisms.
* **Construct:** The physical grid or entity assembly that the script runs upon. A Sprocket program belongs strictly to its own construct.
* **Block:** An individual addressable device or component located on a construct. Each block has a type and exposes members defined by the host.
* **Program:** A single script file executed on a construct, consisting of a version header, variable declarations, systems or system blueprints, and blueprint instances.

---

## 2. Lexical Structure and File Shape

### 2.1 EBNF Grammar

```ebnf
file         ::= version entry*
entry        ::= annotation? statement
statement    ::= head (value | keyword | operator)* newline
               | head (value | keyword | operator)* block
block        ::= newline? "{" newline entry* "}" newline
annotation   ::= description attributes? | attributes
description  ::= ("#" (" " line)? newline)+
attributes   ::= ("#@" " " identifier ":" " " value newline)+
```

### 2.2 Top-Level Ordering

Following the mandatory version declaration, a script file may contain:
1. File-level variable declarations (`let`).
2. Systems (`system`).
3. System blueprints (`system blueprint`).
4. Blueprint instances (`instance`).

**Ordering Rules:**
* A variable must be defined prior to any statement, system, or instance that references it.
* An instance must be declared strictly after the system blueprint it instantiates.
* Systems and system blueprints may otherwise appear in any order.

### 2.3 Statement Boundaries and Formatting

* **Line Termination:** Statements are terminated strictly by a newline character. There are no statement continuation operators, line-joining characters, or semicolons.
* **Header Exception:** The clauses of a `system` or `system blueprint` header each stand on their own line.
* **Brace Placement:** The opening brace `{` of a block may appear either at the end of the statement line or on a new line immediately beneath it.
* **Statement Heads:** The head of a statement is an unquoted identifier, a quoted name, or a language keyword.
* **Case Sensitivity:** Identifiers and keywords are strictly case-sensitive (`run` and `Run` are distinct tokens).
* **Contextual Keywords:** Keywords are contextual. A quoted identifier is never evaluated as a keyword (e.g., an action named `every` must be escaped as `'every'`).

---

## 3. Annotations

Annotations attach structured and unstructured metadata directly to the statement immediately below them. An annotation cannot stand alone.

```sprocket
# The call button, by the airlock's inner door.
#
# Pressing it cycles the airlock from whichever side is open.
#@ short: "Call"
#@ count: 14
optional slot button expects signal
```

* **Structure:** An annotation consists of an optional human-readable description followed by optional machine-readable attributes. The description must always precede attributes.
* **Description:** One or more lines beginning with `#`. Used for documentation.
* **Attributes:** Prefixed by `#@ <identifier>: <value>`. Attributes attach arbitrary key-value metadata to the statement. Attribute values may use any valid Sprocket literal type (such as integers, text, or flags).
* **Uniqueness:** Attribute identifiers must be unique per statement; repeating the same attribute key on a single statement is a syntax error.

---

## 4. Value Types and Literals

| Value Type | Syntax Example | Definition and Rules |
| :--- | :--- | :--- |
| **Name** | `inner`, `'Airlock System'` | Begins with an ASCII letter, followed by letters, digits, and underscores. Quoted if containing spaces or special characters. Escape embedded quotes by doubling: `'Mal''s airlock'`. |
| **Qualified Name** | `MalTek.Airlock`, `'Corp Inc.'.'Air System'` | Identifiers joined by the `.` operator. Segment identity is preserved (`'a.b'.c` is distinct from `a.'b.c'`). Whitespace around the dot is ignored. |
| **Text** | `"Airlock Inner Door"` | Enclosed in double quotes. Doubled double-quotes escape quotes inside text: `"the ""inner"" door"`. Line breaks are written as `&br;` and literal ampersands as `&amp;`. |
| **Integer** | `60`, `74857693847562934` | 64-bit signed whole integer. |
| **Real** | `0.25`, `1E-05` | Floating-point value, denoted by a decimal point or an exponent. An integer may substitute for a real where accepted. |
| **Duration** | `600t`, `2s`, `1.5s` | Token pairing a numeric scalar with a unit suffix: `t` (game ticks) or `s` (seconds, where `1s = 60t`). Seconds can have fractional parts and round to the nearest whole tick (`0.25s` = 15t, `0.01s` = 1t). |
| **Flag** | `on`, `off` | Boolean literal values. |
| **Choice** | `inside`, `side.inside`, `Gearbox.status.closed` | Enumerated choice defined by a system `type` or host enum. Can be bare or qualified. |
| **Color** | `#FF8800`, `#FF8800CC` | Hexadecimal color literal (RGB or RGBA). |
| **Vector** | `(3 0 -2)` | Three space-separated integers or reals enclosed in parentheses. |
| **List** | `[ 1, "two", three ]` | Comma-separated elements in square brackets. May nest lists and vectors: `[ 7, [ (0 2 -1), (3 0 0) ] ]`. |
| **Block** | `block 'Door/LargeBlockDoor' [ … ]` | Concrete hardware block binding descriptor. |

### 4.1 Identifiers vs. IDs (References)

Any entity possessing both an ID and a name (such as a sequence or a system blueprint) can be referenced by either:
* **By ID:** A bare integer or quoted GUID (e.g., `run 2`, `instance 1 of "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43"`). References written as IDs survive renames.
* **By Name:** An identifier or quoted name (e.g., `run 'Cycle'`, `instance 1 of MalTek.Airlock`). If the target is renamed, references using the name must be updated.
* **Numeric Disambiguation:** A sequence explicitly named `"2"` must be quoted as `'2'`; a bare `2` is always parsed as sequence ID 2.

---

## 5. Host Integration

The host engine supplies runtime members and types beyond the core language grammar. The examples in this document are written for a host with a namespace called `Gearbox`.

### 5.1 Block Members

Every member exposed by a host on a block is an **action**, a **value**, a **function**, or a **signal**:

1. **Action:** An operation executed via `call`. May declare parameters.
2. **Value:** A block property read via `if` or `when`, and mutated via `set` if writable by the host. `set` on a value the host offers only to be read is an error.
3. **Function:** A parameterized reader evaluated like a value. Must take at least one argument.
4. **Signal:** An asynchronous event emitted by a block (e.g., `Gearbox.closes`). Functions may also return signals, named by where they are sent from: a place the host offers, such as a position on a block's toolbar, or a thing of a host type. Used with `whenever` or bound to signal slots.

#### Member Semantics and Validation
* Invoking a non-existent member, or using a member as the wrong kind (e.g., calling `call` on a value, or `set` on a function), is an error.
* An action and a value on the same block may share the same ID. The statement head (`call` vs. `set`/read) disambiguates them.
* **Arguments:** Supplied in comma-separated parentheses immediately following the member identifier: `Gearbox.rotate_to(0, 90, 2)`.
  * Parentheses are written **only** when arguments are present. Writing empty parentheses `()` is a syntax error.
  * Argument counts and types must match the member's parameter signature. Supplying incompatible types or an integer exceeding parameter limits is an error.
  * A parameter may take slots. Its argument is a list of slot names in square brackets: `call Gearbox.move_items([ cargo ], "Ingot/Iron", 100) on [ output ]`. The parameter's signature says what the slots must hold, blocks or things of a host type.
* **Return Types:** Values and functions return a flag, integer, real, text, color, or a host choice name.

### 5.2 Host Types

A host may provide non-block object types associated with a block (such as an LCD text surface).
* Host types are namespaced: `Gearbox.screen`.
* Returned by host functions: `Gearbox.screen(1) on [ board ]` is the first screen of the block `board`.
* Entities of a host type expose members defined by the host for that type:
  ```sprocket
  call Gearbox.'show text'("Docked") on [ status ]
  ```
  where `status` holds screens. A member the host does not provide for that type is an error.
* Which types a host provides, which functions return each, and which members each has, is the host's to say.

### 5.3 Namespaces

Every block member resides in a namespace:
* **Root Namespace:** Members are written bare (e.g., `Reverse`, `Radius`).
* **Named Namespaces:** Members are written qualified with a dot (e.g., `Gearbox.close`, `Gearbox.rotate_to`).
* Each part of a qualified name is judged on its own, so any part spelled like a keyword is quoted: `Gearbox.'on'`.
* A host may have a root namespace, named namespaces, or both. A host with no root namespace has every member written qualified.
* Referencing a member in a non-existent namespace is an error.

---

## 6. Version Header

The first statement of every program must declare the language format version:

```sprocket
sprocket <format> [alpha <draft>]
```

* `<format>`: Major format integer (currently `1`). Increments only when a change would break existing programs outright.
* `alpha <draft>`: Indicates an unfinished development format (e.g., `sprocket 1 alpha 18`). The draft goes up for every change to the language: a change that makes an existing program read differently or not at all, and every addition, so that a build refuses a program from a draft it does not know rather than misread it.
* **Precedence:** A finalized format omitting `alpha` is strictly higher and later than all of its drafts:
  ```
  sprocket 1 alpha 1  <  sprocket 1 alpha 18  <  sprocket 1  <  sprocket 2 alpha 1  <  sprocket 2
  ```

---

## 7. Systems and System Blueprints

### 7.1 Architecture

* **System (`system`):** Encapsulates a singleton automation unit consisting of sequences, watchers, clocks, triggers, states, and types. It runs directly against file-level variables on the construct.
* **System Blueprint (`system blueprint`):** An abstract, reusable template defined against abstract **slots** rather than concrete blocks. It can be instantiated multiple times via `instance` declarations and serves as the exportable/shareable unit of automation.
* Everything said of a system below holds for a system blueprint, except where it says otherwise.

### 7.2 Header Specification

```sprocket
system "<guid>" as <namespace>.<name>
[changed "<iso>"]
{
    <types>
    <states>
    <triggers, watchers and clocks>
    <sequences>
}

system blueprint "<guid>" as <namespace>.<name>
[changed "<iso>"]
[from blueprint "<guid>" "<iso>"]
{
    <slots>
    <types>
    <states>
    <triggers, watchers and clocks>
    <sequences>
}
```

* **Clauses:** Each header clause must stand on its own line.
* **GUID:** A permanent, immutable unique identifier generated at creation.
* **Namespace and Name:** Human-readable display identifiers. They may be modified by users, and multiple systems may share the same name.
* **`changed "<iso>"`:** When the system was last edited, which tells two versions of a system apart. Quoted ISO 8601 in any of its spellings (e.g., `"2026-09-14T19:02:11Z"`); a time with no zone is UTC. Metadata with no runtime effect.
* **`from blueprint "<guid>" "<iso>"`:** Metadata on a system blueprint naming the stored blueprint it was made from, by that blueprint's GUID, and the version of it, by its `changed`, that it was made from or last brought up to date with. A blueprint as it is stored carries none. Has no runtime effect.
* **Clause Order:** `changed` and `from blueprint` may appear in either order, but at most once each.

---

## 8. Slots (System Blueprints)

Slots define the hardware dependencies that a construct must supply to instantiate a blueprint. Only a system blueprint has slots, and everything else in it refers to a slot by its name.

```sprocket
required slot inner expects Door
required slot hangar expects 'AirtightHangarDoor/LargeBlockAirtightHangarDoor'
optional slot lamp expects LightingBlock
optional slot button expects signal
optional slot display expects Gearbox.screen
optional slot generic
```

* **Cardinality:** Must be declared as either `required` or `optional`. An instance may leave an optional slot unassigned. Leaving a required slot unassigned marks the instance as missing that slot.
* **Expectation (`expects`):**
  * **Game Type:** A game type id with the `MyObjectBuilder_` prefix dropped (e.g., `LightingBlock`, `Door`). Takes every block of that type and of every type the game builds on it, so `LightingBlock` takes interior lights and spotlights alike.
  * **Exact Subtype:** A quoted name `'Type/Subtype'`, taking that one exact block.
  * **`signal`:** For slots bound to functions returning signals.
  * **Host Type:** A qualified host type identifier (e.g., `Gearbox.screen`), for slots answered by functions that return one. `optional slot output expects Gearbox.inventory`, answered by `Gearbox.inventory(2) on [ refinery ]`, holds that inventory of the block.
  * **Omitted:** A slot declared with no `expects` clause accepts any block.
* When a slot is populated from multiple blocks, it expects the nearest common ancestor type shared by all assigned blocks.

---

## 9. Types, Choices, and States

### 9.1 Declared Types

Systems and system blueprints can declare custom enumerated types:

```sprocket
type side = inside | outside
type stage = idle | 'cycling in' | 'cycling out'
```

* **Syntax:** `type <name> = <choice> | <choice> ...`
* A declared type must contain at least two choices; duplicate choices within the same type are prohibited.
* Choice names follow identifier rules and may be quoted. Two distinct types may share identically named choices.
* **Reserved Words:** Built-in type names (`flag`, `text`, `integer`, `real`, `duration`, `color`, `vector`, `list`, `block`) cannot be used as type names. They may, however, be used as state names or choice names.

### 9.2 Choice Resolution

* Choices can be written bare (`inside`) or qualified by type (`side.inside`).
* Host enum choices are qualified by namespace and member (`Gearbox.status.closed`).
* In an expression, a bare identifier resolves first to a state of that name, and then to a choice of that name.
* If a bare choice is ambiguous (defined across multiple declared types) and its type cannot be told from where it stands, as in a state's starting value, it is an error and must be explicitly qualified:
  ```sprocket
  type valve = open | shut
  type hatch = open | shut

  let intake = valve.open  # `open` alone is an ambiguous error
  ```

### 9.3 States

A state is a persistent, mutable memory cell scoped to an instance of a system or blueprint.

```sprocket
let where = inside
let home = side.outside
let cycling = off
```

* **Declaration:** `let <name> = <value>`. The starting value is mandatory.
* **Typing:** The state's type is inferred entirely from its initial value; a type is never written in the declaration. It must be a flag (`on` or `off`) or a choice of a declared type. Other types are an error.
* **Scoping:** States must be declared after their associated types and before any statement referencing them.
* **Namespace Collision:** Slots, types, and states within a single system share one flat namespace. No duplicate names are permitted. In a plain system, a state cannot share a name with a file-level variable. Names in other systems do not count.
* **Referencing:** States are referenced bare (without block brackets `[]`, `any`, or `all`). They are mutated via `set`, queried via `if`, and watched via `when`.

---

## 10. Reactive Automation (Triggers, Watchers, Clocks)

Triggers, watchers, and clocks react to asynchronous conditions and initiate a single step (`run`, `stop`, `call`, or `set`). They have no identifiers.

### 10.1 Signal Triggers (`whenever`)

```sprocket
whenever 'call pressed' then run 1
whenever Gearbox.closes on [ inner ] then run 2
whenever Gearbox.closes on [ inner, outer ] then run 2
```

* Executes its target step every time the specified signal is fired.
* Signals may be bound to variables, slots expecting signals, or declared in-place.
* When multiple blocks are specified, the trigger fires whenever the signal originates from **any** block in the set.

### 10.2 Block Watchers (`when`)

```sprocket
when all Gearbox.status on [ inner, outer ] == closed then call Gearbox.open on [ hangar ]
when any Gearbox.filled on [ vent ] == on every 30t then run 4
when sum Gearbox.amount("Ingot/Iron") on [ cargo ] >= 1000 then run 6
when average Gearbox.fill on [ tank1, tank2 ] < 0.25 then run 7
```

* **Syntax:** `when <aggregation> <member> on [ <names> ] <comparison> <value> [every <duration>] then <step>`
* **Edge-Triggered:** Executes its target step at the instant the condition transitions from false to true. It does not fire again until the condition evaluates to false and transitions back to true.
* **Aggregations:**
  * `any`: The condition holding on any single block triggers execution. Each block transitioning to true fires independently.
  * `all`: The condition must hold across all specified blocks. Fires once when the final block satisfies the condition.
  * `sum`: Numeric values are summed across all blocks and compared once. Valid only on integers and reals.
  * `average`: Numeric values are averaged across all blocks and compared once. Valid only on integers and reals.
  * **No blocks:** Over an empty set, `sum` is 0, `any` is false and `all` is true. `average` has no value, and the step fails.
* **Polling Interval (`every`):** Sets the evaluation frequency. Defaults to `10t` if omitted. Hosts that provide push event updates for member changes may evaluate immediately, bypassing the poll timer.

### 10.3 State Watchers (`when`)

```sprocket
when where == outside then run 2
```

* **Syntax:** `when <state> <comparison> <value> then <step>`
* Fires when an explicit `set <state>` step causes the comparison to become true, and not again until it stops holding. Comparing two states, it fires when either is set that way.
* Evaluated per instance. Takes no `every` clause.
* Does **not** fire for starting values assigned during initial declaration.

### 10.4 Periodic Clocks (`every`)

```sprocket
every 600t stop 3
```

* **Syntax:** `every <duration> <step>`
* Fires the target step periodically at fixed time intervals.

---

## 11. Sequences and Control Flow

### 11.1 Sequence Structure

```sprocket
sequence 1 as Cycle {
    call Gearbox.close on [ inner ]
    await
    call Gearbox.open on [ outer ]
}
```

* **Identification:** Defined by a numeric ID (`sequence <n>`) scoped to the system. The number remains stable across imports, exports, and renames.
* **Naming:** May declare an optional name (`as <name>`).
* **Concurrency:** Steps execute sequentially. An asynchronous action or sequence execution starts immediately, allowing the script to proceed to the next statement without blocking unless paused by `await` or `wait`.
* `await`, `wait` and `if` belong to sequences alone.

### 11.2 Steps

| Step Statement | Semantics |
| :--- | :--- |
| `call <member>[(<args>)] on [ <names> ]` | Invokes an action on all blocks identified by `<names>`. |
| `set <member> on [ <names> ] to <value>` | Assigns a writable host block property. Where an id names both an action and a value, `set` always means the value. |
| `set <state> to <value>` | Updates a system state within the current instance. Value must be a valid choice, flag, or another state of identical type. |
| `run <sequence>` | Starts a sequence of the same system asynchronously. If already running, does nothing. Self-recursion or cyclical invocation chains produce an error. |
| `stop <sequence>` | Halts a sequence of the same system if running. A sequence can stop itself, terminating execution immediately. |
| `await` | Blocks the sequence until all actions and sequences started above it in the current sequence have completed. |
| `wait <duration>` | Pauses sequence execution for the specified duration (e.g., `wait 60t`). |

### 11.3 Block Targets (`[ <names> ]`)

* The bracketed list after a member indicates variable names or slot names.
* Target names are resolved and deduplicated into a unique set of the things they hold: physical blocks, or things of a host type, so two screens of one block are two things.
* The operation is performed on every thing in the set once, regardless of how many names refer to it.

### 11.4 Step Disabling (`//`)

Within sequences, prefixing a step with `//` (following indentation) disables it:

```sprocket
sequence 1 as Cycle {
    call Gearbox.close on [ inner ]
    await
    // call Gearbox.open on [ outer ]
    wait 60t
}
```

* Disabled steps are fully validated by the parser but perform no runtime operation.
* Only steps inside sequences can be switched off using `//`.

---

## 12. Conditional Branching (`if`)

### 12.1 Condition on Blocks

```sprocket
if all Gearbox.pressurized on [ vent ] == on then {
    call Gearbox.open on [ outer ]
}
else if any Gearbox.status on [ inner ] == open then {
    call Gearbox.close on [ inner ]
}
else {
    call Gearbox.close on [ inner, outer ]
}
```

* **Syntax:** `if <aggregation> <member> on [ <names> ] <comparison> <value> then { ... }`. The member is tested once, when the step is reached.
* **Branches:** An `else if` after the closing brace is tested in turn where nothing before it held; an `else` runs where nothing held, comes last, and only once. The sequence then carries on after the whole `if`. The statements between the braces are steps like any other, an `if` among them.
* **Comparisons:**
  * `==` and `!=`: Evaluated across all types.
  * `<`, `>`, `<=`, `>=`: Valid only on integers and reals.
* **Aggregations:** Uses `any`, `all`, `sum`, or `average` under the same evaluation rules as `when`.
* **Disabling:** Placing `//` before an `if` statement disables the entire conditional block, including all subsequent `else if` and `else` branches. A step inside can be switched off on its own.

### 12.2 Condition on States

```sprocket
if where == inside then {
    call Gearbox.close on [ inner ]
}
else if where == outside then {
    call Gearbox.open on [ outer ]
}
```

* **Syntax:** `if <state> <comparison> <value> then { ... }`
* Evaluates equality (`==` or `!=`) against a choice, flag, or another state of matching type; comparing with a value of another type is an error.
* Block conditions and state conditions may be freely mixed within the same `if / else if / else` chain.

---

## 13. Construct Definition: Variables and Instances

### 13.1 Variables (`let`)

File-level variables assign names to hardware blocks or construct-level signals:

```sprocket
let door1 = block 'Door/LargeBlockDoor' [ 74857693847562934, [ (3 0 -2) ], "Airlock Inner Door" ]
let button = Gearbox.toolbar(1, 1, 4) on [ panel1 ]
```

* Defined once at the file level before any system or instance references them.
* Globally visible across all systems and instances in the file.
* A `let` inside a system's body declares a state instead (see 9.3).

### 13.2 Block Descriptors (`block`)

```sprocket
block '<Type/Subtype>' [ <keys> ]
```

* Quoted string defines the block's type and subtype.
* **Re-identification Keys:** The bracketed list holds the ways to find the block again, in the order they are tried; the first that finds a block decides. The language does not read them, and a host may write whatever it recognizes its own blocks by, for example:
  1. **Entity ID:** 64-bit integer.
  2. **Cell Position:** A list of cells: the cell of each mechanical joint crossed from the grid the program runs on, then the block's own cell on its grid: `[ (3 0 -2) ]` or `[ (0 2 -1), (1 0 0) ]`.
  3. **Terminal Name:** Quoted game display name (`"Airlock Inner Door"`).

### 13.3 Blueprint Instances (`instance`)

An instance binds concrete blocks and signals on the construct to the abstract slots of a system blueprint. A system blueprint may have any number of instances, each on blocks of its own:

```sprocket
instance 1 of MalTek.Airlock as 'Port Airlock' {
    inner = [ door1 ]
    hangar = [ door2, door3 ]
    button = [ Gearbox.toolbar(1, 1, 4) on [ panel1 ] ]
}
```

* **Identification:** Designated by a numeric ID (`instance <n> of <blueprint>`) and an optional name (`as '<name>'`).
* **Slot Assignment (`<slot> = [ <answers> ]`):**
  * Populated by variables, inline host functions returning host types, or signals.
  * A slot may receive multiple answers. For a slot expecting `signal`, supplying multiple answers indicates that a signal from **any** of the answers triggers the associated automation.
  * Leaving a `required` slot unassigned marks the instance as missing that slot.
