using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>The AI path recorder: playing a path back and whether it plays, recording one, and what it shows.</summary>
    /// <remarks>
    /// The game's own on and off for playing is the order a sequence gives, and whether it plays is the game's answer, which
    /// goes off by itself at the end of a path played once; so the order is offered as its two actions and its toggle, its
    /// value is left out, and whether it plays is Sprocket's own.
    /// </remarks>
    public sealed class PathRecorder : Capability
    {
        static readonly OwnValue Playing = OwnValue.Flag(Reading.Of<IMyPathRecorderBlock>(block =>
        {
            IMyPathRecorderComponent recorder;
            return block.GetComponent(out recorder) && recorder.IsPlaying;
        }));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyPathRecorderBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("playing", "playing", Playing).Status().Announced()
                .Renaming("ID_PLAY_CHECKBOX_On", false, "play the path")
                .Renaming("ID_PLAY_CHECKBOX_Off", false, "stop playing")
                .Renaming("ID_PLAY_CHECKBOX", false, "toggle playing")
                .Renaming("ReversePath", false, "reverse order")
                .Renaming("ID_REPEAT_CHEKBOX", true, "repeating").Stating("ID_REPEAT_CHEKBOX")
                .Renaming("ID_REPEAT_CHEKBOX", false, "toggle repeating")
                .Renaming("ID_RECORD_CHECKBOX", true, "recording").Stating("ID_RECORD_CHECKBOX")
                .Renaming("ID_RECORD_CHECKBOX", false, "toggle recording")
                .Renaming("ID_ADD_WAYPOINT", false, "add waypoint")
                .Renaming("ID_RECORD_STEPS_SLIDER", true, "record interval")
                .Measuring("ID_RECORD_STEPS_SLIDER", " s")
                .Renaming("ID_MINIMAL_DISTANCE_SLIDER", true, "minimal distance")
                .Measuring("ID_MINIMAL_DISTANCE_SLIDER", " m")
                .Renaming("ID_SHOW_PATH_CHECKBOX", true, "show path on HUD")
                .Renaming("ID_SHOW_PATH_CHECKBOX", false, "toggle show path on HUD")
                .Renaming("ID_SHOW_POINTS_CHECKBOX", true, "show selected points")
                .Renaming("ID_SHOW_POINTS_CHECKBOX", false, "toggle show selected points");
        }

        /// <summary>Told as the recorder starts or stops playing, which its component says.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var recorder = block as IMyPathRecorderBlock;
            IMyPathRecorderComponent component;
            if (recorder == null || !recorder.GetComponent(out component))
                return null;

            return new Listening(component, told);
        }

        /// <summary>A recorder being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyPathRecorderComponent _recorder;
            readonly Action _told;

            public Listening(IMyPathRecorderComponent recorder, Action told)
            {
                _recorder = recorder;
                _told = told;
                _recorder.IsPlayingChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _recorder.IsPlayingChanged -= Changed;
            }

            void Changed(IMyPathRecorderComponent recorder)
            {
                _told();
            }
        }
    }
}
