using System;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Input;

namespace Sprocket.Host.SpaceEngineers.Lists
{
    /// <summary>A status row showing a <see cref="StatusEntry" />, sending each press to the host by the entry's command numbers.</summary>
    /// <remarks>For the screens still described in components. A ported screen sets its rows itself and gives them commands of its own.</remarks>
    public class EntryRow
    {
        readonly ComponentView _view;
        readonly StatusRow _row = new StatusRow();
        readonly Relay _opens;
        readonly Relay _runs;
        readonly Relay _stops;
        readonly Relay _flips;
        readonly Relay _copies;
        readonly Relay _offers;
        readonly Relay _deletes;
        StatusEntry _entry;

        /// <summary>An empty row belonging to <paramref name="view" />, filled in by <see cref="Update" />.</summary>
        public EntryRow(ComponentView view)
        {
            _view = view;
            _opens = new Relay(delegate { Send(_row, e => e.Command, e => e.Question); });
            _runs = new Relay(delegate { Send(_row.RunPart, e => e.RunCommand, null); });
            _stops = new Relay(delegate { Send(_row.RunPart, e => e.StopCommand, null); });
            _flips = new Relay(Flip);
            _copies = new Relay(delegate { Send(_row.CopyPart, e => e.DuplicateCommand, null); });
            _offers = new Relay(delegate { Send(_row.ButtonPart, e => e.ButtonCommand, e => e.ButtonQuestion); });
            _deletes = new Relay(delegate { Send(_row.DeletePart, e => e.DeleteCommand, null); });
        }

        /// <summary>The row itself, for the list to hold.</summary>
        public StatusRow Row => _row;

        /// <summary>Shows <paramref name="entry" /> on the row.</summary>
        public void Update(StatusEntry entry)
        {
            _entry = entry;

            _row.Opens = entry.Command != 0 ? _opens : null;
            _row.Runs = entry.RunCommand != 0 ? _runs : null;
            _row.Stops = entry.StopCommand != 0 ? _stops : null;
            _row.Flips = entry.CanSwitch ? _flips : null;
            _row.Copies = entry.DuplicateCommand != 0 ? _copies : null;
            _row.Offers = entry.ButtonCommand != 0 ? _offers : null;
            _row.Deletes = entry.DeleteCommand != 0 ? _deletes : null;

            _row.Depth = entry.Depth;
            _row.Summary = entry.Summary;
            _row.IsAction = entry.IsAction;
            _row.Asks = entry.Question.Length > 0;
            _row.About = entry.About;
            _row.Problems = entry.Problems;
            _row.Remarks = entry.Remarks;
            _row.Running = entry.Running;
            _row.Parked = entry.Parked;
            _row.Chosen = entry.Chosen;
            _row.JustRan = entry.JustRan;
            _row.Status = entry.Status;
            _row.LastRun = entry.LastRun;
            _row.Problem = entry.Problem;
            _row.ProblemIsWarning = entry.ProblemIsWarning;
            _row.SwitchIsOn = entry.SwitchIsOn;
            _row.SwitchGovernsAll = entry.SwitchGovernsAll;
            _row.DuplicateText = entry.DuplicateText;
            _row.ButtonText = entry.ButtonText;
            _row.ButtonAbout = entry.ButtonAbout;

            _row.Show(_view.Host.Texts);
        }

        /// <summary>Sends the entry's command, read by <paramref name="command" />, from <paramref name="at" />, asking what <paramref name="question" /> reads first; nothing before the row has an entry and a host.</summary>
        void Send(Element at, Func<StatusEntry, int> command, Func<StatusEntry, string> question)
        {
            var entry = _entry;
            var host = _view.Host;
            if (entry == null || host == null)
                return;

            host.Send(at, command(entry), question == null ? null : question(entry));
        }

        void Flip()
        {
            var entry = _entry;
            var host = _view.Host;
            if (entry != null && host != null)
                host.Switch(_row.SwitchPart, entry.SwitchCommand, !entry.SwitchIsOn);
        }
    }
}
