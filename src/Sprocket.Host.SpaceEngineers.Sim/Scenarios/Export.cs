using System;
using System.Globalization;
using System.Text;
using Sprocket.Sim.Blueprints;

namespace Sprocket.Sim.Scenarios;

/// <summary>A scenario written out for the game: its blueprint, and its checks for the Testing artifact.</summary>
public static class Export
{
    /// <summary>The variable naming this machine's local blueprints folder.</summary>
    public const string FolderVariable = "SPROCKET_BLUEPRINTS";

    /// <summary>Writes the scenario called <paramref name="name" /> as a blueprint into <paramref name="folder" />, or the one <see cref="FolderVariable" /> names.</summary>
    public static string Blueprint(string name, string folder)
    {
        folder = folder ?? Environment.GetEnvironmentVariable(FolderVariable);
        if (string.IsNullOrEmpty(folder))
            return $"No blueprints folder: name one after the scenario, or set {FolderVariable} to the game's local blueprints folder.";

        var scenario = Scenarios.Named(name);
        var built = scenario.Build(BlockDefinitions.FromGame());
        return "wrote " + Blueprints.Blueprint.Save(folder, scenario.Name, scenario.Blueprint(built));
    }

    /// <summary>The checks of the scenario called <paramref name="name" />, as JSON rows for the Testing artifact, numbered from <paramref name="first" />.</summary>
    public static string Checks(string name, int first)
    {
        var scenario = Scenarios.Named(name);
        var json = new StringBuilder("[\n");
        var n = first;
        foreach (var check in scenario.Checks)
        {
            if (n > first)
                json.Append(",\n");

            json.Append("  {")
                .Append("\"n\": ").Append(n.ToString(CultureInfo.InvariantCulture))
                .Append(", \"title\": ").Append(Quoted(check.Title))
                .Append(", \"where\": ").Append(Quoted("Blueprint " + scenario.Name))
                .Append(", \"why\": ").Append(Quoted(check.Why))
                .Append(", \"setup\": ").Append(Quoted(Instructions.First + " " + check.Setup))
                .Append(", \"steps\": [");
            for (var i = 0; i < check.Steps.Count; i++)
                json.Append(i > 0 ? ", " : "").Append(Quoted(check.Steps[i]));

            json.Append("], \"expect\": ").Append(Quoted(check.Expect))
                .Append(", \"from\": ").Append(Quoted("scenario " + scenario.Name))
                .Append(", \"state\": \"waiting\", \"note\": \"\"}");
            n++;
        }

        return json.Append("\n]\n").ToString();
    }

    static string Quoted(string text)
    {
        var quoted = new StringBuilder("\"");
        foreach (var c in text)
        {
            switch (c)
            {
                case '"': quoted.Append("\\\""); break;
                case '\\': quoted.Append("\\\\"); break;
                case '\n': quoted.Append("\\n"); break;
                case '\r': break;
                default:
                    if (c < ' ')
                        quoted.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        quoted.Append(c);
                    break;
            }
        }

        return quoted.Append('"').ToString();
    }
}
