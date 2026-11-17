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

And no, it doesn't make programmable block scripts. Your programmable blocks are just where you get to
Sprocket. So it works fine on servers where in-game scripts are switched off.

## One logic system, multiple installations

Describe your airlock logic _once_, then add **installations** to run the same logic on several airlocks, without
adding more programmable blocks.

## Share

Keep **blueprints** of your logic systems in the library, and share them, whether it's just with your best friend,
your faction, or the entire system.

The mod API gives no way to share them through the Workshop, so sharing happens within a world. To share outside the
game, open a blueprint's **Share...** and pick **Copy...**: as plain text, or compact enough to post in a chat. Paste it
into a programmable block's Custom Data in any world, and press **Import** in the library.

## Getting started

Subscribe to Sprocket Automation on the Steam Workshop, then find any programmable block in the terminal and press
**Automation**. Not sure what something does? Rest the pointer on it for a tip, and hold Alt to keep the tip open and
explore the words it links.

## Why I made it

I want more players to have a go at complex automation, programmer or not. Timer chains get fragile fast, and the
programmable block wants you to write _code_. That's a pretty steep wall if all you want is an airlock that cycles itself. Honestly?
I'd love for Keen to take a look at this, and maybe take Space Engineers somewhere similar.

It's not just for beginners though. Even if you can write scripts, writing one for every little routine job gets old
fast. Sprocket lets you knock out the common stuff quickly.

Oh, and you shouldn't have to dig through code to figure out what a machine does. In Sprocket the description _is_
the program. What you read is exactly what runs.

## Building the mod

Requires an installed copy of Space Engineers, .NET, [MDK2](https://malforge.github.io/spaceengineers/mdk2/), and a GitHub
account for the malforge package feed. How a machine is set up, built and tested is in
[CONTRIBUTING.md](CONTRIBUTING.md).

How the repository is put together, and how its parts work, is in the developer guide, starting at
[docs/Overview.md](docs/Overview.md).

## License

Released under the Malforge Attribution License for Space Engineers Mods v1.0 (BRUTAL variant), in [LICENSE.txt](src/Sprocket.Host.SpaceEngineers/LICENSE.txt). The short version, in plain words: [the summary](https://github.com/malforge/licenses/blob/main/MAL-SEMods-1.0-summary.md).

Use it, change it, re-upload it, as long as you credit this mod and link back to it.

Before you publish your own copy though, please consider contributing here instead. Several copies of the same mod just leave people confused about which one has which bug, and every author involved, original or copy, ends up getting reports about bugs that aren't theirs.
