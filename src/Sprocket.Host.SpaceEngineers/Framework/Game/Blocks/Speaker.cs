using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A sound block, which plays the sound a step chooses from the game's own list.</summary>
    /// <remarks>A step playing a sound moves on at once: nothing tells a mod when a sound has finished.</remarks>
    public sealed class Speaker : Capability
    {
        /// <summary>Playing a chosen sound, which is Sprocket's own.</summary>
        public const string Play = "play";

        /// <summary>The sound the block holds ready, which is Sprocket's own.</summary>
        public const string Selected = "selected_sound";

        const string Unheard = "A sound block raises no event when its sound is chosen.";

        static readonly OwnAction Plays = OwnAction.Of<IMySoundBlock, Chosen>(Chosen.Read, Played);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySoundBlock && !Jukebox.Is(block);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var words = new List<string>();
            var means = new List<string>();
            Sounds(block, words, means);

            var sound = Field.Among("sound", words, means);
            sound.Searched = true;
            sound.Entry = FieldInput.Text;
            sound.Needed = true;

            kind.WithAction(Play, "play", Plays, sound).In(Category.Sound)
                .WithValue(Selected, "selected sound", Holding(means.ToArray())).Looked(Unheard).In(Category.Sound)
                .Hiding("PlaySound")
                .Renaming("StopSound", false, "stop")
                .Renaming("VolumeSlider", true, "volume")
                .Measuring("VolumeSlider", " %", 100)
                .Renaming("RangeSlider", true, "range")
                .Measuring("RangeSlider", " m")
                .Renaming("LoopableSlider", true, "loop time")
                .Measuring("LoopableSlider", " s");
        }

        /// <summary>Every sound the block can play, by the names the terminal shows into <paramref name="words" /> and by id into <paramref name="means" />.</summary>
        static void Sounds(IMyTerminalBlock block, List<string> words, List<string> means)
        {
            var definitions = MyDefinitionManager.Static;
            if (definitions != null)
            {
                foreach (var category in definitions.GetSoundCategoryDefinitions())
                {
                    if (!category.Public)
                        continue;

                    for (var i = 0; i < category.Sounds.Count; i++)
                    {
                        words.Add(category.Sounds[i].SoundText);
                        means.Add(category.Sounds[i].SoundId);
                    }
                }
            }

            if (means.Count > 0)
                return;

            var sounds = block as IMySoundBlock;
            if (sounds != null)
                sounds.GetSounds(means);

            words.AddRange(means);
        }

        /// <summary>The sound the block holds ready, as one of <paramref name="sounds" /> by id, which a step can choose.</summary>
        /// <remarks>A look is a pass over the list, made only where an <c>if</c> reads the value or a <c>when</c> watches it.</remarks>
        static OwnValue Holding(string[] sounds)
        {
            return OwnValue.OneOf<IMySoundBlock>(sounds, block => Array.IndexOf(sounds, block.SelectedSound))
                .Writing<IMySoundBlock>((block, value) => block.SelectedSound = sounds[(int)value.Whole]);
        }

        /// <summary>Plays the sound a step chose, or the one left selected where it chose none.</summary>
        static void Played(IMySoundBlock sounds, Chosen chosen, ReaderMemory seen)
        {
            if (chosen.Sound.Length > 0)
                sounds.SelectedSound = chosen.Sound;

            sounds.Play();
        }

        /// <summary>The sound a step chose.</summary>
        sealed class Chosen
        {
            /// <summary>The sound's id, or empty where the step chose none.</summary>
            public string Sound = "";

            /// <summary>The sound a step playing one was given.</summary>
            public static Chosen Read(IList<Literal> arguments)
            {
                return new Chosen { Sound = arguments != null && arguments.Count > 0 ? arguments[0].Text ?? "" : "" };
            }
        }
    }
}
