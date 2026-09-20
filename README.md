<p align="center"><img src="art/thumbnail/sprocket-mark.svg" width="180" alt="Sprocket"></p>

<h1 align="center">SPROCKET</h1>

<p align="center"><b>MALTEK INDUSTRIAL</b></p>

---

## Your ship already knows how to open a door. Now it knows when.

Bolt it on. Point at the doors. Tell it what happens.

Sprocket runs your ship the way you described it, every time, whether you are aboard or not.

---

### A complete airlock

```
When the Airlock Button is pressed
  1. close the inner door
  2. lock the inner door
  3. depressurize
  4. unlock the outer door
  5. open the outer door
```

That is the whole program. What you read is what runs.

---

### How it works

**Something sets it off.** A button. A door. A tank filling. Or the clock.

**Steps run in order.** Each one finishes before the next starts. "Close the inner door" is done
when the door is shut. If it jams, the sequence stops there and tells you.

**You point at the blocks.** Pick the doors on your ship. Sprocket writes the rest.

---

### Specifications

Runs in the ship's own terminal, on a screen the game provides. One sequence, top to bottom, is the
whole story: a glance tells you what your ship does.

---

### Why we built it

The hard part of a program is holding it in your head. Take the syntax away and that part is still
there, which is why most people stop.

So Sprocket goes the other way. What your ship does should be something you read, like a gauge.

---

<sub>Requires Space Engineers. Building from source requires .NET, <a href="https://github.com/malforge/mdk2">MDK2</a> and the game installed;
<code>dotnet build src/Sprocket.Host.SpaceEngineers</code> compiles the mod and deploys it.
Specifications subject to change without notice.</sub>
