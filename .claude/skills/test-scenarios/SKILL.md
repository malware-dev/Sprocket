---
name: test-scenarios
description: Write a game test scenario for Mal - a build and its program written once in the harness, run there as a test, then written as a blueprint to paste in Space Engineers, with its game-only checks as rows on the Testing artifact. Use whenever a change needs checking in the game, or Mal asks for something to test there.
---

A scenario is code in the harness, under `src/Sprocket.Host.SpaceEngineers.Sim/Scenarios/`. It lays its blocks on a deck,
writes its program through the editor driver as a person would, and lists the checks only the game can answer. The
harness runs it as a test; `--blueprint` writes it for the game; `--checks` prints its checks for the Testing artifact.

## The harness comes first

**A test that runs fully in the harness and can be trusted without the game is always preferred**, because it runs
fastest. Write those tests first. The harness runs every automation through the same `Roster` the session does, and the
rig splits and merges grids as the game does (`Rig.Split`, `Rig.Merge`, `Rig.Apart` for another ship; `Deck.Parts` says
which blocks stay joined when a cell is ground away), so a scenario's whole flow - split, press, merge, press - is a test
(`ScenarioTests`). A game check covers only what is left: that the game raises its events as the harness assumes, what a
paste does, what a screen draws. Every game check says, in its `why`, what the harness cannot reach.

## Writing one

1. Make a class beside `SplitTwin`, deriving from `Scenario`, and add it to `Scenarios.All`.
2. **`Lay`**: weld the blocks on the rig and lay them on the deck. `Deck.Lay` stands blocks in rows on an armor floor,
   each on the face its definition mounts by; `Deck.Floor` adds single armor blocks, such as a bridge between two floors.
   Use the game's real subtypes: the plain large door is the empty subtype, so `rig.Door(id, name, "")`. A block the game
   does not define is refused.
3. **`Program`**: build it through the driver, the way the presentation tests do: `NewSystem`, `PressStart`,
   `PressBlock`, `PressChoice`, `PressPrimary`. Fill another installation on its panel, slot by slot, so its buttons are
   wired as a person's are. Leave it in the state the checks start from, usually switched on.
   **A program is repeatable:** it ends as it starts and moves every block it names on every run, so pressing it again
   shows it working again. Its harness test runs it at least twice. A value typed into a box is set outright
   (`Box.Text`), since a box may already hold a default that typing would add to.
4. **`Checks`**: each a title, the setup, the steps, what to see, and why it is in the game.
5. Its tests run through `ScenarioTests`, which checks every scenario: the program finds every block by the cell the
   blueprint puts it in, and every button is wired. Add a test of what the scenario does in the harness.

## Writing it out

```
dotnet run --no-build --project src/Sprocket.Host.SpaceEngineers.Sim -- --blueprint "<name>" [folder]
dotnet run --no-build --project src/Sprocket.Host.SpaceEngineers.Sim -- --checks "<name>" [first row number]
```

The folder is the game's local blueprints folder, which is per machine: name it, or set `SPROCKET_BLUEPRINTS`. It is never
written into the repository. Writing a blueprint deletes the `bp.sbcB5` beside it, since the binary wins over the text.

The checks come out as JSON rows for the `checks` collection of the Testing artifact; add them with `ArtifactData`, then
give Mal the blueprint's name and the artifact's link. He pastes, follows the rows and marks them.

**A pasted blueprint gives its blocks no owner.** A check that depends on who owns the blocks, or on who may open the
automation, says in its own setup who takes ownership of which blocks; every other check starts from the blocks as pasted.

## What the export knows, and why

- **Blocks come from the game's definitions** (`BlockDefinitions.FromGame`, the content folder the harness finds): size,
  center, mount points. Nothing about a block is written from memory.
- **Position is the game's rule.** A program finds a pasted copy's blocks by the cell the game calls each block's
  position: the definition's center turned by the block's orientation, from its lowest corner, shifted along any axis the
  turn runs backward on. `Placement.Position` computes it, and the deck gives each block that cell before the program is
  written.
- **Toolbars on paste.** A button panel, timer, sensor, target dummy, turret controller or event controller gets a
  `Toolbar`, empty where it holds nothing, or the paste fails.
- **A sign.** Every scenario gets one without asking: a wide LCD stood upright against a short armor wall at the edge of
  the first floor, beside the programmable block, showing the checks (`Instructions`), wrapped by hand since a text panel
  never wraps. Its text is turned upright by the surface's rotation index, 1, seen right in the game; keep the first floor's edge clear for it.
- **Power.** Nothing runs without it, so every part of a build that has to work carries a battery, and the writer charges
  every battery full. A part that splits off needs a battery of its own.
- **Storage.** The program and switches go in the programmable block's mod storage under Sprocket's two keys, written
  lowercase, as a save writes them.
- **Heads.** A large piston, rotor or hinge is written with its head, a grid of its own at the offset the game puts it,
  measured on the dedicated server (`HeadOffsetGameTest`, which also checks the offsets still hold). Another block with a
  head gets its offset measured there first.
- **Not yet supported:** more than one grid of blocks laid on the deck.

The first time a scenario uses a block type, Mal sees it pasted. A block type that pastes wrong gets its rule fixed here,
with a test. The harness is Claude's; never ask Mal to look at it.
