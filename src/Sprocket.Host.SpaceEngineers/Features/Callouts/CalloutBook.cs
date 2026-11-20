using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Versioning;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Text;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Features.Callouts
{
    /// <summary>The callouts one editor offers a player: which are due, the one hanging now, and what the player has dismissed.</summary>
    /// <remarks>Decides only when the editor opens, when a screen changes and when a place comes into view, and never on a tick.</remarks>
    public sealed class CalloutBook
    {
        /// <summary>Every callout there is.</summary>
        /// <remarks>A static: the one list, the same for every editor.</remarks>
        public static readonly IList<Callout> All = new[]
        {
            new Callout("share", new ModRelease(1, 1), texts => texts.CalloutShare, CalloutPlace.HeaderShare, CalloutPlace.HomeShare, CalloutPlace.LibraryShare),
            new Callout("import", new ModRelease(1, 1), texts => texts.CalloutImport, CalloutPlace.LibraryImport),
            new Callout("settings", new ModRelease(1, 1), texts => texts.CalloutSettings, CalloutPlace.SlotAdd)
        };

        readonly IList<Callout> _all;
        readonly List<Callout> _due = new List<Callout>();

        PlayerSettings _held = new PlayerSettings(true);
        Func<PlayerSettings> _read;
        Action<PlayerSettings> _save;
        CoachMark _mark;
        Callout _hung;

        /// <summary>A book of every callout there is, with nothing stored beyond this object, as a host with no player file has them.</summary>
        public CalloutBook() : this(All)
        {
        }

        /// <summary>A book of <paramref name="all" />, for a test about which callouts fall due.</summary>
        public CalloutBook(IList<Callout> all)
        {
            _all = all;
            _read = () => _held;
            _save = choices => _held = choices;
        }

        /// <summary>The callouts due and not yet dismissed in this editor's session, in the order they are offered.</summary>
        public IList<Callout> Due => _due;

        /// <summary>Raised when a callout has been dismissed, or a place one hangs from may have come into view, so the next may hang.</summary>
        public event Action Changed;

        /// <summary>The callout hanging now, or null where none is.</summary>
        public Callout Shown => _mark != null && _mark.IsOpen ? _hung : null;

        /// <summary>The player's choices as they stand.</summary>
        public PlayerSettings Choices => _read();

        /// <summary>Has the callouts read and save the player's choices through <paramref name="read" /> and <paramref name="save" />.</summary>
        public void Saving(Func<PlayerSettings> read, Action<PlayerSettings> save)
        {
            _read = read;
            _save = save;
        }

        /// <summary>Works out which callouts are due as the editor opens.</summary>
        /// <remarks>A callout is due where its release is newer than the one recorded as seen, or, for a player with none recorded, is the current one; and it has not been dismissed. The current release is recorded as seen once none is due.</remarks>
        public void Begin()
        {
            Close();
            _due.Clear();

            var choices = _read();
            for (var i = 0; i < _all.Count; i++)
            {
                var callout = _all[i];
                var newer = choices.Seen.HasValue ? callout.Release > choices.Seen.Value : callout.Release == ModVersion.Release;
                if (newer && !choices.HasDismissed(callout.Id))
                    _due.Add(callout);
            }

            if (_due.Count == 0)
                RecordSeen();
        }

        /// <summary>Hangs the first due callout whose place has an anchor on the screen, if none is hanging, with <paramref name="anchorOf" /> finding the element at a place.</summary>
        public void Offer(Texts texts, Func<CalloutPlace, Element> anchorOf)
        {
            if (_due.Count == 0 || _mark != null && _mark.IsOpen)
                return;

            for (var i = 0; i < _due.Count; i++)
            {
                var callout = _due[i];
                for (var p = 0; p < callout.Places.Length; p++)
                {
                    var anchor = anchorOf(callout.Places[p]);
                    if (!IsShown(anchor))
                        continue;

                    Hang(callout, texts, anchor);
                    return;
                }
            }
        }

        /// <summary>Says a place a callout hangs from may have come into view, as a tab does when it is shown.</summary>
        public void Reconsider()
        {
            Changed?.Invoke();
        }

        /// <summary>Puts away the callout hanging, without dismissing it.</summary>
        public void Close()
        {
            var mark = _mark;
            _mark = null;
            _hung = null;
            mark?.Close();
        }

        /// <summary>Whether <paramref name="element" /> is in a window, with nothing holding it hidden and nothing covering it, so a callout can hang from it.</summary>
        public static bool IsShown(Element element)
        {
            if (element == null || element.Windows == null || element.IsCovered)
                return false;

            for (var at = element; at != null; at = at.Parent)
            {
                if (!at.IsVisible)
                    return false;
            }

            return true;
        }

        /// <summary>Opens <paramref name="callout" /> from <paramref name="anchor" />, which dismisses it for good once pressed.</summary>
        void Hang(Callout callout, Texts texts, Element anchor)
        {
            var mark = new CoachMark(Template.Fill(texts.CalloutNew, "said", callout.Said(texts))) { Style = Styles.Callout, Anchor = anchor, AutomationId = callout.Said(texts) };
            mark.Dismissed += () => Dismiss(callout);
            mark.Closed += reason => Lapsed(mark);
            _mark = mark;
            _hung = callout;
            mark.Open();
        }

        /// <summary>Lets the next place offer what <paramref name="mark" /> held, where it closed by itself, as it does when its anchor goes out of view.</summary>
        void Lapsed(CoachMark mark)
        {
            if (_mark != mark)
                return;

            _mark = null;
            _hung = null;
            Changed?.Invoke();
        }

        /// <summary>Records the current release as seen, once nothing of it is left to offer.</summary>
        void RecordSeen()
        {
            var choices = _read();
            if (!choices.Seen.HasValue || choices.Seen.Value < ModVersion.Release)
                _save(choices.WithSeen(ModVersion.Release));
        }

        /// <summary>Records <paramref name="callout" /> as dismissed, so no place offers it again.</summary>
        void Dismiss(Callout callout)
        {
            _due.Remove(callout);
            _save(_read().WithDismissed(callout.Id));
            if (_due.Count == 0)
                RecordSeen();

            Changed?.Invoke();
        }
    }
}
