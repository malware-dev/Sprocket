<p align="center"><img src="art/thumbnail/sprocket-mark.svg" width="180" alt="Sprocket"></p>

<h1 align="center">SPROCKET</h1>

**Stop Building Disposable Chains.**

You wire a button panel to start an airlock sequence and guess the countdown. When a door runs slow,
the next timer fires anyway and vents the room. You build a second airlock, but you still have to
wire an entirely new chain of timers from scratch. Then a strike destroys your logic bay, and
welding replacement blocks just leaves you with empty boxes.

Maltek Industrial presents Sprocket.

Select blocks from your terminal list and choose your actions. Sprocket writes your setup as a plain
English sentence. The sequence runs exactly as you read it, from top to bottom, every time.

Steps wait for completion. A sequence proceeds only when the door is actually shut.

Systems scale immediately. Build a sequence once and apply it to every matching airlock on your
ship. Write another for your docking clamps, mining rigs, or cargo sorting, and run every instance
from that single setup. Your logic lives safely on your terminal screens, immune to a single lost
block.

Command your ship in plain words.

---

## What this actually is

Sprocket adds an automation editor directly to the Space Engineers terminal. Choose blocks from your
construct and assign their actions. Sprocket writes your choices as a sequence in plain English. The
sentence you read is the program that runs.

Everything is selected from menus rather than typed. A sequence is a straight path read from top to
bottom. Understanding your machinery is as simple as reading a sentence.

## Accessing the editor

Enable the mod in your world. Build a **Programmable Block** on your construct, open its terminal
panel, and press **Automation**.

A single block holds and runs the systems for your entire construct.

The Programmable Block is simply where Sprocket lives. The automation belongs entirely to the mod.
Because it runs independently, Sprocket works seamlessly even on worlds that disable in-game
scripts.

## How a sequence works

**Triggers start the sequence.** It begins with a button press, a closed door, a filling tank, or a
schedule.

**Steps run in order.** Each action finishes before the next begins. "Close the inner door"
completes only when the door is physically shut. Your sequence holds together even when hardware
runs slow.

**Systems separate logic from hardware.** A single airlock system runs every airlock on your ship.
You simply assign the specific doors, vents, and buttons for each location.

## Why I made it

I built Sprocket to give more players access to complex automation, making ship logic approachable
for everyone regardless of background.

Basic timer chains quickly grow fragile, while the Programmable Block usually requires custom code.
That creates a steep barrier for players who simply want their ship to work. My goal was to
demonstrate an inclusive approach to automation and hopefully inspire Keen toward a similar
direction for the future of Space Engineers.

There is a practical benefit for experienced players too. Writing a full script for routine tasks
takes time that could be spent elsewhere. Sprocket lets you set up common systems quickly, giving
non-programmers a tool they can master while offering veterans a fast path through repetitive jobs.

Understanding a machine should be as immediate as reading a gauge. Sprocket achieves that by making
the description and the program the exact same document.

## Building the mod

Requires an installed copy of Space Engineers, .NET, and [MDK2](https://github.com/malforge/mdk2).
