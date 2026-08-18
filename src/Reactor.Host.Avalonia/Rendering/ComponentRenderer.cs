using Avalonia.Controls;
using Reactor.Editing;
using Reactor.Ui;

namespace Reactor.Host.Rendering;

/// <summary>
/// Draws one kind of editor component and keeps it up to date.
/// </summary>
/// <remarks>
/// This is the entire job of a target: one renderer per component, plus the screen
/// chrome. Nothing here decides what the editor means, what a sequence says, what can go
/// in a slot, or where pressing something leads - all of that arrives already
/// decided, and goes back as a command id.
/// </remarks>
abstract class ComponentRenderer
{
    protected ComponentRenderer(ReactorEditor editor)
    {
        Editor = editor;
    }

    protected ReactorEditor Editor { get; }

    /// <summary>The control handed to the layout, created once.</summary>
    public abstract Control Control { get; }

    /// <summary>
    /// Points this renderer at a component of the same kind after a structural
    /// rebuild, so controls the user is interacting with survive.
    /// </summary>
    /// <summary>
    /// Whether this renderer manages its own scrolling and should not be wrapped.
    /// </summary>
    /// <remarks>
    /// True for anything that scrolls sideways or scrolls several regions
    /// independently; wrapping one of those in a vertical scroller gives two
    /// scrollbars fighting over the same gesture.
    /// </remarks>
    public virtual bool ScrollsItself => false;

    /// <summary>Re-reads live values. Called every tick.</summary>
    public abstract void Update();

    public static ComponentRenderer Create(EditorComponent component, ReactorEditor editor)
    {
        if (component is HeadingComponent heading)
            return new HeadingRenderer(editor, heading);
        if (component is NoticeComponent notice)
            return new NoticeRenderer(editor, notice);
        if (component is BlockSearchComponent search)
            return new BlockSearchRenderer(editor, search);
        if (component is BlockListComponent blocks)
            return new BlockListRenderer(editor, blocks);
        if (component is ChoiceListComponent choices)
            return new ChoiceListRenderer(editor, choices);
        if (component is SequenceListComponent sequences)
            return new SequenceListRenderer(editor, sequences);
        if (component is PathComponent path)
            return new PathRenderer(editor, path);
        if (component is SequenceComponent sentence)
            return new SequenceRenderer(editor, sentence);

        return new HeadingRenderer(editor, new HeadingComponent($"(no renderer for {component.GetType().Name})"));
    }
}
