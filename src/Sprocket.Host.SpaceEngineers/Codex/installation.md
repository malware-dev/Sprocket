The logic system says what happens; each installation says which blocks it happens to. There is only ever one [logic system](#logic-system): every installation runs the same one.

## One logic system, many installations

An airlock logic system can have three installations, one for each airlock on the ship. Each fills the logic system's slots with blocks of its own: its own inner door, its own outer door, its own vent, its own triggers. All three do the same thing, each to its own airlock, and each runs on its own: cycling one airlock leaves the other two as they are.

Changing the logic system changes all of its installations at once. Fix a step in the airlock, and every airlock gets the fix.

## Making and changing one

**+ installation** under a logic system adds one, and opens the configuration panel to choose its blocks. Pressing an installation opens the same panel again, to swap its blocks and triggers.

Each installation has a switch of its own, so one airlock can be turned off while the others keep working. A logic system always has at least one installation; the last one cannot be deleted.

## When a block is missing

If a block an installation uses is gone, the installation's row turns red, and so does its logic system's. A sequence that reaches the missing block stops there. Pressing the installation opens its configuration panel, where another block can fill the slot. A slot marked optional is the exception: the steps that use it are skipped instead.

## Not to be confused with

A blueprint is a copy of the logic system itself, kept in the library. Installing a blueprint on another construct gives that construct the logic system, with its first installation.
