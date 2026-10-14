<p align="center"><img src="art/thumbnail/sprocket-mark.svg" width="180" alt="Sprocket"></p>

<h1 align="center">Sprocket Automation</h1>

Event controllers, timer blocks, sensors... there is already a means for automation in Space Engineers, sure. But it
takes up a whole bunch of space, is limited in what it can do, and fragile. One railgun shot to your automation bay and
you have to set _everything_ up again.

## Enter Sprocket Automation

Describe your automation in simple but powerful **logic systems**. Set up **triggers** to run sequential steps. Easily
wait until a door is actually closed before pressurizing that airlock, without manual timing. Go one step further, with
more complex expressions and states.

Sprocket lives within _all_ of your programmable blocks. If you lose one, but there's still another on your ship, your
automation is still fully functional. And even if the last programmable block is lost, the automatic backup system has
stored away your logic, so you can restore it to a new ship or back to your repaired old one.

## One logic system, multiple installations

Describe your airlock logic _once_, then add **installations** to run the same logic on several airlocks, without
adding more programmable blocks.

## Share

Keep **blueprints** of your logic systems in the library, and share them, whether it's just with your best friend,
your faction, or the entire system.

The mod API gives no way to share them through the Workshop, so sharing happens within a world. To share outside the
game, share a grid blueprint holding the logic, or the blueprint files themselves, which live in the game's Storage folder
(`%AppData%\SpaceEngineers\Storage` by default).

## Getting started

Subscribe to Sprocket Automation on the Steam Workshop, then find any programmable block in
the terminal and press **Automation**. The in-game codex, behind the question mark on every screen, walks you through
your first airlock step by step.

## Why I made it

I built Sprocket to give more players access to complex automation, making ship logic approachable
for everyone regardless of background.

Basic timer chains quickly grow fragile, while the Programmable Block requires custom code.
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

How the repository is put together, how to build and test it, and how its parts work is in the developer guide,
starting at [docs/Overview.md](docs/Overview.md).
