using System;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Host.SpaceEngineers.World
{
    // Every program the harness writes is copied in memory and written again, so the whole suite proves a copy says exactly
    // what its original says. A .debug.cs file, compiled into local builds only and never packed for the game.
    public sealed partial class SprocketUnit
    {
        partial void Wrote(string program, string state)
        {
            var values = new Values();
            var copy = new Automation();
            ProgramCopy.Into(_program, _world.Values, copy, values);

            var copied = ProgramWriter.Save(copy, values);
            if (copied != program)
                throw new InvalidOperationException($"A copy of the program writes out differently.\nOriginal:\n{program}\nCopy:\n{copied}");

            if (BlockSwitches.Save(copy, _binary) != state)
                throw new InvalidOperationException("A copy of the program has different switches.");
        }
    }
}
