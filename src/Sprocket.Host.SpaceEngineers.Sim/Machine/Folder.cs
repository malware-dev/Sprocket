using System.Collections.Generic;
using System.IO;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Sim.Machine;

/// <summary>The game's storage folder, kept in memory for as long as the harness runs.</summary>
public sealed class Folder : IFiles
{
    readonly Dictionary<string, string> _files = new Dictionary<string, string>();

    /// <summary>Every file and what it holds.</summary>
    public IReadOnlyDictionary<string, string> Files => _files;

    /// <inheritdoc />
    public bool Has(string name)
    {
        return _files.ContainsKey(name);
    }

    /// <inheritdoc />
    public string Read(string name)
    {
        string text;
        if (!_files.TryGetValue(name, out text))
            throw new FileNotFoundException(name);

        return text;
    }

    /// <inheritdoc />
    public void Write(string name, string text)
    {
        _files[name] = text;
    }

    /// <inheritdoc />
    public void Delete(string name)
    {
        _files.Remove(name);
    }
}
