using System;
using System.IO;
using Reactor.Sequences;
using Reactor.Storage;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Sim.Machine;

/// <summary>
/// Keeps the prototype's program on disk between runs.
/// </summary>
/// <remarks>
/// The harness half of storage. Core turns a program into text and back and never touches a
/// file, because Space Engineers does not whitelist IO - a mod puts its data in the save. So
/// everything here is the part that will be written differently in the game and identically
/// in spirit: work out where this construct's program lives, read it, write it back.
///
/// One file per construct, named by the construct. The prototype only has one, but naming it
/// that way now means the game host is not tempted into a single global blob.
///
/// Failing to load is not fatal. A prototype that refuses to start because of a bad save is
/// worse than one that starts empty and says so - and the file is text, so a broken one can
/// be looked at.
/// </remarks>
sealed class Store
{
    readonly string _path;

    string _lastWritten = "";

    Store()
    {
        _path = "";
    }

    /// <summary>
    /// A store that remembers nothing.
    /// </summary>
    /// <remarks>
    /// For tests. Without it every test would load whatever program happened to be sitting in
    /// the real save and pass or fail depending on what somebody last built by hand, which is
    /// the sort of flakiness that takes a day to track down.
    /// </remarks>
    public static Store Nowhere()
    {
        return new Store();
    }

    public Store(string construct)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Reactor");

        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, construct + ".reactor");
    }

    public string Path_ => _path;

    /// <summary>What went wrong last time, or empty.</summary>
    public string Trouble { get; private set; } = "";

    public void Load(Automation program)
    {
        try
        {
            if (_path.Length == 0 || !File.Exists(_path))
                return;

            _lastWritten = File.ReadAllText(_path);
            ProgramText.Load(_lastWritten, program);
        }
        catch (Exception error)
        {
            Trouble = error.Message;
            _lastWritten = "";
        }
    }

    /// <summary>
    /// Writes the program out, but only when it differs from what is already there.
    /// </summary>
    /// <remarks>
    /// Called on a timer rather than hooked to every edit, because there is no single place an
    /// edit lands - and comparing the text is cheap next to writing a file sixty times a
    /// second. Saving is silent by design: somebody building an airlock should never be
    /// thinking about whether their work is safe.
    /// </remarks>
    public void SaveIfChanged(Automation program)
    {
        string text;
        try
        {
            text = ProgramText.Save(program);
        }
        catch (Exception error)
        {
            Trouble = error.Message;
            return;
        }

        if (text == _lastWritten || _path.Length == 0)
            return;

        try
        {
            File.WriteAllText(_path, text);
            _lastWritten = text;
            Trouble = "";
        }
        catch (Exception error)
        {
            Trouble = error.Message;
        }
    }
}
