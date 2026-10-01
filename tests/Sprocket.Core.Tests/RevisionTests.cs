using System.Reflection;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Lumel.Input;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// Everything around a page says when it changes.
/// </summary>
/// <remarks>
/// The chrome writes what the screen says onto its controls only when the screen's revision has moved, which means a
/// value that changes quietly is a screen that stops updating - and nothing about that looks wrong. It draws the right
/// picture, it simply draws an old one.
/// <para>
/// So it is not left to care. Every value on the screen and on a part of its heading is set to something different here
/// and has to move the revision. A property that forgets <c>Set</c> fails this the moment it is written, which is the
/// only time it is cheap to fix.
/// </para>
/// </remarks>
public class RevisionTests
{
    [Fact]
    public void EveryValueAroundAPageCountsAChange()
    {
        var screen = new EditorScreen();
        var part = new HeadingPart("something");
        screen.Heading.Add(part);
        screen.Reheaded();

        var quiet = new List<string>();
        var looked = 0;

        foreach (var subject in new object[] { screen, part })
        {
            foreach (var property in subject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var setter = property.GetSetMethod();
                if (setter == null || !setter.IsPublic)
                    continue;

                var next = Different(property.GetValue(subject, null), property.PropertyType);
                if (next == null)
                    continue;

                var before = screen.Revision;
                property.SetValue(subject, next, null);
                looked++;

                if (screen.Revision == before)
                    quiet.Add(subject.GetType().Name + "." + property.Name);
            }
        }

        Assert.True(quiet.Count == 0,
            "these changed and said nothing, so the chrome would keep the old value: " + string.Join(", ", quiet));

        // A guard against the guard: a reflection sweep that matched nothing would pass in silence.
        Assert.True(looked > 20, $"only {looked} values were checked, which is too few to be right");
    }

    /// <summary>A value of this type that is not the one it already holds, or null for a type nothing here sets.</summary>
    /// <remarks>
    /// The point of the whole exercise is that nothing counts when a value has not moved, so a test that wrote the same
    /// value back would be asserting the opposite of the rule.
    /// </remarks>
    static object? Different(object? held, Type type)
    {
        if (type == typeof(string))
            return (string?)held == "moved" ? "moved again" : "moved";

        if (type == typeof(bool))
            return !(bool)held!;

        if (type == typeof(ICommand))
            return new Relay(delegate { });

        if (type == typeof(Action<string>))
            return new Action<string>(_ => { });

        if (type == typeof(Func<string, bool>))
            return new Func<string, bool>(_ => true);

        return null;
    }
}
