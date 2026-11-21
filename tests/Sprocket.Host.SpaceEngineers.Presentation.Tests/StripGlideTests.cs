using Lumel.Input;
using Lumel.Viewing;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The selector's strip glides sideways to the column being answered.</summary>
public class StripGlideTests
{
    /// <summary>A column more than fits moves the view to the end over several frames, never past it, rather than at once; laid out again with no new column, it stays where it is.</summary>
    [Fact]
    public void TheStripGlidesToItsLastColumnOverSeveralFrames()
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewSystem("Airlock");
        rig.Editor.PressStart(SentenceText.WhenWord);
        rig.Editor.PressBlock("Airlock Light");
        rig.Tick();
        rig.Editor.TickBlock("Cockpit Light");
        rig.Tick();
        var reads = rig.Editor.Pick().Stages[0].Options.OfType<ListField>().Single();
        while (reads.Text != "any")
        {
            ((IInputTarget)reads).Text('t');
            rig.Tick();
        }

        var strip = rig.Editor.Pick();
        var view = Assert.IsType<ScrollView>(strip.View);
        var easel = new Easel(view, 1200f, 800f);
        strip.Stages[1].Entries().First(entry => entry.Text == "radius" && !entry.IsHeading).Pick(HeldModifier.Alone, 1);

        // A frame is laid out and drawn between ticks, as the game draws one.
        var offsets = new List<float>();
        for (var i = 0; i < 40; i++)
        {
            rig.Tick();
            easel.Draw(1200f, 800f);
            offsets.Add(view.HorizontalOffset);
        }

        var end = offsets[offsets.Count - 1];

        // Scrolled back by hand, and laid out again with no new column, the view stays where it was put.
        view.ScrollToHorizontalOffset(0f);
        strip.Show();
        for (var i = 0; i < 40; i++)
        {
            rig.Tick();
            easel.Draw(1200f, 800f);
        }

        Assert.Equal(0f, view.HorizontalOffset);
        Assert.True(end > 0f && end < 2000f, $"the view ends at {end}, not at the strip's last column");
        Assert.All(offsets, offset => Assert.True(offset <= end, $"the view went to {offset}, past its end at {end}"));
        Assert.True(offsets.Count(offset => offset > 0f && offset < end) >= 5,
            $"the view reached its end in {offsets.Count(offset => offset > 0f && offset < end)} frames between: {string.Join(", ", offsets)}");
    }
}
