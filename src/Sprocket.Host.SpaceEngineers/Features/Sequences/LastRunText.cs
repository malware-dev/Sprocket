using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System.Globalization;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>The bar's words for how the last run of a sequence ended and what it skipped.</summary>
    public sealed class LastRunText
    {
        readonly Sequence _sequence;
        int _busy;
        int _off;
        string _skips = "";
        string _calledOff;
        StopCause _stoppedBy;
        int _stoppedAt;
        int _stoppedFrom;

        /// <summary>The words about the runs of <paramref name="sequence" />.</summary>
        public LastRunText(Sequence sequence)
        {
            _sequence = sequence;
        }

        /// <summary>Has the skips joined again the next time they are asked for.</summary>
        public void Forget()
        {
            _busy = -1;
        }

        /// <summary>What stopped the last run and where, in one line.</summary>
        /// <remarks>Called every tick while it is the status; makes a new line only when the stop changes.</remarks>
        public string CalledOff(Texts texts, SequenceRunner state)
        {
            if (state.StoppedBy == _stoppedBy && state.StoppedAt == _stoppedAt && state.StoppedFrom == _stoppedFrom
                && _calledOff != null)
            {
                return _calledOff;
            }

            _stoppedBy = state.StoppedBy;
            _stoppedAt = state.StoppedAt;
            _stoppedFrom = state.StoppedFrom;
            _calledOff = SequenceText.CalledOff(texts, _sequence, state);
            return _calledOff;
        }

        /// <summary>What was asked for and did not happen, and why not, in one line when both happened.</summary>
        /// <remarks>Called every tick; makes a new line only when a count changes.</remarks>
        public string Skips(Texts texts, int busy, int off)
        {
            if (busy == _busy && off == _off)
                return _skips;

            _busy = busy;
            _off = off;

            var missed = busy.ToString(CultureInfo.InvariantCulture);
            var refused = off.ToString(CultureInfo.InvariantCulture);
            if (busy == 0 && off == 0)
                _skips = "";
            else if (busy == 0)
                _skips = off == 1 ? texts.SequenceSkippedOffOne : Template.Fill(texts.SequenceSkippedOff, "off", refused);
            else if (off == 0)
                _skips = busy == 1 ? texts.SequenceSkippedBusyOne : Template.Fill(texts.SequenceSkippedBusy, "busy", missed);
            else
                _skips = busy == 1
                    ? Template.Fill(texts.SequenceSkippedBothOne, "off", refused)
                    : Template.Fill(Template.Fill(texts.SequenceSkippedBoth, "busy", missed), "off", refused);

            return _skips;
        }
    }
}
