"""Points a saved world at the renamed mod, and at the renamed things inside it.

Five things in a world carry the old name, and all of them are exact strings, so nothing here can
touch the game's own reactors by accident:

  Sandbox_config.sbc   the mod list the game reads when it loads the world - the one that decides
                       which mod folder is used, and the one this tool originally forgot
  Sandbox.sbc          the checkpoint's own copy of the same list
  SANDBOX_0_0_0_.sbs   the toolbar action written into every button slot
                       the status app chosen on an LCD
                       the first word of every saved program

And one thing that has to go rather than be changed: the binary cache beside the XML, named with a
B5 suffix. Where one exists the game reads it and never looks at the XML at all, so editing the XML
does nothing whatever. It is moved aside rather than deleted, and the game writes a fresh one from
the XML the next time it saves.

**The world must not be loaded.** The game holds a world it has loaded and writes the whole folder
back out when it saves, from its own idea of the mod list - so a conversion done while it is open is
undone at the next save, and anything extra left in the folder goes with it. Exit to the main menu
first. That is also why the backups are written beside the world rather than inside it.

    python tools/rename-world.py "F:/Source/se-test-appdata/Saves/<steam id>/<world>"

Nothing is released, so this exists for the worlds we test with rather than for anybody else.
"""

import os
import shutil
import sys

SWAPS = [
    ('Reactor.Host.SpaceEngineers', 'Sprocket.Host.SpaceEngineers'),
    ('<Action>Reactor_Trigger</Action>', '<Action>Sprocket_Trigger</Action>'),
    ('<SelectedScript>Reactor_Status</SelectedScript>',
     '<SelectedScript>Sprocket_Status</SelectedScript>'),
    ('<Value>reactor ', '<Value>sprocket '),
]

FILES = ['Sandbox_config.sbc', 'Sandbox.sbc', 'SANDBOX_0_0_0_.sbs']

CACHES = ['SANDBOX_0_0_0_.sbsB5', 'Sandbox.sbcB5', 'Sandbox_config.sbcB5']


def fix(folder):
    folder = folder.rstrip('/\\')
    if not os.path.isdir(folder):
        print('no such world:', folder)
        return 1

    kept = folder + '.before-rename'
    if not os.path.isdir(kept):
        os.makedirs(kept)

    touched = 0
    for name in FILES:
        path = os.path.join(folder, name)
        if not os.path.exists(path):
            print(name, '- not here')
            continue

        with open(path, encoding='utf-8', errors='surrogateescape', newline='') as f:
            was = f.read()

        now = was
        counts = []
        for old, new in SWAPS:
            found = now.count(old)
            if found:
                counts.append('%d x %s' % (found, old))
                now = now.replace(old, new)

        if now == was:
            print(name, '- nothing of ours')
            continue

        backup = os.path.join(kept, name)
        if not os.path.exists(backup):
            shutil.copy2(path, backup)

        with open(path, 'w', encoding='utf-8', errors='surrogateescape', newline='') as f:
            f.write(now)

        print(name, '-', ', '.join(counts))
        touched += 1

    for name in CACHES:
        path = os.path.join(folder, name)
        if not os.path.exists(path):
            continue

        shutil.move(path, os.path.join(kept, name))
        print(name, '- moved aside, or the XML would never be read')
        touched += 1

    print('done,', touched, 'files changed; originals in', os.path.basename(kept))
    return 0


if __name__ == '__main__':
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(2)

    sys.exit(fix(sys.argv[1]))
