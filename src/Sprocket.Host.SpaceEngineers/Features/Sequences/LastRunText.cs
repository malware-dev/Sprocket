using System.Globalization;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>The bar's words for how the last run of a sequence ended and what it skipped, joined again only when they change.</summary>
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
        /// <remarks>Asked every tick while it is the status, so the line is joined again only when the stop it says is another.</remarks>
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
        /// <remarks>Asked every tick, so the line is joined again only when a count has changed.</remarks>
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
