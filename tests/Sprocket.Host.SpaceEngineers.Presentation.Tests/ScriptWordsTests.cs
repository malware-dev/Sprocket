using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Sim.Content;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The words of the language are offered as the sentences say them, in whatever language the rest is in.</summary>
public class ScriptWordsTests
{
    /// <summary>With Norwegian loaded, adding a trigger or a step offers the language's own English words.</summary>
    [Fact]
    public void TheMenusOfferTheLanguagesWordsInNorwegian()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Luftsluse");
        sim.Editor.Back();
        sim.Tick();
        sim.Texts.Load(new ResxTexts(ResxTexts.Beside, "no"));
        sim.Editor.Invalidate();
        sim.Tick();

        var triggers = Offered(sim);
        Assert.Contains(SentenceText.WhenWord, triggers);
        Assert.Contains(SentenceText.EveryWord, triggers);

        var system = sim.Editor.Program.Systems[0];
        system.Enabled = false;
        sim.Editor.Program.Edited();
        sim.Editor.Push(new SequencePage(sim.Editor, system.Sequences[0]));
        sim.Tick();

        var steps = Offered(sim);
        foreach (var word in new[] { SentenceText.WaitWord, SentenceText.RunWord, SentenceText.StopWord, SentenceText.IfWord })
            Assert.Contains(word, steps);
    }

    /// <summary>Every choice the plus menus on the screen showing offer.</summary>
    static List<string> Offered(Simulation sim) =>
        sim.Editor.Current.View!.Within().OfType<PlusMenu>().SelectMany(menu => menu.Choices).Select(choice => choice.Text).ToList();
}
