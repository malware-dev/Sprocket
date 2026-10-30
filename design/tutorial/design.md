# Sprocket tutorial scenario

Card: "A tutorial scenario takes a player through building real automation".

A scenario world that takes a player from opening one door to a ship whose airlocks, ramp and screens all run on
Sprocket. Draft; nothing here is settled until Mal says so. Facts marked *(ask)* wait on sprocket-main.

Every challenge has to do three things at once: make sense in the story, show something Sprocket can do, and ask a
little more of the player than the one before. And every challenge can be helped along with tips when the player is
stuck.

## The "story"

The player is the newest **QA assistant** at **Sprocket Automation**, the company that makes the mod (in the world, a
company that makes automation for ships). First day on the job. They have two things to do: prove themselves, and
help customers with their problems. Their desk is a test ship, the *Trainee*, fresh from the shipyard with Sprocket
installed and nothing programmed. Their boss, **the QA lead** (name to pick), sends them work over chat as test
tickets.

### The QA lead's voice

Booming corporate confidence, in the register of J.K. Simmons' Cave Johnson, but our own man, not a copy. The rules:

- **Unshakeable belief in the product.** Sprocket is the finest thing ever made, and every failure is the tester's.
- **Short, loud sentences.** He announces rather than explains. One idea a line.
- **Procedure is sacred, and he keeps getting it slightly wrong.** HR rules, certifications, forms he half remembers.
- **He is never cruel.** He is proud of the player when they pass, in his own way, and the pride is real.
- **The teaching is always plain.** The joke is in the first line; the instruction is the second, and it says exactly
  what to press. A player who skips the jokes still learns.

No Portal references, no cake, no science, no lemons. The voice is the borrowing; the jokes are ours.

The tickets are why each challenge exists:

- Early tickets are **"verify the feature works"**: you are the first person to try it on this hull.
- Middle tickets are **customer complaints**: "Customer reports the airlock opens both doors at once." The player
  fixes what a customer would hit, which is a reason to add an if, a second installation, a lockdown.
- The last tickets are **shipping it**: the work goes out as a blueprint, to the shuttle and to a "customer".

The story also excuses the scenario's tricks. The *Trainee*'s doors are "Sprocket-certified", locked to automation
only, so the first door shuts again if it is opened by hand. QA can switch a vent off mid-cycle on purpose,
because that is what testers do.

## The ship and the station

The *Trainee*, docked by connector at the company's test station. Everything is built and powered; the challenges
are about automation, never about building.

The **station** holds the QA office, with the ticket board and the office lights, seen through a window from the
ship's dock. Its own automation, *QA Office*, runs them (see [Built with Sprocket](#built-with-sprocket)). The player
doesn't need to go there until a late ticket.

The ship's rooms, in the order the player meets them:

1. **The test cabin**, where the player starts. One door out, a programmable block, an "I'm stuck" button.
2. **The corridor**, with a button panel by the cabin door.
3. **The airlock**: inner door, outer door, an air vent, a light, a button panel on each side.
4. **The cargo bay**, with a ramp on pistons and a second airlock built like the first.
5. **The shuttle**, docked by connector, with its own programmable block and airlock. Docked, it is a construct of its
   own.

## How a challenge is met

Recommended: **the mod reads the player's automation** as well as watching the blocks *(ask: is there a way today)*.

Watching blocks alone can't tell a door opened by hand from one a sequence opened. Reading the automation lets the
scenario react part way ("You have a sequence. Now give it a step."), and lets the tips know exactly where the player
is. A challenge is met when the program has the right shape and a run of it did the thing to the blocks.

If the scenario is its own Workshop item, reading the automation is an API between two mods; inside Sprocket it is
internal. That leans towards shipping inside the mod; Mal's call.

## Built with Sprocket

The scenario is also a test of Sprocket. The rule: **Sprocket runs what would make automation sense to someone in
the world**, and the scenario's code runs the scenario. A company office whose board, lights and doors are automated
is in-world automation; a door that shuts because the tutorial wants it shut is a scenario mechanic, and is code.
Wherever in-world automation needs something Sprocket can't do yet, that is a finding for the mod.

### Director and office

- **The director** is the scenario's code. It watches the player, talks, keeps the quest log and gives tips, and it
  does every scenario mechanic: shutting the certified door after a hand-open, switching the vent off for a test,
  breaking a block for the failure ticket, spawning and walking NPCs, resetting the ship.
- **The office** is the station's own automation, *QA Office*, as Sprocket Automation would really run its own
  building. It lives on the station, so the player's home screen on the ship starts empty and everything on it is
  theirs.

What the office automates, each because it would in a real office:

| In the office | Sprocket it puts to work |
|:--|:--|
| The ticket board, seen from the ship's dock window: open tickets, passed ones struck through | Screens, `show text`, rich text |
| Lights that flash green when a test passes, an alarm when one fails | Tasks on several blocks, delays |
| The office's own doors and airlock to the dock | The same airlock the player builds, done properly |
| A clock and a "days without incident" counter, reset by the alarm | `every`, values |
| A flag light per open ticket, read by the board and the alarm | Flags between logic systems through block values |

The director tells the office a ticket's news by switching a hidden light on the station, which the office has a
`when` on, as a real office might take a feed from its test bench.

A late ticket sends the player to the station to read how *QA Office* works. By then they know installations and
blueprints, so a large logic system they didn't write teaches rather than confuses.

### Action relays

Relays are kept for where they make in-world sense: the shuttle and the station talking to each other, with Sprocket
on both ends, as a late ticket ("The shuttle must tell the station when it's docked"). Notes from sprocket-main,
none tried in the game:

- A task sets a relay's `channel` and does `send signal`.
- Receiving is offered only under `Terminal ›`, as `runs a toolbar slot`. That slot must be left free, since filling
  the signal slot puts Sprocket's action in it.
- The receiving relay must be on the trigger's own construct.
- `receives from` may need "everyone" between the player's shuttle and the company's station.

### NPCs and the world

The director can walk NPCs only where it knows the ground: **fixed, well-known prefabs that the player can't build on
or destroy**. The station, at least the office and the dock, has to be that: protected, owned by the company. The
*Trainee*'s rooms are the player's, so an NPC on the ship stays to fixed spots or seats, or the walk checks the route
before it starts.

### Findings so far, for Mal

- A relay receiving is only offered under `Terminal ›`, as `runs a toolbar slot`. A relay is the game's way for
  constructs to talk, so it deserves a known signal that names the channel, like *when Station Relay receives on
  channel 3*, so one relay can fire different triggers for different channels. Today it looks like one relay per
  channel, each its own trigger (to confirm with sprocket-main).

## NPCs

Read from the game's own assemblies, build of 2026-10-16, since nothing is written about the new NPCs yet. What the
API offers, not what works: none of it tried.

**What a mod can reach** (the public mod API, `Sandbox.ModAPI` and `VRage.Game.ModAPI`):

- **Spawn:** `IMyNPCSpawnerSessionComponent.SpawnBot(botDefinitionId, position, direction, up, name, factionTag,
  weaponsContainerSubtype, lootContainerSubtype, chatMessageSetSubtype)`. A named bot, in a faction, with its own
  chat lines.
- **Seat:** `IMyCockpit.AttachPilot(pilot, animation)` puts any character in a seat, and `RemovePilot` takes it out.
- **Puppet:** a character is an `IMyControllableEntity`, with `MoveAndRotate`, `Use` (use what it faces, such as a
  button or a door), `Jump`, `Crouch`, `SwitchHelmet`. `IMyCharacter.TriggerCharacterAnimationEvent` plays an
  animation event. `IsBot` tells a bot from a player.
- **Paths:** `IMyPathfindingSessionComponent.FindAvoidPath` finds a path, though it reads as made for ships.

**What a mod can't reach:** the bots' own brains. Route planning (`PlanGoto`, `PlanFollow`), cover, alerts, chat
squads and the station anchors that place trade station NPCs are all in the game's code, not the mod API.

**By definition (SBC), no code:** a humanoid bot definition has `ChatMessageSet`, `PlayerGreetingAnimation` and
`BlockInteractionAnimation` beside its combat settings; emote definitions have animations. A bot of our own, unarmed
and with no wish to fight, looks writable.

**So:** walking NPCs are likely possible by puppeting, driving `MoveAndRotate` from the director along a route we
lay out. The unknown is whether the bot's own AI fights the puppeting. Seated NPCs are close to certain. Seated is
the floor, not the aim.

To try first, in this order:

1. Spawn an unarmed bot of our own definition; does it stand still, or wander and fight?
2. Seat it with `AttachPilot`; does a Sprocket trigger see the seat occupied?
3. Walk it a few metres with `MoveAndRotate`; does its own AI take over?
4. Have it `Use` a button panel; does Sprocket's trigger fire?
5. Does a sensor detect it, and by which of its filters?

Ideas, from certain to hoped for:

- **Seated:** the QA lead at a desk in the office, seen through the window, chat sent under their name, a greeting
  animation when the player comes by. A "customer" seated in the shuttle for the shipping ticket, the seat being
  occupied part of the ticket.
- **Walking:** a customer who walks up to the airlock and presses its button. The player's automation has to cycle
  them through, and a sensor or the seat tells the office it worked. The best test of the player's work in the whole
  scenario, if the puppet holds.

## Monitoring the director needs

The director's eyes are the one part of the scenario Sprocket isn't meant to provide as a player feature, but it has
to see into Sprocket to do its job. Good enough, not complete: what the challenges and tips need, and no more. A
first list, to be cut down as the challenges settle:

- **The program as it changes:** logic systems, sequences and their steps (kind, task, blocks, options), triggers,
  installations and their slots, blueprints in the library. A snapshot to read plus "something changed" is likely
  enough; the director can compare.
- **Runs:** a sequence's run starting, reaching each step, and ending, failing or holding, with its installation and
  whether it was started by hand or by a trigger.
- **Tasks done on blocks:** which block a run acted on and with what. This is how the director tells the player's
  sequence opening the cabin door from a hand-open.
- **The editor:** which screen is open, so tips can wait for the player to be in the right place, and whether the
  logic system is switched on.
- **Opening a codex page** for a tip's third level, and later perhaps pointing at a button on screen.

sprocket-main says none of this is exposed today but the model knows all of it, and that it is much easier with the
tutorial inside the mod.

## Tips

Each challenge is split into **beats**, the small things the scenario can see happen (the home screen opened, a
logic system made, a step added). Each beat has up to three tips, from a nudge to the answer:

1. **Nudge:** what to aim for. "The door needs a job to do. Jobs live in sequences."
2. **Where:** which screen and button. "On the *Cabin* logic system, press `+ sequence`."
3. **Show:** the exact clicks, and the codex page if one covers it.

A tip comes when the player has made no progress on the beat for a while (a minute or so for the first, longer
after), or at once when they ask. Asking is the "I'm stuck" button in the cabin, or a chat command
*(ask: which is possible)*. In the story, tips come from the QA lead, a little louder each time.

Getting a beat wrong gets its own line where the scenario can see it: a step on the wrong door, a trigger on the wrong
button, editing while the logic system is on.

## The challenges

For each: the ticket (why, in the story), what it shows, the beats with what the player is told, how it is met, and
what the ship needs. "Chat" is a message from the QA lead; "Task" the mission task line (the quest log); "Board" the ticket board
in the QA office.

### 1. Open the door

*Shows:* the home screen, a logic system, a sequence, a task step, the block selector, running a sequence by hand.

The player is assumed to know nothing, not even that Sprocket lives in the programmable block.

**Arrival.** The player spawns standing in the *Trainee*'s test cabin, helmet off. The cabin is lit and pressurized,
the door shut. On the wall facing them is the programmable block, a plate beside it: *Sprocket Automation - Test
Cabin 1*. Through the cabin window: the dock, and across it the QA office, the lead at his desk. Nothing happens for
about three seconds, so they look around.

**The welcome**, in chat under the lead's name:

> "Good morning! You're the new QA assistant. Welcome to Sprocket Automation, the finest automation company in
> known space. HR says I have to tell you that you're valued. You're valued."
>
> "Before I let you anywhere near a customer, you prove you can use the product. You are standing in Test Cabin 1.
> Get out of it."

Quest log: **Probation. Test 1: Leave the test cabin.**

**The door.** Most players go for the door first, and that's the joke. It opens, and the director shuts it again at
once:

> "That's a Sprocket-certified door! It does not respond to hands. It responds to *automation*."
>
> "The programmable block on the wall. Open its terminal and press `Automation`."

A player who goes to the programmable block first never sees the joke, which is fine.

**The beats.** Each line comes when the beat before it is done. Tips come if the player stalls (see [Tips](#tips)).

| Beat | The lead says when it starts | Tips |
|:--|:--|:--|
| Open Automation | (the welcome, or the door joke) | 1: "Sprocket lives in the programmable block. Every one on the ship." 2: "Terminal. Programmable block. The `Automation` button. In that order." |
| Make a logic system | "This is the home screen. Everything this ship does for itself starts here. Each machine gets a logic system. Make one for the cabin: `+ logic system`, and call it *Cabin*." | 1: "Look for the plus." 2: "`+ logic system`. Name it *Cabin*." |
| Make a sequence | "A logic system's jobs are sequences. A sequence is a list of steps, done top to bottom. Press `+ sequence` and call it *Open Door*." | 2: "Under `Sequences`, `+ sequence`, *Open Door*." |
| Add the step | "One step. A task: something done to a block. `+ step`, `task`, select the cabin door, then `open`." | 1: "Not a delay. Not an if. A task." 2: the exact clicks again. |
| Run it | "Beautiful. Now run it. The play mark on the sequence's row." | 2: "The triangle. On *Open Door*'s row." |

*Wrong:* a task on another block: "That is not the door. That is a *different* block. The door is the one between you
and the rest of your career."

*Met when:* a run of a sequence with an `open` task on the cabin door ends with the door open.

**Out.** The door opens on their own run. The office board ticks the line off, and its lights flash green.

> "Test passed! You are now qualified to open doors. Don't let it go to your head. Most people never get that far.
> Most people use their hands."

*Ship needs:* the cabin door; the programmable block in reach, with its plate; a window onto the office.

*Director:* shuts the door on a hand-open; tells the office the test passed.

### 2. Close it behind you

*Ticket:* "Facilities says you left the cabin door open. Make *Open Door* close it after you."

*Shows:* awaitable actions, a delay.

*Beats:* add a delay; add `close`. Told that `open` is awaitable, so the sequence waits for it, and that a delay holds
it for a set time.

*Met when:* *Open Door* runs open, delay, close, and ends with the door closed.

### 3. A button for it

*Ticket:* "Running sequences by hand is for testers. Customers press buttons. Put *Open Door* on the corridor
button."

*Shows:* triggers, signals, the logic system's switch.

*Beats:* switch the logic system off to change it (if it is on); add a trigger on the button; switch it on; press it.
Told that a logic system changes only while off, and its triggers fire only while on.

*Met when:* a press of the button starts *Open Door*.

### 4. Cycle out

*Ticket:* "The airlock has never been run. Make it cycle out from the inner button."

*Shows:* several awaitable steps on several blocks.

*Beats:* a new logic system, *Airlock*; *Cycle Out*: close inner, depressurize, open outer; trigger on the inner
button. The codex page *Simple airlock* covers it, and the third tip points there.

### 5. Both doors at once

*Ticket (complaint):* "Customer reports the airlock 'takes forever'. Close both doors together before cycling."

*Shows:* `wait for pending tasks`.

*Met when:* a run closes both doors under one wait.

### 6. One button, both ways

*Ticket (complaint):* "Customer pressed the inside button from outside and lost a crewman. Make each button do the
right thing for the air in the room."

*Shows:* if, otherwise; then otherwise if.

*Beats:* *Cycle In*; one sequence that checks the vent and runs the right way. Then the twist: QA leaves the
room half full *(ask: can the scenario do that)*, and the next ticket asks for an `otherwise if` that closes both
doors.

### 7. Tell me when it's safe

*Ticket:* "Crew want to see it's safe before they go. Green light when the room is empty, red when it has air."

*Shows:* a trigger on a state or a value.

### 8. Down the ramp

*Ticket:* "Cargo wants one button to get off the ship, and one to shut everything in an emergency."

*Shows:* run and stop, an action with options (piston position and speed).

*Beats:* *Disembark* runs *Cycle Out* and lowers the ramp; *Lockdown* stops both cycles and closes both doors.

### 9. The second airlock

*Ticket:* "The cargo bay airlock is the same model. Make it work. You're not allowed to write it twice."

*Shows:* installations, slots, the installation panel.

*Beats:* `+ installation`; fill its slots; name the slots so the panel makes sense.

*Met when:* the second airlock cycles on its own button and the first is untouched.

### 10. Ship it

*Ticket:* "The shuttle goes to a customer tomorrow and its airlock does nothing. It's a separate ship. You know what
to do. (You don't. Ask the library.)"

*Shows:* constructs, blueprints, the library.

*Beats:* `Make blueprint`; from the shuttle's programmable block, `Add to this construct`; fill its installation.

*Met when:* the shuttle's airlock cycles.

*Ending:* "All tickets closed. Welcome to the team." The world stays open to play on.

### Not yet placed

Timed triggers (`every`, a blinking beacon), screens (a status panel that says *Cycling...*), runs that fail, hold
or end (a test where QA breaks a block on purpose), sharing a blueprint.

## What sprocket-main says (2026-10-21)

Game-side answers come from the mod API and still need trying in the game.

- **Mission tasks:** very likely, through the game's scripting provider (quest log title and objective lines,
  updated and marked done).
- **Chat and HUD notifications:** yes, under any sender name, coloured. The QA lead can speak as themselves.
- **Door locked to automation only:** unknown, needs trying. **Noticing a hand-open and closing the door: yes.**
  Fallback for challenge 1: the door snaps shut and the QA lead says "Hands off. Certified door."
- **Changing the world:** switching blocks on and off, placing blocks, spawning grids, moving the player: yes.
  **A half-full room: unknown**, there is no direct way to set a room's oxygen. Challenge 6's twist may need another
  way in, such as the director switching the vent off mid-cycle.
- **Reading the automation:** nothing exposes it today, but the model knows when a program changes, when runs start
  and stop, and which editor screen is open, so it is buildable. Much easier inside the mod.
- **Tips on Sprocket's screens:** new work. Opening a codex page: possible from inside the mod.
- **On screen:** `wait for pending tasks`. The codex is right; the writing skill's vocabulary is out of date.
- **A docked shuttle is its own construct:** confirmed.

So, for Mal: everything that makes the tips good points to shipping the tutorial inside Sprocket. A separate
Workshop item would need a channel into the mod built for it first.
