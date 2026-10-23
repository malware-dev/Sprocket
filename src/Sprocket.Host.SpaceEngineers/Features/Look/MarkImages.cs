using System;
using System.Collections.Generic;
using Lumel.Images;

namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>
    /// The pictures Sprocket's screens find by locator: a kind's or a button's mark for <c>mark:&lt;name&gt;</c>, and what the
    /// catalog it is given finds for anything else.
    /// </summary>
    public sealed class MarkImages : IImageCatalog
    {
        const string Prefix = "mark:";

        readonly IImageCatalog _rest;
        readonly Dictionary<string, IImageSource> _marks = new Dictionary<string, IImageSource>(StringComparer.Ordinal);

        /// <summary>The marks, with <paramref name="rest" /> finding every locator that names none.</summary>
        public MarkImages(IImageCatalog rest)
        {
            _rest = rest;
            Add("sequence", Symbols.Sequence);
            Add("signal", Symbols.Signal);
            Add("trigger", Symbols.Trigger);
            Add("action", Symbols.Action);
            Add("awaited", Symbols.Lasting);
            Add("value", Symbols.Value);
            Add("block", Symbols.Block);
            Add("screen", Symbols.Screen);
            Add("blueprint", Symbols.Blueprint);
            Add("logic-system", Symbols.LogicSystem);
            Add("installation", Symbols.Installation);
            Add("play", Symbols.Play);
            Add("stop", Symbols.Stop);
            Add("pause", Symbols.Pause);
            Add("delete", Symbols.Delete);
            Add("duplicate", Symbols.Duplicate);
            Add("rename", Symbols.Pencil);
            Add("undo", Symbols.Undo);
            Add("redo", Symbols.Redo);
        }

        /// <inheritdoc />
        public IImageSource Find(string locator)
        {
            if (string.IsNullOrEmpty(locator))
                return null;

            if (!locator.StartsWith(Prefix, StringComparison.Ordinal))
                return _rest?.Find(locator);

            IImageSource mark;
            return _marks.TryGetValue(locator, out mark) ? mark : null;
        }

        void Add(string name, string symbol)
        {
            _marks.Add(Prefix + name, new MarkImage(symbol));
        }
    }
}
