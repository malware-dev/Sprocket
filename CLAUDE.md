# Sprocket

How this repository is built and what its code must look like live in the skills: `csharp-standards`
for the general rules, `sprocket-standards` for what is true here and would be wrong anywhere else.

This file is for facts about the machine and the game that are in neither.

## Editing a saved world by hand

A Space Engineers save keeps its sector twice. `SANDBOX_0_0_0_.sbs` is XML and readable;
`SANDBOX_0_0_0_.sbsB5` is the binary beside it, and the binary wins.

**An edit to the XML does nothing until the `.sbsB5` is deleted.** The game writes a fresh one from
the XML on the next save, so deleting it is how an edit takes rather than something to avoid.

The binary holds its strings in the clear, so a replacement of exactly the same length can be made in
both files and each stays valid - a length-prefixed field keeps its length. Anything that changes a
length has to go through the XML with the binary deleted.

Unload the world first, and copy both files somewhere before touching either.

A stored program can be read straight out of the XML: `grep -a "sprocket " SANDBOX_0_0_0_.sbs`. The
version on that line says which turn of the format wrote it, which is far quicker than reasoning
about what the editor did.

## What needs the game restarted, and what does not

**Content is cached for the life of the process. Code is not.**

Fonts, textures and anything else under `Content` are read once when the game starts, so a change
to a page, a `.dds`, or a definition in an `.sbc` is invisible until the game is started again.

A change to the mod's own code is not. The scripts are compiled from the mod folder when a world
loads, so a quickload picks it up.

So after `dotnet build src/Sprocket.Host.SpaceEngineers`, say which of the two changed.
