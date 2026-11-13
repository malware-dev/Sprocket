using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A question answered by choosing one of a few answers, or by typing one.</summary>
public class ChoicePageTests
{
    /// <summary>The answers stand in the order they were offered, each with what giving it does that somebody should know first.</summary>
    [Fact]
    public void AnswersStandInOrderWithTheirCautions()
    {
        var sim = Asking(page =>
        {
            page.Add("Keep it", false, () => { });
            page.Add("Delete it", false, () => { });
            page.WarnOnLast("This cannot be undone.");
        });

        var answers = sim.Editor.Choices();
        Assert.Equal(new[] { "Keep it", "Delete it" }, answers.Select(answer => answer.Text));
        Assert.Equal("", answers[0].Caution);
        Assert.Equal("This cannot be undone.", answers[1].Caution);
    }

    /// <summary>Pressing an answer gives it, and the answer already given is marked.</summary>
    [Fact]
    public void PressingAnAnswerGivesIt()
    {
        var given = "";
        var sim = Asking(page =>
        {
            page.Add("Port", true, () => given = "Port");
            page.Add("Starboard", false, () => given = "Starboard");
        });

        var answers = sim.Editor.Choices();
        Assert.True(answers[0].IsGiven);
        Assert.False(answers[1].IsGiven);

        sim.Editor.PressChoice("Starboard");
        Assert.Equal("Starboard", given);
    }

    static Simulation Asking(Action<ChoicePage> fill)
    {
        var sim = Simulation.Fresh();
        var page = new ChoicePage(sim.Editor, "Which one?");
        fill(page);
        sim.Editor.Push(page);
        sim.Tick();
        return sim;
    }
}
