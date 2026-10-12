using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Sim.Machine;
using Lumel.Text;
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
            page.Add("Keep it", false, delegate { });
            page.Add("Delete it", false, delegate { });
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
            page.Add("Port", true, delegate { given = "Port"; });
            page.Add("Starboard", false, delegate { given = "Starboard"; });
        });

        var answers = sim.Editor.Choices();
        Assert.True(answers[0].IsGiven);
        Assert.False(answers[1].IsGiven);

        sim.Editor.PressChoice("Starboard");
        Assert.Equal("Starboard", given);
    }

    /// <summary>A typed number is taken on Enter, only while there is one, and never past the most the box takes.</summary>
    [Fact]
    public void ATypedNumberIsTakenOnlyWhenThereIsOne()
    {
        var taken = new List<int>();
        var sim = Asking(page => page.AddNumber("Another length", "Use", "0000", 500, taken.Add));

        var box = sim.Editor.TypedBox()!;
        Assert.False(box.Submit!.CanExecute, "an empty box offers to take nothing");

        box.Insert("4x");
        Assert.Equal("", box.Text);

        box.Insert("501");
        Assert.Equal("", box.Text);

        box.Insert("45");
        Assert.True(box.Submit.CanExecute);
        box.Submit.Execute();

        Assert.Equal(new[] { 45 }, taken);
    }

    /// <summary>A question with nothing to choose from says so.</summary>
    [Fact]
    public void NothingToChooseFromIsSaid()
    {
        var sim = Asking(page => page.EmptyText = "No blueprints yet.");

        Assert.Empty(sim.Editor.Choices());
        Assert.Contains(sim.Editor.Current.View!.Within().OfType<Label>(),
            label => label.IsVisible && label.Text == "No blueprints yet.");
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
