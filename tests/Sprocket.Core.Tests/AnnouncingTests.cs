using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Sprocket.Host.SpaceEngineers.Screen;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// Everything a screen is made of says when it changes.
/// </summary>
/// <remarks>
/// A target is told when a value changes rather than reading every value on every frame, which means
/// a value that changes quietly is a screen that stops updating - and nothing about that looks wrong.
/// It draws the right picture, it simply draws an old one, and only somebody who happens to watch that
/// exact row while that exact thing changes would ever see it.
/// <para>
/// So it is not left to care. Every value on everything that inherits <see cref="Observable" /> is set
/// to something different here and has to say so. A property that forgets <c>Set</c> fails this the
/// moment it is written, which is the only time it is cheap to fix.
/// </para>
/// </remarks>
public class AnnouncingTests
{
    [Fact]
    public void EveryValueOnEveryPartOfAScreenSaysWhenItChanges()
    {
        var quiet = new List<string>();
        var looked = 0;

        foreach (var type in typeof(Observable).Assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(Observable).IsAssignableFrom(type))
                continue;

            var subject = (Observable)Make(type);

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var setter = property.GetSetMethod();
                if (setter == null || !setter.IsPublic)
                    continue;

                if (Backwards(type, property))
                    continue;

                var next = Different(property.GetValue(subject, null), property.PropertyType);
                if (next == null && property.PropertyType.IsValueType)
                    continue;

                var heard = false;
                PropertyChangedEventHandler listen = (sender, e) => heard = true;

                subject.PropertyChanged += listen;
                property.SetValue(subject, next, null);
                subject.PropertyChanged -= listen;

                looked++;

                if (!heard)
                    quiet.Add(type.Name + "." + property.Name);
            }
        }

        Assert.True(quiet.Count == 0,
            $"these changed and said nothing, so a screen showing them would keep the old value: "
            + string.Join(", ", quiet));

        // A guard against the guard: a reflection sweep that matched nothing would pass in silence.
        Assert.True(looked > 30, $"only {looked} values were checked, which is too few to be right");
    }

    /// <summary>
    /// Whether this value is one a target writes and core reads, rather than the other way about.
    /// </summary>
    /// <remarks>
    /// The rule is about what a screen shows: core writes it, and something drawing it has to be told.
    /// A banner saying it has finished sliding out goes the other way - the target is the only thing
    /// that knows, and core reads it when it next builds the screen - so there is nobody for it to
    /// announce to. Named one by one on purpose: the exception is rare enough that the next one should
    /// have to be argued for here.
    /// </remarks>
    static bool Backwards(Type type, PropertyInfo property) =>
        type.Name == "NoticeComponent" && property.Name == "Closed";

    /// <summary>One of each, built with whatever its constructor insists on.</summary>
    static object Make(Type type)
    {
        var plain = type.GetConstructor(Type.EmptyTypes);
        if (plain != null)
            return plain.Invoke(null);

        var constructor = type.GetConstructors()[0];
        var parameters = constructor.GetParameters();
        var arguments = new object[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
            arguments[i] = Some(parameters[i].ParameterType);

        return constructor.Invoke(arguments);
    }

    /// <summary>Something of this type, for a constructor that has to be given one.</summary>
    static object Some(Type type)
    {
        if (type == typeof(string))
            return "something";

        if (type.IsEnum)
            return Enum.GetValues(type).GetValue(0);

        if (type.IsValueType)
            return Activator.CreateInstance(type);

        return Make(type);
    }

    /// <summary>
    /// A value of this type that is not the one it already holds.
    /// </summary>
    /// <remarks>
    /// The point of the whole exercise is that nothing is said when a value has not moved, so a test
    /// that wrote the same value back would be asserting the opposite of the rule.
    /// </remarks>
    static object Different(object held, Type type)
    {
        if (type == typeof(string))
            return (string)held == "moved" ? "moved again" : "moved";

        if (type == typeof(bool))
            return !(bool)held;

        if (type == typeof(int))
            return (int)held + 1;

        if (type.IsEnum)
        {
            foreach (var value in Enum.GetValues(type))
            {
                if (!value.Equals(held))
                    return value;
            }

            return held;
        }

        // A list is not a value and is never assigned; anything else gets a fresh one of its own kind.
        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            return null;

        return type.IsValueType ? Activator.CreateInstance(type) : Make(type);
    }
}
