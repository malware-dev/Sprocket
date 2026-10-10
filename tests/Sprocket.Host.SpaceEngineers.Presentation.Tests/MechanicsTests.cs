using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>What a piston and a rotor offer on the sequence screen: the tests an if reads, and the tasks a step does.</summary>
public class MechanicsTests
{
    /// <summary>An if on a piston is offered whether it is out or in, and an if on a rotor whether its head is on.</summary>
    [Theory]
    [InlineData("Piston", "is extended", "is retracted")]
    [InlineData("Rotor", "is attached", "is detached")]
    public void AnIfIsOfferedTheStatesOfTheBlock(string block, string one, string other)
    {
        var entries = Picked(block, "if");

        Assert.Contains(one, entries);
        Assert.Contains(other, entries);
    }

    /// <summary>A task on a piston or a rotor is offered adding a head, and a rotor its lock's toggle, in Sprocket's words.</summary>
    [Theory]
    [InlineData("Piston", "Add head")]
    [InlineData("Rotor", "Add head")]
    [InlineData("Rotor", "Toggle lock")]
    public void ATaskIsOfferedInSprocketsWords(string block, string task)
    {
        Assert.Contains(task, Picked(block, "task"));
    }

    /// <summary>What the picker offers for <paramref name="block" /> under the step <paramref name="step" />, by its words.</summary>
    static List<string> Picked(string block, string step)
    {
        var rig = Rig.Demo();
        rig.Piston(30, "Piston");
        rig.Rotor(31, "Rotor");
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd(step);
        screen.Run(2);
        screen.Editor.PressBlock(block);
        screen.Run(2);

        return screen.Editor.Pick().Stages.Last().Entries().Select(entry => entry.Text).ToList();
    }
}
