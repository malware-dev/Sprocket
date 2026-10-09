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
    /// one has finished, so there is nothing to wait on.
    /// </remarks>
    public sealed class Speaker : Capability
    {
        /// <summary>Playing a chosen sound, which is Sprocket's own.</summary>
        public const string Play = "play";

        static readonly Doing Plays = Doing.Of<IMySoundBlock, Chosen>(Chosen.Read, Played);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySoundBlock;
        }

        /// <inheritdoc />
        /// <remarks>
        /// The game's own play is kept out of the list: it plays whichever sound was left selected,
        /// which is the one thing a step choosing its sound exists to avoid.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Play, "play", Plays, AskedSound(block)).In(Category.Sound)
                .Hiding("PlaySound");
        }

        /// <summary>
        /// The sound to play, from the list the terminal shows and by the names it shows them by.
        /// </summary>
        /// <remarks>
        /// Every sound block has the same list, which is every public sound category the game has
        /// loaded. Where none are loaded - the test harness - the block's own list of ids, each
        /// named by itself.
        /// </remarks>
        static Field AskedSound(IMyTerminalBlock block)
        {
            var words = new List<string>();
            var means = new List<string>();

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

            if (means.Count == 0)
            {
                var sounds = block as IMySoundBlock;
                if (sounds != null)
                    sounds.GetSounds(means);

                words.AddRange(means);
            }

            var sound = Field.Among("sound", words, means);
            sound.Entry = Entry.Text;
            sound.Needed = true;
            return sound;
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
