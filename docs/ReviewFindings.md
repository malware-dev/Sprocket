# Review findings

2026-09-13. Written as the pass went.

**How it was done, so its coverage is not overstated.** The standards turned into mechanical checks
run over all 258 files - naming, folders, statics, one-type-per-file, the summary rule, history in
comments, allocation on repeating paths - then reading wherever a check pointed, plus following the
call chain by hand on every per-frame path. That is not the same as reading 33,000 lines, and it
cannot see what only reading finds: a wrong condition, a misleading name, a type with two reasons to
change, logic that compiles and passes and is still wrong. Those want a pass of their own.

**Guard captured before starting:** 142 tests green, 21 reference frames byte-identical to what is
committed, mod builds and packs. Re-run after every group of changes.

**What this file is for.** What could not be proved, anything that looked like a real defect rather
than untidiness, and any decision that should have been somebody else's. Fixes that are provable
land in the tree; everything else lands here.

---

## Defects found and fixed

### 1. Per-frame string allocation from `ToString()` - four sites

**Cause.** A number is formatted into a string on a path that runs every frame, then assigned to a
label that compares and usually discards it. The allocation happens before the comparison, so the
comparison saves nothing.

Sites, all confirmed on per-frame paths by following the call chain from a component's `Update`:

- `Picking/StageView.cs` `UpdateKinds` - `filter.Count.ToString()`, once per kind filter per frame
  while a picker is open.
- `Picking/StageView.cs` `UpdateRows` - `entry.Badge.ToString()`, once per visible badged row per
  frame.
- `Sequences/SequenceView.cs` `Update` - `step.Number.ToString()`, once per step per frame.
- `Ui/TextBox.cs` `DrawContent` - `_text.ToString()` from a `StringBuilder`, once per frame per
  drawn box.

**Why it is invisible.** The output is correct. Every test passes, every reference frame matches, and
the only symptom is garbage nothing can see. This is the failure mode the standards single out.

**Fixed by** remembering the value each string was made from and re-formatting only when it changes.

### 2. History in comments, three sites my first sweep missed

**Cause.** A comment describing what the code *was*. The earlier pass caught the phrasings that say
so outright - "used to", "previously" - and missed the ones that tell the story in the past tense
without a marker: *"that is the whole of why they changed"*, *"The first cut of it was three steps
darker"*, *"Here because it was not"*.

**Fixed**, keeping in each case the part that earns its place. `Palette`'s remarks are mostly
transcribed game values and contrast ratios pinned to accessibility floors, which the standards keep
explicitly; only the story around them went.

*Worth knowing for next time:* grepping for history markers finds the confident cases and misses the
narrated ones. The reliable tell is past tense about the code rather than about the world, and no
pattern catches that.

### 3. Untidiness, fixed in passing

- `Storage/ProgramText.cs` had a private helper called `Num`. Abbreviation, and the only permitted
  ones are `Id`, `Ui`, `Ok`. Now `Number`.
- `Ui/TextBox.cs` carried two section banners - `// ---- editing ----`,
  `// ---- layout and drawing ----`. Region markers by another name, and they state nothing. Gone.
  The type itself does *not* split: editing and drawing both read the caret and the text, so the
  member groups are not disjoint.

---

## Not defects, recorded so they are not re-raised

- **`Sim/Fakes/MathHelper.cs`** carries a banned role suffix. It is a fake of the game's own type, so
  the name is transcribed rather than chosen. Leave.
- **Three files hold two public types**: `Sim/Content/DdsImage.cs`, `Sim/Content/SeFontMetrics.cs`,
  `Sim/Fakes/MyScreenSpace.cs`. In each the second is nested inside the first and is that type's own
  vocabulary; `MyScissorScope` additionally mirrors the game's own nested type. Leave.
- **Statics are clean.** Every `static` outside the fakes is a pure function, a `const`, or a
  `static readonly` immutable. The two platform-forced ones - `ScreenSpaceDevice.Current` and
  `ReactorSession._current` - each carry the requirement that demands them.
- **No by-kind folders** anywhere - no `Views/`, `Helpers/`, `Utils/`, `Managers/`, `Common/`.
- **No LINQ in shipped code.** It appears only in the simulator and the tests, where allocation does
  not matter.
- **Ten `foreach` loops in shipped code**, all over `Dictionary` or `List`, whose enumerators are
  structs - so none of them allocates.
- **No `<summary>` contains a `<para>`**, so the one-sentence rule holds throughout.
- **No dead types.** Of 208 public or internal types, 18 are referenced nowhere by name: seventeen
  are xUnit classes, which the runner finds by attribute, and one is a fake of a game type.
- **No method name contains "and"**, and no method carries a section banner inside it - the two tells
  for a method that is really two.
- **No allocation on a per-frame path** beyond the four in finding 1. Everything `new`ed inside an
  `Update`, `Draw`, `Arrange` or `Measure` is a struct - `RectangleF`, `Thickness`, `Color`,
  `Vector2` - and `Hit` is a struct whose lists are reused fields.

---

## What the guards cannot see, and was therefore not touched

- **598 `<remarks>` blocks and 154 `<para>`s.** Sampling them says the great majority earn their
  place under one of the two rules - a value transcribed from the game, a contrast ratio against an
  accessibility floor, a statement that something runs every frame. Deciding the rest is a judgement
  per site rather than a sweep, and doing it unattended would mean deleting reasoning on a guess.
  Left for a pass with somebody watching.
- **Member order** - constants, fields, constructors, properties, then methods by descending access.
  Not checkable mechanically at any useful confidence, and reordering members is a large diff that
  the reference frames cannot see either way.
- **`ScreenSpaceDevice.Current`** remains a static mutable field. It is what makes a frame dump and a
  drawn frame share state, it is confined to the simulator, and it is already recorded in
  `docs/CleanupLog.md` as owned by a later phase. Unchanged.
