A logic system automates one job, for example an airlock, a hangar door or a drill rig. Each one on this [construct](#construct) has its own heading on the [home screen](#home).

## What it is made of

- [Sequences](#sequence) say what happens, step by step.
- [Triggers](#trigger) say when, for example when a [signal](#signal) arrives, a door finishes closing or a set time passes.
- [Installations](#installation) say which blocks take part, for example the doors and the vent, and the blocks that send its signals.

Because the blocks are kept apart from the job, one logic system can run every airlock on your ship.

## A first airlock

1. On the home screen, press **+ logic system**, and name it *Airlock* on its heading.
2. Under **Sequences**, press **+ sequence**, and name it *Cycle Out*.
3. Press **+ task**, [pick](#picking) the inner door, and choose the *close* with the clock beside it. The [sequence](#sequence) page explains the clock.
4. Add *depressurize* for the vent and *open* for the outer door the same way.
5. Go back. Under **Triggers**, press **+ when**, pick the airlock's button, and have it run *Cycle Out*.
6. Leave the screen, and switch the logic system on when asked.

## Switch it off to change it

A logic system can only be changed while it is switched off, so a half-built sequence never starts by accident. A new one starts switched off.

Switching it off stops anything running and leaves the blocks as they are. When you leave a changed logic system that is still off, you are asked whether to switch it on.

## Real blocks, kept as slots

The blocks you pick become [slots](#slot), such as *the inner door*. **Using the blocks of**, at the top of the logic system's screen, chooses whose blocks the steps show.

## More than one airlock

On the home screen, press **+ installation** under the logic system, and fill its slots with the second airlock's blocks on the [configuration panel](#configuration-panel). Both airlocks now run the same sequences on their own blocks, and a change to a sequence reaches both.

## The configuration panel

The configuration panel is where an installation gets its blocks. It opens when you press **+ installation**, or an installation on the home screen. It lists every slot, the blocks that fill it, and how many slots still need a block.

There are two kinds of slot. A block slot holds the blocks a sequence acts on or watches, such as the doors and the vent. A signal slot holds a block and the toolbar slot that sends the signal, such as the airlock's button. That toolbar slot is set up for you, and you are asked first if it already does something else.

Anyone who receives your blueprint has never seen your construct, so the panel is their only guide. **Edit configuration panel**, on the logic system's screen, sets what it tells them:

- a description of the logic system, with formatting such as headings and lists,
- each slot's name, and what it is for,
- whether a slot is required or optional.

A slot starts with the name of the block you picked, such as *Airlock Inner Door 2*. Rename it *Inner door* and describe it as the door on the pressurized side, and whoever fills it gets it right the first time.

## When something goes wrong

On the home screen, a row lights green while it runs and red when it needs mending. Open it to see why.

If a sequence cannot carry on, for example because a block is gone or a slot is empty, that installation is switched off, and its row says why: *Stopped at step 1 of Cycle Out: Airlock Inner Door was gone.* Put right what it names, for example by filling the slot on the configuration panel, and switch the installation on again. Steps that use an empty optional slot are skipped instead.

## Keeping and sharing it

**Make blueprint** saves a copy in the [library](#library) as a [blueprint](#blueprint). From then on, the same button reads **Update blueprint**, and brings the library's copy up to date. Putting the blueprint on another construct makes a logic system with one installation, whose slots are filled on the configuration panel.
