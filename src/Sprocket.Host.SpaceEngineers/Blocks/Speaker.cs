using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A sound block, which plays the sound a step chooses from the game's own list.
    /// </summary>
    /// <remarks>
    /// Playing is not waited for. A dedicated server plays no sound at all and nothing tells a mod when
    /// one has finished, so there is nothing to wait on, and nothing to say whether a sound is playing.
    /// <para>
    /// A jukebox is a sound block to the game too, and is not this: it plays a playlist, and a single sound outside it is
    /// what its own terminal hides. See <see cref="Jukebox" />.
    /// </para>
    /// </remarks>
    public sealed class Speaker : Capability
    {
        /// <summary>Playing a chosen sound, which is Sprocket's own.</summary>
        public const string Play = "play";

        /// <summary>The sound the block holds ready, which is Sprocket's own.</summary>
        public const string Selected = "selected-sound";

        const string Unheard = "A sound block raises no event when its sound is chosen.";

        static readonly Doing Plays = Doing.Of<IMySoundBlock, Chosen>(Chosen.Read, Played);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySoundBlock && !Jukebox.Is(block);
        }

        /// <inheritdoc />
        /// <remarks>
        /// The game's own play is kept out of the list: it plays whichever sound was left selected,
        /// which is the one thing a step choosing its sound exists to avoid.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var words = new List<string>();
            var means = new List<string>();
            Sounds(block, words, means);

            var sound = Field.Among("sound", words, means);
            sound.Entry = Entry.Text;
            sound.Needed = true;

            kind.WithAction(Play, "play", Plays, sound).In(Category.Sound)
                .WithValue(Selected, "selected sound", Holding(means.ToArray())).Looked(Unheard)
                .Hiding("PlaySound")
                .Renaming("StopSound", false, "stop")
                .Renaming("VolumeSlider", true, "volume")
                .Renaming("RangeSlider", true, "range")
                .Renaming("LoopableSlider", true, "loop time");
        }

        /// <summary>
        /// Every sound the block can play, by the names the terminal shows into <paramref name="words" /> and by id into
        /// <paramref name="means" />.
        /// </summary>
        /// <remarks>
        /// Every sound block has the same list, which is every public sound category the game has
        /// loaded. Where none are loaded - the test harness - the block's own list of ids, each
        /// named by itself.
        /// </remarks>
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
        /// <remarks>
        /// A look finds the selected sound among the ids, which is a pass over the list; it is made only where an <c>if</c>
        /// reads the value or a <c>when</c> watches it, on its cadence. A sound the list does not hold, or none, is no
        /// position among them, and is none of the names.
        /// </remarks>
        static Being Holding(string[] sounds)
        {
            return Being.OneOf<IMySoundBlock>(sounds, block => Array.IndexOf(sounds, block.SelectedSound))
                .Writing<IMySoundBlock>((block, value) => block.SelectedSound = sounds[(int)value.Whole]);
        }

        /// <summary>Plays the sound a step chose, or the one left selected where it chose none.</summary>
        static void Played(IMySoundBlock sounds, Chosen chosen, Recall seen)
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
