using System.Text;
using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A copy of a sequence says the same thing, starts nothing, and is its own from then on.
/// </summary>
/// <remarks>
/// Duplication is scaffolding: somebody wants this sequence again with something different about
/// it, and typing six lines a second time is what they are avoiding. So the steps come across
/// exactly, and everything that would make the copy act on its own does not.
///
/// The one that would be found late rather than here is a step kind nobody taught it to copy.
/// A copy missing a line reads as complete and does less than it says, which is the class of
/// fault this whole editor exists to avoid, so the kinds are counted rather than trusted.
/// </remarks>
public class DuplicateTests
{
    static Automation Program(out Sequence sequence)
    {
        var trigger = new WhenTrigger("pressed");
        trigger.Sources.Add("button");

        var program = new Automation();
        sequence = program.AddSequence(program.AddSystem(), trigger);
        sequence.Name = "Let me out";
        sequence.Annotation = "the front door";

        sequence.Steps.Add(Acting("close", "inner"));
        sequence.Steps.Add(new Await());
        sequence.Steps.Add(new Wait(120));
        sequence.Steps.Add(Acting("open", "outer", "hatch"));
        sequence.Steps.Add(new Run(7));

        return program;
    }

    static Act Acting(string actionId, params string[] targets)
    {
        var step = new Act(actionId);
        foreach (var target in targets)
            step.Targets.Add(target);
        return step;
    }

    /// <summary>What the steps say, so a difference reads rather than being a bare false.</summary>
    static string Steps(Sequence sequence)
    {
        var text = new StringBuilder();
        foreach (var step in sequence.Steps)
        {
            if (step is Act act)
            {
                text.Append(" | ").Append(act.ActionId).Append(':')
                    .Append(string.Join("+", act.Targets));
                continue;
            }

            if (step is Wait wait)
            {
                text.Append(" | wait ").Append(wait.Ticks);
                continue;
            }

            if (step is Run run)
            {
                text.Append(" | run ").Append(run.Number);
                continue;
            }

            text.Append(" | await");
        }

        return text.ToString();
    }

    [Fact]
    public void ACopySaysExactlyWhatItWasCopiedFrom()
    {
        var program = Program(out var sequence);

        var copy = program.Duplicate(sequence);

        Assert.Equal(Steps(sequence), Steps(copy));
        Assert.Equal(sequence.Annotation, copy.Annotation);
    }

    [Fact]
    public void ACopyIsStartedByNothing()
    {
        var program = Program(out var sequence);

        var copy = program.Duplicate(sequence);

        // A toolbar slot holds one claim on Sprocket's action, so the copy cannot have the trigger it
        // was copied from. It has to be told what starts it.
        Assert.NotNull(sequence.Trigger);
        Assert.Null(copy.Trigger);
    }

    [Fact]
    public void ACopyIsItsOwnSequenceInTheSameSystem()
    {
        var program = Program(out var sequence);

        var copy = program.Duplicate(sequence);

        Assert.Same(sequence.System, copy.System);
        Assert.NotEqual(sequence.Number, copy.Number);
        Assert.NotEqual(sequence.Id, copy.Id);
        Assert.Equal(2, sequence.System.Sequences.Count);
    }

    /// <summary>
    /// The copy has its own steps, not the same objects.
    /// </summary>
    /// <remarks>
    /// The whole point of a copy is that it can be changed, and sharing one step object between two
    /// sequences would mean editing either one edited both - silently, and in the one place somebody
    /// would never think to look.
    /// </remarks>
    [Fact]
    public void ChangingACopyLeavesWhatItCameFromAlone()
    {
        var program = Program(out var sequence);
        var was = Steps(sequence);

        var copy = program.Duplicate(sequence);

        ((Act)copy.Steps[0]).Targets[0] = "outer";
        ((Act)copy.Steps[0]).ActionId = "open";
        ((Wait)copy.Steps[2]).Ticks = 1;
        ((Run)copy.Steps[4]).Number = 9;
        copy.Steps.RemoveAt(1);

        Assert.Equal(was, Steps(sequence));
        for (var i = 0; i < sequence.Steps.Count; i++)
            Assert.DoesNotContain(sequence.Steps[i], copy.Steps);
    }

    /// <summary>
    /// Every kind of step there is comes across.
    /// </summary>
    /// <remarks>
    /// <see cref="Automation.Duplicate" /> copies each kind by hand, because there is no shape a
    /// step shares that would let it be done once. A kind added later and forgotten there would be
    /// dropped from every copy, and nothing else would notice: the sequence would read as whole.
    /// <para>
    /// So the kinds are found rather than listed, one of each is made, and all of them have to
    /// survive being copied. A fifth kind fails this on the day it is written.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryKindOfStepIsCopied()
    {
        var kinds = typeof(Step).Assembly.GetTypes()
            .Where(type => typeof(Step).IsAssignableFrom(type) && !type.IsAbstract)
            .OrderBy(type => type.Name)
            .ToList();

        // A sweep that finds nothing passes in silence, so it says what it expects to have found.
        Assert.True(kinds.Count >= 4, "found only " + kinds.Count + " kinds of step");

        var program = new Automation();
        var sequence = program.AddSequence(program.AddSystem());
        foreach (var kind in kinds)
            sequence.Steps.Add(One(kind));

        var copy = program.Duplicate(sequence);

        var copied = copy.Steps.Select(step => step.GetType()).ToList();
        foreach (var kind in kinds)
            Assert.Contains(kind, copied);

        Assert.Equal(kinds.Count, copy.Steps.Count);
    }

    /// <summary>
    /// One step of a kind, whatever its constructor asks for.
    /// </summary>
    /// <remarks>
    /// Made by reflection rather than by name so that a kind nobody here has heard of is still
    /// made, which is the whole point of the sweep above.
    /// </remarks>
    static Step One(Type kind)
    {
        var made = kind.GetConstructors()
            .OrderBy(found => found.GetParameters().Length)
            .First();

        var arguments = new List<object>();
        foreach (var wanted in made.GetParameters())
        {
            if (wanted.ParameterType == typeof(string))
            {
                arguments.Add("something");
                continue;
            }

            // A value, or anything else that can be made with nothing, is made; the rest is null.
            arguments.Add(wanted.ParameterType.IsValueType || wanted.ParameterType.GetConstructor(Type.EmptyTypes) != null
                ? Activator.CreateInstance(wanted.ParameterType)!
                : null!);
        }

        return (Step)made.Invoke(arguments.ToArray());
    }
}
