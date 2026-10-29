using System.Reflection;
using Lumel.Controls;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A row of a reorderable list dropped somewhere, as the list says when a drag ends there.</summary>
public static class Dropped
{
    /// <summary>Tells whoever listens to <paramref name="list" /> that the row at <paramref name="from" /> was dropped at <paramref name="to" />, not set in.</summary>
    /// <remarks>Raises the list's own event, so what the page does with a drop is tested without laying the rows out and dragging a handle.</remarks>
    public static void On(ReorderList list, int from, int to)
    {
        var field = typeof(ReorderList).GetField(nameof(ReorderList.Reordered), BindingFlags.Instance | BindingFlags.NonPublic)!;
        ((Action<ReorderedEventArgs>?)field.GetValue(list))?.Invoke(new ReorderedEventArgs(from, to, false));
    }
}
