# Sprocket tutorial scenario

This folder is for designing a tutorial scenario for **Sprocket**, a Space Engineers mod that lets players automate
their ships and bases. The person you work with is Mal, who makes the mod. Call him Mal.

## The job

Design a Space Engineers scenario world, to be published on the Workshop, that takes a player through building real
automation with Sprocket on a ship built for learning.

- **People learn by doing, not by reading.** Mission tasks, chat messages and screens on the ship set each challenge
  and react as it is met. A codex page is pointed to only where one already covers the challenge.
- **The challenges build on each other.** A starting idea: a sequence that cycles an airlock's doors, a trigger that
  starts it, an if that checks the vent, then a blueprint installed on a second airlock. That idea is yours and Mal's
  to change.
- **Design, not code.** You decide what the player is asked to do, in what order, what they are told and when, and how
  the world is laid out for it. Building it in the mod is done elsewhere, once the design settles.

Questions still open, worth settling early because they decide what has to be built:

- How a challenge knows it is met: the mod reading the player's program, or watching the blocks.
- Whether the scenario ships inside the mod or as its own Workshop item.

## What you know the mod from

- **The codex** is the mod's in-game guide, written for players, and it is how you learn how Sprocket works:
  `D:\Repos\Reactor\src\Sprocket.Host.SpaceEngineers\Content\Data\Codex\en\` (Norwegian beside it in `no\`). Read it;
  never change it. Anything you cannot learn from it is probably a gap a player would hit too, and worth telling Mal.
- **The vocabulary** is settled and used exactly: the section "The words" in
  `D:\Repos\Reactor\.claude\skills\writing\SKILL.md`. Read that section only; the rest of that folder is for the code.
- Nothing else in `D:\Repos\Reactor` is yours to read. It is code, and the codex says what a player needs.

## When you need to see or check something

Another Claude session works on the mod's code in `D:\Repos\Reactor`. Find it with ListAgents and message it with
SendMessage when you need:

- a picture of a screen, which it renders from the mod's test harness;
- whether a block can do something, or whether a challenge can be built as designed;
- a fact the codex does not give.

Mal reads what passes between the two sessions.

## Where the design goes

Keep the design in this folder, as Markdown files, starting with `design.md`. It is written for Mal and for whoever
builds it: the challenges in order, for each what the player is asked, what they are told and when, how it is known
to be met, and what the ship needs for it. The card for this work on Mal's board is "A tutorial scenario takes a
player through building real automation".
