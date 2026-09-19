using System;
using System.Collections.Generic;
using System.IO;
using VRage.Game.Components;

namespace Sprocket.Sim.Machine;

/// <summary>
/// Mod storage that survives closing the window.
/// </summary>
/// <remarks>
/// The mod puts a construct's program in the block's mod storage and the game writes that into the
/// save. Here the same storage writes through to a file per key, which is what makes the windowed
/// simulator a playground somebody can come back to.
/// <para>
/// Derived from the game's own abstract rather than from its concrete storage: what the mod holds is
/// the real type either way, and only the keeping of it is the harness's.
/// </para>
/// <para>
/// A key names its own file, so the program is a text file a person can read and repair - the same
/// property the script has in the save, and the reason a broken one is worth keeping rather than
/// refusing to start over.
/// </para>
/// </remarks>
sealed class Store : MyModStorageComponentBase
{
    readonly Dictionary<Guid, string> _held = new Dictionary<Guid, string>();
    readonly string _folder;
    readonly string _construct;

    Store()
    {
        _folder = "";
        _construct = "";
    }

    public Store(string construct)
    {
        _construct = construct;
        _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sprocket");

        Directory.CreateDirectory(_folder);
        Read();
    }

    /// <summary>
    /// Storage that remembers nothing.
    /// </summary>
    /// <remarks>
    /// For tests and for captured frames. Without it every one of them would load whatever program
    /// happened to be sitting in the playground and pass or fail on what somebody last built by
    /// hand, which is the sort of flakiness that takes a day to track down.
    /// </remarks>
    public static Store Nowhere()
    {
        return new Store();
    }

    /// <summary>Where this is being kept, for the line at the bottom of the window.</summary>
    public string Where => _folder.Length == 0 ? "nowhere - nothing is being saved" : _folder;

    public override string GetValue(Guid key)
    {
        string held;
        return _held.TryGetValue(key, out held) ? held : null;
    }

    public override void SetValue(Guid key, string value)
    {
        _held[key] = value;
        Wrote(key, value);
    }

    public override bool RemoveValue(Guid key)
    {
        return _held.Remove(key);
    }

    public override bool TryGetValue(Guid key, out string value)
    {
        return _held.TryGetValue(key, out value);
    }

    void Wrote(Guid key, string value)
    {
        if (_folder.Length == 0)
            return;

        // The key first, then what it holds. The name is only the start of the key, which cannot be
        // turned back into one, and a file that says what it is can be read back without guessing.
        File.WriteAllText(Path.Combine(_folder, Name(key)), key.ToString("D") + "\n" + (value ?? ""));
    }

    void Read()
    {
        foreach (var file in Directory.GetFiles(_folder, _construct + ".*.sprocket"))
        {
            var parts = Path.GetFileName(file).Split('.');
            if (parts.Length != 3)
                continue;

            foreach (var pair in Known(parts[1], file))
                _held[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    /// The key a file is named for, where its name is one this store wrote.
    /// </summary>
    /// <remarks>
    /// A file is named by the start of its key, which is enough to tell two apart and short enough
    /// to read. The whole key is in the file's first line, because the short form cannot be turned
    /// back into one.
    /// </remarks>
    IEnumerable<KeyValuePair<Guid, string>> Known(string shortKey, string file)
    {
        var text = File.ReadAllText(file);
        var end = text.IndexOf('\n');
        if (end < 0)
            yield break;

        Guid key;
        if (!Guid.TryParseExact(text.Substring(0, end).Trim(), "D", out key)
            || !Name(key).Contains(shortKey))
            yield break;

        yield return new KeyValuePair<Guid, string>(key, text.Substring(end + 1));
    }

    string Name(Guid key)
    {
        return _construct + "." + key.ToString("D").Substring(0, 8) + ".sprocket";
    }
}
