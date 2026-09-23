using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Screen;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What the screen listens to, and what it lets go of.
/// </summary>
/// <remarks>
/// A view is told when its values change rather than reading them on every frame, which means it
/// holds a subscription to something core owns and outlives it. That is the price of not polling: a
/// view dropped while still listening is kept alive by the very thing it was showing, goes on writing
/// to elements nobody can see, and nothing looks wrong until a session has been walked around in for
/// a while. So the count is asserted rather than trusted.
/// </remarks>
public class ListeningTests
{
    [Fact]
    public void NothingListensToMoreThanIsOnScreen()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        AssertListening(screen, "the construct");

        // Round trips, because a leak shows up as growth rather than as a wrong number once. Each of
        // these walks in and back out again, and the views for the screens left behind are dropped.
        for (var round = 0; round < 3; round++)
        {
            editor.NewSystem("Airlock " + round);
            AssertListening(screen, "a new system");

            editor.PressStart("When…");
            AssertListening(screen, "the picker");

            editor.PressBlock("Airlock Button");
            editor.PressChoice("is pressed");
            editor.PressPrimary();

            // After the first round that button is spoken for, and being asked about it is part of
            // what this counts: a question is on screen and listened to like anything else.
            if (editor.HasAnswer("Take it over"))
                editor.PressChoice("Take it over");

            AssertListening(screen, "a sequence");

            editor.PressAdd("do something");
            AssertListening(screen, "picking what happens");

            editor.Back();
            AssertListening(screen, "back at the sequence");

            editor.Back();
            AssertListening(screen, "back at the system");

            editor.Back();
            AssertListening(screen, "back at the construct");
        }
    }

    /// <summary>
    /// A screen closed and opened again still hears core.
    /// </summary>
    /// <remarks>
    /// Closed is not destroyed. A session keeps one screen per Sprocket and shows the same one again, so
    /// a view that let go on the way out and never took hold on the way back in is a screen that draws
    /// the first thing it ever saw: controls that light and do nothing, a primary button that says DONE
    /// and is not pressable, and nothing about it looking wrong.
    /// </remarks>
    [Fact]
    public void AScreenClosedAndOpenedAgainStillHearsCore()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        // Into the picker with a block chosen, and then put away and shown again from there. The
        // order matters: walking about after a reopening builds new components, and building them
        // writes the chrome by another route, which would hide exactly what this is about.
        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Airlock Button");
        screen.Run(2);

        screen.Reopen();
        screen.Run(2);

        // A change to the chrome alone: choosing the event gives the screen something to move on to,
        // and the picker's own components are the ones it already had.
        editor.PressChoice("is pressed");
        screen.Run(2);

        var button = screen.Text(editor.Screen.PrimaryText);
        var was = screen.Ended;

        // Pressed where it is drawn, rather than invoked through core: what the reopening breaks is
        // the button's own meaning, and a test that went through core would not touch it.
        screen.Do(((int)Screen.Ui(button.At.X, button.At.Y).X).ToString(),
            ((int)Screen.Ui(button.At.X, button.At.Y).Y).ToString());

        Assert.NotEqual(was, screen.Ended);

        AssertListening(screen, "a screen opened for the second time");
    }

    /// <summary>
    /// A view lets go of what it was showing, and the component knows it has been let go.
    /// </summary>
    /// <remarks>
    /// The pair on its own, because the walk above cannot prove it: core builds its components afresh
    /// on every visit today, so a subscription left behind is dropped along with the component nobody
    /// else holds. The day a page keeps its components - which is the very thing that would make this
    /// pushing worth more - a missing release becomes a screen kept alive forever, and this is the
    /// test that says so.
    /// </remarks>
    [Fact]
    public void AViewLetsGoOfWhatItWasShowing()
    {
        var component = new StartComponent();
        var view = ComponentView.For(component);

        Assert.Equal(0, component.Listeners);

        view.Watch(component);
        Assert.Equal(1, component.Listeners);

        view.Release();
        Assert.Equal(0, component.Listeners);

        // Twice over, because a view handed a second component has to let go of the first by itself.
        var another = new StartComponent();
        view.Watch(component);
        view.Watch(another);

        Assert.Equal(0, component.Listeners);
        Assert.Equal(1, another.Listeners);
    }

    static void AssertListening(Screen screen, string where)
    {
        screen.Run(2);

        var components = screen.Editor.Screen.Components;

        Assert.True(components.Count == screen.Watching,
            $"at {where}, {components.Count} components are on screen and {screen.Watching} views "
            + "are listening; a view is reading values nobody will tell it about.");

        // Counted on the component, because that is where a subscription left behind piles up. The
        // screen cannot see it: by the time a view is dropped the screen has already forgotten it.
        foreach (var component in components)
        {
            Assert.True(component.Listeners <= 1,
                $"at {where}, {component.GetType().Name} has {component.Listeners} listeners. A view "
                + "was dropped without letting go, and the component is keeping it, its elements and "
                + "everything they hold alive for the rest of the session.");
        }
    }
}
